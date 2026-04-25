using System;
using System.Collections.Generic;
using System.Text;

namespace AutoTuner.GPU
{
    public interface IGpuMonitoring : IDisposable
    {
        //Monitoring metrics
        //update metrics updates all metrics when called, getters use metrics from the last time metrics were updated
        void UpdateMetrics();
        bool SupportsHotspotTemp();
        double GetHotspotTemp();
        bool SupportsCurrentTemperature();
        double GetCurrentTemperature();
        bool SupportsCurrentClockSpeed();
        int GetCurrentClockSpeed();
        bool SupportsCurrentVramSpeed();
        int GetCurrentVramSpeed();
        bool SupportsCurrentVoltage();
        int GetCurrentVoltage();
        bool SupportsCurrentUsage();
        double GetCurrentUsage();
        bool SupportsTotalBoardPower();
        double GetTotalBoardPower();
        bool SupportsCurrentVramTemperature();
        double GetCurrentVramTemperature();
    }
}
