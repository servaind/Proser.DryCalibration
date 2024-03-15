using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.configuration
{
    public class GainConfig
    {
        public UltrasonicModel UltModel { get; set; }
        public double Min { get; set; }
        public double Max { get; set; }

        public GainConfig()
        {
            Min = 0;
            Max = 0;
            UltModel = UltrasonicModel.Daniel;
        }

    }
}
