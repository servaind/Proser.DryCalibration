﻿using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.fsm.interfaces;
using Proser.DryCalibration.Report;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.configuration;
using Proser.DryCalibration.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace Proser.DryCalibration.fsm.states
{
    public class GeneratingReportState : IState
    {
        private const string REFERENCE_FLOW = "Nitrogeno 5.0";

        public event ExitStateHandler ExitState;
        public event Action<string> RefreshState;
        public event Action<string> GenerateReportSucceeded;

        private ReportModel report;
        private ValidatedResult validatedResults;
        private Sample averages;
        private bool ContinueToNextState;

        public CancellationTokenSource token { get; private set; }
        public FSMState Name { get; private set; }
        public string Description { get; private set; }
        public string ReportPath { get; private set; }

        private UltrasonicModel ultrasonicModel;

        public GeneratingReportState( ReportModel reportModel, ValidatedResult validatedResults, Sample averages, UltrasonicModel ultrasonicModel)
        {
            this.validatedResults = validatedResults;
            this.averages = averages;
            this.report = reportModel;
            this.token = new CancellationTokenSource();
            this.Name = FSMState.GENERATING_REPORT;
            this.Description = "Ingrese los datos requeridos para continuar con el ensayo.";
            this.ReportPath = Utils.GetReportPath();
            this.ultrasonicModel = ultrasonicModel;
        }

        public void Execute()
        {
            ContinueToNextState = false;

            GenerateReportBody();

            Thread th = new Thread(new ThreadStart(excecuteTh));
            th.Start();
        }

        private void GenerateReportBody()
        {
            try
            {
                ReportBody reportBody = new ReportBody();

                List<ValResValue> flowResults = validatedResults.Averages.Where(w => w.ValidationType == ValidationType.FlowAvg).ToList();
                List<ValPercentErrorValue> percentErrResults = validatedResults.PercentErrors;
                List<ValResValue> effResults = validatedResults.Averages.Where(w => w.ValidationType == ValidationType.EffAvg).ToList();
                List<ValResValue> gainResults = validatedResults.Averages.Where(w => w.ValidationType == ValidationType.GainAvg).ToList();

                ValSoundDifference soundDiffResult = validatedResults.SoundDifference;
                ValResValue pressResult = validatedResults.Averages.FirstOrDefault(f => f.ValidationType == ValidationType.PressAVG);

                ValTempDifference tempResult = (ValTempDifference)validatedResults.Averages.FirstOrDefault(f => f.ValidationType == ValidationType.TempAVG);
                ValTempDifference tempEnvResult = (ValTempDifference)validatedResults.Averages.FirstOrDefault(f => f.ValidationType == ValidationType.TempEnvAVG);

                //condiciones ambientales
                report.Header.EnvironmentalCondition.EnvironmentTemperature = Utils.DecimalComplete(tempEnvResult.Value, 2);
                report.Header.EnvironmentalCondition.EnvironmentTempDifference = Utils.DecimalComplete(averages.EnvirontmentTemperature.Uncertainty, 2);

                //condiciones del ensayo
                CalibrationTerms calibrationTerms = new CalibrationTerms()
                {
                    Duration = "", // se asigna al finalizar el ensayo
                    EfficiencyAverage = (effResults.Count > 0) ? Utils.DecimalComplete(effResults.Average(r => r.Value), 2) : "¡Error!",
                    PressureAverage = Utils.DecimalComplete(pressResult.Value, 2),
                    PressureAverageUncertainty = Utils.DecimalComplete(averages.PressureUncertainty, 2),
                    TemperatureAverage = Utils.DecimalComplete(averages.CalibrationTemperature.Value, 2),
                    TemperatureUncertainty = Utils.DecimalComplete(averages.CalibrationTemperature.Uncertainty, 2),
                    TemperatureDifference = Utils.DecimalComplete(averages.CalibrationTemperature.Difference, 2),
                    ReferenceFlow = REFERENCE_FLOW,
                };

                reportBody.CalibrationTerms = calibrationTerms;

                //nivel de ganancias por cuerda
                string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");
                ModbusConfiguration modBusConfg = ModbusConfiguration.Read(path);

                GainConfig gainConfig = modBusConfg.UltGainConfig.FirstOrDefault(u => u.UltModel.Equals(this.ultrasonicModel));


                List<RopeResult> gainRopeResult = new List<RopeResult>();

                gainResults.ForEach(v => gainRopeResult.Add(
                    new RopeResult()
                    {
                        Name = v.Name,
                        Min = Utils.DecimalComplete(gainConfig.Min, 1),
                        Max = Utils.DecimalComplete(gainConfig.Max, 1),
                        Value = Utils.DecimalComplete(v.Value, 1),
                        Uncertainty = Utils.DecimalComplete(
                            Utils.CalculateUncertainty(averages.Ropes.Find(f => f.Name == v.Name).DeviationGain, 1, 20), 2)
                    }));

                GainResult gainResult = new GainResult();
                gainResult.AverageResults.AddRange(gainRopeResult);

                reportBody.GainResults = gainResult;

                // velocidades de flujo
                List<RopeResult> flowReportResults = new List<RopeResult>();

                flowResults.ForEach(v => flowReportResults.Add(
                    new RopeResult()
                    {
                        Name = v.Name,
                        Value = Utils.DecimalComplete(v.Value, 3),
                        Uncertainty = Utils.DecimalComplete(
                                               Utils.CalculateUncertainty(averages.Ropes.Find(f => f.Name == v.Name).DeviationFlowSpeed, (1d / 1000d)), 3)
                    }));

                FlowSpeedResult flowSpeedResult = new FlowSpeedResult();
                flowSpeedResult.AverageResults.AddRange(flowReportResults);

                reportBody.FlowSpeedResults = flowSpeedResult;

                // velocidades de sonido
                List<RopeResult> soundReportResults = new List<RopeResult>();
                percentErrResults.ForEach(v => soundReportResults.Add(new RopeResult()
                {
                    Name = v.Name,
                    Value = Utils.DecimalComplete(v.Value, 3),
                    Uncertainty = Utils.DecimalComplete(Utils.CalculateUncertainty(averages.Ropes.Find(f => f.Name == v.Name).DeviationSoundSpeed,
                        (1d / 100d), 10, 0, (1d / 10d)), 3),
                    Error = Utils.DecimalComplete(v.PercentError, 3)
                }));

                SoundSpeedResult soundSpeedResult = new SoundSpeedResult();
                soundSpeedResult.AverageResults.AddRange(soundReportResults);
                soundSpeedResult.SoundSpeedDifferece = Utils.DecimalComplete(soundDiffResult.Value, 3);
                soundSpeedResult.SoundSpeedValMax = Utils.DecimalComplete(soundDiffResult.Max, 3);
                soundSpeedResult.SoundSpeedValMin = Utils.DecimalComplete(soundDiffResult.Min, 3);
                soundSpeedResult.TheoreticalSoundSpeed = Utils.DecimalComplete(validatedResults.TheoreticalSoundSpeed, 3);

                reportBody.SoundSpeedResults = soundSpeedResult;

                // intrumentos de medición (se completa al final)
                reportBody.CalibrationMeasuring = new CalibrationMeasuring();

                // agregar el body al reporte
                this.report.Body = reportBody;
            }
            catch (Exception e)
            {
                log.Log.WriteIfExists("GenerateReportBody", e);

                this.report.Body = new ReportBody();
            }

           
        }

        private void excecuteTh()
        {
            do
            {  
                if (ContinueToNextState)
                {
                    Thread.Sleep(1000);
                    ExitState?.Invoke(FSMState.ENDING);
                    break;
                }

                Thread.Sleep(200);

            } while (!token.IsCancellationRequested);
        }

        public void Dispose()
        {
            token.Cancel();   
        }

        public void GenerateReport(CalibrationMeasuring calibrationMeasuring, int totalCalibrationDuration, string currentPressureSensorType)
        {
            report.Body.CalibrationTerms.Duration = Convert.ToString(totalCalibrationDuration);
            report.Body.CalibrationMeasuring = calibrationMeasuring;
            report.Body.CalibrationTerms.PressureSensorType = currentPressureSensorType;

            RefreshState?.Invoke("Generando el reporte del ensayo...");
            log.Log.WriteIfExists("Generando el reporte del ensayo...");

            Thread.Sleep(1000);

            Thread th = new Thread(new ThreadStart(generateReportTh));
            th.Start();
            //th.Join();
        }

       private static Mutex mutex = new Mutex();
        private void generateReportTh()
        {
            mutex.WaitOne();

            ReportApp reporteManager = new ReportApp();
            string currentFullReportPath = reporteManager.Generate(report, ReportPath);

            if (!string.IsNullOrEmpty(currentFullReportPath))
            {
                GenerateReportSucceeded?.Invoke(currentFullReportPath);
                RefreshState?.Invoke("El reporte del ensayo se generó correctamente.");
            }
            else
            {
                RefreshState?.Invoke("Ocurrió un error al generar el reporte del ensayo.");
            }
           
            Thread.Sleep(2000);
            ContinueToNextState = true;

            mutex.ReleaseMutex();
        }
    }
}
