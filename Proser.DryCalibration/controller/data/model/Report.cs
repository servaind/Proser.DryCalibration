using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.model
{
    public class Report
    {
        public int Id { get; set; }
        public string Number { get; set; }
        public Ultrasonic Ultrasonic { get; set; }
        public Customer Customer { get; set; }
        public Place Place { get; set; }
        public List<Responsible> Responsibles { get; set; }
        public List<ReportEquipment> Equipments { get; set; }
        public List<EnvironmentCondition> EnvironmentConditions { get; set; }
        public List<ReportSample> Samples { get; set; }
        public Calculation Calculation { get; set; }
        public DateTime CreationDate { get; set; }

    }
}
