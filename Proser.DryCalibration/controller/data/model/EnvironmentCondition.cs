using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.model
{
    public class EnvironmentCondition
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public string Description { get; set; }
        public decimal Value { get; set; }
    }
}
