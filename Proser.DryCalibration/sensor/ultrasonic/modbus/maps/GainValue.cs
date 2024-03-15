using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.maps
{
    public class GainValue
    {
        public double T1 { get; set; }
        public double T2 { get; set; }
        
        public GainValue()
        {
            T1 = 0;
            T2 = 0;
        }
    }
}
