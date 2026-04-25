using AutoTuner.GPU;
using AutoTuner.GPU.AMD;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace AutoTuner.Tests
{
    internal class CurrentTempMonitoring
    {
        public static void MonitoringMain()
        {
            //Initialise ADLX for AMD
            AdlxWrapper adlxWrapper;
            List<IGpuTuning> allGpus = new List<IGpuTuning>();
            List<IGpuMonitoring> allMonitors = new List<IGpuMonitoring>();
            try
            {
                adlxWrapper = new AdlxWrapper();
                foreach (IADLXGPU gpu in adlxWrapper.GetGPUs())
                {
                    AdlxAdapter adapter = new AdlxAdapter(adlxWrapper, gpu);
                    allGpus.Add(adapter);
                    allMonitors.Add(adapter);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not load ADLX: {e.Message}");
                return;
            }

            //can add other sdks like nvapi here
            //
            //

            //get gpu object and loop temperature monitoring
            if (allGpus.Count > 0)
            {
                IGpuTuning myGpu = allGpus[1];
                IGpuMonitoring myMonitor = allMonitors[1];
                Console.WriteLine("Monitoring started. Press any key to stop...");

                Console.WriteLine("Gpu Name: " + myGpu.GetGpuName());
                Console.WriteLine("Fan: " + myGpu.SupportsFanMode(FanMode.Auto));
                Console.WriteLine("Power: " + myGpu.GetPowerLimit());
                Console.WriteLine("Power Range: " + myGpu.GetPowerLimitRange().Min + " to " + myGpu.GetPowerLimitRange().Max + " step " + myGpu.GetPowerLimitRange().Step);
                //Console.WriteLine("Temp" + myGpu.GetTempLimit());
                Console.WriteLine("Tdc: " + myGpu.GetTdcLimit());
                Console.WriteLine("Tdc Range: " + myGpu.GetTdcLimitRange().Min + " to " + myGpu.GetTdcLimitRange().Max + " step " + myGpu.GetTdcLimitRange().Step);
                Console.WriteLine("Clock: " + myGpu.GetMaxClockSpeedOffset());
                Console.WriteLine("Clock Range: " + myGpu.GetMaxClockSpeedOffsetRange().Min + " to " + myGpu.GetMaxClockSpeedOffsetRange().Max + " step " + myGpu.GetMaxClockSpeedOffsetRange().Step);
                Console.WriteLine("Voltage: " + myGpu.GetVoltageOffset());
                Console.WriteLine("Voltage Range: " + myGpu.GetVoltageOffsetRange().Min + " to " + myGpu.GetVoltageOffsetRange().Max + " step " + myGpu.GetVoltageOffsetRange().Step);
                Console.WriteLine("Vram Speed: " + myGpu.GetVramSpeed());
                Console.WriteLine("Vram Speed Range: " + myGpu.GetVramSpeedRange().Min + " to " + myGpu.GetVramSpeedRange().Max + " step " + myGpu.GetVramSpeedRange().Step);
                Console.WriteLine("Vram Timing: " + myGpu.GetVramTiming());
                Console.WriteLine("Vram Timing List: " + string.Join(", ", myGpu.GetVramTimingList()));

                while (!Console.KeyAvailable)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    myMonitor.UpdateMetrics();
                    sw.Stop();
                    Console.WriteLine("refresh took: " + sw.ElapsedMilliseconds);
                    if (myMonitor.SupportsCurrentTemperature())
                    {
                        var metrics = myMonitor.GetCurrentTemperature();
                        Console.WriteLine($"Temp: {metrics} degrees celcius");
                        Thread.Sleep(100);
                    }
                }
            }
            foreach(IGpuTuning gpu in allGpus)
            {
                gpu.Dispose();
            }
            adlxWrapper.Dispose();
        }
    }
}
