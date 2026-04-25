using AutoTuner.GPU;
using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTuner
{
    public class GpuState
    {
        //https://stackoverflow.com/questions/70894115/dereference-of-a-possibly-null-reference-can-my-code-be-simplified
        public FanModeValues? FanModeValues { get; set; }
        public int? PowerLimit { get; set; }
        public int? TempLimit { get; set; }
        public int? TdcLimit { get; set; }
        public int? ClockSpeed { get; set; }
        public int? Voltage { get; set; }
        public int? VramSpeed { get; set; }
        public TimingMode? VramTiming { get; set; }

        public GpuState() { }

        //https://stackoverflow.com/questions/5359318/how-to-clone-objects#comment61462762_5359346
        public GpuState(GpuState oldState)
        {
            this.FanModeValues = oldState.FanModeValues;
            this.PowerLimit = oldState.PowerLimit;
            this.TempLimit = oldState.TempLimit;
            this.TdcLimit = oldState.TdcLimit;
            this.ClockSpeed = oldState.ClockSpeed;
            this.Voltage = oldState.Voltage;
            this.VramSpeed = oldState.VramSpeed;
            this.VramTiming = oldState.VramTiming;
        }
    }

    //memento pattern
    public class TuningState
    {
        public enum TuningStatus 
        { 
            Base, Applying, Testing, Success, Fail
        }
        public enum TuningStage
        {
            Coarse, Fine, Check, Done
        }
        public enum TuningVariable
        {
            PowerLimit, TempLimit, TdcLimit, Fan, Voltage, ClockSpeed, VramSpeed, VramTiming
        }
        public TuningStatus Status { get; set; }
        public TuningStage Stage { get; set; }
        public TuningVariable Variable { get; set; }
        public GpuState LastStableState { get; set; }
        public GpuState AttemptedState { get; set; }
    }

    //saves state before making changes to use in case of a crash
    public interface IGpuState
    {
        TuningState LoadState();
        Task SaveState(TuningState state);
        Task ClearState();
    }
}
