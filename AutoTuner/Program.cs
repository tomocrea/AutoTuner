// See https://aka.ms/new-console-template for more information
using AutoTuner.GPU;
using AutoTuner.GPU.AMD;
using AutoTuner.Tests;
using System;
using System.Diagnostics;
using System.Xml.Linq;
namespace AutoTuner
{
    class Program
    {
        //https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/program-structure/main-command-line
        static async Task Main(string[] args)
        {
            int gpuIndex = 1;

            //https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.enableraisingevents?view=net-10.0
            //

            //Initialise ADLX for AMD and gpu interfaces
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

            //select gpu object
            if (allGpus.Count > 0)
            {
                IGpuTuning myGpu = allGpus[gpuIndex];
                IGpuMonitoring myMonitor = allMonitors[gpuIndex];

                //Process stressTest = Process.Start("StressTest.exe", $"-name \"{myGpu.GetGpuName()}\" -mode expected -iterations 50");
                //await Task.Delay(5000);
                //Process stressTest2 = Process.Start("StressTest.exe", $"-name \"{myGpu.GetGpuName()}\" -mode sustained -iterations 50");

                TuningGpu tuner = new TuningGpu(myGpu, new JsonGpuState(), TuningGpu.TuningTarget.MaxPerformance);
                await tuner.TuningLoop(myGpu, myMonitor);
            }
            else
            {
                Console.WriteLine("No GPUs found");
            }

            //clean up with dispose
            foreach (IGpuTuning gpu in allGpus)
            {
                gpu.Dispose();
            }
            foreach (IGpuMonitoring monitor in allMonitors)
            {
                monitor.Dispose();
            }
            adlxWrapper.Dispose();
        }
    }
}