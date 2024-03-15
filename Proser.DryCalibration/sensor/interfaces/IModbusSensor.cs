using Modbus.Device;
using Proser.DryCalibration.sensor.ultrasonic.modbus;
using Proser.DryCalibration.sensor.ultrasonic.modbus.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.interfaces
{
    public interface IModbusSensor
    {
        int Number { get; }
        byte SlaveId { get; }
        IModbusSerialMaster Master { get;}

        ModbusCommunication CommunicationModbus { get; set; }
        ModbusFrameFormat FrameFormat { get; set; }

        bool Init();
        void Connect();
        void Dispose();

    }
}
