using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.configuration
{
    public class SlaveConfig
    {
        public byte SlaveId = 1;
        public int Model = 0; // Daniel;
        public int ModbusCommunication = 0; // Serial
        public int ModbusFrameFormat = 0; // ASCII
        public int SampleInterval = 0;
        public int SampleTimeOut = 0;
    }
}
