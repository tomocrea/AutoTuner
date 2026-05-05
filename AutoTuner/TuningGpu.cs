using AutoTuner.GPU;
using AutoTuner.GPU.AMD;
using AutoTuner.Tests;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace AutoTuner
{
    internal class TuningGpu
    {
        private CancellationTokenSource cancelMonitor;
        private CancellationTokenSource cancelOverheat;
        private Task? monitoringTask;
        private IGpuState gpuState;
        enum TestMode
        {
            transient, sustained, expected
        }

        //parameters for tuning loop
        public enum TuningTarget
        {
            overclock, undervolt
        }
        //todo: find best values
        public int maxTemp = 85;
        public int maxHotspotTemp = 95;
        public int maxVramTemp = 95;
        public int coarseStep = 25;
        public int fineStep = 5;
        public int saturateIterations = 700; //700 = about 5 minutes
        public int coarseTestIterations = 50; //50
        public int fineTestIterations = 50; //50
        public int checkTestIterations = 100;
        public TuningTarget tuningTarget;
        private double baselineTemp;
        private double baselineHotspotTemp;
        private double baselineVramTemp;
        private int baselineClockSpeed;

        //bools for reason for cancelling
        private bool isOverheat = false;
        private bool isLowClock = false;
        private bool isCooling = false;

        //for fake gpu test
        public static bool FakeTestRunning = false;

        public TuningGpu(IGpuTuning gpu, IGpuState state, TuningTarget target)
        {
            cancelMonitor = new CancellationTokenSource();
            cancelOverheat = new CancellationTokenSource();
            gpuState = state;
            tuningTarget = target;
        }

        private async Task BackgroundMonitoring(IGpuMonitoring monitor, CancellationToken token, TuningState state)
        {
            Console.WriteLine("Monitoring started on background thread");
            baselineTemp = monitor.SupportsCurrentTemperature() ? monitor.GetCurrentTemperature() : 0;
            baselineHotspotTemp = monitor.SupportsHotspotTemp() ? monitor.GetHotspotTemp() : 0;
            baselineVramTemp = monitor.SupportsCurrentVramTemperature() ? monitor.GetCurrentVramTemperature() : 0;
            baselineClockSpeed = 0;

            while (!token.IsCancellationRequested)
            {
                //avoid access violation race condition with cooling check in RunStressTest
                if (isCooling)
                {
                    await Task.Delay(1000, token);
                    continue;
                }
                try
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    monitor.UpdateMetrics();
                    sw.Stop();
                    if (monitor.SupportsCurrentTemperature() && monitor.GetCurrentTemperature() >= maxTemp)
                    {
                        if (!cancelOverheat.IsCancellationRequested)
                        {
                            Console.WriteLine("Temperature too high: " + monitor.GetCurrentTemperature());
                            isOverheat = true;
                            try { cancelOverheat.Cancel(); }
                            catch { Console.WriteLine("Couldn't cancel"); }
                        }
                    }
                    if (monitor.SupportsHotspotTemp() && monitor.GetHotspotTemp() >= maxHotspotTemp)
                    {
                        if (!cancelOverheat.IsCancellationRequested)
                        {
                            Console.WriteLine("Temperature hotspot too high: " + monitor.GetHotspotTemp());
                            isOverheat = true;
                            try { cancelOverheat.Cancel(); }
                            catch { Console.WriteLine("Couldn't cancel"); }
                        }
                    }
                    if (monitor.SupportsCurrentVramTemperature() && monitor.GetCurrentVramTemperature() >= maxVramTemp)
                    {
                        if (!cancelOverheat.IsCancellationRequested)
                        {
                            Console.WriteLine("Vram temperature too high: " + monitor.GetCurrentVramTemperature());
                            isOverheat = true;
                            try { cancelOverheat.Cancel(); }
                            catch { Console.WriteLine("Couldn't cancel"); }
                        }
                    }

                    //first get boosted clock with no tuning
                    if(state.Status == TuningState.TuningStatus.Testing && (state.AttemptedState.ClockSpeed ?? 0) == 0 && monitor.GetCurrentUsage() > 95)
                    {
                        if(monitor.GetCurrentClockSpeed() > baselineClockSpeed)
                        {
                            baselineClockSpeed = monitor.GetCurrentClockSpeed();
                        }
                    }
                    //monitor for clock stretching when undervolting
                    if(baselineClockSpeed > 0 && state.Status == TuningState.TuningStatus.Testing && monitor.GetCurrentUsage() > 95)
                    {
                        if(monitor.GetCurrentClockSpeed() < ((baselineClockSpeed + (state.AttemptedState.ClockSpeed ?? 0)) - 150))
                        {
                            if(!cancelOverheat.IsCancellationRequested)
                            {
                                Console.WriteLine("Clock stretching detected");
                                isLowClock = true;
                                try { cancelOverheat.Cancel(); }
                                catch { Console.WriteLine("Couldn't cancel"); }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error monitoring: " + ex.Message);
                }
                //https://stackoverflow.com/questions/20082221/when-to-use-task-delay-when-to-use-thread-sleep
                try
                {
                    await Task.Delay(100, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public void StopMonitoring()
        {
            cancelMonitor.Cancel();
        }

        //async for save state
        public async Task TuningLoop(IGpuTuning gpu, IGpuMonitoring monitor)
        {
            TuningState state = gpuState.LoadState();
            monitoringTask = Task.Run(() => BackgroundMonitoring(monitor, cancelMonitor.Token, state));

            //variables to be tuned, e.g. clock speed, voltage & vram
            TuningState.TuningVariable[] variables;
            if(tuningTarget == TuningTarget.overclock)
            {
                //if max performance, focus on clock speed and vram speed
                variables = new[]
                {
                    TuningState.TuningVariable.ClockSpeed,
                    TuningState.TuningVariable.VramSpeed
                };
            }
            else if (tuningTarget == TuningTarget.undervolt)
            {
                //if max efficiency, focus on undervolting and lowest power limit
                variables = new[]
                {
                    TuningState.TuningVariable.Voltage,
                    TuningState.TuningVariable.PowerLimit,
                    TuningState.TuningVariable.VramSpeed
                };
            }
            else
            {
                variables = Enum.GetValues<TuningState.TuningVariable>();
            }

            if (state.Status == TuningState.TuningStatus.Base)
            {
                Console.WriteLine("Starting tuning process at: " + DateTime.Now);
                gpu.RestoreToDefault();

                //apply starting values based on tuning target/preset
                switch (tuningTarget)
                {
                    case TuningTarget.overclock:
                        state.AttemptedState = new GpuState()
                        {
                            //use ternary to avoid exceptions
                            FanModeValues = gpu.SupportsFanMode(FanMode.FixedManual) ? new FixedManualValue() { Percentage = 100 } : null,
                            PowerLimit = gpu.SupportsPowerLimit() ? gpu.GetPowerLimitRange().Max : null,
                            TempLimit = gpu.SupportsTempLimit() ? gpu.GetTempLimitRange().Max : null,
                            TdcLimit = gpu.SupportsTdcLimit() ? gpu.GetTdcLimitRange().Max : null,
                            ClockSpeed = gpu.SupportsMaxClockSpeedOffset() ? gpu.GetMaxClockSpeedOffset() : null,
                            Voltage = gpu.SupportsVoltageOffset() ? gpu.GetVoltageOffset() : null,
                            VramSpeed = gpu.SupportsVramSpeed() ? gpu.GetVramSpeed() : null,
                            VramTiming = gpu.SupportsVramTiming() ? TimingMode.Fast : null
                        };
                        break;

                    case TuningTarget.undervolt:
                        state.AttemptedState = new GpuState()
                        {
                            FanModeValues = gpu.SupportsFanMode(FanMode.Auto) ? new AutoValue() : null,
                            PowerLimit = gpu.SupportsPowerLimit() ? gpu.GetPowerLimit() : null,
                            TempLimit = gpu.SupportsTempLimit() ? gpu.GetTempLimit() : null,
                            TdcLimit = gpu.SupportsTdcLimit() ? gpu.GetTdcLimit() : null,
                            ClockSpeed = gpu.SupportsMaxClockSpeedOffset() ? gpu.GetMaxClockSpeedOffset() : null,
                            Voltage = gpu.SupportsVoltageOffset() ? gpu.GetVoltageOffset() : null,
                            VramSpeed = gpu.SupportsVramSpeed() ? gpu.GetVramSpeed() : null,
                            VramTiming = gpu.SupportsVramTiming() ? TimingMode.Fast : null
                        };
                        break;
                }
                state.Status = TuningState.TuningStatus.Applying;
                state.Variable = variables[0];
                state.Stage = TuningState.TuningStage.Coarse;
                state.LastStableState = new GpuState(state.AttemptedState);
                await gpuState.SaveState(state);
                ApplyState(gpu, state);
            }

            if (state.Status == TuningState.TuningStatus.Testing)
            {
                Console.WriteLine("Crash detected, reverting to last stable state.");
                state.Status = TuningState.TuningStatus.Fail;
                state.AttemptedState = new GpuState(state.LastStableState);
                ApplyState(gpu, state);
                await gpuState.SaveState(state);
            }

            //calculate expected values on default tuning
            Console.WriteLine("Calculating expected matrix result");
            await RunStressTest(gpu, state, true, TestMode.expected, 1, monitor);

            //first saturate with heat
            Console.WriteLine("Running 5 minute stress test to saturate GPU with heat");
            await RunStressTest(gpu, state, true, TestMode.sustained, saturateIterations, monitor);

            //todo: if initial matrix multiplication has been done
            //https://stackoverflow.com/questions/6719630/how-to-escape-a-while-loop-in-c-sharp
            bool stable = true;
            while (!cancelMonitor.IsCancellationRequested)
            {
                //once variable tested and stable after all stages, move to next
                if (state.Stage == TuningState.TuningStage.Done)
                {
                    Console.WriteLine($"{state.Variable} done, advancing to next variable");
                    //https://stackoverflow.com/questions/972307/how-to-loop-through-all-enum-values-in-c
                    //https://learn.microsoft.com/en-gb/dotnet/fundamentals/code-analysis/quality-rules/ca2263

                    //https://stackoverflow.com/questions/642542/how-to-get-next-or-previous-enum-value-in-c-sharp
                    //TuningState.TuningVariable[] variables = Enum.GetValues<TuningState.TuningVariable>();
                    int index = Array.IndexOf(variables, state.Variable);
                    if (index < variables.Length - 1)
                    {
                        state.Variable = variables[index + 1];
                        state.Stage = TuningState.TuningStage.Coarse;
                        await gpuState.SaveState(state);
                        continue;
                    }
                    else
                    {
                        Console.WriteLine("Fully tuned at: " + DateTime.Now);
                        state.AttemptedState = new GpuState(state.LastStableState);
                        ApplyState(gpu, state);
                        await gpuState.SaveState(state);
                        //await gpuState.ClearState();
                        StopMonitoring();
                        break;
                    }
                }

                int step = 0;
                if (state.Stage == TuningState.TuningStage.Coarse)
                {
                    step = coarseStep;
                }
                else if (state.Stage == TuningState.TuningStage.Fine)
                {
                    step = fineStep;
                }

                state.AttemptedState = new GpuState(state.LastStableState);
                bool adjusted = AdjustStateVariable(gpu, state, step);
                //if variable cannot be adjusted further, continue to next variable
                if (!adjusted)
                {
                    Console.WriteLine("Cannot adjust further, limit reached for " + state.Variable);

                    state.AttemptedState = new GpuState(state.LastStableState);
                    state.Stage = TuningState.TuningStage.Done;
                    await gpuState.SaveState(state);
                    continue;
                }

                //amend testing status to state
                state.Status = TuningState.TuningStatus.Applying;
                //apply state to gpu
                ApplyState(gpu, state);
                Console.WriteLine($"Applied {state.Variable} with step {step} at stage {state.Stage}");
                //must await to ensure state saved
                await gpuState.SaveState(state);

                //wait to let gpu stabilise
                await Task.Delay(500);

                //run stress test
                state.Status = TuningState.TuningStatus.Testing;
                if (state.Stage == TuningState.TuningStage.Coarse)
                {
                    bool stableSustained = await RunStressTest(gpu, state, stable, TestMode.sustained, coarseTestIterations/2, monitor);
                    bool stableTransient = await RunStressTest(gpu, state, stable, TestMode.transient, coarseTestIterations/2, monitor);
                    if(stableSustained && stableTransient) { stable = true; }
                    else { stable = false; }
                }
                else if (state.Stage == TuningState.TuningStage.Fine)
                {
                    bool stableSustained = await RunStressTest(gpu, state, stable, TestMode.sustained, fineTestIterations/2, monitor);
                    bool stableTransient = await RunStressTest(gpu, state, stable, TestMode.transient, fineTestIterations/2, monitor);
                    if (stableSustained && stableTransient) { stable = true; }
                    else { stable = false; }
                }
                else if (state.Stage == TuningState.TuningStage.Check)
                {
                    stable = await RunStressTest(gpu, state, stable, TestMode.transient, checkTestIterations, monitor);
                }

                //after test
                if (stable)
                {
                    state.LastStableState = new GpuState(state.AttemptedState);
                    if (state.Stage == TuningState.TuningStage.Check)
                    {
                        state.Stage = TuningState.TuningStage.Done;
                    }
                    await gpuState.SaveState(state);
                }
                else
                {
                    //advance stage
                    if(state.Stage == TuningState.TuningStage.Coarse)
                    {
                        state.Stage = TuningState.TuningStage.Fine;
                    }
                    else if (state.Stage == TuningState.TuningStage.Fine)
                    {
                        state.Stage = TuningState.TuningStage.Check;
                    }
                    //if check fails revert to last state
                    else if(state.Stage == TuningState.TuningStage.Check)
                    {
                        state.AttemptedState = new GpuState(state.LastStableState);

                        state.Stage = TuningState.TuningStage.Fine;
                        AdjustStateVariable(gpu, state, -fineStep); //inverts last fine step
                        state.Stage = TuningState.TuningStage.Check;

                        state.LastStableState = new GpuState(state.AttemptedState);
                    }
                    await gpuState.SaveState(state);
                }
            }
            if (monitoringTask != null)
            {
                await monitoringTask;
            }
        }

        //adjusts the attempted state variable by step size
        //returns false if variable cannot be adjusted further due to hardware limits
        //adjusts based on target (performance vs efficiency), defaults to performance
        private bool AdjustStateVariable(IGpuTuning gpu, TuningState state, int step)
        {
            //if in check stage dont adjust
            if (state.Stage == TuningState.TuningStage.Check) return true;
            switch (state.Variable)
            {
                case TuningState.TuningVariable.Fan: 
                    FanModeValues currFan = new FixedManualValue() { Percentage = 100 };
                    //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns
                    if (state.LastStableState.FanModeValues is FixedManualValue {Percentage: 100}) return false;
                    if (gpu.SupportsFanMode(currFan.Mode))
                    {
                        state.AttemptedState.FanModeValues = currFan;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case TuningState.TuningVariable.PowerLimit:
                    if(!gpu.SupportsPowerLimit()) return false;

                    int currPower;
                    if (state.LastStableState.PowerLimit is int) currPower = state.LastStableState.PowerLimit.Value;
                    else return false;

                    if (tuningTarget == TuningTarget.overclock)
                    {
                        if(currPower == gpu.GetPowerLimitRange().Max) return false; //if already at max, cant increase further
                        currPower = gpu.GetPowerLimitRange().Max;
                    }
                    else if (tuningTarget == TuningTarget.undervolt)
                    {
                        currPower -= step; //todo find best tradeoff for efficiency, need to measure performance impact
                    }

                    if(currPower >= gpu.GetPowerLimitRange().Min && currPower <= gpu.GetPowerLimitRange().Max)
                    {
                        state.AttemptedState.PowerLimit = currPower;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case TuningState.TuningVariable.TempLimit:
                    if(!gpu.SupportsTempLimit()) return false;

                    int currTemp;
                    if(state.LastStableState.TempLimit is int) currTemp = state.LastStableState.TempLimit.Value;
                    else return false;

                    if(tuningTarget == TuningTarget.overclock)
                    {
                        if(currTemp == gpu.GetTempLimitRange().Max) return false; //if already at max, cant increase further
                        currTemp = gpu.GetTempLimitRange().Max;
                    }
                    else if (tuningTarget == TuningTarget.undervolt)
                    {
                        currTemp -= step; //todo find best tradeoff for efficiency, need to measure performance impact
                    }

                    if(currTemp >= gpu.GetTempLimitRange().Min && currTemp <= gpu.GetTempLimitRange().Max)
                    {
                        state.AttemptedState.TempLimit = currTemp;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case TuningState.TuningVariable.TdcLimit:
                    if(!gpu.SupportsTdcLimit()) return false;

                    int currTdc;
                    if(state.LastStableState.TdcLimit is int) currTdc = state.LastStableState.TdcLimit.Value;
                    else return false;

                    if(tuningTarget == TuningTarget.overclock)
                    {
                        if(currTdc == gpu.GetTdcLimitRange().Max) return false; //if already at max, cant increase further
                        currTdc = gpu.GetTdcLimitRange().Max;
                    }
                    else if (tuningTarget == TuningTarget.undervolt)
                    {
                        currTdc -= step; //todo find best tradeoff for efficiency, need to measure performance impact
                    }

                    if(currTdc >= gpu.GetTdcLimitRange().Min && currTdc <= gpu.GetTdcLimitRange().Max)
                    {
                        state.AttemptedState.TdcLimit = currTdc;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case TuningState.TuningVariable.ClockSpeed:
                    if(!gpu.SupportsMaxClockSpeedOffset()) return false;

                    int currClock;
                    if (state.LastStableState.ClockSpeed is int) currClock = state.LastStableState.ClockSpeed.Value;
                    else return false;

                    if (tuningTarget == TuningTarget.overclock)
                    {
                        currClock += step;
                    }
                    else if (tuningTarget == TuningTarget.undervolt)
                    {
                        return false;
                    }

                    if(currClock >= gpu.GetMaxClockSpeedOffsetRange().Min && currClock <= gpu.GetMaxClockSpeedOffsetRange().Max)
                    {
                        state.AttemptedState.ClockSpeed = currClock;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case TuningState.TuningVariable.Voltage:
                    if(!gpu.SupportsVoltageOffset()) return false;

                    int currVoltage;
                    if(state.LastStableState.Voltage is int) currVoltage = state.LastStableState.Voltage.Value;
                    else return false;

                    if(tuningTarget == TuningTarget.overclock)
                    {
                        return false;
                    }
                    else if (tuningTarget == TuningTarget.undervolt)
                    {
                        currVoltage -= step;
                    }

                    if(currVoltage >= gpu.GetVoltageOffsetRange().Min && currVoltage <= gpu.GetVoltageOffsetRange().Max)
                    {
                        state.AttemptedState.Voltage = currVoltage;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case TuningState.TuningVariable.VramSpeed:
                    if(!gpu.SupportsVramSpeed()) return false;

                    int currVramSpeed;
                    if(state.LastStableState.VramSpeed is int) currVramSpeed = state.LastStableState.VramSpeed.Value + step;
                    else return false;

                    if(currVramSpeed >= gpu.GetVramSpeedRange().Min && currVramSpeed <= gpu.GetVramSpeedRange().Max)
                    {
                        state.AttemptedState.VramSpeed = currVramSpeed;
                    }
                    else
                    {
                        return false;
                    }
                    break;
                case TuningState.TuningVariable.VramTiming:
                    if(!gpu.SupportsVramTiming()) return false;

                    TimingMode currVramTiming = TimingMode.Fast;
                    if(state.LastStableState.VramTiming == currVramTiming)
                    {
                        return false;
                    }

                    if (gpu.GetVramTimingList().Contains(currVramTiming))
                    {
                        state.AttemptedState.VramTiming = currVramTiming;
                    }
                    else
                    {
                        return false;
                    }
                    break;
            }
            return true;
        }

        //sets gpu to current attempted state
        private void ApplyState(IGpuTuning gpu, TuningState state)
        {
            if(state.AttemptedState.FanModeValues != null && gpu.SupportsFanMode(state.AttemptedState.FanModeValues.Mode)) gpu.SetFanMode(state.AttemptedState.FanModeValues);
            if(gpu.SupportsPowerLimit() && state.AttemptedState.PowerLimit is int) gpu.SetPowerLimit(state.AttemptedState.PowerLimit.Value);
            if(gpu.SupportsTempLimit() && state.AttemptedState.TempLimit is int) gpu.SetTempLimit(state.AttemptedState.TempLimit.Value);
            if(gpu.SupportsTdcLimit() && state.AttemptedState.TdcLimit is int) gpu.SetTdcLimit(state.AttemptedState.TdcLimit.Value);
            if(gpu.SupportsMaxClockSpeedOffset() && state.AttemptedState.ClockSpeed is int) gpu.SetMaxClockSpeedOffset(state.AttemptedState.ClockSpeed.Value);
            if(gpu.SupportsVoltageOffset() && state.AttemptedState.Voltage is int) gpu.SetVoltageOffset(state.AttemptedState.Voltage.Value);
            if(gpu.SupportsVramSpeed() && state.AttemptedState.VramSpeed is int) gpu.SetVramSpeed(state.AttemptedState.VramSpeed.Value);
            if(gpu.SupportsVramTiming() && state.AttemptedState.VramTiming is TimingMode) gpu.SetVramTiming(state.AttemptedState.VramTiming.Value);
        }

        private async Task<bool> RunStressTest(IGpuTuning gpu, TuningState state, bool stable, TestMode mode, int iterations, IGpuMonitoring monitor)
        {
            //run regular stress test if real gpu, if using fake gpu to test dont need to run real stress test
            if (gpu.GetGpuName() != "Fake GPU Adapter")
            {
                //start stress test
                //https://stackoverflow.com/questions/25966983/how-to-get-the-exitcode-of-a-running-process
                string name = gpu.GetGpuName();
                Process stressTest = new Process();
                stressTest.StartInfo.FileName = "StressTest.exe";
                stressTest.StartInfo.Arguments = $"-name \"{name}\" -mode {mode} -iterations {iterations}";
                //https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.processstartinfo.redirectstandardoutput?view=net-10.0
                stressTest.StartInfo.UseShellExecute = false;
                //stressTest.StartInfo.RedirectStandardOutput = true;
                stressTest.Start();

                try
                {
                    await stressTest.WaitForExitAsync(cancelOverheat.Token);
                }
                catch
                {
                    //kill stress test and reset token
                    try { stressTest.Kill(); }
                    catch { Console.WriteLine("Couldn't kill stress test"); }

                    //revert to last stable state
                    state.Status = TuningState.TuningStatus.Fail;
                    state.AttemptedState = new GpuState(state.LastStableState);
                    ApplyState(gpu, state);

                    isCooling = true;
                    baselineClockSpeed = 0;
                    //wait to cool down to just above baseline
                    if (isOverheat)
                    {
                        Console.WriteLine("Stress test cancelled due to overheating");
                        while (monitor.GetHotspotTemp() >= baselineHotspotTemp + 5 || monitor.GetCurrentTemperature() >= baselineTemp + 5 || monitor.GetCurrentVramTemperature() >= baselineVramTemp + 5)
                        {
                            Console.WriteLine($"Current Temp: {monitor.GetCurrentTemperature()}, Hotspot Temp: {monitor.GetHotspotTemp()}, Vram Temp: {monitor.GetCurrentVramTemperature()}");
                            await Task.Delay(1000);
                        }
                    }
                    else if (isLowClock)
                    {
                        Console.WriteLine("Stress test cancelled due to dropping clock speeds");
                        await Task.Delay(1000);
                    }

                    isOverheat = false;
                    isLowClock = false;
                    isCooling = false;

                    cancelOverheat.Dispose();
                    cancelOverheat = new CancellationTokenSource();
                    Console.WriteLine("Resuming tuning process");
                    return false;
                }
                int code = stressTest.ExitCode; //0 if fine, 4 if multiplication returned wrong result so unstable, 5 if tdr
                //string output = await stressTest.StandardOutput.ReadToEndAsync();
                
                switch (code)
                {
                    case 0:
                        Console.WriteLine("Test passed successfully");
                        state.Status = TuningState.TuningStatus.Success;
                        state.LastStableState = new GpuState(state.AttemptedState);
                        await gpuState.SaveState(state);
                        stable = true;
                        break;
                    case 4:
                    case 5:
                    case 6:
                    case 8:
                        if (code == 4 || code == 5)
                        {
                            Console.WriteLine("Test failed due to instability, reverting to last stable state");
                        }
                        else if (code == 6 || code == 8)
                        {
                            Console.WriteLine("Test failed due to driver timeout/crash, reverting to last stable state");
                        }
                        state.Status = TuningState.TuningStatus.Fail;
                        state.AttemptedState = new GpuState(state.LastStableState);
                        ApplyState(gpu, state);
                        await gpuState.SaveState(state);
                        stable = false;
                        break;
                    default:
                        Console.WriteLine($"Test failed with exit code: {code}");
                        stable = false;
                        break;
                }
                baselineClockSpeed = 0; //reset avg clock speed
                return stable;
            }
            else
            {
                //Fake GPU test
                FakeTestRunning = true;
                try
                {
                    await Task.Delay(100 * iterations, cancelOverheat.Token);
                }
                catch (OperationCanceledException)
                {
                    FakeTestRunning = false;

                    state.Status = TuningState.TuningStatus.Fail;
                    state.AttemptedState = new GpuState(state.LastStableState);
                    ApplyState(gpu, state);
                    //wait to cool down
                    while (monitor.GetHotspotTemp() >= baselineHotspotTemp + 5 || monitor.GetCurrentTemperature() >= baselineTemp + 5 || monitor.GetCurrentVramTemperature() >= baselineVramTemp + 5)
                    {
                        await Task.Delay(1000);
                    }
                    Console.WriteLine("GPU cooled down, resuming tuning process");
                    cancelOverheat.Dispose();
                    cancelOverheat = new CancellationTokenSource();
                    return false;
                }
                if (state.Variable == TuningState.TuningVariable.ClockSpeed && state.AttemptedState.ClockSpeed > 321)
                {
                    Console.WriteLine("Fake gpu crashed");
                    state.Status = TuningState.TuningStatus.Fail;
                    state.AttemptedState = new GpuState(state.LastStableState);
                    ApplyState(gpu, state);
                    FakeTestRunning = false;
                    return false;
                }
                FakeTestRunning = false;
                return true;
            }
        }
    }
}
