using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.pressure.calibration
{
    [Serializable]
    public class PressureCalibration
    {
        public string DaqModuleIPAddress { get; set; }
        public decimal Error { get; set; }
        public int Zero { get; set; }
        public int Span { get; set; }
        public int SensorType { get; set; }
        public decimal AtmosphericPresssure { get; set; }

        public static string Generate(string fullPath, string daqModuleIPAddress)
        {
            try
            {
                PressureCalibration pressureCalibration = new PressureCalibration()
                {
                    DaqModuleIPAddress = daqModuleIPAddress,
                    Error = 0,
                    Zero = 0,
                    Span = 20,
                    SensorType = 0, // absoluto
                    AtmosphericPresssure = 0 // presión atmosférica 
                };

                util.xml.Serializer s = new util.xml.Serializer();
                s.Serialize<PressureCalibration>(pressureCalibration, fullPath);

                return fullPath;
            }
            catch
            {
                return "";
            }  

        }


        public static string Generate(string fullPath, PressureCalibration pressureCalibration)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                util.xml.Serializer s = new util.xml.Serializer();
                s.Serialize<PressureCalibration>(pressureCalibration, fullPath);

                return fullPath;
            }
            catch
            {
                return "";
            }
        }


        public static PressureCalibration Read(string fullPath)
        {
            try
            {
                util.xml.Serializer s = new util.xml.Serializer();
                return s.Deserialize<PressureCalibration>(fullPath);
            }
            catch
            {
                return null;
            }
        }

    }
}
