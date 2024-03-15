using Modbus.Device;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.maps
{
    [Serializable]
    public class ModbusMap : IModBusMap
    {
        public UltrasonicModel Measurer { get; set; }
        public List<Rope> Ropes { get; set; }
        public List<EfficiencyArg> EfficiencyAddresses { get; set; }
        public List<GainArg> GainAddresses { get; set; }

        protected virtual void MapMeasurer() { }
        protected virtual void MapRopeEfficiency() { }
        protected virtual void MapRopeGain() { }
        
        protected virtual double ApplyEfficiencyEquation(params double[] args) { return 0; }
        protected virtual GainValue ApplyGainEquation(params double[] args) { return new GainValue(); }

        public virtual double CalculateRopeEfficiency(string ropeName, IModbusSerialMaster modbusMaster, byte slaveId) { return 0; }
        public virtual GainValue CalculateRopeGain(string ropeName, IModbusSerialMaster modbusMaster, byte slaveId) { return new GainValue(); }

        public ModbusMap()
        {
            Ropes = new List<Rope>();
            EfficiencyAddresses = new List<EfficiencyArg>();
            GainAddresses = new List<GainArg>();
        }

        public ModbusMap(ModbusMap map) : this()
        {
            Measurer = map.Measurer;
            Ropes.AddRange(map.Ropes);
            EfficiencyAddresses.AddRange(map.EfficiencyAddresses);
            GainAddresses.AddRange(map.GainAddresses);
        }

    }
}
