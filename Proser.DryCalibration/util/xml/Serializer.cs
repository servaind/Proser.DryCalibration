using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Proser.DryCalibration.util.xml
{
    public class Serializer
    {
        public bool Serialize<T>(T toSerialize, string fullPath)
        {
            try
            {
                XmlSerializer writer = new XmlSerializer(typeof(T));

                using (FileStream file = File.OpenWrite(fullPath))
                {
                    writer.Serialize(file, toSerialize);
                }

                return true;
            }
            catch
            {
                return false;
            }
            
        }


        public T Deserialize<T>(string fullPath)
        {

            try
            {
                XmlSerializer reader = new XmlSerializer(typeof(T));
                using (FileStream input = File.OpenRead(fullPath))
                {
                    return ((T)reader.Deserialize(input));
                }
            }
            catch
            {
                return default(T);
            }
        }

        internal void Serialize<T>(object rtdTable, string fullPath)
        {
            throw new NotImplementedException();
        }
    }
}
