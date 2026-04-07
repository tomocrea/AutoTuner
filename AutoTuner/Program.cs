// See https://aka.ms/new-console-template for more information
using AutoTuner.GPU;
using AutoTuner.GPU.AMD;
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
            //ADLXCheckAutoTuningExample example = new ADLXCheckAutoTuningExample();
            //example.ExampleMain();

            //Run Test
            CurrentTempMonitoring.MonitoringMain();

            //exited event
            //https://learn.microsoft.com/en-us/dotnet/api/system.diagnostics.process.enableraisingevents?view=net-10.0


        }
    }
}