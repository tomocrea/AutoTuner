using AutoTuner.GPU;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Text;

namespace AutoTuner.GPU.AMD
{
    internal class AdlxAdapter : IGpuTuning, IGpuMonitoring, IDisposable
    {
        //Create ADLX interface objects
        //IADLXManualFanTuning1 and IADLXManualPowerTuning1 exists but not in docs
        //Tuning1/2 and VRAM1/2: 1 is for older pre-RDNA cards and 2 for newer RDNA cards
        private IADLXManualFanTuning? fanTuning;

        private IADLXManualPowerTuning? powerTuning;

        private IADLXManualGraphicsTuning1? graphicsTuning1;
        private IADLXManualGraphicsTuning2? graphicsTuning2;

        private IADLXManualVRAMTuning1? vramTuning1;
        private IADLXManualVRAMTuning2? vramTuning2;

        private IADLXGPUTuningServices? tuningServices;

        private IADLXGPUMetrics? metrics;
        private IADLXGPUMetricsSupport? metricsSupport;

        private IADLXGPU gpuCopy;
        private AdlxWrapper initCopy;

        public AdlxAdapter(AdlxWrapper init, IADLXGPU gpu) 
        {
            fanTuning = init.GetManualFanTuning(gpu);

            powerTuning = init.GetManualPowerTuning(gpu);

            var graphicsTuning = init.GetManualGraphicsTuning(gpu);
            graphicsTuning1 = graphicsTuning.tuning1;
            graphicsTuning2 = graphicsTuning.tuning2;

            var vramTuning = init.GetManualVRAMTuning(gpu);
            vramTuning1 = vramTuning.tuning1;
            vramTuning2 = vramTuning.tuning2;

            tuningServices = init.tuningService;

            metrics = init.GetGPUMetrics(gpu);
            metricsSupport = init.GetGPUMetricsSupport(gpu);

            gpuCopy = gpu;
            initCopy = init;
        }

        public bool SupportsFanMode(FanMode mode)
        {
            if (fanTuning == null)
            {
                return false; 
            }
            else if (mode == FanMode.ZeroRPM)
            {
                fanTuning.IsSupportedZeroRPM(out bool zeroSupported);
                return zeroSupported;
            }
            else if (mode == FanMode.FixedManual)
            {
                fanTuning.IsSupportedMinFanSpeed(out bool minSupported);
                return minSupported;
            }
            else if (mode == FanMode.CurveManual)
            {
                //use swigtype for custom type
                //check if curve (made from states(points)) is supported by trying to get states
                SWIGTYPE_p_p_adlx__IADLXManualFanTuningStateList states = ADLX.new_manualFanTuningStateListP_Ptr();
                ADLX_RESULT res = fanTuning.GetFanTuningStates(states);
                ADLX.delete_manualFanTuningStateListP_Ptr(states);
                if(res == ADLX_RESULT.ADLX_OK)
                {
                    return true;
                }
            }
            else if(mode == FanMode.Auto)
            {
                //default auto
                return true;
            }
            return false;
        }

        public void SetFanMode(FanModeValues value)
        {
            if (fanTuning == null)
            {
                return;
            }
            else if (value is ZeroRPMValue zeroVal)
            {
                fanTuning.SetZeroRPMState(zeroVal.Zero);
                return;
            }
            else if (value is FixedManualValue fixedVal)
            {
                fanTuning.SetMinFanSpeed(fixedVal.Percentage);
                return;
            }
            else if (value is CurveManualValue curveVal)
            {
                //not needed for now
                throw new Exception("Manual fan curve not supported");
            }
            else if (value is AutoValue autoVal)
            {
                //TODO: unsure how to directly set fan back to auto other than back to factory settings
                return;
            }
            return;
        }

        public bool SupportsPowerLimit()
        {
            if(powerTuning != null && powerTuning.GetPowerLimit(out int val) == ADLX_RESULT.ADLX_OK)
            {
                return true;
            }
            return false;
        }

        public int GetPowerLimit()
        {
            if(powerTuning != null)
            {
                powerTuning.GetPowerLimit(out int val);
                return val;
            }
            throw new Exception("Power tuning null");
        }
        public RangeValues GetPowerLimitRange()
        {
            if (powerTuning != null)
            {
                using ADLX_IntRange range = new ADLX_IntRange();
                powerTuning.GetPowerLimitRange(range);
                return new RangeValues(range.minValue, range.maxValue, range.step);
            }
            throw new Exception("Power tuning null");
        }

        public void SetPowerLimit(int percent)
        {
            if(powerTuning != null)
            {
                powerTuning.SetPowerLimit(percent);
            }
        }

        public bool SupportsTempLimit()
        {
            return false;
        }
        public int GetTempLimit()
        {
            throw new Exception("Temp limit unsupported in ADLX");
        }
        public RangeValues GetTempLimitRange()
        {
            throw new Exception("Temp limit unsupported in ADLX");
        }
        public void SetTempLimit(int degC)
        {
            throw new Exception("Temp limit unsupported in ADLX");
        }

        public bool SupportsTdcLimit()
        {
            if(powerTuning != null)
            {
                powerTuning.IsSupportedTDCLimit(out bool supported);
                return supported;
            }
            return false;
        }
        public int GetTdcLimit()
        {
            if (powerTuning != null)
            {
                powerTuning.GetTDCLimit(out int tdc);
                return tdc;
            }
            throw new Exception("Power tuning null");
        }
        public RangeValues GetTdcLimitRange()
        {
            if (powerTuning != null)
            {
                using ADLX_IntRange range = new ADLX_IntRange();
                powerTuning.GetTDCLimitRange(range);
                return new RangeValues(range.minValue, range.maxValue, range.step);
            }
            throw new Exception("Power tuning null");
        }
        public void SetTdcLimit(int tdc)
        {
            if (powerTuning != null)
            {
                powerTuning.SetTDCLimit(tdc);
            }
        }

        public bool SupportsMaxClockSpeed()
        {
            if (graphicsTuning2 != null)
            {
                //workaround since no issupported, check if clock speeds supported by getting max frequency
                ADLX_RESULT res = graphicsTuning2.GetGPUMaxFrequency(out int maxFreq);
                if (res == ADLX_RESULT.ADLX_OK)
                {
                    return true;
                }
            }
            else if (graphicsTuning1 != null)
            {
                //only way to check, supports both frequency/clock speed and voltage
                SWIGTYPE_p_p_adlx__IADLXManualTuningStateList states = ADLX.new_manualTuningStateListP_Ptr();
                ADLX_RESULT res = graphicsTuning1.GetGPUTuningStates(states);
                ADLX.delete_manualTuningStateListP_Ptr(states);

                if (res == ADLX_RESULT.ADLX_OK)
                {
                    return true;
                }
            }
            return false;
        }
        public int GetMaxClockSpeed()
        {
            if(graphicsTuning2 != null)
            {
                //sets max frequency under load
                graphicsTuning2.GetGPUMaxFrequency(out int maxFreq);
                return maxFreq;
            }
            else if (graphicsTuning1 != null)
            {
                //gpu tuning state list, gets the last in list to set max
                //note: for pre-rdna using tuning states the last state is used
                //as the first state is for idle, and goes up to the last state which is used for the highest demanding workloads
                //seen in https://wiki.archlinux.org/title/AMDGPU#Manual is that the highest state is the max limit essentially
                SWIGTYPE_p_p_adlx__IADLXManualTuningStateList states = ADLX.new_manualTuningStateListP_Ptr();
                graphicsTuning1.GetGPUTuningStates(states);
                using IADLXManualTuningStateList stateList = ADLX.manualTuningStateListP_Ptr_value(states);

                uint stateListSize = stateList.Size();
                SWIGTYPE_p_p_adlx__IADLXManualTuningState state = ADLX.new_manualTuningStateP_Ptr();
                stateList.At(stateListSize-1, state);
                using IADLXManualTuningState lastState = ADLX.manualTuningStateP_Ptr_value(state);

                lastState.GetFrequency(out int frequency);

                ADLX.delete_manualTuningStateListP_Ptr(states);
                ADLX.delete_manualTuningStateP_Ptr(state);
                return frequency;
            }
            throw new Exception("Graphics tuning null");
        }
        public RangeValues GetMaxClockSpeedRange()
        {
            if (graphicsTuning2 != null)
            {
                using ADLX_IntRange range = new ADLX_IntRange();
                graphicsTuning2.GetGPUMaxFrequencyRange(range);
                return new RangeValues(range.minValue, range.maxValue, range.step);
            }
            else if (graphicsTuning1 != null)
            {
                using ADLX_IntRange rangeFreq = new ADLX_IntRange();
                using ADLX_IntRange rangeVolt = new ADLX_IntRange();
                graphicsTuning1.GetGPUTuningRanges(rangeFreq, rangeVolt);
                return new RangeValues(rangeFreq.minValue, rangeFreq.maxValue, rangeFreq.step);
            }
            throw new Exception("Graphics tuning null");
        }
        public void SetMaxClockSpeed(int mhz)
        {
            if(graphicsTuning2 != null)
            {
                graphicsTuning2.SetGPUMaxFrequency(mhz);
                return;
            }
            if(graphicsTuning1 != null)
            {
                //sets last state frequency
                SWIGTYPE_p_p_adlx__IADLXManualTuningStateList states = ADLX.new_manualTuningStateListP_Ptr();
                graphicsTuning1.GetGPUTuningStates(states);
                using IADLXManualTuningStateList stateList = ADLX.manualTuningStateListP_Ptr_value(states);

                uint stateListSize = stateList.Size();
                SWIGTYPE_p_p_adlx__IADLXManualTuningState state = ADLX.new_manualTuningStateP_Ptr();
                stateList.At(stateListSize - 1, state);
                using IADLXManualTuningState lastState = ADLX.manualTuningStateP_Ptr_value(state);

                lastState.SetFrequency(mhz);

                ADLX.delete_manualTuningStateListP_Ptr(states);
                ADLX.delete_manualTuningStateP_Ptr(state);
                return;
            }
        }

        public bool SupportsVoltage()
        {
            if (graphicsTuning2 != null)
            {
                //check if voltage supported by getting voltage offset
                ADLX_RESULT res = graphicsTuning2.GetGPUVoltage(out int voltageOffset);
                if (res == ADLX_RESULT.ADLX_OK)
                {
                    return true;
                }
            }
            else if (graphicsTuning1 != null)
            {
                //check supports both frequency/clock speed and voltage
                SWIGTYPE_p_p_adlx__IADLXManualTuningStateList states = ADLX.new_manualTuningStateListP_Ptr();
                ADLX_RESULT res = graphicsTuning1.GetGPUTuningStates(states);
                ADLX.delete_manualTuningStateListP_Ptr(states);

                if (res == ADLX_RESULT.ADLX_OK)
                {
                    return true;
                }
            }
            return false;
        }
        public int GetVoltage()
        {
            if (graphicsTuning2 != null)
            {
                //sets max voltage under load
                graphicsTuning2.GetGPUVoltage(out int maxFreq);
                return maxFreq;
            }
            else if (graphicsTuning1 != null)
            {
                //gpu tuning state list, gets the last in list to set max
                SWIGTYPE_p_p_adlx__IADLXManualTuningStateList states = ADLX.new_manualTuningStateListP_Ptr();
                graphicsTuning1.GetGPUTuningStates(states);
                using IADLXManualTuningStateList stateList = ADLX.manualTuningStateListP_Ptr_value(states);

                uint stateListSize = stateList.Size();
                SWIGTYPE_p_p_adlx__IADLXManualTuningState state = ADLX.new_manualTuningStateP_Ptr();
                stateList.At(stateListSize - 1, state);
                using IADLXManualTuningState lastState = ADLX.manualTuningStateP_Ptr_value(state);

                lastState.GetVoltage(out int voltage);

                ADLX.delete_manualTuningStateListP_Ptr(states);
                ADLX.delete_manualTuningStateP_Ptr(state);
                return voltage;
            }
            throw new Exception("Graphics tuning null");
        }
        public RangeValues GetVoltageRange()
        {
            if (graphicsTuning2 != null)
            {
                using ADLX_IntRange range = new ADLX_IntRange();
                graphicsTuning2.GetGPUVoltageRange(range);
                return new RangeValues(range.minValue, range.maxValue, range.step);
            }
            else if (graphicsTuning1 != null)
            {
                using ADLX_IntRange rangeFreq = new ADLX_IntRange();
                using ADLX_IntRange rangeVolt = new ADLX_IntRange();
                graphicsTuning1.GetGPUTuningRanges(rangeFreq, rangeVolt);
                return new RangeValues(rangeVolt.minValue, rangeVolt.maxValue, rangeVolt.step);
            }
            throw new Exception("Graphics tuning null");
        }
        public void SetVoltage(int mv)
        {
            if (graphicsTuning2 != null)
            {
                graphicsTuning2.SetGPUVoltage(mv);
                return;
            }
            if (graphicsTuning1 != null)
            {
                SWIGTYPE_p_p_adlx__IADLXManualTuningStateList states = ADLX.new_manualTuningStateListP_Ptr();
                graphicsTuning1.GetGPUTuningStates(states);
                using IADLXManualTuningStateList stateList = ADLX.manualTuningStateListP_Ptr_value(states);

                uint stateListSize = stateList.Size();
                SWIGTYPE_p_p_adlx__IADLXManualTuningState state = ADLX.new_manualTuningStateP_Ptr();
                stateList.At(stateListSize - 1, state);
                using IADLXManualTuningState lastState = ADLX.manualTuningStateP_Ptr_value(state);

                lastState.SetVoltage(mv);

                ADLX.delete_manualTuningStateListP_Ptr(states);
                ADLX.delete_manualTuningStateP_Ptr(state);
                return;
            }
        }

        public bool SupportsVramSpeed()
        {
            if (vramTuning2 != null)
            {
                //workaround since no issupported check if voltage supported by getting max frequency
                ADLX_RESULT res = vramTuning2.GetMaxVRAMFrequency(out int maxFreq);
                if (res == ADLX_RESULT.ADLX_OK)
                {
                    return true;
                }
            }
            else if (vramTuning1 != null)
            {
                //only way to check workaround
                SWIGTYPE_p_p_adlx__IADLXManualTuningStateList states = ADLX.new_manualTuningStateListP_Ptr();
                ADLX_RESULT res = vramTuning1.GetVRAMTuningStates(states);
                ADLX.delete_manualTuningStateListP_Ptr(states);

                if (res == ADLX_RESULT.ADLX_OK)
                {
                    return true;
                }
            }
            return false;
        }
        public int GetVramSpeed()
        {
            if (vramTuning2 != null)
            {
                //sets max frequency under load
                vramTuning2.GetMaxVRAMFrequency(out int maxFreq);
                return maxFreq;
            }
            else if (vramTuning1 != null)
            {
                //gpu tuning state list, gets the last in list to set max
                SWIGTYPE_p_p_adlx__IADLXManualTuningStateList states = ADLX.new_manualTuningStateListP_Ptr();
                vramTuning1.GetVRAMTuningStates(states);
                using IADLXManualTuningStateList stateList = ADLX.manualTuningStateListP_Ptr_value(states);

                uint stateListSize = stateList.Size();
                SWIGTYPE_p_p_adlx__IADLXManualTuningState state = ADLX.new_manualTuningStateP_Ptr();
                stateList.At(stateListSize - 1, state);
                using IADLXManualTuningState lastState = ADLX.manualTuningStateP_Ptr_value(state);

                lastState.GetFrequency(out int frequency);

                ADLX.delete_manualTuningStateListP_Ptr(states);
                ADLX.delete_manualTuningStateP_Ptr(state);
                return frequency;
            }
            throw new Exception("Vram tuning null");
        }
        public RangeValues GetVramSpeedRange()
        {
            if (vramTuning2 != null)
            {
                using ADLX_IntRange range = new ADLX_IntRange();
                vramTuning2.GetMaxVRAMFrequencyRange(range);
                return new RangeValues(range.minValue, range.maxValue, range.step);
            }
            else if (vramTuning1 != null)
            {
                using ADLX_IntRange rangeFreq = new ADLX_IntRange();
                using ADLX_IntRange rangeVolt = new ADLX_IntRange();
                vramTuning1.GetVRAMTuningRanges(rangeFreq, rangeVolt);
                return new RangeValues(rangeFreq.minValue, rangeFreq.maxValue, rangeFreq.step);
            }
            throw new Exception("Vram tuning null");
        }
        public void SetVramSpeed(int mhz)
        {
            if (vramTuning2 != null)
            {
                vramTuning2.SetMaxVRAMFrequency(mhz);
                return;
            }
            if (vramTuning1 != null)
            {
                //sets last state frequency
                SWIGTYPE_p_p_adlx__IADLXManualTuningStateList states = ADLX.new_manualTuningStateListP_Ptr();
                vramTuning1.GetVRAMTuningStates(states);
                using IADLXManualTuningStateList stateList = ADLX.manualTuningStateListP_Ptr_value(states);

                uint stateListSize = stateList.Size();
                SWIGTYPE_p_p_adlx__IADLXManualTuningState state = ADLX.new_manualTuningStateP_Ptr();
                stateList.At(stateListSize - 1, state);
                using IADLXManualTuningState lastState = ADLX.manualTuningStateP_Ptr_value(state);

                lastState.SetFrequency(mhz);

                ADLX.delete_manualTuningStateListP_Ptr(states);
                ADLX.delete_manualTuningStateP_Ptr(state);
                return;
            }
        }
        public bool SupportsVramTiming()
        {
            if (vramTuning2 != null)
            {
                vramTuning2.IsSupportedMemoryTiming(out bool supported);
                return supported;
            }
            else if (vramTuning1 != null)
            {
                vramTuning1.IsSupportedMemoryTiming(out bool supported);
                return supported;
            }
            return false;
        }
        //loops through description list and returns vram timing descriptions
        public List<TimingMode> GetVramTiming()
        {
            if (vramTuning2 != null)
            {
                List<TimingMode> result = new List<TimingMode>();
                SWIGTYPE_p_p_adlx__IADLXMemoryTimingDescriptionList descs = ADLX.new_memoryTimingDescriptionListP_Ptr();
                vramTuning2.GetSupportedMemoryTimingDescriptionList(descs);
                using IADLXMemoryTimingDescriptionList descList = ADLX.memoryTimingDescriptionListP_Ptr_value(descs);

                uint descListSize = descList.Size();

                for(uint i = 0; i < descListSize; i++)
                {
                    SWIGTYPE_p_p_adlx__IADLXMemoryTimingDescription desc = ADLX.new_memoryTimingDescriptionP_Ptr();
                    descList.At(i,desc);
                    using IADLXMemoryTimingDescription currDesc = ADLX.memoryTimingDescriptionP_Ptr_value(desc);

                    SWIGTYPE_p_ADLX_MEMORYTIMING_DESCRIPTION descVal = ADLX.new_memoryTimingDescriptionP();
                    currDesc.GetDescription(descVal);
                    ADLX_MEMORYTIMING_DESCRIPTION descEnum = ADLX.memoryTimingDescriptionP_value(descVal);
                    //https://www.delftstack.com/howto/csharp/how-to-get-int-value-from-enum-in-csharp/
                    int descInt = (int)descEnum;
                    TimingMode getTiming = descInt switch
                    {
                        0 => TimingMode.Default,
                        1 => TimingMode.Fast,
                        2 => TimingMode.Fast2,
                        3 => TimingMode.Auto,
                        4 => TimingMode.Level1,
                        5 => TimingMode.Level2,
                        _ => throw new NotImplementedException()
                    };
                    result.Add(getTiming);
                    ADLX.delete_memoryTimingDescriptionP(descVal);
                    ADLX.delete_memoryTimingDescriptionP_Ptr(desc);
                }
                ADLX.delete_memoryTimingDescriptionListP_Ptr(descs);
                return result;
            }
            else if (vramTuning1 != null)
            {
                List<TimingMode> result = new List<TimingMode>();
                SWIGTYPE_p_p_adlx__IADLXMemoryTimingDescriptionList descs = ADLX.new_memoryTimingDescriptionListP_Ptr();
                vramTuning1.GetSupportedMemoryTimingDescriptionList(descs);
                using IADLXMemoryTimingDescriptionList descList = ADLX.memoryTimingDescriptionListP_Ptr_value(descs);

                uint descListSize = descList.Size();

                for (uint i = 0; i < descListSize; i++)
                {
                    SWIGTYPE_p_p_adlx__IADLXMemoryTimingDescription desc = ADLX.new_memoryTimingDescriptionP_Ptr();
                    descList.At(i, desc);
                    using IADLXMemoryTimingDescription currDesc = ADLX.memoryTimingDescriptionP_Ptr_value(desc);

                    SWIGTYPE_p_ADLX_MEMORYTIMING_DESCRIPTION descVal = ADLX.new_memoryTimingDescriptionP();
                    currDesc.GetDescription(descVal);
                    ADLX_MEMORYTIMING_DESCRIPTION descEnum = ADLX.memoryTimingDescriptionP_value(descVal);
                    //https://www.delftstack.com/howto/csharp/how-to-get-int-value-from-enum-in-csharp/
                    int descInt = (int)descEnum;
                    TimingMode getTiming = descInt switch
                    {
                        0 => TimingMode.Default,
                        1 => TimingMode.Fast,
                        2 => TimingMode.Fast2,
                        3 => TimingMode.Auto,
                        4 => TimingMode.Level1,
                        5 => TimingMode.Level2,
                        _ => throw new NotImplementedException()
                    };
                    result.Add(getTiming);
                    ADLX.delete_memoryTimingDescriptionP(descVal);
                    ADLX.delete_memoryTimingDescriptionP_Ptr(desc);
                }
                ADLX.delete_memoryTimingDescriptionListP_Ptr(descs);
                return result;
            }
            throw new Exception("Vram tuning null");
        }
        public void SetVramTiming(TimingMode timing)
        {
            //map TimingMode to ADLX_MEMORYTIMING_DESCRIPTION enum
            ADLX_MEMORYTIMING_DESCRIPTION adlxTiming = timing switch
            {
                TimingMode.Default => ADLX_MEMORYTIMING_DESCRIPTION.MEMORYTIMING_DEFAULT,
                TimingMode.Fast => ADLX_MEMORYTIMING_DESCRIPTION.MEMORYTIMING_FAST_TIMING,
                TimingMode.Fast2 => ADLX_MEMORYTIMING_DESCRIPTION.MEMORYTIMING_FAST_TIMING_LEVEL_2,
                TimingMode.Auto => ADLX_MEMORYTIMING_DESCRIPTION.MEMORYTIMING_AUTOMATIC,
                TimingMode.Level1 => ADLX_MEMORYTIMING_DESCRIPTION.MEMORYTIMING_MEMORYTIMING_LEVEL_1,
                TimingMode.Level2 => ADLX_MEMORYTIMING_DESCRIPTION.MEMORYTIMING_MEMORYTIMING_LEVEL_2,
                _ => throw new ArgumentException("Unsupported timing mode requested.")
            };
            if (vramTuning2 != null)
            {
                vramTuning2.SetMemoryTimingDescription(adlxTiming);
                return;
            }
            else if (vramTuning1 != null)
            {
                vramTuning1.SetMemoryTimingDescription(adlxTiming);
                return;
            }
            throw new Exception("Vram tuning null");
        }

        public void UpdateMetrics()
        {
            if (metrics != null)
            {
                metrics.Destroy();
            }
            metrics = initCopy.GetGPUMetrics(gpuCopy);
        }
        public bool SupportsHotspotTemp()
        {
            if(metricsSupport != null)
            {
                metricsSupport.IsSupportedGPUHotspotTemperature(out bool supported);
                return supported;
            }
            return false;
        }
        public double GetHotspotTemp()
        {
            if (metrics != null)
            {
                metrics.GPUHotspotTemperature(out double temp);
                return temp;
            }
            throw new Exception("GPU metrics null");
        }
        public bool SupportsCurrentTemperature()
        {
            if (metricsSupport != null)
            {
                metricsSupport.IsSupportedGPUTemperature(out bool supported);
                return supported;
            }
            return false;
        }
        public double GetCurrentTemperature()
        {
            if (metrics != null)
            {
                metrics.GPUTemperature(out double temp);
                return temp;
            }
            throw new Exception("GPU metrics null");
        }
        public bool SupportsCurrentClockSpeed()
        {
            if (metricsSupport != null)
            {
                metricsSupport.IsSupportedGPUClockSpeed(out bool supported);
                return supported;
            }
            return false;
        }
        public int GetCurrentClockSpeed()
        {
            if (metrics != null)
            {
                metrics.GPUClockSpeed(out int speed);
                return speed;
            }
            throw new Exception("GPU metrics null");
        }
        public bool SupportsCurrentVramSpeed()
        {
            if (metricsSupport != null)
            {
                metricsSupport.IsSupportedGPUVRAMClockSpeed(out bool supported);
                return supported;
            }
            return false;
        }
        public int GetCurrentVramSpeed()
        {
            if (metrics != null)
            {
                metrics.GPUVRAMClockSpeed(out int speed);
                return speed;
            }
            throw new Exception("GPU metrics null");
        }
        public bool SupportsCurrentVoltage()
        {
            if (metricsSupport != null)
            {
                metricsSupport.IsSupportedGPUVoltage(out bool supported);
                return supported;
            }
            return false;
        }
        public int GetCurrentVoltage()
        {
            if (metrics != null)
            {
                metrics.GPUVoltage(out int voltage);
                return voltage;
            }
            throw new Exception("GPU metrics null");
        }
        public bool SupportsCurrentUsage()
        {
            if (metricsSupport != null)
            {
                metricsSupport.IsSupportedGPUUsage(out bool supported);
                return supported;
            }
            return false;
        }
        public double GetCurrentUsage()
        {
            if (metrics != null)
            {
                metrics.GPUUsage(out double usage);
                return usage;
            }
            throw new Exception("GPU metrics null");
        }
        public bool SupportsTotalBoardPower()
        {
            if (metricsSupport != null)
            {
                metricsSupport.IsSupportedGPUTotalBoardPower(out bool supported);
                return supported;
            }
            return false;
        }
        public double GetTotalBoardPower()
        {
            if (metrics != null)
            {
                metrics.GPUTotalBoardPower(out double power);
                return power;
            }
            throw new Exception("GPU metrics null");
        }

        public void RestoreToDefault()
        {
            if (tuningServices != null && gpuCopy != null)
            {
                tuningServices.ResetToFactory(gpuCopy);
                return;
            }
            throw new Exception("Tuning services or gpu null");
        }

        public void Dispose()
        {
            //https://stackoverflow.com/questions/26142574/what-does-the-question-mark-in-member-access-mean-in-c
            fanTuning?.Destroy();
            powerTuning?.Destroy();
            metrics?.Destroy();
            graphicsTuning1?.Destroy();
            graphicsTuning2?.Destroy();
            vramTuning1?.Destroy();
            vramTuning2?.Destroy();
            metrics?.Destroy();
            metricsSupport?.Destroy();
            gpuCopy?.Destroy();
        }
    }
}
