using log4net;
using Proser.DryCalibration.controller.interfaces;
using Proser.DryCalibration.log;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.exceptions;
using Proser.DryCalibration.monitor.interfaces;
using Proser.DryCalibration.monitor.statistic;
using System;

namespace Proser.DryCalibration.controller
{
    public class MonitorController : IController
    {

        public event Action<MonitorType, StatisticValue> StatisticReceived;
        public event Action<MonitorType, object> UpdateSensorReceived;
        public event Action<string> RefreshState;

        protected string logFile;

        public IMonitor Monitor { get; protected set; }

        public MonitorController()
        {
            logFile = "Proser.Monitor.log";
        }
     
        public void Initialize()
        {
            try
            {        
                initMonitor();
            }
            catch (Exception e)
            {
                // log
                log.Log.WriteIfExists("Error: MonitorController. Initialize", e);

                throw new MonitorInitializationException(e.Message);
            }
        }

        public virtual void initMonitor() { }

        protected void StatusMessage(string message)
        {
            RefreshState?.Invoke(message);
        }

        protected void MonitorController_UpdateSensorReceived(MonitorType type, object value)
        {
            UpdateSensorReceived?.Invoke(type, value);
        }

        protected void MonitorController_StatisticReceived(MonitorType type, StatisticValue value)
        {
            StatisticReceived?.Invoke(type, value);
        }

    }
}
