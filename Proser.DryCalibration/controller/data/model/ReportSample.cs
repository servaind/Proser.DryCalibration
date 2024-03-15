using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.model
{
    public class ReportSample
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public int Number { get; set; }
        public List<SampleCondition> Conditions { get; set; }
        public List<SampleTempDetail>TempDetail { get; set; }
        public List<SampleRope> SampleRopes { get; set; }
        public decimal Pressure { get; set; }
        public decimal Temperature { get; set; }
    }
}
