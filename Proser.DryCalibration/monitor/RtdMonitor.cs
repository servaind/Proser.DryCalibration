using log4net;
using NationalInstruments.DAQmx;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.exceptions;
using Proser.DryCalibration.sensor.rtd;
using Proser.DryCalibration.sensor.rtd.calibration;
using Proser.DryCalibration.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Proser.DryCalibration.monitor
{
    public class RtdMonitor : MonitorBase
    {
        //rtds
        private const double VAR_ERROR = 0.3;
        //private const double VAR_ERROR = 0.375;  //debug
        private const int HIST_CAPACITY = 15;

        private DataStore<double> tempDifferenceHist;
        private RtdTable calibration;
        private RTDValue monitorValue;
        private List<Rtd> Rtds;
        private bool allRTDUpdated;

        private double UP_TEMP = 0;

        public RtdMonitor(RtdTable calibration)
        {
            this.calibration = calibration;
            this.Type = enums.MonitorType.RTD;
            this.monitorValue = new RTDValue();
        }

        public override void InitMonitor(Device daqModule)
        {
            tempDifferenceHist = new DataStore<double>(HIST_CAPACITY);

            base.InitMonitor(daqModule);

            State = MonitorState.Running;

            this.UP_TEMP = 0.18;
        }

        public override void LoadSensor()
        {
            Rtds = new List<Rtd>();

            for (int i = 0; i <= 11; i++)
            {
                Rtd rtd = new Rtd(calibration.RtdSensors[i].Number,
                                  calibration.RtdSensors[i].ResPoints,
                                  100); //calibration.RtdSensors[i].R0);

                rtd.Active = calibration.RtdSensors[i].Active.Equals(1);

                Rtds.Add(rtd);
            }
        }

        public void ChangeRTDActiveState(int number, bool active)
        {
            State = MonitorState.Stoped;

            if (active)
            {
                bool exist = Rtds.Find(r => r.Number == number) != null;

                if (!exist)
                {
                    Rtd rtd = new Rtd(number, 
                                      calibration.RtdSensors[number].ResPoints,
                                      100); //calibration.RtdSensors[number].R0);
                    Rtds.Add(rtd);
                }              
            }
            else
            {
                Rtd rtd = Rtds.Find(r => r.Number == number);
                Rtds.Remove(rtd);
            }

            Rtds = Rtds.OrderBy(o => o.Number).ToList();

            InitDaqModuleMonitorThread();

        }

        public override void DoMonitorWork()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-AR");

            while (State.Equals(MonitorState.Running))
            {
                UpdateSensor();
            }
        }

        public override void UpdateSensor()
        {
            lock (objLock)
            {
                //System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                //stopWatch.Start();

                List<Rtd> activeList = Rtds.Where(r => r.Active).ToList();
             
                allRTDUpdated = false;
                activeList.ForEach(r => r.ValueObtained = false);// reset

                List<Rtd> activeListM0 = activeList.Where(w => w.Module == 0).ToList();
                List<Rtd> activeListM1 = activeList.Where(w => w.Module == 1).ToList();
                List<Rtd> activeListM2 = activeList.Where(w => w.Module == 2).ToList();

                Thread th0 = new Thread(new ParameterizedThreadStart(goUpdateM0Th));
                th0.Start(activeListM0);
                //th0.Join();

                Thread th1 = new Thread(new ParameterizedThreadStart(goUpdateM1Th));
                th1.Start(activeListM1);
                th1.Join();

                Thread th2 = new Thread(new ParameterizedThreadStart(goUpdateM2Th));
                th2.Start(activeListM2);
                //th2.Join();

                //foreach (Rtd rtd in activeList)
                //{
                //    RTDRealValue value = ReadAI0(rtd.Module, rtd.AI, rtd.R0);
                //    rtd.Update(value.RealResValue, calibration.TempPoints);

                //    Console.WriteLine("RTD {0:00}: {1}", rtd.Number, rtd.TempValue);
                //}

                int updated = 0;

                do
                {
                    updated = activeListM0.Where(r => r.ValueObtained == true).ToList().Count()
                              + activeListM1.Where(r => r.ValueObtained == true).ToList().Count()
                              + activeListM2.Where(r => r.ValueObtained == true).ToList().Count();

                    allRTDUpdated = (updated == activeList.Count);

                } while (!allRTDUpdated && State.Equals(MonitorState.Running));

                foreach (Rtd rtd in activeList)
                {
                    Console.WriteLine("RTD {0:00}: {1}", rtd.Number, rtd.TempValue);
                }

                try
                {
                    List<Rtd> evaluateList = activeList.Where(r => r.Number < 10).ToList(); // 1 a 10 son las de calibración
                    List<Rtd> environtmentList = activeList.Where(r => r.Number >= 10).ToList(); // 11 y 12 son las de ambiente

                    monitorValue.Rtds = Rtds;

                    // calibración
                    monitorValue.CalibrationRTD.Average = (evaluateList.Count > 0)? Math.Round(evaluateList.Average(r => r.TempValue), 3) : -99;
                    monitorValue.CalibrationRTD.Minimum = (evaluateList.Count > 0)? evaluateList.Min(r => r.TempValue) : 0;
                    monitorValue.CalibrationRTD.Maximum = (evaluateList.Count > 0)? evaluateList.Max(r => r.TempValue) : 0;

                    monitorValue.CalibrationRTD.Difference = (activeList.Count > 0)? Math.Abs(monitorValue.CalibrationRTD.Maximum - monitorValue.CalibrationRTD.Minimum) : -99;
                    monitorValue.CalibrationRTD.Uncertainty = (activeList.Count > 0)?
                        Math.Round(Utils.CalculateUncertainty(Utils.CalculateStandardDeviation(evaluateList.Select(r => r.TempValue).ToList()), (1 / 100), evaluateList.Count, UP_TEMP),3) : -99;

                    //tempDifferenceHist.Add(monitorValue.CalibrationRTD.Uncertainty);
                    tempDifferenceHist.Add(monitorValue.CalibrationRTD.Difference);

                    Console.WriteLine("Calibración:--------");
                    Console.WriteLine("Min: {0}", monitorValue.CalibrationRTD.Minimum);
                    Console.WriteLine("Max: {0}", monitorValue.CalibrationRTD.Maximum);
                    Console.WriteLine("Avg: {0}", monitorValue.CalibrationRTD.Average);
                    Console.WriteLine("Unc: {0}", monitorValue.CalibrationRTD.Uncertainty);
                    Console.WriteLine("Dif: {0}", monitorValue.CalibrationRTD.Difference);

                    // ambiente
                    monitorValue.EnvironmentRTD.Average = (environtmentList.Count > 0) ? Math.Round(environtmentList.Average(r => r.TempValue), 3) : -99;
                    monitorValue.EnvironmentRTD.Minimum = (environtmentList.Count > 0) ? environtmentList.Min(r => r.TempValue) : 0;
                    monitorValue.EnvironmentRTD.Maximum = (environtmentList.Count > 0) ? environtmentList.Max(r => r.TempValue) : 0;
                    monitorValue.EnvironmentRTD.Uncertainty = (environtmentList.Count > 0) ?
                      Math.Round(Utils.CalculateUncertainty(Utils.CalculateStandardDeviation(environtmentList.Select(r => r.TempValue).ToList()), (1 / 100), environtmentList.Count, UP_TEMP), 3) : -99;
                    monitorValue.EnvironmentRTD.Difference = (environtmentList.Count > 0) ? Math.Abs(monitorValue.EnvironmentRTD.Maximum - monitorValue.EnvironmentRTD.Minimum) : -99;

                    Console.WriteLine("Ambiente:--------");
                    Console.WriteLine("Min: {0}", monitorValue.EnvironmentRTD.Minimum);
                    Console.WriteLine("Max: {0}", monitorValue.EnvironmentRTD.Maximum);
                    Console.WriteLine("Avg: {0}", monitorValue.EnvironmentRTD.Average);
                    Console.WriteLine("Unc: {0}", monitorValue.EnvironmentRTD.Uncertainty);
                    Console.WriteLine("Dif: {0}", monitorValue.EnvironmentRTD.Difference);

                    //stopWatch.Stop();

                    // Get the elapsed time as a TimeSpan value.
                    //TimeSpan ts = stopWatch.Elapsed;

                    // Format and display the TimeSpan value.
                    //string elapsedTime = String.Format("{0:00}.{1:00}",
                                                //ts.Seconds,
                                                //ts.Milliseconds / 10);

                    //Console.WriteLine("Time elapsed: " + elapsedTime);
                    //logger.Info("Time elapsed: " + elapsedTime);

                }
                catch (Exception ex)
                {
                    if (logger != null)
                    {
                        logger.ErrorFormat("RTDMonitor.UpdateSensor: {0}.", ex.Message);
                    }
                }

                if (activeList.Count > 0)
                {
                    double maxDiff = tempDifferenceHist.Data.Max();
                    monitorValue.IsStable = maxDiff < VAR_ERROR;

                    if (monitorValue.IsStable)
                    {
                        Console.WriteLine("Temperatura estable.");
                    }
                }
               
                Console.WriteLine("---------------------------------------------------");

                // send values
                SendMonitorValues(monitorValue);

                // send statistic
                SendMonitorStatistics();
            }
        }

        private void goUpdateM0Th(object obj)
        {
            List<Rtd> list = (List<Rtd>)obj;

            foreach (var rtd in list)
            {
                RTDRealValue realValue = ReadAI0(rtd.Module, rtd.AI, rtd.R0);
                rtd.Update(realValue.RealResValue, calibration.TempPoints);
                rtd.ValueObtained = true;
            }
        }

        private void goUpdateM1Th(object obj)
        {
            List<Rtd> list = (List<Rtd>)obj;

            foreach (var rtd in list)
            {
                RTDRealValue realValue = ReadAI1(rtd.Module, rtd.AI, rtd.R0);
                rtd.Update(realValue.RealResValue, calibration.TempPoints);
                rtd.ValueObtained = true;
            }
        }

        private void goUpdateM2Th(object obj)
        {
            List<Rtd> list = (List<Rtd>)obj;

            foreach (var rtd in list)
            {
                RTDRealValue realValue = ReadAI2(rtd.Module, rtd.AI, rtd.R0);
                rtd.Update(realValue.RealResValue, calibration.TempPoints);
                rtd.ValueObtained = true;
            }
        }   

        private RTDRealValue ReadAI0(int module, string ai, double r0)
        {
            var rtdType = AIRtdType.Pt3851;
            var resistanceConfiguration = AIResistanceConfiguration.ThreeWire;
            var excitationSource = AIExcitationSource.Internal;

            RTDRealValue rtdRealValue = new RTDRealValue();
            var task = new NationalInstruments.DAQmx.Task();

            try
            {
                task = new NationalInstruments.DAQmx.Task();

                task.AIChannels.CreateResistanceChannel(Modules[module] + "/" + ai, "res",
                    0, 100, resistanceConfiguration, excitationSource, 0.001, AIResistanceUnits.Ohms);

                task.Timing.ConfigureSampleClock("", 120, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 60);
                var analogInReader = new AnalogMultiChannelReader(task.Stream);

                var result = analogInReader.ReadWaveform(1);
                rtdRealValue.RealResValue = (double)Math.Round(result[0].GetRawData().Average(), 2);

            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.ErrorFormat("RTDMonitor.ReadAI: {0}.", ex.Message);
                }

                rtdRealValue.RealTempValue = null;
                rtdRealValue.RealResValue = null;
            }
            finally
            {
                task.Dispose();
            }

            return rtdRealValue;
        }

        private RTDRealValue ReadAI1(int module, string ai, double r0)
        {
            var rtdType = AIRtdType.Pt3851;
            var resistanceConfiguration = AIResistanceConfiguration.ThreeWire;
            var excitationSource = AIExcitationSource.Internal;

            RTDRealValue rtdRealValue = new RTDRealValue();
            var task = new NationalInstruments.DAQmx.Task();

            try
            {             
                task = new NationalInstruments.DAQmx.Task();

                task.AIChannels.CreateResistanceChannel(Modules[module] + "/" + ai, "res",
                    0, 100, resistanceConfiguration, excitationSource, 0.001, AIResistanceUnits.Ohms);

                task.Timing.ConfigureSampleClock("", 120, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 60);
                var analogInReader = new AnalogMultiChannelReader(task.Stream);

                var result = analogInReader.ReadWaveform(1);
                rtdRealValue.RealResValue = (double)Math.Round(result[0].GetRawData().Average(), 2);

            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.ErrorFormat("RTDMonitor.ReadAI: {0}.", ex.Message);
                }

                rtdRealValue.RealTempValue = null;
                rtdRealValue.RealResValue = null;
            }
            finally
            {
                task.Dispose();
            }

            return rtdRealValue;
        }

        private RTDRealValue ReadAI2(int module, string ai, double r0)
        {
            var rtdType = AIRtdType.Pt3851;
            var resistanceConfiguration = AIResistanceConfiguration.ThreeWire;
            var excitationSource = AIExcitationSource.Internal;

            RTDRealValue rtdRealValue = new RTDRealValue();

            var task = new NationalInstruments.DAQmx.Task();

            try
            {
                task = new NationalInstruments.DAQmx.Task();

                task.AIChannels.CreateResistanceChannel(Modules[module] + "/" + ai, "res",
                    0, 100, resistanceConfiguration, excitationSource, 0.001, AIResistanceUnits.Ohms);

                task.Timing.ConfigureSampleClock("", 120, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 60);
                var analogInReader = new AnalogMultiChannelReader(task.Stream);

                var result = analogInReader.ReadWaveform(1);
                rtdRealValue.RealResValue = (double)Math.Round(result[0].GetRawData().Average(), 2);

            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.ErrorFormat("RTDMonitor.ReadAI: {0}.", ex.Message);
                }

                rtdRealValue.RealTempValue = null;
                rtdRealValue.RealResValue = null;
            }
            finally
            {
                task.Dispose();
            }

            return rtdRealValue;
        }

        protected override void SendMonitorValues(object value)
        {
            IsStable = ((RTDValue)value).IsStable;
            base.SendMonitorValues(value);
        }

    }


    public class RTDValue
    {
        public List<Rtd> Rtds { get; set; }
        public ResultRTDValue CalibrationRTD { get; set; }
        public ResultRTDValue EnvironmentRTD { get; set; }
        public bool IsStable { get; set; }

        public RTDValue()
        {
            CalibrationRTD = new ResultRTDValue();
            EnvironmentRTD = new ResultRTDValue();
        }
    }

    public class ResultRTDValue
    {
        public double Average { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }
        public double Uncertainty { get; set; }
        public double Difference { get; set; }
    }

    internal class RTDRealValue
    {
        public double? RealTempValue { get; set; }
        public double? RealResValue { get; set; }
    }

}
