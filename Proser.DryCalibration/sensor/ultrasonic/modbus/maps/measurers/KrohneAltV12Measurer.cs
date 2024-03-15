using Modbus.Device;
using Modbus.Utility;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.maps.measurers
{
    public class KrohneAltV12Measurer : ModbusMap
    {
        public KrohneAltV12Measurer()
        {
            Measurer = UltrasonicModel.KrohneAltosonicV12;
           
            MapMeasurer();
        }

        public KrohneAltV12Measurer(ModbusMap map) : base(map) { }

        protected override void MapMeasurer()
        {
            //mapear cuerdas
            string[] names = new string[] { "A", "B", "C", "D", "E", "F" };

            int name = 0;

            for (int add = 7031; add <= 7036; add++)
            {

                Rope rope = new Rope()
                {
                    Name = names[name],
                    FlowSpeedAddress = (ushort)add,  //velocidad de flujo
                    SoundSpeedAddress = (ushort)(add + 6),  //velocidad del sonido
                    AddressFormat = AddressPointFormat.Floating
                };

                Ropes.Add(rope);

                name++;
            }

            //mapear argumentos para el calculo
            MapRopeEfficiency();

            MapRopeGain();
        }

        protected override void MapRopeGain()
        {
            //mapear argumentos para el calculo
            string[,] names = new string[,] {{"Ch1_GainAB", "A"}, {"Ch2_GainAB", "B"}, {"Ch3_GainAB", "C"}, {"Ch4_GainAB", "D"},{"Ch5_GainAB", "E"}, {"Ch6_GainAB", "F"},
                                             {"Ch1_GainBA", "A"}, {"Ch2_GainBA", "B"}, {"Ch3_GainBA", "C"}, {"Ch4_GainBA", "D"},{"Ch5_GainBA", "E"}, {"Ch6_GainBA", "F"}};
            int name = 0;

            for (int add = 7001; add <= 7012; add++)
            {
                GainArg arg = new GainArg()
                {
                    Name = names[name, 0],
                    RopeName = names[name, 1],
                    ArgAddress = (ushort)add,
                    AddressFormat = AddressPointFormat.Floating
                };

                GainAddresses.Add(arg);

                name++;
            }

        }

        public override GainValue CalculateRopeGain(string ropeName, IModbusSerialMaster modbusMaster, byte slaveId)
        {
            List<GainArg> addresses = GainAddresses.FindAll(a => a.RopeName == ropeName);

            ushort GainXT1Add = addresses.Find(f => f.Name.Contains("GainAB")).ArgAddress;
            ushort GainXT2Add = addresses.Find(f => f.Name.Contains("GainBA")).ArgAddress;

            ushort numberOfPoints = 7;

            ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, GainXT1Add, numberOfPoints);
            
            float GainX1Value = ModbusUtility.GetSingle(registers[0], registers[1]);
            float GainX2Value = ModbusUtility.GetSingle(registers[12], registers[13]);

            System.Console.WriteLine(string.Format("Cuerda {0} - Ganancia de la cuerda T1 ({1}): {2}", ropeName, GainXT1Add, GainX1Value));
            System.Console.WriteLine(string.Format("Cuerda {0} - Ganancia de la cuerda T2 ({1}): {2}", ropeName, GainXT2Add, GainX2Value));

            return new GainValue() { T1 = GainX1Value, T2 = GainX2Value };
        }

        protected override void MapRopeEfficiency()
        {
            string[,] names = new string[,] { { "PerformancePath1", "A" }, { "PerformancePath2", "B" },
                                              { "PerformancePath3", "C" }, { "PerformancePath4", "D" },
                                              { "PerformancePath5", "E" }, { "PerformancePath6", "F" } };

            int name = 0;

            for (int add = 7043; add <= 7048; add++)
            {
                EfficiencyArg arg = new EfficiencyArg()
                {
                    Name = names[name, 0],
                    RopeName = names[name, 1],
                    ArgAddress = (ushort)add,
                    AddressFormat = AddressPointFormat.Floating
                };

                EfficiencyAddresses.Add(arg);

                name++;
            }
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
