using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.configuration
{

    [Serializable]
    public class TcpConfig
    {
        public string IPAddress = "127.0.0.1";
        public int PortNumber = 5251;
    }
}
