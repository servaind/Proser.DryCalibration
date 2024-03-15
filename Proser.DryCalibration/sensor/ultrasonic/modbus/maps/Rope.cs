using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.maps
{
    [Serializable]
    public class Rope
    {
        public string Name { get; set; }
        public ushort FlowSpeedAddress { get; set; }
        public ushort SoundSpeedAddress { get; set; }
        public AddressPointFormat AddressFormat { get; set; }
    }
}
