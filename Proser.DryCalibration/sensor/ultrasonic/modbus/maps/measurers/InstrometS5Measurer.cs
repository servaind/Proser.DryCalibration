using Modbus.Device;
using Modbus.Utility;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.maps.measurers
{
    public class InstrometS5Measurer : ModbusMap
    {
        public InstrometS5Measurer()
        {
            Measurer = UltrasonicModel.InstrometS5;
           
            MapMeasurer();
        }

        public InstrometS5Measurer(ModbusMap map) : base(map) { }
      
        protected override void MapMeasurer()
        {
            //mapear cuerdas
            string[] names = new string[] { "A", "B", "C", "D", "E" };

            int name = 0;

            for (int add = 411; add <= 415; add++)
            {

                Rope rope = new Rope()
                {
                    Name = names[name],
                    FlowSpeedAddress = (ushort) add,  //velocidad de flujo
                    SoundSpeedAddress = (ushort) (add - 5),  //velocidad del sonido
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
            string[,] names = new string[,] {{"AgcLevel:Trd1A", "A"}, {"AgcLevel:Trd1B", "A"}, {"AgcLevel:Trd2A", "B"}, {"AgcLevel:Trd2B", "B"}, {"AgcLevel:Trd3A", "C"}, {"AgcLevel:Trd3B", "C"},
                                             {"AgcLevel:Trd4A", "D"}, {"AgcLevel:Trd4B", "D"}, {"AgcLevel:Trd5A", "E"}, {"AgcLevel:Trd5B", "E"}};
            int name = 0;

            for (int add = 10; add <= 19; add++)
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

            GainArg GainXT1Arg = addresses.Find(f => f.Name.EndsWith("A"));
            ushort GainXT1Add = GainXT1Arg.ArgAddress;

            GainArg GainXT2Arg = addresses.Find(f => f.Name.EndsWith("B"));
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

        protected override void MapRopeEfficiency()
        {
            //mapear argumentos para el calculo
            string[,] names = new string[,] {{"ValidSamplesL1", "A"}, {"ValidSamplesL2", "B"}, {"ValidSamplesL3", "C"},
                                             {"ValidSamplesL4", "D"}, {"ValidSamplesL5", "E"}};
            int name = 0;

            for (int add = 5; add <= 9; add++)
            {
                EfficiencyArg arg = new EfficiencyArg()
                {
                    Name = names[name, 0],
                    RopeName = names[name, 1],
                    ArgAddress = (ushort)add
                };

                EfficiencyAddresses.Add(arg);

                name++;
            }
        }


        public override double CalculateRopeEfficiency(string ropeName, IModbusSerialMaster modbusMaster, byte slaveId)
        {
            EfficiencyArg validSamplesArg = EfficiencyAddresses.Find(a => a.RopeName == ropeName);

            ushort validSamplesAdd = validSamplesArg.ArgAddress;
            ushort sampleRateAdd = 4; // común a todas las cuerdas

            float validSamplesValue = 0;
            float sampleRateValue = 0;

            if (validSamplesArg.AddressFormat == AddressPointFormat.Floating) 
            {
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, validSamplesAdd, 2);
                validSamplesValue = registers[0];

                registers = modbusMaster.ReadHoldingRegisters(slaveId, sampleRateAdd, 2);
                sampleRateValue = registers[0];

            }

            if (validSamplesArg.AddressFormat == AddressPointFormat.Integer) 
            {
                ushort[] registers = modbusMaster.ReadInputRegisters(slaveId, validSamplesAdd, 1);
                validSamplesValue = registers[0];

                registers = modbusMaster.ReadInputRegisters(slaveId, sampleRateAdd, 1);
                sampleRateValue = registers[0];
            }

            return ApplyEfficiencyEquation(validSamplesValue, sampleRateValue);
        }


        protected override double ApplyEfficiencyEquation(params double[] args)
        {
            double validSamples = args[0];
            double sampleRate = args[1];

            double result = validSamples / sampleRate * 100;

            return result;
        }
    }
}
