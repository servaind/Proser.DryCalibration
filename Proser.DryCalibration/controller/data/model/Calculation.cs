using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.model
{
    public class Calculation
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public decimal TempAverage { get; set; }
        public decimal PressAverage { get; set; }
        public decimal ThermalGradient { get; set; }
        public int EfficiencyAverage { get; set; }
        public decimal TheoricalSoundSpeed { get; set; }
        public decimal MaxSoundSpeedDispersion { get; set; }
        public List<Uncertainty> Uncertainties { get; set; }
        public List<RopeError> Errors { get; set; }
        public int Duration { get; set; }

    }
}
