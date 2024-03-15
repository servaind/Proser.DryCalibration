using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.configuration
{

    [Serializable]
    public class SerialConfig
    {
        public string PortName = "COM1";
        public int BaudRate = 1; // 9600
        public int DataBits = 4; // 8
        public int Parity = 2; // None;
        public int StopBits = 0; // One;
    }

}
