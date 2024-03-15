using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.model
{
    public class SampleRope
    {
        public int Id { get; set; }
        public int SampleId { get; set; }
        public string Name  { get; set; }
        public decimal FlowSpeed { get; set; }
        public decimal SoundSpeed { get; set; }
        public int Efficiency { get; set; }
    }
}
