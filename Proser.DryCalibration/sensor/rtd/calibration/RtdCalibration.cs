using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.rtd.calibration
{
    [Serializable]
    public class RtdCalibration
    {
        public int Number { get; set; }
        public List<ResPoint> ResPoints { get; set; }
        //public double R0 { get; set; }
        public int Active { get; set; }
    }

}
