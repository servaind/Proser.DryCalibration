using log4net;
using NationalInstruments.DAQmx;
using Proser.DryCalibration.modules.interfaces;
using Proser.DryCalibration.monitor;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.interfaces;
using Proser.DryCalibration.monitor.statistic;
using Proser.DryCalibration.sensor.pressure.calibration;
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
    public class NIDaqModule : NIDaqModuleBase, IDisposable, INIDaqModule
    {
    
        // monitor
        private IMonitor monitor;
        private static NIDaqModule instance;

        public IMonitor Monitor { get { return monitor; } }

        public static NIDaqModule Instance()
        {
            if (instance == null)
            {
                instance = new NIDaqModule();
            }

            return instance;
        }

        public NIDaqModule()
        {
    
        }

        public void Connect(string ip)
        {         
            connect(ip);
            reserveDevice(); 
            detectModules();
        }

        public void InitRtdMonitor(RtdTable calibration)
        {
            if (device != null)
            {
                monitor = new RtdMonitor(calibration);
                monitor.UpdateSensorReceived += Monitor_UpdateSensorReceived;
                monitor.StatisticReceived += Monitor_StatisticReceived;

                monitor.InitMonitor(device);
            }
            else
            {
                throw new Exception("El dispositivo no está conectado");
            }
        }

        public void InitPressureMonitor(PressureCalibration calibration)
        {

            if (device != null)
            {
                monitor = new PressureMonitor(calibration);
                monitor.UpdateSensorReceived += Monitor_UpdateSensorReceived;
                monitor.StatisticReceived += Monitor_StatisticReceived;
              
                monitor.InitMonitor(device);
            }
            else
            {
                throw new Exception("El dispositivo no está conectado");
            }
        }

       

        public void Dispose()
        {
            try
            {
                device.UnreserveNetworkDevice();
                device.Dispose();
            }
            catch
            {

            }
        }
        
    }
}
