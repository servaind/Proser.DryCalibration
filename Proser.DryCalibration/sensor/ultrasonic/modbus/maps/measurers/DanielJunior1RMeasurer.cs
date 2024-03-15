﻿using Modbus.Device;
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
    public class DanielJunior1RMeasurer : ModbusMap
    {
        public DanielJunior1RMeasurer()
        {
            Measurer = UltrasonicModel.DanielJunior1R;

            MapMeasurer();
        }

        public DanielJunior1RMeasurer(ModbusMap map) : base(map) { }

        protected override void MapMeasurer()
        {
            //mapear cuerdas

            int add = 352;

            Rope rope = new Rope()
            {
                Name = "A",
                FlowSpeedAddress = (ushort)add,  //velocidad de flujo
                SoundSpeedAddress = (ushort)(add + 10),  //velocidad del sonido
                AddressFormat = AddressPointFormat.Floating
            };

            Ropes.Add(rope);

            //mapear argumentos para el calculo
            MapRopeEfficiency();

            MapRopeGain();

        }

        protected override void MapRopeEfficiency()
        {
            //mapear argumentos para el calculo
            string[,] names = new string[,] {{"PctGoodA1", "A"},{"PctGoodA2", "A"}};

            int[] adds = new int[] { 67, 71 };

            int name = 0;

            for (int i = 0; i < adds.Length; i++)
            {
                EfficiencyArg arg = new EfficiencyArg()
                {
                    Name = names[name, 0],
                    RopeName = names[name, 1],
                    ArgAddress = (ushort)adds[i],
                    AddressFormat = AddressPointFormat.Floating
                };

                EfficiencyAddresses.Add(arg);

                name++;
            }
        }

        protected override void MapRopeGain()
        {
            //mapear argumentos para el calculo
            string[,] names = new string[,] {{"GainA1", "A"}, {"GainA2", "A"}};
            int name = 0;

            for (int add = 77; add <= 78; add++)
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

            GainArg GainXT1Arg = addresses.Find(f => f.Name.Contains("1"));
            ushort GainXT1Add = GainXT1Arg.ArgAddress;


            GainArg GainXT2Arg = addresses.Find(f => f.Name.Contains("2"));
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

            return ApplyGainEquation(GainX1Value, GainX2Value);

        }

        protected override GainValue ApplyGainEquation(params double[] args)
        {
            double gainX1Value = args[0];
            double gainX2Value = args[1];

            GainValue gain = new GainValue()
            {
                T1 = Math.Log10(gainX1Value) * 20d,
                T2 = Math.Log10(gainX2Value) * 20d
            };

            return gain;
        }


        public override double CalculateRopeEfficiency(string ropeName, IModbusSerialMaster modbusMaster, byte slaveId)
        {

            List<EfficiencyArg> addresses = EfficiencyAddresses.FindAll(a => a.RopeName == ropeName);

            EfficiencyArg PctGoodX1Arg = addresses.Find(f => f.Name.Contains("1"));
            ushort PctGoodX1Add = PctGoodX1Arg.ArgAddress;

            EfficiencyArg PctGoodX2Arg = addresses.Find(f => f.Name.Contains("2"));
            ushort PctGoodX2Add = PctGoodX2Arg.ArgAddress;

            float PctGoodX1Value = 0;
            float PctGoodX2Value = 0;

            if (PctGoodX1Arg.AddressFormat == AddressPointFormat.Floating)
            {
                ushort[] registers = modbusMaster.ReadHoldingRegisters(slaveId, PctGoodX1Add, 2);
                PctGoodX1Value = registers[0];

                registers = modbusMaster.ReadHoldingRegisters(slaveId, PctGoodX2Add, 2);
                PctGoodX2Value = registers[0];
            }

            if (PctGoodX2Arg.AddressFormat == AddressPointFormat.Integer)
            {
                ushort[] registers = modbusMaster.ReadInputRegisters(slaveId, PctGoodX1Add, 1);
                PctGoodX1Value = registers[0];

                registers = modbusMaster.ReadInputRegisters(slaveId, PctGoodX2Add, 1);
                PctGoodX2Value = registers[0];

            }

            return ApplyEfficiencyEquation(PctGoodX1Value, PctGoodX2Value);
        }


        protected override double ApplyEfficiencyEquation(params double[] args)
        {
            double PctGoodX1 = args[0];
            double PctGoodX2 = args[1];

            double result = ((PctGoodX1 + PctGoodX2) / 2d);

            return result;
        }




    }




}