using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.model
{
    public class SampleTempDetail
    {
        public int Id { get; set; }
        public int SampleId { get; set; }
        public string Rtd { get; set; }
        public decimal Value { get; set; }

    }
}
