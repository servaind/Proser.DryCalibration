using NationalInstruments.DAQmx;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.exceptions;
using Proser.DryCalibration.sensor.interfaces;
using Proser.DryCalibration.sensor.pressure;
using Proser.DryCalibration.sensor.pressure.calibration;
using System;
using System.Threading;

namespace Proser.DryCalibration.monitor
{
    public class PressureMonitor : MonitorBase
    {
        ISensor sensor;
        private PressureCalibration calibration;
        private PressureValue pressureValue;


        public PressureMonitor(PressureCalibration calibration)
        {
            this.calibration = calibration;
            this.Type = enums.MonitorType.Pressure;
            this.pressureValue = new PressureValue();
        }

        public override void LoadSensor()
        {
            sensor = new PressureSensor(this.calibration);
            sensor.Init();     
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

                System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
                stopWatch.Start();

                decimal? value = ReadAI(sensor.Module, sensor.AI);

                sensor.Update(value);

                pressureValue.Number = sensor.Number;
                pressureValue.Value = Convert.ToDouble(sensor.Value);

                Console.WriteLine("Pressure: {0}: {1} bar", sensor.Number, sensor.Value);

                stopWatch.Stop();

                // Get the elapsed time as a TimeSpan value.
                TimeSpan ts = stopWatch.Elapsed;

                // Format and display the TimeSpan value.
                string elapsedTime = String.Format("{0:00}.{1:00}",
                    ts.Seconds,
                    ts.Milliseconds / 10);
                Console.WriteLine("Time elapsed: " + elapsedTime);
            }

            // send values
            SendMonitorValues(pressureValue);

            // send statistic
            SendMonitorStatistics();

        }

        public override decimal? ReadAI(int module, string ai)
        {
            NationalInstruments.DAQmx.Task task = new NationalInstruments.DAQmx.Task();

            double minRangeCurrent = (double)((PressureSensor)sensor).MinRangeCurrent;
            double maxRangeCurrent = (double)((PressureSensor)sensor).MaxRangeCurrent;

            task.AIChannels.CreateCurrentChannel(
                             Modules[module] + "/" + ai, //"dev1/ai0",
                             "",
                             AITerminalConfiguration.Differential,
                             minRangeCurrent,//.004, //4 ma
                             maxRangeCurrent,// 02, //20 ma
                             AICurrentUnits.Amps);

            task.Timing.ConfigureSampleClock("", 1.95, SampleClockActiveEdge.Rising, SampleQuantityMode.ContinuousSamples, 10);

            AnalogSingleChannelReader reader = new AnalogSingleChannelReader(task.Stream);    

            try
            {
                double result = reader.ReadSingleSample();
                return (decimal)Math.Round(result, 5);
            }
            catch (Exception ex)
            {

                if (logger != null)
                {
                    logger.ErrorFormat("ReadAI: {0}.", ex.Message);
                }

                return null;
            }
            finally
            {
                task.Dispose();
            }

        }
    }


    public class PressureValue
    {
        public int Number { get; set; }
        public double Value { get; set; }
    }

}
