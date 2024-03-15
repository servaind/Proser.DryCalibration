using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.rtd.calibration
{
    [Serializable]
    public class ResPoint
    {
        public int Number { get; set; }
        public double Value { get; set; }
    }
}
