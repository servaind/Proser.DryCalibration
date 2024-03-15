using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.rtd.calibration
{
    [Serializable]
    public class RtdTable
    {
        public string DaqModuleIPAddress { get; set; }
        public List<TempPoint> TempPoints { get; set; }
        public List<RtdCalibration> RtdSensors { get; set; }

        public static string Generate(string fullPath, string daqModuleIPAddress)
        {

            try
            {
                RtdTable table = new RtdTable();
                table.DaqModuleIPAddress = daqModuleIPAddress;

                List<TempPoint> tempPoints = new List<TempPoint>();

                for (int i = 1; i <= 5; i++)
                {
                    TempPoint point = new TempPoint()
                    {
                        Number = i,
                        Value = 0
                    };

                    tempPoints.Add(point);
                }

                table.TempPoints = tempPoints;

                table.RtdSensors = new List<RtdCalibration>();

                List<ResPoint> resPoints = new List<ResPoint>();

                for (int i = 1; i <= 5; i++)
                {
                    ResPoint point = new ResPoint()
                    {
                        Number = i,
                        Value = 0
                    };

                    resPoints.Add(point);
                }


                for (int i = 0; i < 12; i++)
                {
                    RtdCalibration sensor = new RtdCalibration()
                    {
                        Number = i,
                        ResPoints = resPoints,
                        //R0 = 100, // default
                        Active = 1
                    };

                    table.RtdSensors.Add(sensor);
                }

                util.xml.Serializer s = new util.xml.Serializer();
                s.Serialize<RtdTable>(table, fullPath);

                return fullPath;
            }
            catch
            {
                return "";
            }

        }


        public static string Generate(string fullPath, RtdTable rtdTable)
        {
            try
            {
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                util.xml.Serializer s = new util.xml.Serializer();
                s.Serialize<RtdTable>(rtdTable, fullPath);

                return fullPath;
            }
            catch
            {
                return "";
            }
        }

        public static RtdTable Read(string fullPath)
        {
            try
            {
                util.xml.Serializer s = new util.xml.Serializer();
                return s.Deserialize<RtdTable>(fullPath);
            }
            catch
            {
                return null;
            }  
        }

    }
}
