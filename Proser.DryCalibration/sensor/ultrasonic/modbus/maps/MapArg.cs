using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.maps
{
    [Serializable]
    public class MapArg
    {
        public string Name { get; set; }
        public string RopeName { get; set; }
        public ushort ArgAddress { get; set; }
        public AddressPointFormat AddressFormat { get; set; }
    }
}
