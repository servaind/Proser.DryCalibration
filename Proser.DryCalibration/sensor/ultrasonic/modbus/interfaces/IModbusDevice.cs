using Proser.DryCalibration.sensor.ultrasonic.modbus.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.interfaces
{
    public interface IModbusDevice
    {
        ModbusCommunication CommunicationModbus { get; set; }
        ModbusFrameFormat FrameFormat { get; set; } //LUNES: agregar acá slave id para que lo tome de la configuracion!!!
        void Connect();
    }
}
