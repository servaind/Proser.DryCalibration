using Modbus.Device;
using Modbus.Utility;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.maps.measurers
{
    public class FMUMeasurer : ModbusMap
    {
        public FMUMeasurer()
        {
            Measurer = UltrasonicModel.FMU;
            
            MapMeasurer();
        }

        public FMUMeasurer(ModbusMap map) : base(map) { }

        protected override void MapMeasurer()
        {
            //mapear cuerdas
            string[] names = new string[] { "A", "B", "C", "D" };

            int name = 0;

            for (int add = 1015; add <= 1021; add += 2)
            {

                Rope rope = new Rope()
                {
                    Name = names[name],
                    FlowSpeedAddress = (ushort)add,  //velocidad de flujo
                    SoundSpeedAddress = (ushort)(add + 8),  //velocidad del sonido
                    AddressFormat = AddressPointFormat.Floating
                };

                Ropes.Add(rope);

                name++;
            }

            //mapear argumentos para el calculo
            MapRopeEfficiency();

            MapRopeGain();
        }

        protected override void MapRopeEfficiency()
        {
            string[,] names = new string[,] {{"PerformancePath1", "A"}, { "PerformancePath2", "B"}, { "PerformancePath3", "C"}, { "PerformancePath4", "D"}};

            int name = 0;

            for (int add = 2006; add <= 2009; add++)
            {
                EfficiencyArg arg = new EfficiencyArg()
                {
                    Name = names[name, 0],
                    RopeName = names[name, 1],
                    ArgAddress = (ushort)add,
                    AddressFormat =  AddressPointFormat.Integer
                };

                EfficiencyAddresses.Add(arg);

                name++;
            }
        }

        protected override void MapRopeGain()
        {
            //mapear argumentos para el calculo
            string[,] names = new string[,] {{"AgcDacValAB1", "A"}, {"AgcDacValAB2", "B"}, {"AgcDacValAB3", "C"}, {"AgcDacValAB4", "D"},
                                             {"AgcDacValBA1", "A"}, {"AgcDacValBA2", "B"}, {"AgcDacValBA3", "C"}, {"AgcDacValBA4", "D"}};
            int name = 0;

            for (int add = 2010; add <= 2017; add++)
            {
                GainArg arg = new GainArg()
                {
                    Name = names[name, 0],
                    RopeName = names[name, 1],
                    ArgAddress = (ushort)add,
                    AddressFormat = AddressPointFormat.Integer
                };

                GainAddresses.Add(arg);

                name++;
            }

        }

        public override GainValue CalculateRopeGain(string ropeName, IModbusSerialMaster modbusMaster, byte slaveId)
        {
            List<GainArg> addresses = GainAddresses.FindAll(a => a.RopeName == ropeName);

            GainArg GainXT1Arg = addresses.Find(f => f.Name.Contains("ValAB"));
            ushort GainXT1Add = GainXT1Arg.ArgAddress;

            GainArg GainXT2Arg = addresses.Find(f => f.Name.Contains("ValBA"));
            ushort GainXT2Add = GainXT2Arg.ArgAddress;

            float GainX1Value = 0;
            float GainX2Value = 0;

            if (GainXT2Arg.AddressFormat == AddressPointFormat.Floating)
            {
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, GainXT1Add, 2);
                GainX1Value = registers[0];

                registers = modbusMaster.ReadHoldingRegisters(slaveId, GainXT2Add, 2);
                GainX2Value = registers[0];
            }

            if (GainXT2Arg.AddressFormat == AddressPointFormat.Integer)
            {
                ushort[] registers = modbusMaster.ReadInputRegisters(slaveId, GainXT1Add, 1);
                GainX1Value = registers[0];

                registers = modbusMaster.ReadInputRegisters(slaveId, GainXT2Add, 1);
                GainX2Value = registers[0];
            }

            Console.WriteLine(string.Format("Cuerda {0} - Ganancia de la cuerda T1 ({1}): {2}", ropeName, GainXT1Add, GainX1Value));
            Console.WriteLine(string.Format("Cuerda {0} - Ganancia de la cuerda T2 ({1}): {2}", ropeName, GainXT2Add, GainX2Value));

            return new GainValue() { T1 = GainX1Value, T2 = GainX2Value };
        }

        public override double CalculateRopeEfficiency(string ropeName, IModbusSerialMaster modbusMaster, byte slaveId)
        {
            EfficiencyArg address = EfficiencyAddresses.Find(a => a.RopeName == ropeName);

            ushort PerformanceAdd = address.ArgAddress;
            float PerformanceValue = 0;

            if (address.AddressFormat == AddressPointFormat.Floating) 
            {
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, PerformanceAdd, 2);
                PerformanceValue = registers[0];
            }

            if (address.AddressFormat == AddressPointFormat.Integer) 
            {
                ushort[] registers = modbusMaster.ReadInputRegisters(slaveId, PerformanceAdd, 1);
                PerformanceValue = registers[0];
            }


            return PerformanceValue;

        }

    }
}
