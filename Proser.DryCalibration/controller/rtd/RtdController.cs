using Proser.DryCalibration.modules;
using Proser.DryCalibration.sensor.rtd.calibration;
using Proser.DryCalibration.util;
using System.IO;

namespace Proser.DryCalibration.controller.rtd
{
    public class RtdController : MonitorController
    {

        public RtdController()
        {
            logFile = "Proser.Rtd.Monitor.log";
        }   

        public override void initMonitor()
        {
            string path = Path.Combine(Utils.ConfigurationPath, "RtdCalibration.xml");
            RtdTable calibration = RtdTable.Read(path);

            NIDaqModule device = NIDaqModule.Instance();
            device.StatisticReceived += MonitorController_StatisticReceived;
            device.UpdateSensorReceived += MonitorController_UpdateSensorReceived;

            device.Connect(calibration.DaqModuleIPAddress);

            device.InitRtdMonitor(calibration);

            Monitor = device.Monitor;
        }

        
    }
}
