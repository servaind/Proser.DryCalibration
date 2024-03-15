using log4net;
using NationalInstruments.DAQmx;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.interfaces;
using Proser.DryCalibration.monitor.statistic;
using Proser.DryCalibration.sensor.rtd;
using Proser.DryCalibration.sensor.rtd.calibration;
using Proser.DryCalibration.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proser.DryCalibration.modules
{
    public class NIDaqModuleBase
    {
        //event
        public event Action<MonitorType, StatisticValue> StatisticReceived;
        public event Action<MonitorType, object> UpdateSensorReceived;

        //device
        private const int TIMEOUT = 20;
        public const int AI_PER_MODULE = 4;

        protected Device device;
        protected ILog logger;

        public ILog Logger { set { logger = value; } }

        protected void connect(string ip)
        {
            try
            {
                device = DaqSystem.Local.AddNetworkDevice(ip, "", TIMEOUT);
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.ErrorFormat("NIDeviceHelper.Connect.AddNetworkDevice: {0}.", ex.Message);
                }

                throw new Exception("No se pudo conectar al equipo DAQ.");
            }
        }

       
        protected void reserveDevice()
        {
            try
            {
                device.ReserveNetworkDevice(true);
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.ErrorFormat("NIDeviceHelper.Connect.ReserveNetworkDevice: {0}.", ex.Message);
                }

                throw new Exception("No se pudo reservar el equipo DAQ.");
            }
        }

      

        protected void detectModules()
        {
            try
            {
                device.SelfTest();
            }
            catch (Exception ex)
            {

                if (logger != null)
                {
                    logger.ErrorFormat("NIDeviceHelper.SelfTest: {0}.", ex.Message);
                }

                throw new Exception("El equipo DAQ no puede detectar los módulos.");
            }
        }



        protected void Monitor_StatisticReceived(MonitorType type, StatisticValue value)
        {
            StatisticReceived?.Invoke(type, value);
        }

        protected void Monitor_UpdateSensorReceived(MonitorType type, object value)
        {
            UpdateSensorReceived?.Invoke(type, value);
        }

    }
}
