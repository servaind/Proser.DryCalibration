using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.configuration
{

    [Serializable]
    public class ModbusConfiguration
    {
        
        public SerialConfig MasterSerial { get; set; }
        public SlaveConfig SlaveConfig { get; set; }
        public TcpConfig Tcp { get; set; }
        public List<GainConfig> UltGainConfig { get; set; }
        public int UltrasonicSampleMode { get; set; }


        public ModbusConfiguration()
        {
            MasterSerial = new SerialConfig();
            SlaveConfig = new SlaveConfig();

            Tcp = new TcpConfig();
        }


        public static string Generate(string fullPath)
        {
            try
            {
                ModbusConfiguration modbusConfiguration = new ModbusConfiguration()
                {
                    MasterSerial = new SerialConfig(),
                    SlaveConfig = new SlaveConfig(),
                    Tcp = new TcpConfig()
                };

                modbusConfiguration.PopulateUltGain();

                util.xml.Serializer s = new util.xml.Serializer();
                s.Serialize<ModbusConfiguration>(modbusConfiguration, fullPath);

                return fullPath;
            }
            catch
            {
                return "";
            }

        }


        public static string Generate(string fullPath, ModbusConfiguration modbusConfiguration)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                util.xml.Serializer s = new util.xml.Serializer();
                s.Serialize<ModbusConfiguration>(modbusConfiguration, fullPath);

                return fullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "";
            }
        }

        public static ModbusConfiguration Read(string fullPath)
        {
            try
            {
                util.xml.Serializer s = new util.xml.Serializer();
                return s.Deserialize<ModbusConfiguration>(fullPath);
            }
            catch
            {
                return null;
            }
        }


        public void PopulateUltGain()
        {
            UltGainConfig = new List<GainConfig>();


            foreach (UltrasonicModel model in (UltrasonicModel[])Enum.GetValues(typeof(UltrasonicModel)))
            {
                UltGainConfig.Add(new GainConfig()
                {
                    UltModel = model,
                });
                
            }
        }


    }
}
