using Proser.DryCalibration.monitor.interfaces;
using Proser.DryCalibration.sensor.pressure.calibration;
using Proser.DryCalibration.sensor.rtd.calibration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.modules.interfaces
{
    public interface INIDaqModule
    {
        IMonitor Monitor { get; }

        void Connect(string ip);

        void InitRtdMonitor( RtdTable calibration );
        void InitPressureMonitor( PressureCalibration calibration );
    }
}
