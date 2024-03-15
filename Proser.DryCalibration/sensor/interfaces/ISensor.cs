using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.interfaces
{
    public interface ISensor
    {
        int Number { get; }
        decimal? Value { get; }
        int Module { get; }
        string AI { get; }

        bool Init();
        void Update(decimal? value);
    }
}
