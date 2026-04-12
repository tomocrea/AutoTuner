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

                while (!Console.KeyAvailable)
                {
                    Stopwatch sw = Stopwatch.StartNew();
                    myMonitor.UpdateMetrics();
                    sw.Stop();
                    Console.WriteLine("refresh took: " + sw.ElapsedMilliseconds);
                    if (myMonitor.SupportsCurrentTemperature())
                    {
                        var metrics = myMonitor.GetCurrentTemperature();
                        Console.WriteLine($"[LIVE] Temp: {metrics}°C");
                        Thread.Sleep(100);
                    }
                }
            }
        }
    }
}
