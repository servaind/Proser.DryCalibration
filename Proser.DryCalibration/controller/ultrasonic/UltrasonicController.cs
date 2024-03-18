using Proser.DryCalibration.monitor;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.configuration;
using Proser.DryCalibration.sensor.ultrasonic.modbus.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.interfaces;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.measurers;
using Proser.DryCalibration.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.ultrasonic
{
    public class UltrasonicController : MonitorController 
    {

        private ModbusConfiguration configuration;

        public UltrasonicModel UltrasonicModel { get; private set; }

        public UltrasonicController()
        {
            logFile = "Proser.Ultrasonic.Monitor.log";

            string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");

            if (!File.Exists(path))
            {
                ModbusConfiguration.Generate(path);
            }

            configuration = ModbusConfiguration.Read(path);

            UltrasonicModel = (UltrasonicModel)configuration.SlaveConfig.Model; // modelo del sensor ultrasónico
        }

        public override void initMonitor()
        {

            IModBusMap measurer = getMeasurerModel(configuration);
            ModbusCommunication modbusCommunication = getModBusCommunication(configuration);
            ModbusFrameFormat modbusFrameFormat = getModBusFrameFormat(configuration);

            Monitor = new UltrasonicMonitor(measurer,
                                            modbusCommunication,
                                            modbusFrameFormat);

            Monitor.StatisticReceived += MonitorController_StatisticReceived;
            Monitor.UpdateSensorReceived += MonitorController_UpdateSensorReceived;

            Monitor.InitMonitor();
        }

        private ModbusFrameFormat getModBusFrameFormat(ModbusConfiguration config)
        {
            return config.SlaveConfig.ModbusFrameFormat == 0 ? ModbusFrameFormat.ASCII : ModbusFrameFormat.RTU;
        }

        private ModbusCommunication getModBusCommunication(ModbusConfiguration config)
        {
            return config.SlaveConfig.ModbusCommunication == 0 ? ModbusCommunication.Serial : ModbusCommunication.Tcp;
        }

        private IModBusMap getMeasurerModel(ModbusConfiguration config)
        {
            IModBusMap measurer = new DanielMeasurer(); //default
            UltrasonicModel model = (UltrasonicModel)config.SlaveConfig.Model;

            string file = model.ToString() + ".xml";
            string path = Path.Combine(Utils.ModbusMapPath, file);

            if (!File.Exists(path))
            {
                ModbusMapConfig.Generate(path, model);
            }

            ModbusMap map = ModbusMapConfig.Read(path);

            switch (model)
            {
                case UltrasonicModel.Daniel: // Daniel
                    measurer = new DanielMeasurer(map);
                    break;
                case UltrasonicModel.DanielJunior1R: // Daniel Junior 1 Cuerda
                    measurer = new DanielJunior1RMeasurer(map);
                    break;
                case UltrasonicModel.DanielJunior2R: // Daniel Junior 2 Cuerdas 
                    measurer = new DanielJunior2RMeasurer(map);
                    break;
                case UltrasonicModel.InstrometS5: // Instromet Series 3 al 5
                    measurer = new InstrometS5Measurer(map);
                    break;
                case UltrasonicModel.InstrometS6: //  Instromet Serie 6
                    measurer = new InstrometS6Measurer(map);
                    break;
                case UltrasonicModel.Sick: // Sick
                    measurer = new SickMeasurer(map);
                    break;
                case UltrasonicModel.FMU: // FMU
                    measurer = new FMUMeasurer(map);
                    break;
                case UltrasonicModel.KrohneAltosonicV12: // Krohne Altosonic V12
                    measurer = new KrohneAltV12Measurer(map);
                    break;
               
            }

            return measurer;
        }

              

    }
}
