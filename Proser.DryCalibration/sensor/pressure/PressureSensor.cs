using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using Modbus.Device;
using Modbus.Utility;
using Proser.Common.Extensions;
using Proser.DryCalibration.sensor.interfaces;
using Proser.DryCalibration.sensor.pressure.calibration;

namespace Proser.DryCalibration.sensor.pressure
{

    public class PressureSensor : ISensor
    {
        public int Number { get; private set; }
        public decimal? Value { get; private set; }
        public int Module { get; private set; }
        public string AI { get; private set; }

        public decimal MinRangeCurrent { get; set; }
        public decimal MaxRangeCurrent { get; set; }
        public decimal MinRangePressure { get; set; }
        public decimal MaxRangePressure { get; set; }

        private PressureCalibration calibration;
        public decimal Difference { get; private set; }

        public PressureSensor(PressureCalibration calibration)
        {
            this.calibration = calibration;
        }   

        public bool Init()
        {
            Number = 0;
            Module = 3;
            Value = 0;

            AI = "ai" + Number.ToString();

            MinRangeCurrent = .004M;
            MaxRangeCurrent = .02M;

            // calibration
            Difference = calibration.Error;
            MinRangePressure = calibration.Zero;
            MaxRangePressure = calibration.Span;

            return true;
        }

       
        public void Update(decimal? value)
        {
            if (value != null)
            {
                // Value = 1; //Debug
                Value = CalculatePressure((decimal)value);
            }
            else 
            {
                Value = -99;
            }
        }

        private decimal? CalculatePressure(decimal current)
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            try
            {
                decimal result = (((MaxRangePressure - MinRangePressure) * (current - MinRangeCurrent)) / (MaxRangeCurrent - MinRangeCurrent)) + MinRangePressure;

                result -= Difference;
                
                return result;
            }
            catch (Exception e)
            {
                //log
                return null;
            }
        }

    }
}
