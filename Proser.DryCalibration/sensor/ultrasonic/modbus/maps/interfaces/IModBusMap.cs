using Modbus.Device;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.maps.interfaces
{
    public interface IModBusMap
    {
        UltrasonicModel Measurer { get;  }
        List<Rope> Ropes { get; }
        List<EfficiencyArg> EfficiencyAddresses { get; }
        List<GainArg> GainAddresses { get; }
        
        double CalculateRopeEfficiency(string ropeName, IModbusSerialMaster modbusMaster, byte slaveId);
        GainValue CalculateRopeGain(string ropeName, IModbusSerialMaster modbusMaster, byte slaveId);
    }
}
