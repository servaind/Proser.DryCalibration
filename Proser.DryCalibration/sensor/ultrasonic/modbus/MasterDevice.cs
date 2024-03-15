using Modbus.Device;
using Modbus.IO;
using Proser.DryCalibration.sensor.ultrasonic.modbus.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.interfaces;
using Proser.DryCalibration.sensor.ultrasonic.modbus.IO;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.interfaces;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus
{
    public class MasterDevice : ModbusDevice, Modbus.Device.IModbusMaster, IDisposable
    {
        private static SerialPortAdapter adapter;


        public IModbusSerialMaster Master { get; private set; }
        public byte SlaveId { get; private set; }
        public SerialPort MasterPort { get; private set; }

        public MasterDevice() : base()
        {
            //slave configuration
            SlaveId = configuration.SlaveConfig.SlaveId;

            //serial configuration
            portName = configuration.MasterSerial.PortName;
            baudrate = GetSerialBaudRate(configuration.MasterSerial.BaudRate);
            databits = GetSerialDataBits(configuration.MasterSerial.DataBits);
            parity = GetSerialParity(configuration.MasterSerial.Parity);
            stopbits = GeSerialStopBits(configuration.MasterSerial.StopBits);
        }


        protected override void StartModbusSerialAscii()
        {
            if (MasterPort != null)
            {
                MasterPort.Close();
            }

            MasterPort = new SerialPort(portName);

        
            // configure serial port
            MasterPort.BaudRate = baudrate;
            MasterPort.DataBits = databits;
            MasterPort.Parity = parity;
            MasterPort.StopBits = stopbits;

            MasterPort.Open();

            adapter = new SerialPortAdapter(MasterPort);

            adapter.ReadTimeout = 3000; // default

            if (configuration.SlaveConfig.SampleTimeOut > 0)
            {
                adapter.ReadTimeout = configuration.SlaveConfig.SampleTimeOut;
            }

            // create modbus master
            Master = ModbusSerialMaster.CreateAscii(adapter);
        }

        protected override void StartModbusSerialRtu()
        {

            if (MasterPort != null)
            {
                MasterPort.Close();
            }

            MasterPort = new SerialPort(portName);

            // configure serial port
            MasterPort.BaudRate = baudrate;
            MasterPort.DataBits = databits;
            MasterPort.Parity = parity;
            MasterPort.StopBits = stopbits;

            MasterPort.Open();

            adapter = new SerialPortAdapter(MasterPort);

            adapter.ReadTimeout = 3000; // default

            if (configuration.SlaveConfig.SampleTimeOut > 0)
            {
                adapter.ReadTimeout = configuration.SlaveConfig.SampleTimeOut;
            }

            // create modbus master
            Master = ModbusSerialMaster.CreateRtu(adapter);

            if (configuration.SlaveConfig.SampleTimeOut > 0)
            {
                Master.Transport.ReadTimeout = configuration.SlaveConfig.SampleTimeOut;
            }
        }

        protected override void StartModbusTcpAscii()
        {
            throw new NotImplementedException();
        }

        protected override void StartModbusTcpRtu()
        {
            throw new NotImplementedException();
        }

       
        #region IModbusMaster (MIT)

        public int LastRequestAttempts => throw new NotImplementedException();

        public ModbusTransport Transport => throw new NotImplementedException();

        public void Dispose()
        {
            if (MasterPort != null)
            {
                adapter.Dispose();

                if (MasterPort.IsOpen) 
                {
                    MasterPort.Close();
                }

                MasterPort.Dispose();
            }
        }

        public bool[] ReadCoils(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public ushort[] ReadHoldingRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            ushort[] registers = Master.ReadHoldingRegisters(slaveAddress, startAddress, numberOfPoints);

            return registers;
        }

        public ushort[] ReadInputRegisters(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public bool[] ReadInputs(byte slaveAddress, ushort startAddress, ushort numberOfPoints)
        {
            throw new NotImplementedException();
        }

        public ushort[] ReadWriteMultipleRegisters(byte slaveAddress, ushort startReadAddress, ushort numberOfPointsToRead, ushort startWriteAddress, ushort[] writeData)
        {
            throw new NotImplementedException();
        }

        public void WriteMultipleCoils(byte slaveAddress, ushort startAddress, bool[] data)
        {
            throw new NotImplementedException();
        }

        public void WriteMultipleRegisters(byte slaveAddress, ushort startAddress, ushort[] data)
        {
            throw new NotImplementedException();
        }

        public void WriteSingleCoil(byte slaveAddress, ushort coilAddress, bool value)
        {
            throw new NotImplementedException();
        }

        public void WriteSingleRegister(byte slaveAddress, ushort registerAddress, ushort value)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}
