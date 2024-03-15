using Proser.DryCalibration.sensor.interfaces;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.interfaces
{
    public interface IUltrasonicSensor : IModbusSensor
    {
        List<RopeValue> Ropes { get; set; }
    }
}
