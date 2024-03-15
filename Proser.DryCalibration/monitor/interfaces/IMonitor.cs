using log4net;
using NationalInstruments.DAQmx;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.statistic;
using System;

namespace Proser.DryCalibration.monitor.interfaces
{

    public interface IMonitor
    {
        event Action<MonitorType, StatisticValue> StatisticReceived;
        event Action<MonitorType, object> UpdateSensorReceived;

        MonitorType Type { get;}
        StatisticValue Statistic { get; }
        bool IsStable { get; }
      
        void InitMonitor(Device daqModule);
        void InitMonitor();
        void StopMonitor();
        void LoadSensor();
        void UpdateSensor();
        decimal? ReadAI(int module, string ai);
        void DoMonitorWork();
    }
}
