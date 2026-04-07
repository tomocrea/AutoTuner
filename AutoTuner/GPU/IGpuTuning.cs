using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTuner.GPU
{
    //generic fan modes, covers:
    //Auto: drivers decide,
    //FixedManualfixed: manual speed, takes percentage value,
    //CurveManual: manual fan curve, takes ,
    //ZeroRPM: zero rpm which is efficient/quiet
    public enum FanMode
    {
        Auto, FixedManual, CurveManual, ZeroRPM
    }

    //classes for fan mode values
    //https://www.geeksforgeeks.org/c-sharp/c-sharp-abstract-classes/
    public abstract class FanModeValues
    {
        public abstract FanMode Mode { get; }
    }
    public class AutoValue : FanModeValues
    {
        public override FanMode Mode => FanMode.Auto;
        public bool Auto { get; set; }
    }
    public class FixedManualValue : FanModeValues
    {
        public override FanMode Mode => FanMode.FixedManual;
        public int Percentage { get; set; }
    }
    public class CurveManualValue : FanModeValues
    {
        public override FanMode Mode => FanMode.CurveManual;
        //https://www.dotnetperls.com/keyvaluepair
        public List<KeyValuePair<int,int>> CurveValuesList { get; set; } = new List<KeyValuePair<int, int>>();
    }
    public class ZeroRPMValue : FanModeValues
    {
        public override FanMode Mode => FanMode.ZeroRPM;
        public bool Zero { get; set; }
    }

    //range values for clock speed, voltage, etc.
    //min, max and step
    //minimum and maximum values for the modifiable frequency/voltage etc. variable
    //and step for increments to the modifiable variable
    public class RangeValues
    {
        public int Min {  get; set; }
        public int Max { get; set; }
        public int Step { get; set; }
        public RangeValues(int min, int max, int step)
        {
            Min = min;
            Max = max;
            Step = step;
        }
    }

    //timing modes, useually default and fast supported by AMD cards
    //but enum for all available in ADLXDefines.h line 852
    //currently only amd supports changing memory timings
    public enum TimingMode
    {
        Default, Fast, Fast2, Auto, Level1, Level2
    }

    //Interface for GPU tuning to be used by patterns for specific implementations
    public interface IGpuTuning : IDisposable
    {
        //Fans
        bool SupportsFanMode(FanMode mode);
        void SetFanMode(FanModeValues value);

        //Power
        bool SupportsPowerLimit();
        int GetPowerLimit();
        RangeValues GetPowerLimitRange();
        void SetPowerLimit(int percent);

        //Temperature in degrees celcius
        bool SupportsTempLimit();
        int GetTempLimit();
        RangeValues GetTempLimitRange();
        void SetTempLimit(int degC);

        //Thermal Design Current
        bool SupportsTdcLimit();
        int GetTdcLimit();
        RangeValues GetTdcLimitRange();
        void SetTdcLimit(int tdc);

        //Clock speeds and voltage offset
        bool SupportsMaxClockSpeed();
        int GetMaxClockSpeed();
        RangeValues GetMaxClockSpeedRange();
        void SetMaxClockSpeed(int mhz);

        bool SupportsVoltage();
        int GetVoltage();
        RangeValues GetVoltageRange();
        void SetVoltage(int mv);

        //VRAM
        bool SupportsVramSpeed();
        int GetVramSpeed();
        RangeValues GetVramSpeedRange();
        void SetVramSpeed(int mhz);
        //nvapi seemingly doesnt support memory timings so only adlx
        bool SupportsVramTiming();
        List<TimingMode> GetVramTiming();
        void SetVramTiming(TimingMode timing);

        //restore everything to their default settings
        void RestoreToDefault();
    }
}
