using Microsoft.VisualStudio.TestTools.UnitTesting;
using Proser.DryCalibration.controller.enums;
using Proser.DryCalibration.fsm;
using Proser.DryCalibration.fsm.states;
using Proser.DryCalibration.Report;
using Proser.DryCalibration.sensor.rtd;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using Proser.DryCalibration.util;
using System;
using System.Linq;
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

            //// sin icertidumbre previa (ej. vel flujo)

            //List<double> values = new List<double>()
            //{
            //    3.125,
            //    3.084,
            //    3.076,
            //    3.126,
            //    3.094,
            //    3.069,
            //    3.105,
            //    3.126,
            //    3.092,
            //    3.089
            //};

            //double deviation = Utils.CalculateStandardDeviation(values);

            //double resolution = 0.001;

            //double uncertainty = Utils.CalculateUncertainty(deviation, resolution);

            // con incertidumbre previa (ej. temperatura ambiente)

            //double uncertaintyUP = Utils.CalculateUncertaintyUP(4);
            //double uncertaintyRes = Utils.CalculateUncertaintyRes(4);
            //Assert.AreEqual(0.09, uncertaintyUP);
            //Assert.AreEqual(0.01, uncertaintyRes);

            List<Sample> samples = new List<Sample>();
            var sample = new Sample();
            sample.PressureValue = 10.72;
            samples.Add(sample);
            sample.PressureValue = 10.72;
            samples.Add(sample);
            sample.PressureValue = 10.72;
            samples.Add(sample);
            sample.PressureValue = 10.72;
            samples.Add(sample);
            sample.PressureValue = 10.72;
            samples.Add(sample);
            sample.PressureValue = 10.72;
            samples.Add(sample);
            sample.PressureValue = 10.72;
            samples.Add(sample);
            sample.PressureValue = 10.72;
            samples.Add(sample);
            sample.PressureValue = 10.72;
            samples.Add(sample);
            sample.PressureValue = 10.71;
            samples.Add(sample);


            //sample.TemperatureDetail.Add(new Rtd(1, 17.784));
            //sample.TemperatureDetail.Add(new Rtd(2, 17.784));
            //sample.TemperatureDetail.Add(new Rtd(3, 17.784));
            //sample.TemperatureDetail.Add(new Rtd(4, 17.784));
            //samples.Add(sample);
            //sample = new Sample();
            //sample.TemperatureDetail.Add(new Rtd(1, 17.783));
            //sample.TemperatureDetail.Add(new Rtd(2, 17.783));
            //sample.TemperatureDetail.Add(new Rtd(3, 17.783));
            //sample.TemperatureDetail.Add(new Rtd(4, 17.783));
            //samples.Add(sample);

            //var resultado = Utils.CalculateMinMaxRtd(samples);
            //Assert.AreEqual(17.783, resultado[1].Minimo);
            //Assert.AreEqual(17.784, resultado[1].Maximo);
            //Assert.AreEqual(0.001, Math.Round(resultado[1].Diferencia, 3));

            //Sample averages = null;
            //averages = new Sample();
            //averages.TemperatureDetail.Add(new Rtd(1, 17.783));
            //averages.TemperatureDetail.Add(new Rtd(2, 17.783));
            //averages.TemperatureDetail.Add(new Rtd(3, 17.783));
            //averages.TemperatureDetail.Add(new Rtd(4, 17.783));
            //averages.PressureValue = 10;
            //averages.CalibrationTemperature.Value = 17.783;

            //var UP_PRESS = 0.019;
            //var rtdCount = averages.TemperatureDetail.Count;
            //var averageTemp = averages.CalibrationTemperature.Value;
            //var uncertaintyCertTemp = Utils.CalculateUncertaintyUP(rtdCount);
            //var utSOS = Math.Sqrt(8.317 * 1.41 / 0.028014) * 0.5 * (1 / Math.Sqrt(273 + averageTemp));
            //var uncertaintyCertPres = UP_PRESS;
            //var presatm = 1004.11;
            //var pabs = (averages.PressureValue * 100000) + (presatm * 100000);
            //var x = averageTemp;
            //var densidad = 0.028014 * pabs / 8.317 * (x + 273);
            //var upSOS = Math.Sqrt(1.41 / densidad) * (1 / Math.Sqrt(pabs)) * 0.5;

            //var uptSOS = Math.Pow(utSOS * uncertaintyCertTemp, 2) + Math.Pow(upSOS * uncertaintyCertPres, 2)
            //    + Math.Pow(averages.Uat, 2) + Math.Pow(averages.Uap, 2);

            //double uPurgado = 0.00075;
            //double uImpurezas = 0.1;
            //double uSosEst = 0.1;
            //double uSosAGA = 0.1;

            ////var uc = Utils.DecimalComplete(Utils.CalculateUncertainty(averages.Ropes.Find(f => f.Name == v.Name).DeviationSoundSpeed,
            ////            (1d / 100d), 10, 0, 0, 0, uptSOS, uPurgado, uImpurezas, uSosEst, uSosAGA), 3);
            //var uc = Utils.DecimalComplete(Utils.CalculateUncertainty(1,
            //            (1d / 100d), 10, 0, 0, 0, uptSOS, uPurgado, uImpurezas, uSosEst, uSosAGA), 3);


            ////var resultado = Utils.CalculateUncertainty(
            ////                Utils.CalculateStandardDeviation(samples.Select(s => s.PressureValue).ToList()), (1d / 100d), 10, 0.019);

            //Console.WriteLine(uc.ToString());
            //Console.ReadLine();

            //List<double> values = new List<double>()
            //{
            //    19.0065,
            //    19.0065,
            //    19.0065,
            //    19.0065,
            //    19.0065,
            //    19.0065,
            //    19.0065,
            //    19.0065,
            //    19.0065,
            //    19.0065
            //};

            //double deviation = Utils.CalculateStandardDeviation(values);

            //double resolution = 0.01;

            //double Up = 0.18;

            //double uncertainty = Utils.CalculateUncertainty(deviation, resolution, 10, Up);

            //Assert.AreEqual(0.307, uncertainty);
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
