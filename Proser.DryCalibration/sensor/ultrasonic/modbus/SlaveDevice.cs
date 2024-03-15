using Modbus.Data;
using Modbus.Device;
using Proser.DryCalibration.sensor.ultrasonic.modbus.configuration;
using Proser.DryCalibration.sensor.ultrasonic.modbus.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.interfaces;
using Proser.DryCalibration.sensor.ultrasonic.modbus.IO;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus
{
    public class SlaveDevice : ModbusDevice, IDisposable
    {
        private ModbusSerialSlave slave;

        public byte UnitId { get; set; }

        public SlaveDevice( byte uniiId ) : base()
        {
            this.UnitId = uniiId; // slave unit address

            //portName = configuration.SlaveSerial.PortName;
            //baudrate = configuration.SlaveSerial.BaudRate;
            //databits = configuration.SlaveSerial.DataBits;
            //parity = GetSerialParity(configuration.SlaveSerial.Parity);
            //stopbits = GeSerialStopBits(configuration.SlaveSerial.StopBits);
        }

        protected override void StartModbusSerialAscii()
        {

            using (SerialPort slavePort = new SerialPort(portName))
            {
                // configure serial port
                slavePort.BaudRate = baudrate;
                slavePort.DataBits = databits;
                slavePort.Parity = parity;
                slavePort.StopBits = stopbits;

                slavePort.Open();

                var adapter = new SerialPortAdapter(slavePort);
              
                // create modbus slave
                slave = ModbusSerialSlave.CreateAscii(UnitId, slavePort);

                slave.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;

                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();

                slave.Listen();
            }
        }

        private void Slave_ModbusSlaveRequestReceived(object sender, ModbusSlaveRequestEventArgs e)
        {
            
        }

        protected override void StartModbusSerialRtu()
        {
            throw new NotImplementedException();
        }

        protected override void StartModbusTcpAscii()
        {
            throw new NotImplementedException();
        }

        protected override void StartModbusTcpRtu()
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            if(slave != null)
            {
                slave.Dispose();
            }
        }
    }
}
