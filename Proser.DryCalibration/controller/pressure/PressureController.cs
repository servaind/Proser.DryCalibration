using Proser.DryCalibration.modules;
using Proser.DryCalibration.monitor;
using Proser.DryCalibration.sensor.pressure.calibration;
using Proser.DryCalibration.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.pressure
{
    public class PressureController : MonitorController
    {
        public PressureController()
        {
            logFile = "Proser.Pressure.Monitor.log";
        }

        public override void initMonitor()
        {
            string path = Path.Combine(Utils.ConfigurationPath, "PressureCalibration.xml");

            PressureCalibration calibration = PressureCalibration.Read(path);

            NIDaqModule device = NIDaqModule.Instance();
            device.StatisticReceived += MonitorController_StatisticReceived;
            device.UpdateSensorReceived += MonitorController_UpdateSensorReceived;

            device.Connect(calibration.DaqModuleIPAddress);

            device.InitPressureMonitor(calibration);

            Monitor = device.Monitor;
        }


    }
}
