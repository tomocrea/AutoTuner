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
        //https://stackoverflow.com/questions/277771/how-to-run-a-winform-from-console-application
        [STAThread]
        static async Task Main(string[] args)
        {
            Application.EnableVisualStyles();
            
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
                IGpuState myState = new JsonGpuState();

                Process stressTest = Process.Start("StressTest.exe", $"-name \"{myGpu.GetGpuName()}\" -mode expected -iterations 50");
                await stressTest.WaitForExitAsync();
                Process stressTest2 = Process.Start("StressTest.exe", $"-name \"{myGpu.GetGpuName()}\" -mode sustained -iterations 500");
                await stressTest2.WaitForExitAsync();
                Console.WriteLine("Benchmark complete");
                //TuningGpu tuner = new TuningGpu(myGpu, myState, TuningGpu.TuningTarget.overclock);
                //await tuner.TuningLoop(myGpu, myMonitor);

                using (UserInterface ui = new UserInterface(myGpu, myMonitor, myState))
                {
                    Application.Run(ui);
                }
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