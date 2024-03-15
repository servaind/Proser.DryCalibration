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
    public class SickMeasurer : ModbusMap
    {
        public SickMeasurer()
        {
            Measurer = UltrasonicModel.Sick;
           
            MapMeasurer();
        }

        public SickMeasurer(ModbusMap map) : base(map) { }
      
        protected override void MapMeasurer()
        {
            //mapear cuerdas
            string[] names = new string[] { "A", "B", "C", "D" };

            int name = 0;

            for (int add = 7009; add <= 7012; add++)
            {

                Rope rope = new Rope()
                {
                    Name = names[name],
                    FlowSpeedAddress = (ushort) add,  //velocidad de flujo
                    SoundSpeedAddress = (ushort) (add - 4),  //velocidad del sonido
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
            string[,] names = new string[,] {{"AGCpath1AB", "A"}, {"AGCpath1BA", "A"}, {"AGCpath2AB", "B"}, {"AGCpath2BA", "B"},
                                             {"AGCpath3AB", "C"}, {"AGCpath3BA", "C"}, {"AGCpath4AB", "D"}, {"AGCpath4BA", "D"}};
            int name = 0;

            for (int add = 3012; add <= 3019; add++)
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

            GainArg GainXT1Arg = addresses.Find(f => f.Name.Contains("AB"));
            ushort GainXT1Add = GainXT1Arg.ArgAddress;

            GainArg GainXT2Arg = addresses.Find(f => f.Name.Contains("BA"));
            ushort GainXT2Add = GainXT1Arg.ArgAddress;

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
            string[,] names = new string[,] {{ "InvalidSamplesPath1", "A"} , { "InvalidSamplesPath2", "B"}, { "InvalidSamplesPath3", "C"},{ "InvalidSamplesPath4", "D"}};
            int name = 0;

            for (int add = 3008; add <= 3011; add++)
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
            EfficiencyArg invalidSamplesPathArg = EfficiencyAddresses.Find(a => a.RopeName == ropeName);
            ushort invalidSamplesPathAdd = invalidSamplesPathArg.ArgAddress;
          
            ushort currentFrequencyAdd = 3029; // común a todas las cuerdas

            float invalidSamplesPathValue = 0;
            float currentFrequencyValue = 0;

            if (invalidSamplesPathArg.AddressFormat == AddressPointFormat.Floating) 
            {
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, invalidSamplesPathAdd, 2);
                invalidSamplesPathValue = registers[0];

                registers = modbusMaster.ReadHoldingRegisters(slaveId, currentFrequencyAdd, 2);
                currentFrequencyValue = registers[0];
            }

            if (invalidSamplesPathArg.AddressFormat == AddressPointFormat.Integer) 
            {
                ushort[] registers = modbusMaster.ReadInputRegisters(slaveId, invalidSamplesPathAdd, 1);
                invalidSamplesPathValue = registers[0];

                registers = modbusMaster.ReadInputRegisters(slaveId, currentFrequencyAdd, 1);
                currentFrequencyValue = registers[0];
            }

            return ApplyEfficiencyEquation(invalidSamplesPathValue, currentFrequencyValue);
        }


        protected override double ApplyEfficiencyEquation(params double[] args)
        {
            double invalidSamplesPath = args[0];
            double currentFrequency = args[1];

            double result = (((currentFrequency - invalidSamplesPath) / (currentFrequency))) * 100; //revisar

            return result;
        }

    }
}
