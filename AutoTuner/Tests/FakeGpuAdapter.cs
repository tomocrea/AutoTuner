using AutoTuner.GPU;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AutoTuner.Tests
{
    public class FakeGpuAdapter : IGpuTuning, IDisposable
    {
        private FanModeValues fanMode = new AutoValue();
        private int powerLimit = 0;
        private int tempLimit = 85;
        private int tdcLimit = 0;
        private int maxClockOffset = 0;
        private int voltageOffset = 0;
        private int vramSpeed = 2500;
        private TimingMode vramTiming = TimingMode.Default;

        //Fans
        public bool SupportsFanMode(FanMode mode) => FanMode.Auto == mode || FanMode.ZeroRPM == mode || FanMode.FixedManual == mode;
        public void SetFanMode(FanModeValues value) => fanMode = value;

        //Power
        public bool SupportsPowerLimit() => true;
        public int GetPowerLimit() => powerLimit;
        public RangeValues GetPowerLimitRange() => new RangeValues(-50, 20, 1);
        public void SetPowerLimit(int percent) => powerLimit = percent;

        //Temperature in degrees celsius
        public bool SupportsTempLimit() => true;
        public int GetTempLimit() => tempLimit;
        public RangeValues GetTempLimitRange() => new RangeValues(65, 90, 1);
        public void SetTempLimit(int degC) => tempLimit = degC;

        //Thermal Design Current
        public bool SupportsTdcLimit() => true;
        public int GetTdcLimit() => tdcLimit;
        public RangeValues GetTdcLimitRange() => new RangeValues(0, 0, 0);
        public void SetTdcLimit(int tdc) => tdcLimit = tdc;

        //Clock speeds and voltage offset
        public bool SupportsMaxClockSpeedOffset() => true;
        public int GetMaxClockSpeedOffset() => maxClockOffset;
        public RangeValues GetMaxClockSpeedOffsetRange() => new RangeValues(-500, 500, 5);
        public void SetMaxClockSpeedOffset(int mhz) => maxClockOffset = mhz;

        public bool SupportsVoltageOffset() => true;
        public int GetVoltageOffset() => voltageOffset;
        public RangeValues GetVoltageOffsetRange() => new RangeValues(-200, 0, 5);
        public void SetVoltageOffset(int mv) => voltageOffset = mv;

        //VRAM
        public bool SupportsVramSpeed() => true;
        public int GetVramSpeed() => vramSpeed;
        public RangeValues GetVramSpeedRange() => new RangeValues(2500, 3000, 2);
        public void SetVramSpeed(int mhz) => vramSpeed = mhz;

        //nvapi seemingly doesnt support memory timings so only adlx
        public bool SupportsVramTiming() => true;
        public TimingMode GetVramTiming() => vramTiming;
        public List<TimingMode> GetVramTimingList() => new List<TimingMode> { TimingMode.Default, TimingMode.Fast };
        public void SetVramTiming(TimingMode timing) => vramTiming = timing;

        //restore everything to their default settings
        public void RestoreToDefault()
        {
            fanMode = new AutoValue();
            powerLimit = 0;
            tempLimit = 85;
            tdcLimit = 0;
            maxClockOffset = 0;
            voltageOffset = 0;
            vramSpeed = 2500;
            vramTiming = TimingMode.Default;
        }

        //get gpu name for stress test to match to
        public string GetGpuName() => "Fake GPU Adapter"; //change code in RunStressTest to run test with fake gpu

        public void Dispose()
        {
            //nothing to dispose
        }
    }

    public class FakeGpuState : IGpuState
    {
        private TuningState? saveState = null;
        public TuningState LoadState()
        {
            if(saveState != null)
            {
                return saveState;
            }
            return new TuningState
            {
                Status = TuningState.TuningStatus.Base,
                Stage = TuningState.TuningStage.Coarse,
                Variable = TuningState.TuningVariable.PowerLimit,
                LastStableState = new GpuState
                {
                    FanModeValues = new AutoValue(),
                    PowerLimit = 0,
                    TempLimit = 85,
                    TdcLimit = 0,
                    ClockSpeed = 0,
                    Voltage = 0,
                    VramSpeed = 2500,
                    VramTiming = TimingMode.Default
                },
                AttemptedState = new GpuState
                {
                    FanModeValues = new AutoValue(),
                    PowerLimit = 0,
                    TempLimit = 85,
                    TdcLimit = 0,
                    ClockSpeed = 0,
                    Voltage = 0,
                    VramSpeed = 2500,
                    VramTiming = TimingMode.Default
                }
            };
        }
        public Task SaveState(TuningState state)
        {
            saveState = state;
            return Task.CompletedTask;
        }
        public Task ClearState()
        {
            return Task.CompletedTask;
        }
    }

    public class FakeGpuMonitoring : IGpuMonitoring
    {
        private double currHotspotTemp = 70;
        private double currTemp = 50;
        private double currVramTemp = 50;
        private double heatSaturation = 96; //increase to simulate overheating

        public void UpdateMetrics() 
        {
            //https://stackoverflow.com/questions/262280/how-can-i-know-if-a-process-is-running
            if (TuningGpu.FakeTestRunning|| Process.GetProcessesByName("StressTest").Length > 0)
            {
                if(currHotspotTemp < heatSaturation) currHotspotTemp += 0.1;
                if(currTemp < heatSaturation) currTemp += 0.2;
                if(currVramTemp < heatSaturation) currVramTemp += 0.1;
            }
            else
            {
                if (currTemp >= 50) currTemp -= 2;
                if (currHotspotTemp >= 70) currHotspotTemp -= 2;
                if (currVramTemp >= 50) currVramTemp -= 2;
            }
        }
        public bool SupportsHotspotTemp() => true;
        public double GetHotspotTemp() => currHotspotTemp;
        public bool SupportsCurrentTemperature() => true;
        public double GetCurrentTemperature() => currTemp;
        public bool SupportsCurrentClockSpeed() => true;
        public int GetCurrentClockSpeed() => 50;
        public bool SupportsCurrentVramSpeed() => true;
        public int GetCurrentVramSpeed() => 2500;
        public bool SupportsCurrentVoltage() => true;
        public int GetCurrentVoltage() => 1000;
        public bool SupportsCurrentUsage() => true;
        public double GetCurrentUsage() => 75.0;
        public bool SupportsTotalBoardPower() => true;
        public double GetTotalBoardPower() => 200.0;
        public bool SupportsCurrentVramTemperature() => true;
        public double GetCurrentVramTemperature() => currVramTemp;
        public void Dispose()
        {
            //nothing to dispose
        }
    }
}
