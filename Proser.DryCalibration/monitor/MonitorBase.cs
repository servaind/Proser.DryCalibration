using log4net;
using NationalInstruments.DAQmx;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.interfaces;
using Proser.DryCalibration.monitor.statistic;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proser.DryCalibration.monitor
{
    public class MonitorBase : IMonitor
    {   
        protected object objLock;

        protected Thread MonitorWorkThread;
        protected MonitorState State; 
        protected ILog logger;

        public event Action<MonitorType, StatisticValue> StatisticReceived;
        public event Action<MonitorType, object> UpdateSensorReceived;

        public ILog Logger { set { this.logger = value; } }        
        public List<string> Modules { get; protected set; }
        public MonitorType Type { get; protected set; }
        public StatisticValue Statistic { get; private set; }
        public bool IsStable { get; protected set; }

        public MonitorBase()
        {
            Statistic = new StatisticValue();
            IsStable = false;
            State = MonitorState.Stoped;
        }

        public virtual void InitMonitor(Device daqModule)
        {
            LoadSensor();

            if (daqModule != null)
            {
                Modules = daqModule.ChassisModuleDeviceNames.ToList();
            }
            else
            {
                throw new Exception("El dispositivo no está conectado");
            }

            InitDaqModuleMonitorThread();
        }

        protected void InitDaqModuleMonitorThread()
        {
            objLock = new object();

            MonitorWorkThread = new Thread(new ThreadStart(DoMonitorWork));
            MonitorWorkThread.Start();

            State = MonitorState.Running;
        }

        public virtual void InitMonitor() { }

        public virtual void StopMonitor() 
        {
            State = MonitorState.Stoped;
        }
        
        public virtual void LoadSensor() { }

        public virtual void UpdateSensor() { }

        public virtual decimal? ReadAI(int module, string ai) { return null; }
       
        public virtual void DoMonitorWork() { }

        protected virtual void SendMonitorValues(object value)
        {
            UpdateSensorReceived?.Invoke(Type, value);
        }

        protected void SendMonitorStatistics()
        {
            StatisticReceived?.Invoke(Type, Statistic);
        }
    }
}
