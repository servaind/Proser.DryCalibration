using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proser.DryCalibration.controller.enums;
using Proser.DryCalibration.fsm;
using Proser.DryCalibration.fsm.states;
using Proser.DryCalibration.Report;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using Proser.DryCalibration.util;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Proser.DryCalibration.Test
{
    [TestClass]
    public class DryCalibrationTest
    {
        [TestMethod]
        public void CalcualteUncertaintyTest()
        {

            // sin icertidumbre previa (ej. vel flujo)

            List<double> values = new List<double>()
            {
                3.125,
                3.084,
                3.076,
                3.126,
                3.094,
                3.069,
                3.105,
                3.126,
                3.092,
                3.089
            };

            double deviation = Utils.CalculateStandardDeviation(values);

            double resolution = 0.001;

            double uncertainty = Utils.CalculateUncertainty(deviation, resolution);

            // con incertidumbre previa (ej. temperatura ambiente)

            values = new List<double>()
            {
                5.08,
                5.04,
                5.03,
                5.09,
                5.01,
                5.10,
                5.03,
                5.01,
                5.02,
                5.02
            };

            deviation = Utils.CalculateStandardDeviation(values);

            resolution = 0.01;

            double Up = 0.18;

            uncertainty = Utils.CalculateUncertainty(deviation, resolution, 10, Up);


        }

#if ISDEBUG

        [TestMethod]
        public void GenerateCsv()
        {
            List<Sample> samples = new List<Sample>();

            for (int s = 1; s <= 10  ; s++)
            {
                Sample sample = new Sample()
                {
                    CalibrationTemperature = new SampleTemperature() { Value = 25 },
                    EnvirontmentTemperature = new SampleTemperature() { Value = 26 },
                    Number = 1,
                    PressureValue = 1.23,
                    TemperatureDetail = new List<sensor.rtd.Rtd>() { new sensor.rtd.Rtd(1, 21), new sensor.rtd.Rtd(2, 22), new sensor.rtd.Rtd(3, 23), new sensor.rtd.Rtd(4, 24) },
                    Ropes = new List<RopeValue>() {
                        new RopeValue() { Name = "A", FlowSpeedValue = 0.0075654885656, SoundSpeedValue = 350.5868658678, EfficiencyValue = 100, GainValues = new GainValue(){ T1 = 41.68565, T2 = 42.6785678 }},
                        new RopeValue() { Name = "B", FlowSpeedValue = 0.007867856865886878, SoundSpeedValue = 350.58687686576, EfficiencyValue = 100, GainValues = new GainValue(){ T1 = 43.6756856, T2 = 44.67568 }},
                       // new RopeValue() { Name = "C", FlowSpeedValue = 0.007, SoundSpeedValue = 350.58, EfficiencyValue = 100, GainValues = new GainValue(){ T1 = 45, T2 = 46 }},
                       // new RopeValue() { Name = "D", FlowSpeedValue = 0.007, SoundSpeedValue = 350.58, EfficiencyValue = 100, GainValues = new GainValue(){ T1 = 47, T2 = 48 }}
                    }
                };

                samples.Add(sample);

            }

            Sample averages = new Sample()
            {
                CalibrationTemperature = new SampleTemperature() { Value = 24, Difference = 0.157 },
                EnvirontmentTemperature = new SampleTemperature() { Value = 25},
                PressureValue = 1.02,
                Ropes = new List<RopeValue>() {
                    new RopeValue() { Name = "A", FlowSpeedValue = 0.007678678, SoundSpeedValue = 350.58768856, EfficiencyValue = 100, GainValue = 41.567858 },
                    new RopeValue() { Name = "B", FlowSpeedValue = 0.00776878568, SoundSpeedValue = 350.5867876865, EfficiencyValue = 100, GainValue = 43.567856865 },
                    //new RopeValue() { Name = "C", FlowSpeedValue = 0.007, SoundSpeedValue = 350.58, EfficiencyValue = 100, GainValue = 45.5 },
                    //new RopeValue() { Name = "D", FlowSpeedValue = 0.007, SoundSpeedValue = 350.58, EfficiencyValue = 100, GainValue = 47.5 }
                }

            };

            EnvironmentalCondition environmentalConditions = new EnvironmentalCondition() { AtmosphericPressure = "1.00" };

            string csv = Utils.MakeCsv(samples, averages, environmentalConditions, UltrasonicModel.DanielJunior2R, 0);

            using (StreamWriter sw = new StreamWriter("c:\\testcsv\\test.csv",false,Encoding.UTF8))
            {
                sw.Write(csv);
                sw.Flush();
            }

        }
    
    
#endif    
    }

}
