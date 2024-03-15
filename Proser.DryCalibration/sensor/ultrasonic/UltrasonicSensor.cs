using Modbus.Device;
using Proser.DryCalibration.sensor.ultrasonic.interfaces;
using Proser.DryCalibration.sensor.ultrasonic.modbus;
using Proser.DryCalibration.sensor.ultrasonic.modbus.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.interfaces;
using System;
using System.Collections.Generic;

namespace Proser.DryCalibration.sensor.ultrasonic
{
    public class UltrasonicSensor : IUltrasonicSensor
    {
        private IModBusMap measurer;

        public int Number { get; private set; }
        public byte SlaveId { get; private set; }

        public IModbusSerialMaster Master { get { return masterDevice.Master; } }

        public List<RopeValue> Ropes { get; set; }

        public ModbusCommunication CommunicationModbus { get; set; }
        public ModbusFrameFormat FrameFormat { get; set; }

        private MasterDevice masterDevice;
        private SlaveDevice slaveDevice;


        public UltrasonicSensor( IModBusMap measurer,
                                 ModbusCommunication communicationMosbus,
                                 ModbusFrameFormat frameFormat )
        {
            this.measurer = measurer;
            this.CommunicationModbus = communicationMosbus;
            this.FrameFormat = frameFormat;
        }

      
        public bool Init()
        {
            this.Number = 0;

            mapMeasurerValues();

            Connect();

            this.SlaveId = masterDevice.SlaveId;

            return true;
        }


        public void Connect()
        {
            try
            {
                if (masterDevice != null)
                {
                    masterDevice.Dispose();
                }

                //master
                masterDevice = new MasterDevice()
                {
                    CommunicationModbus = this.CommunicationModbus,
                    FrameFormat = this.FrameFormat
                };

                masterDevice.Connect();
            }
            catch (Exception e)
            {
                //Log
                throw;               
            }
        }

      
        private void mapMeasurerValues()
        {
            Ropes = new List<RopeValue>();

            foreach (Rope rope in measurer.Ropes)
            {
                RopeValue value = new RopeValue()
                {
                    Name = rope.Name,
                    FlowSpeedValue = 0,
                    SoundSpeedValue = 0,
                    EfficiencyValue = 0,
                    GainValues = new GainValue()
                };

                Ropes.Add(value);
            }
        }

        public void Dispose()
        {
            try
            {
                masterDevice.Master.Transport.Dispose();

                if (masterDevice.MasterPort.IsOpen) 
                {
                    masterDevice.MasterPort.Close();
                }

                masterDevice.Dispose();
            }
            catch { }
        }
    }
}
