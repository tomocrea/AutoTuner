using AutoTuner.GPU;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace AutoTuner.Tests
{
    internal class FakeGpuTuningTest
    {
        public static async Task FakeTest() 
        { 
            //edit RunStressTest when running fake gpu test
            IGpuTuning gpu = new FakeGpuAdapter();
            IGpuMonitoring monitor = new FakeGpuMonitoring();
            IGpuState state = new FakeGpuState();
            TuningGpu tuner = new TuningGpu(gpu, state, TuningGpu.TuningTarget.MaxPerformance);
            await tuner.TuningLoop(gpu, monitor);
            TuningState tState = state.LoadState();
            Console.WriteLine($"Tuning Status: {tState.Status}, Tuning Stage: {tState.Stage}, Tuning Variable: {tState.Variable}");
            Console.WriteLine(JsonSerializer.Serialize(tState.LastStableState, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
