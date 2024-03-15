using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.model
{
    public class Place
    {
        public int Id { get; set; }
        public string Street { get; set; }
        public string PostalCode { get; set; }
        public string Town { get; set; }
        public string Province { get; set; }
        public string Country { get; set; }
    }
}
