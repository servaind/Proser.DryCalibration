using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.interfaces;
using Proser.DryCalibration.monitor.statistic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.interfaces
{
    public interface IController
    {
        event Action<MonitorType, StatisticValue> StatisticReceived;
        event Action<MonitorType, object> UpdateSensorReceived;
        event Action<string> RefreshState;

        IMonitor Monitor { get; }
        void Initialize();
    }
}
