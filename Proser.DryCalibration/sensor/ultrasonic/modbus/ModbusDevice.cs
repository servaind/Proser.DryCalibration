using Proser.DryCalibration.sensor.ultrasonic.modbus.configuration;
using Proser.DryCalibration.sensor.ultrasonic.modbus.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.interfaces;
using Proser.DryCalibration.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus
{
    public class ModbusDevice: IModbusDevice
    {
        protected ModbusConfiguration configuration;

        protected string portName;
        protected int baudrate;
        protected int databits;
        protected Parity parity;
        protected StopBits stopbits;

        public  ModbusCommunication CommunicationModbus { get; set; }
        public  ModbusFrameFormat FrameFormat { get; set; }

        public ModbusDevice()
        {
            getModbusConfiguration();
        }

        public void Connect()
        {
            switch (CommunicationModbus)
            {
                case ModbusCommunication.Serial:
                    switch (FrameFormat)
                    {
                        case ModbusFrameFormat.ASCII:
                            StartModbusSerialAscii();
                            break;
                        case ModbusFrameFormat.RTU:
                            StartModbusSerialRtu();
                            break;
                        default:
                            break;
                    }

                    break;
                case ModbusCommunication.Tcp:

                    switch (FrameFormat)
                    {
                        case ModbusFrameFormat.ASCII:
                            StartModbusTcpAscii();
                            break;
                        case ModbusFrameFormat.RTU:
                            StartModbusTcpRtu();
                            break;
                        default:
                            break;
                    }

                    break;
                default:
                    break;
            }
        }

       
        protected virtual void StartModbusSerialAscii() { }
       
        protected virtual void StartModbusSerialRtu() { } 
 
        protected virtual void StartModbusTcpAscii() { }
       
        protected virtual void StartModbusTcpRtu() { }


        private void getModbusConfiguration()
        {
            try
            {
                string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");

                if (!System.IO.File.Exists(path))
                {
                    ModbusConfiguration.Generate(path);
                }

                configuration = ModbusConfiguration.Read(path);

            }
            catch (Exception e)
            {
                //Log 
                
                throw new Exception("MODBUS: Ocurrió un error al intentar obtener la configuración del protocolo.");
            }

        }

        protected int GetSerialBaudRate(int baudRate)
        {
            switch (baudRate)
            {
                case 0:
                    return 4800;
                    break;
                case 1:
                    return 9600;
                    break;
                case 2:
                    return 14400;
                    break;
                case 3:
                    return 19200;
                    break;
                case 4:
                    return 38400;
                    break;
                case 5:
                    return 57600;
                    break;
                case 6:
                    return 115200;
                    break;
                default:
                    return 9600; //default
                    break;
            }
        }

        protected int GetSerialDataBits(int dataBits)
        {
            switch (dataBits)
            {
                case 0:
                    return 4;
                    break;
                case 1:
                    return 5;
                    break;
                case 2:
                    return 6;
                    break;
                case 3:
                    return 7;
                    break;
                case 4:
                    return 8;
                    break;
                default:
                    return 8; //default
                    break;
            }
        }

        protected Parity GetSerialParity(int parity)
        {
            switch (parity)
            {
                case 0:
                    return Parity.Even;
                    break;
                case 1:
                    return Parity.Odd;
                    break;
                case 2:
                    return Parity.None;
                    break;
                default:
                    return Parity.None; //default
                    break;
            }
        }

        protected StopBits GeSerialStopBits(int stopBits)
        {
            switch (stopBits)
            {
                case 0:
                    return StopBits.One;
                    break;
                case 1:
                    return StopBits.OnePointFive;
                    break;
                case 2:
                    return StopBits.Two;
                    break;
                default:
                    return StopBits.One; //default
                    break;
            }
        }

    
    }
}
