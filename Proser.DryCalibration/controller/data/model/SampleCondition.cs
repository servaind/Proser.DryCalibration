using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.model
{
    public class SampleCondition
    {
        public int Id { get; set; }
        public int SampleId { get; set; }
        public string Description { get; set; }
        public decimal Value { get; set; }
    }
}
