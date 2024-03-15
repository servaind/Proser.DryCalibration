using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.measurers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.configuration
{
    public class ModbusMapConfig
    {
        public static string Generate(string fullPath, UltrasonicModel model)
        {
            try
            {
                ModbusMap map = null;
              
                switch (model)
                {
                    case UltrasonicModel.Daniel:
                        map = new DanielMeasurer();
                        break;
                    case UltrasonicModel.DanielJunior1R:
                        map = new DanielJunior1RMeasurer();
                        break;
                    case UltrasonicModel.DanielJunior2R:
                        map = new DanielJunior2RMeasurer();
                        break;
                    case UltrasonicModel.InstrometS5:
                        map = new InstrometS5Measurer();                     
                        break;
                    case UltrasonicModel.InstrometS6:
                        map = new InstrometS6Measurer();
                        break;
                    case UltrasonicModel.Sick:
                        map = new SickMeasurer();
                        break;
                    case UltrasonicModel.FMU:
                        map = new FMUMeasurer();
                        break;
                    case UltrasonicModel.KrohneAltosonicV12:
                        map = new KrohneAltV12Measurer();
                        break;
                    default:
                        map = new ModbusMap();
                        break;
                }

                ModbusMap m = new ModbusMap()
                {
                    Measurer = map.Measurer,
                    Ropes = map.Ropes,
                    EfficiencyAddresses = map.EfficiencyAddresses,
                    GainAddresses = map.GainAddresses
                };

                util.xml.Serializer s = new util.xml.Serializer();
                s.Serialize<ModbusMap>(m, fullPath);

                return fullPath;
            }
            catch(Exception e)
            {
                return "";
            }
        }
      
        public static ModbusMap Read(string fullPath)
        {
            try
            {
                util.xml.Serializer s = new util.xml.Serializer();
                return s.Deserialize<ModbusMap>(fullPath);
            }
            catch
            {
                return null;
            }
        }

    }
}
