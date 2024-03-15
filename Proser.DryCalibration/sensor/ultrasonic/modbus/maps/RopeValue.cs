using Proser.DryCalibration.monitor.statistic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.ultrasonic.modbus.maps
{
    public class RopeValue
    {
        public string Name { get; set; }
       
        public int EfficiencyValue { get; set; }
        public GainValue GainValues { get; set; }
        public double FlowSpeedValue { get; set; }
        public double SoundSpeedValue { get; set; }
        public double GainValue { get; set; }

        public double DeviationGain { get; set; }
        public double DeviationFlowSpeed { get; set; }
        public double DeviationSoundSpeed { get; set; }

        public RopeValue()
        {
            EfficiencyValue = 0;
        }

        public RopeValue(string name)
        {
            Name = name;
            
            EfficiencyValue = 0;
            GainValues = new GainValue();
            FlowSpeedValue = 0;
            SoundSpeedValue = 0;
            GainValue = 0;
        }

    }
}
