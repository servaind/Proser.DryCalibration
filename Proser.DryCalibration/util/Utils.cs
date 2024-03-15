using Proser.DryCalibration.fsm.states;
using Proser.DryCalibration.Report;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Proser.DryCalibration.util
{
    public class Utils
    {
        public static string ConfigurationPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DryCalibration\\Configuration");
            }
        }

        public static string ModbusMapPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DryCalibration\\ModbusMaps");
            }
        }

        public static string GetReportPath()
        {
            string reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dry Calibration\\Certificados");

            if (!Directory.Exists(reportPath))
            {
                Directory.CreateDirectory(reportPath);
            }

            return reportPath;
        }

        public static string GetCurrentPath()
        {
            return Path.GetDirectoryName(
                 Assembly.GetExecutingAssembly().GetName().CodeBase).Replace(@"file:\", "") + "\\";
        }

        public static double CalculateStandardDeviation(List<double> values)
        {
            // promedio
            double avg = values.Average();

            // varianza
            double sumV = 0;

            foreach (double value in values)
            {
                sumV += Math.Pow((value - avg), 2);
            }

            double variance = sumV / (values.Count - 1);

            // desviación estandar
            double deviation = Math.Sqrt(variance);

            return deviation;
        }

        public static double CalculateUncertainty(double deviation, double resolution, int n = 10, double Up = 0, double uSOS = 0)
        {
            double Ua = deviation / Math.Sqrt(n);
            double Ures = resolution / (2 * Math.Sqrt(3));
            double Ucomb = Math.Sqrt(Math.Pow(Ua, 2) + Math.Pow(Ures, 2) + Math.Pow(Up,2) + Math.Pow(uSOS,2));
            double U = 2 * Ucomb;

            return U;
        }

        public static string MakeCsv(List<Sample> samples, Sample averages, EnvironmentalCondition environmentalCondition, UltrasonicModel ultrasonicModel, int processDuration)
        {
            StringBuilder result = new StringBuilder();

            decimal atmPres = Convert.ToDecimal(environmentalCondition.AtmosphericPressure);
            atmPres = (atmPres / 1000);

            // Header
            result.AppendLine(";");
            result.AppendLine(";");
            result.AppendLine(";Condiciones ambientales:");
            result.AppendLine(";");
            result.AppendLine(string.Format(";Temp. ambiente [ºC]:;;{0}", DecimalComplete(averages.EnvirontmentTemperature.Value, 3)));
            result.AppendLine(string.Format(";Presión atmosf. [bar]:;;{0}", atmPres));
            result.AppendLine(";");
            result.AppendLine(";Condiciones del ensayo:");
            result.AppendLine(";");
            result.AppendLine(string.Format(";Gradiente Térmico [ºC]:;;{0}", DecimalComplete(averages.CalibrationTemperature.Difference, 3)));
            result.AppendLine(string.Format(";Duración del ensayo [s]:;;{0}", processDuration));
            result.AppendLine(";");

            string spaces = "";
            samples[0].Ropes.ForEach((rb) => spaces += (";"));
            //spaces = ";;;;";

            result.AppendLine(string.Format(";;;;Temperatura [ºC];;;;;;;;;;;Velocidad de flujo [m/s];{0}Velocidad del sonido [m/s];{0}Eficiencia [%];{0}Nivel de ganancia [dB]", spaces));

            string ropeH = "";
            samples[0].Ropes.ForEach((rh) => ropeH += (";" + rh.Name));
            //ropeH = ";A;B;C;D";

            string ropeHGains = "";
            samples[0].Ropes.ForEach((rh) => ropeHGains += (";" + rh.Name + "-1;" + rh.Name + "-2"));
            //ropeHGains = ";A-1;A-2;B-1;B-2;C-1;C-2;D-1;D-2";

            string ropeHRtd = ";1;2;3;4;5;6;7;8;9;10";

            string rtdTemp = "";
            string rvFlow = "";
            string rvSound = "";
            string rEff = "";
            string rGain = "";

            rtdTemp = ";{1};{2};{3};{4};{5};{6};{7};{8};{9};{10}";

            int vPos = 10;
            samples[0].Ropes.ForEach((rb) => rvFlow += (";{" + ++vPos + "}"));
            //rvFlow = ";{11};{12};{13};{14}";

            samples[0].Ropes.ForEach((rb) => rvSound += (";{" + ++vPos + "}"));
            //rvSound = ";{15};{16};{17};{18}";

            samples[0].Ropes.ForEach((rb) => rEff += (";{" + ++vPos + "}"));
            //rEff = ";{19};{20};{21};{22}";

            samples[0].Ropes.ForEach((rb) => rGain += (";{" + ++vPos + "}"));
            samples[0].Ropes.ForEach((rb) => rGain += (";{" + ++vPos + "}"));

            //rGain = ";{23};{24};{25};{26};{27};{28};{29};{30}";

            ropeH = string.Format(";Muestra;Presión [bar];{0};{1};{1};{1};{2}", ropeHRtd, ropeH, ropeHGains);
            string rBody = string.Format(";{0};;{1};{2};{3};{4};{5}", "{0}", rtdTemp, rvFlow, rvSound, rEff, rGain);

            result.AppendLine(ropeH);

            int rtdCount = samples[0].TemperatureDetail.Count;

            // Body
            for (int n = 0; n < 10; n++)
            {
                switch (ultrasonicModel)
                {
                    case UltrasonicModel.Daniel:
                    case UltrasonicModel.Sick:
                    case UltrasonicModel.FMU:
                        result.AppendLine(
                            string.Format(rBody, n + 1,

                            rtdCount > 0 ? DecimalComplete(samples[n].TemperatureDetail[0].TempValue, 3) : "0",
                            rtdCount > 1 ? DecimalComplete(samples[n].TemperatureDetail[1].TempValue, 3) : "0",
                            rtdCount > 2 ? DecimalComplete(samples[n].TemperatureDetail[2].TempValue, 3) : "0",
                            rtdCount > 3 ? DecimalComplete(samples[n].TemperatureDetail[3].TempValue, 3) : "0",
                            rtdCount > 4 ? DecimalComplete(samples[n].TemperatureDetail[4].TempValue, 3) : "0",
                            rtdCount > 5 ? DecimalComplete(samples[n].TemperatureDetail[5].TempValue, 3) : "0",
                            rtdCount > 6 ? DecimalComplete(samples[n].TemperatureDetail[6].TempValue, 3) : "0",
                            rtdCount > 7 ? DecimalComplete(samples[n].TemperatureDetail[7].TempValue, 3) : "0",
                            rtdCount > 8 ? DecimalComplete(samples[n].TemperatureDetail[8].TempValue, 3) : "0",
                            rtdCount > 9 ? DecimalComplete(samples[n].TemperatureDetail[9].TempValue, 3) : "0",

                            DecimalComplete(samples[n].Ropes[0].FlowSpeedValue, 8),
                            DecimalComplete(samples[n].Ropes[1].FlowSpeedValue, 8),
                            DecimalComplete(samples[n].Ropes[2].FlowSpeedValue, 8),
                            DecimalComplete(samples[n].Ropes[3].FlowSpeedValue, 8),
                            DecimalComplete(samples[n].Ropes[0].SoundSpeedValue, 8),
                            DecimalComplete(samples[n].Ropes[1].SoundSpeedValue, 8),
                            DecimalComplete(samples[n].Ropes[2].SoundSpeedValue, 8),
                            DecimalComplete(samples[n].Ropes[3].SoundSpeedValue, 6),
                            samples[n].Ropes[0].EfficiencyValue, 
                            samples[n].Ropes[1].EfficiencyValue, 
                            samples[n].Ropes[2].EfficiencyValue, 
                            samples[n].Ropes[3].EfficiencyValue, 
                            DecimalComplete(samples[n].Ropes[0].GainValues.T1, 2),
                            DecimalComplete(samples[n].Ropes[0].GainValues.T2, 2),
                            DecimalComplete(samples[n].Ropes[1].GainValues.T1, 2),
                            DecimalComplete(samples[n].Ropes[1].GainValues.T2, 2),
                            DecimalComplete(samples[n].Ropes[2].GainValues.T1, 2),
                            DecimalComplete(samples[n].Ropes[2].GainValues.T2, 2),
                            DecimalComplete(samples[n].Ropes[3].GainValues.T1, 2),
                            DecimalComplete(samples[n].Ropes[3].GainValues.T2, 2)
                            ));
                        break;
                    case UltrasonicModel.DanielJunior1R:
                        result.AppendLine(
                            string.Format(rBody, n + 1,

                            rtdCount > 0 ? DecimalComplete(samples[n].TemperatureDetail[0].TempValue, 6) : "0",
                            rtdCount > 1 ? DecimalComplete(samples[n].TemperatureDetail[1].TempValue, 3) : "0",
                            rtdCount > 2 ? DecimalComplete(samples[n].TemperatureDetail[2].TempValue, 3) : "0",
                            rtdCount > 3 ? DecimalComplete(samples[n].TemperatureDetail[3].TempValue, 3) : "0",
                            rtdCount > 4 ? DecimalComplete(samples[n].TemperatureDetail[4].TempValue, 3) : "0",
                            rtdCount > 5 ? DecimalComplete(samples[n].TemperatureDetail[5].TempValue, 3) : "0",
                            rtdCount > 6 ? DecimalComplete(samples[n].TemperatureDetail[6].TempValue, 3) : "0",
                            rtdCount > 7 ? DecimalComplete(samples[n].TemperatureDetail[7].TempValue, 3) : "0",
                            rtdCount > 8 ? DecimalComplete(samples[n].TemperatureDetail[8].TempValue, 3) : "0",
                            rtdCount > 9 ? DecimalComplete(samples[n].TemperatureDetail[9].TempValue, 3) : "0",

                            DecimalComplete(samples[n].Ropes[0].FlowSpeedValue, 8),
                            DecimalComplete(samples[n].Ropes[0].SoundSpeedValue, 8),
                            samples[n].Ropes[0].EfficiencyValue,
                            DecimalComplete(samples[n].Ropes[0].GainValues.T1, 2),
                            DecimalComplete(samples[n].Ropes[0].GainValues.T2, 2)
                            ));
                        break;
                    case UltrasonicModel.DanielJunior2R:
                        result.AppendLine(
                        string.Format(rBody, n + 1,

                         rtdCount > 0 ? DecimalComplete(samples[n].TemperatureDetail[0].TempValue, 3) : "0",
                         rtdCount > 1 ? DecimalComplete(samples[n].TemperatureDetail[1].TempValue, 3) : "0",
                         rtdCount > 2 ? DecimalComplete(samples[n].TemperatureDetail[2].TempValue, 3) : "0",
                         rtdCount > 3 ? DecimalComplete(samples[n].TemperatureDetail[3].TempValue, 3) : "0",
                         rtdCount > 4 ? DecimalComplete(samples[n].TemperatureDetail[4].TempValue, 3) : "0",
                         rtdCount > 5 ? DecimalComplete(samples[n].TemperatureDetail[5].TempValue, 3) : "0",
                         rtdCount > 6 ? DecimalComplete(samples[n].TemperatureDetail[6].TempValue, 3) : "0",
                         rtdCount > 7 ? DecimalComplete(samples[n].TemperatureDetail[7].TempValue, 3) : "0",
                         rtdCount > 8 ? DecimalComplete(samples[n].TemperatureDetail[8].TempValue, 3) : "0",
                         rtdCount > 9 ? DecimalComplete(samples[n].TemperatureDetail[9].TempValue, 3) : "0",

                         DecimalComplete(samples[n].Ropes[0].FlowSpeedValue, 8),
                         DecimalComplete(samples[n].Ropes[1].FlowSpeedValue, 8),
                         DecimalComplete(samples[n].Ropes[0].SoundSpeedValue, 8),
                         DecimalComplete(samples[n].Ropes[1].SoundSpeedValue, 8),
                         samples[n].Ropes[0].EfficiencyValue, 
                         samples[n].Ropes[1].EfficiencyValue,
                         DecimalComplete(samples[n].Ropes[0].GainValues.T1, 2),
                         DecimalComplete(samples[n].Ropes[0].GainValues.T2, 2),
                         DecimalComplete(samples[n].Ropes[1].GainValues.T1, 2),
                         DecimalComplete(samples[n].Ropes[1].GainValues.T2, 2)
                         ));
                        break; 
                    case UltrasonicModel.InstrometS5:
                        result.AppendLine(
                            string.Format(rBody, n + 1,

                            rtdCount > 0 ? DecimalComplete(samples[n].TemperatureDetail[0].TempValue, 3) : "0",
                            rtdCount > 1 ? DecimalComplete(samples[n].TemperatureDetail[1].TempValue, 3) : "0",
                            rtdCount > 2 ? DecimalComplete(samples[n].TemperatureDetail[2].TempValue, 3) : "0",
                            rtdCount > 3 ? DecimalComplete(samples[n].TemperatureDetail[3].TempValue, 3) : "0",
                            rtdCount > 4 ? DecimalComplete(samples[n].TemperatureDetail[4].TempValue, 3) : "0",
                            rtdCount > 5 ? DecimalComplete(samples[n].TemperatureDetail[5].TempValue, 3) : "0",
                            rtdCount > 6 ? DecimalComplete(samples[n].TemperatureDetail[6].TempValue, 3) : "0",
                            rtdCount > 7 ? DecimalComplete(samples[n].TemperatureDetail[7].TempValue, 3) : "0",
                            rtdCount > 8 ? DecimalComplete(samples[n].TemperatureDetail[8].TempValue, 3) : "0",
                            rtdCount > 9 ? DecimalComplete(samples[n].TemperatureDetail[9].TempValue, 3) : "0",

                            samples[n].Ropes[0].FlowSpeedValue, samples[n].Ropes[1].FlowSpeedValue, samples[n].Ropes[2].FlowSpeedValue, samples[n].Ropes[3].FlowSpeedValue, samples[n].Ropes[4].FlowSpeedValue,
                            samples[n].Ropes[0].SoundSpeedValue, samples[n].Ropes[1].SoundSpeedValue, samples[n].Ropes[2].SoundSpeedValue, samples[n].Ropes[3].SoundSpeedValue, samples[n].Ropes[4].SoundSpeedValue,
                            samples[n].Ropes[0].EfficiencyValue, samples[n].Ropes[1].EfficiencyValue, samples[n].Ropes[2].EfficiencyValue, samples[n].Ropes[3].EfficiencyValue, samples[n].Ropes[4].EfficiencyValue,
                            samples[n].Ropes[0].GainValues.T1, samples[n].Ropes[0].GainValues.T2, samples[n].Ropes[1].GainValues.T1, samples[n].Ropes[1].GainValues.T2,
                            samples[n].Ropes[2].GainValues.T1, samples[n].Ropes[2].GainValues.T2, samples[n].Ropes[3].GainValues.T1, samples[n].Ropes[3].GainValues.T2,
                            samples[n].Ropes[4].GainValues.T1, samples[n].Ropes[4].GainValues.T2
                            ));
                        break;
                    case UltrasonicModel.InstrometS6:
                        result.AppendLine(
                            string.Format(rBody, n + 1,

                            rtdCount > 0 ? DecimalComplete(samples[n].TemperatureDetail[0].TempValue, 3) : "0",
                            rtdCount > 1 ? DecimalComplete(samples[n].TemperatureDetail[1].TempValue, 3) : "0",
                            rtdCount > 2 ? DecimalComplete(samples[n].TemperatureDetail[2].TempValue, 3) : "0",
                            rtdCount > 3 ? DecimalComplete(samples[n].TemperatureDetail[3].TempValue, 3) : "0",
                            rtdCount > 4 ? DecimalComplete(samples[n].TemperatureDetail[4].TempValue, 3) : "0",
                            rtdCount > 5 ? DecimalComplete(samples[n].TemperatureDetail[5].TempValue, 3) : "0",
                            rtdCount > 6 ? DecimalComplete(samples[n].TemperatureDetail[6].TempValue, 3) : "0",
                            rtdCount > 7 ? DecimalComplete(samples[n].TemperatureDetail[7].TempValue, 3) : "0",
                            rtdCount > 8 ? DecimalComplete(samples[n].TemperatureDetail[8].TempValue, 3) : "0",
                            rtdCount > 9 ? DecimalComplete(samples[n].TemperatureDetail[9].TempValue, 3) : "0",

                            samples[n].Ropes[0].FlowSpeedValue, samples[n].Ropes[1].FlowSpeedValue, samples[n].Ropes[2].FlowSpeedValue, samples[n].Ropes[3].FlowSpeedValue, samples[n].Ropes[4].FlowSpeedValue, samples[n].Ropes[5].FlowSpeedValue, samples[n].Ropes[6].FlowSpeedValue, samples[n].Ropes[7].FlowSpeedValue,
                            samples[n].Ropes[0].SoundSpeedValue, samples[n].Ropes[1].SoundSpeedValue, samples[n].Ropes[2].SoundSpeedValue, samples[n].Ropes[3].SoundSpeedValue, samples[n].Ropes[4].SoundSpeedValue, samples[n].Ropes[5].SoundSpeedValue, samples[n].Ropes[6].SoundSpeedValue, samples[n].Ropes[7].SoundSpeedValue,
                            samples[n].Ropes[0].EfficiencyValue, samples[n].Ropes[1].EfficiencyValue, samples[n].Ropes[2].EfficiencyValue, samples[n].Ropes[3].EfficiencyValue, samples[n].Ropes[4].EfficiencyValue, samples[n].Ropes[5].EfficiencyValue, samples[n].Ropes[6].EfficiencyValue, samples[n].Ropes[7].EfficiencyValue,
                            samples[n].Ropes[0].GainValues.T1, samples[n].Ropes[0].GainValues.T2, samples[n].Ropes[1].GainValues.T1, samples[n].Ropes[1].GainValues.T2,
                            samples[n].Ropes[2].GainValues.T1, samples[n].Ropes[2].GainValues.T2, samples[n].Ropes[3].GainValues.T1, samples[n].Ropes[3].GainValues.T2,
                            samples[n].Ropes[4].GainValues.T1, samples[n].Ropes[4].GainValues.T2, samples[n].Ropes[5].GainValues.T1, samples[n].Ropes[5].GainValues.T2,
                            samples[n].Ropes[6].GainValues.T1, samples[n].Ropes[6].GainValues.T2, samples[n].Ropes[7].GainValues.T1, samples[n].Ropes[7].GainValues.T2

                            ));
                        break;
                    case UltrasonicModel.KrohneAltosonicV12:
                        result.AppendLine(
                            string.Format(rBody, n + 1,

                            rtdCount > 0 ? DecimalComplete(samples[n].TemperatureDetail[0].TempValue, 3) : "0",
                            rtdCount > 1 ? DecimalComplete(samples[n].TemperatureDetail[1].TempValue, 3) : "0",
                            rtdCount > 2 ? DecimalComplete(samples[n].TemperatureDetail[2].TempValue, 3) : "0",
                            rtdCount > 3 ? DecimalComplete(samples[n].TemperatureDetail[3].TempValue, 3) : "0",
                            rtdCount > 4 ? DecimalComplete(samples[n].TemperatureDetail[4].TempValue, 3) : "0",
                            rtdCount > 5 ? DecimalComplete(samples[n].TemperatureDetail[5].TempValue, 3) : "0",
                            rtdCount > 6 ? DecimalComplete(samples[n].TemperatureDetail[6].TempValue, 3) : "0",
                            rtdCount > 7 ? DecimalComplete(samples[n].TemperatureDetail[7].TempValue, 3) : "0",
                            rtdCount > 8 ? DecimalComplete(samples[n].TemperatureDetail[8].TempValue, 3) : "0",
                            rtdCount > 9 ? DecimalComplete(samples[n].TemperatureDetail[9].TempValue, 3) : "0",

                            samples[n].Ropes[0].FlowSpeedValue, samples[n].Ropes[1].FlowSpeedValue, samples[n].Ropes[2].FlowSpeedValue, samples[n].Ropes[3].FlowSpeedValue, samples[n].Ropes[4].FlowSpeedValue, samples[n].Ropes[5].FlowSpeedValue,
                            samples[n].Ropes[0].SoundSpeedValue, samples[n].Ropes[1].SoundSpeedValue, samples[n].Ropes[2].SoundSpeedValue, samples[n].Ropes[3].SoundSpeedValue, samples[n].Ropes[4].SoundSpeedValue, samples[n].Ropes[5].SoundSpeedValue,
                            samples[n].Ropes[0].EfficiencyValue, samples[n].Ropes[1].EfficiencyValue, samples[n].Ropes[2].EfficiencyValue, samples[n].Ropes[3].EfficiencyValue, samples[n].Ropes[4].EfficiencyValue, samples[n].Ropes[5].EfficiencyValue,
                            samples[n].Ropes[0].GainValues.T1, samples[n].Ropes[0].GainValues.T2, samples[n].Ropes[1].GainValues.T1, samples[n].Ropes[1].GainValues.T2,
                            samples[n].Ropes[2].GainValues.T1, samples[n].Ropes[2].GainValues.T2, samples[n].Ropes[3].GainValues.T1, samples[n].Ropes[3].GainValues.T2,
                            samples[n].Ropes[4].GainValues.T1, samples[n].Ropes[4].GainValues.T2, samples[n].Ropes[5].GainValues.T1, samples[n].Ropes[5].GainValues.T2
                            ));
                        break;
                }
            }

            string avgs = string.Format(";Promedios;{0};{1};{2};{3};{4};{5}", "{0}", rtdTemp, rvFlow, rvSound, rEff, rGain);

            result.AppendLine(";");

            switch (ultrasonicModel)
            {
                case UltrasonicModel.Daniel:
                case UltrasonicModel.Sick:
                case UltrasonicModel.FMU:
                    result.AppendLine(
                        string.Format(avgs, averages.PressureValue,
                        DecimalComplete(averages.CalibrationTemperature.Value,3), "", "", "", "", "", "", "", "", "",
                        DecimalComplete(averages.Ropes[0].FlowSpeedValue, 8),
                        DecimalComplete(averages.Ropes[1].FlowSpeedValue, 8),
                        DecimalComplete(averages.Ropes[2].FlowSpeedValue, 8),
                        DecimalComplete(averages.Ropes[3].FlowSpeedValue, 8),
                        DecimalComplete(averages.Ropes[0].SoundSpeedValue, 8),
                        DecimalComplete(averages.Ropes[1].SoundSpeedValue, 8),
                        DecimalComplete(averages.Ropes[2].SoundSpeedValue, 8),
                        DecimalComplete(averages.Ropes[3].SoundSpeedValue, 8),
                        averages.Ropes[0].EfficiencyValue, 
                        averages.Ropes[1].EfficiencyValue, 
                        averages.Ropes[2].EfficiencyValue, 
                        averages.Ropes[3].EfficiencyValue,
                        DecimalComplete(averages.Ropes[0].GainValue, 2), "",
                        DecimalComplete(averages.Ropes[1].GainValue, 2), "",
                        DecimalComplete(averages.Ropes[2].GainValue, 2), "",
                        DecimalComplete(averages.Ropes[3].GainValue, 2), ""
                        ));
                    break;
                case UltrasonicModel.DanielJunior1R:
                    result.AppendLine(
                        string.Format(avgs, averages.PressureValue,
                        DecimalComplete(averages.CalibrationTemperature.Value, 3), "", "", "", "", "", "", "", "", "",
                        DecimalComplete(averages.Ropes[0].FlowSpeedValue, 8),
                        DecimalComplete(averages.Ropes[0].SoundSpeedValue, 8),
                        averages.Ropes[0].EfficiencyValue, 
                        DecimalComplete(averages.Ropes[0].GainValue, 2), ""
                        ));
                    break;
                case UltrasonicModel.DanielJunior2R:
                    result.AppendLine(
                        string.Format(avgs, averages.PressureValue,
                        DecimalComplete(averages.CalibrationTemperature.Value, 3), "", "", "", "", "", "", "", "", "",
                        DecimalComplete(averages.Ropes[0].FlowSpeedValue, 8),
                        DecimalComplete(averages.Ropes[1].FlowSpeedValue, 8),
                        DecimalComplete(averages.Ropes[0].SoundSpeedValue, 8),
                        DecimalComplete(averages.Ropes[1].SoundSpeedValue, 8),
                        averages.Ropes[0].EfficiencyValue, 
                        averages.Ropes[1].EfficiencyValue, 
                        DecimalComplete(averages.Ropes[0].GainValue, 2), "",
                        DecimalComplete(averages.Ropes[1].GainValue, 2), ""
                        ));
                    break;
                case UltrasonicModel.InstrometS5:
                    result.AppendLine(
                        string.Format(avgs, averages.PressureValue,
                        DecimalComplete(averages.CalibrationTemperature.Value, 3), "", "", "", "", "", "", "", "", "",
                        averages.Ropes[0].FlowSpeedValue, averages.Ropes[1].FlowSpeedValue, averages.Ropes[2].FlowSpeedValue, averages.Ropes[3].FlowSpeedValue, averages.Ropes[4].FlowSpeedValue,
                        averages.Ropes[0].SoundSpeedValue, averages.Ropes[1].SoundSpeedValue, averages.Ropes[2].SoundSpeedValue, averages.Ropes[3].SoundSpeedValue, averages.Ropes[4].SoundSpeedValue,
                        averages.Ropes[0].EfficiencyValue, averages.Ropes[1].EfficiencyValue, averages.Ropes[2].EfficiencyValue, averages.Ropes[3].EfficiencyValue, averages.Ropes[4].EfficiencyValue,
                        averages.Ropes[0].GainValue, "", averages.Ropes[1].GainValue, "", averages.Ropes[2].GainValue, "", averages.Ropes[3].GainValue, "", averages.Ropes[4].GainValue, ""

                        ));
                    break;
                case UltrasonicModel.InstrometS6:
                    result.AppendLine(
                        string.Format(avgs, averages.PressureValue,
                        DecimalComplete(averages.CalibrationTemperature.Value, 3), "", "", "", "", "", "", "", "", "",
                        averages.Ropes[0].FlowSpeedValue, averages.Ropes[1].FlowSpeedValue, averages.Ropes[2].FlowSpeedValue, averages.Ropes[3].FlowSpeedValue, averages.Ropes[4].FlowSpeedValue, averages.Ropes[5].FlowSpeedValue, averages.Ropes[6].FlowSpeedValue, averages.Ropes[7].FlowSpeedValue,
                        averages.Ropes[0].SoundSpeedValue, averages.Ropes[1].SoundSpeedValue, averages.Ropes[2].SoundSpeedValue, averages.Ropes[3].SoundSpeedValue, averages.Ropes[4].SoundSpeedValue, averages.Ropes[5].SoundSpeedValue, averages.Ropes[6].SoundSpeedValue, averages.Ropes[7].SoundSpeedValue,
                        averages.Ropes[0].EfficiencyValue, averages.Ropes[1].EfficiencyValue, averages.Ropes[2].EfficiencyValue, averages.Ropes[3].EfficiencyValue, averages.Ropes[4].EfficiencyValue, averages.Ropes[5].EfficiencyValue, averages.Ropes[6].EfficiencyValue, averages.Ropes[7].EfficiencyValue,
                        averages.Ropes[0].GainValue, "", averages.Ropes[1].GainValue, "", averages.Ropes[2].GainValue, "", averages.Ropes[3].GainValue, "", averages.Ropes[4].GainValue, "", averages.Ropes[5].GainValue, "", averages.Ropes[6].GainValue, "", averages.Ropes[7].GainValue, ""
                        ));
                    break;
                case UltrasonicModel.KrohneAltosonicV12:
                    result.AppendLine(
                        string.Format(avgs, averages.PressureValue,
                        DecimalComplete(averages.CalibrationTemperature.Value, 3), "", "", "", "", "", "", "", "", "",
                        averages.Ropes[0].FlowSpeedValue, averages.Ropes[1].FlowSpeedValue, averages.Ropes[2].FlowSpeedValue, averages.Ropes[3].FlowSpeedValue, averages.Ropes[4].FlowSpeedValue, averages.Ropes[5].FlowSpeedValue,
                        averages.Ropes[0].SoundSpeedValue, averages.Ropes[1].SoundSpeedValue, averages.Ropes[2].SoundSpeedValue, averages.Ropes[3].SoundSpeedValue, averages.Ropes[4].SoundSpeedValue, averages.Ropes[5].SoundSpeedValue,
                        averages.Ropes[0].EfficiencyValue, averages.Ropes[1].EfficiencyValue, averages.Ropes[2].EfficiencyValue, averages.Ropes[3].EfficiencyValue, averages.Ropes[4].EfficiencyValue, averages.Ropes[5].EfficiencyValue,
                        averages.Ropes[0].GainValue, "", averages.Ropes[1].GainValue, "", averages.Ropes[2].GainValue, "", averages.Ropes[3].GainValue, "", averages.Ropes[4].GainValue, "", averages.Ropes[5].GainValue, ""
                        ));
                    break;
            }

            result.AppendLine(";");

            // Footer
            result.AppendLine(";");
            result.AppendLine(";");
            result.AppendLine(";");

            string res = result.ToString();

            return res;
        }


        public static string DecimalComplete(double num, int decQ)
        {

            if (Double.IsNaN(num)) 
            {
                return "¡Error!";
            }

            string toConvert = Convert.ToString(Math.Round(num, decQ));

            toConvert = toConvert.Replace(".", ",");

            string[] parts = toConvert.Split(',');
            bool fracExist = parts.Length > 1;

            string integer = fracExist ? parts[0] : toConvert;
            string frac = fracExist ? parts[1] : "";

            frac = frac.PadRight(decQ, '0');

            string result = string.Format("{0},{1}", integer, frac);

            return result;
        }
    }

}
