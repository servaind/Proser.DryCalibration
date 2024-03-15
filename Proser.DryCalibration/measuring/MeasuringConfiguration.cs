using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.measuring
{
    [Serializable]
    public class MeasuringConfiguration
    {
        public List<MeasuringInstrument> MeasuringInstruments { get; set; }

        public MeasuringConfiguration()
        {
            MeasuringInstruments = new List<MeasuringInstrument>(); 
        }

        public static string Generate(string fullPath)
        {
            try
            {
                MeasuringConfiguration measuringConfiguration = new MeasuringConfiguration();
              
                util.xml.Serializer s = new util.xml.Serializer();
                s.Serialize<MeasuringConfiguration>(measuringConfiguration, fullPath);
           
                return fullPath;
            }
            catch
            {
                return "";
            }

        }

        public static string Generate(string fullPath, MeasuringConfiguration measuringConfiguration)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                util.xml.Serializer s = new util.xml.Serializer();
                s.Serialize<MeasuringConfiguration>(measuringConfiguration, fullPath);

                return fullPath;
            }
            catch
            {
                return "";
            }
        }

        public static MeasuringConfiguration Read(string fullPath)
        {
            try
            {
                util.xml.Serializer s = new util.xml.Serializer();
                return s.Deserialize<MeasuringConfiguration>(fullPath);
            }
            catch
            {
                return null;
            }
        }


    }
}
