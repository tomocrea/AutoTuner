using AutoTuner.GPU;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTuner
{
    public class GpuState
    {
        public FanModeValues FanModeValues { get; set; }
        public int PowerLimit { get; set; }
        public int TempLimit { get; set; }
        public int TdcLimit { get; set; }
        public int ClockSpeed { get; set; }
        public int Voltage { get; set; }
        public int VramSpeed { get; set; }
        public TimingMode VramTiming { get; set; }
    }

    public class TuningState
    {
        public enum TuningStatus 
        { 
            Idle, Applying, Testing, Recovering, Completed 
        }
        public TuningStatus Status { get; set; }
        public GpuState LastStableState { get; set; }
        public GpuState AttemptedState { get; set; }
    }

    //saves state before making changes to use in case of a crash
    public interface IStates
    {
        TuningState LoadState();
        void SaveState(TuningState state);
        void ClearState();
    }
}
