using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.monitor.statistic
{
    [Serializable]
    public class StatisticValue
    {
        public int TotalRequests { get; set; }
        public int TotalWrongRequests { get; set; }
        public int TotalChecksumWrongRequests{ get; set; }
        public int TotalTimeoutWrongRequests { get; set; }
    }
}
