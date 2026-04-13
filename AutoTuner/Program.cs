// See https://aka.ms/new-console-template for more information
using AutoTuner.GPU;
using AutoTuner.Tests;
using System;
using System.Diagnostics;
namespace AutoTuner
{
    class Program
    {
        static void Main(string[] args)
        {
            //Run example
            ADLXCheckAutoTuningExample.ExampleMain();

            //Run Test
            //CurrentTempMonitoring.MonitoringMain();

            //exited event
            //https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.enableraisingevents?view=net-10.0
            string gpuName = "test";
            //Process.Start("StressTest.exe", $"-name \"{gpuName}\" -mode transient -iterations 100").EnableRaisingEvents = true;
        }
    }
}