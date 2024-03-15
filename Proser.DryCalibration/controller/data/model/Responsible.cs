using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.model
{
    public class Responsible
    {
        public int Id { get; set; }
        public int ReportId { get; set; }
        public string Name { get; set; }
        public  string WorkPosition { get; set; }
    }
}
