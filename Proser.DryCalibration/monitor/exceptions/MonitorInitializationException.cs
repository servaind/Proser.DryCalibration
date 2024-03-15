using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.monitor.exceptions
{
    public class MonitorInitializationException : Exception
    {
        public MonitorInitializationException(string message) 
            : base(message)
        {
           
        }
    }
}
