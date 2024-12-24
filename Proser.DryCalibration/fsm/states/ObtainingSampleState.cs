using log4net;
using Proser.DryCalibration.controller.data.model;
using Proser.DryCalibration.controller.interfaces;
using Proser.DryCalibration.controller.ultrasonic;
using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.fsm.interfaces;
using Proser.DryCalibration.log;
using Proser.DryCalibration.monitor.exceptions;
using Proser.DryCalibration.Report;
using Proser.DryCalibration.sensor.rtd;
using Proser.DryCalibration.sensor.rtd.calibration;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.configuration;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using Proser.DryCalibration.util;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Proser.DryCalibration.fsm.states
{
    public class ObtainingSampleState : IState, ITimerControl
    {
        private static readonly ConcurrentDictionary<int, List<Rtd>> StaticTemperatureDetails = new ConcurrentDictionary<int, List<Rtd>>();

        private readonly object objLock = new object();
        private const int SAMPLE_INTERVAL = 30000; // 30 segundos

        private readonly double UP_TEMP;
        private readonly double UP_PRESS;

        public event ExitStateHandler ExitState;
        public event Action<string> RefreshState;
        public event Action<string> ElapsedTimeControl;
        public event Action ObtainingSampleFinished;
        public event Action<List<Sample>, Sample> GenerateCsv;
       
        private IController rtdController;
        private IController pressureController;
        private IController ultrasonicController;
     
        private bool AllSamplesObtained;
        private bool ContinueToNextState;
        private int rtdCount;
        private int rtdEnvCount;
        private ILog logger;

        public CancellationTokenSource token { get; private set; }
        public FSMState Name { get; private set; }
        public string Description { get; private set; }
        public System.Timers.Timer TimerControl { get; set; }

        private static  UltrasonicModel ultrasonicModel;

        public int SampleNumber { get; set; }
        public List<Sample> Samples{ get; private set; }
        public Sample Averages { get; private set; }
        
        public ObtainingSampleState()
        {
            token = new CancellationTokenSource();
            this.Name = FSMState.OBTAINING_SAMPLES;
            this.Description = "Obteniendo muestras...";

            this.UP_TEMP = 0.09;
            //this.UP_PRESS = 0.019;
        }

        public ObtainingSampleState(IController rtdController, IController pressureController, IController ultrasonicController, double up_press)
            : this()
        {
            UP_PRESS = up_press;
            string path = Path.Combine(Utils.ConfigurationPath, "RtdCalibration.xml");
            RtdTable rtdCal = RtdTable.Read(path);

            this.rtdCount = rtdCal.RtdSensors.Where(s => s.Active == 1 && s.Number < 10).Count(); // total de rtd usadas en el ensayo (sin contar las de ambiente)
            this.rtdEnvCount = rtdCal.RtdSensors.Where(s => s.Active == 1 && s.Number >= 10).Count(); // total de rtd usadas en el ensayo para el ambiente

            this.rtdController = rtdController;
            this.pressureController = pressureController;
            this.ultrasonicController = ultrasonicController;
      
            Samples = new List<Sample>(); // muestras obtenidas
            SampleNumber = 0; // número de muestras obtenidas

            TimerControl = new System.Timers.Timer(SAMPLE_INTERVAL);
            TimerControl.Elapsed += TimerControl_Elapsed;
        }
       
        public void Execute()
        {
            try
            {
                ultrasonicController.Initialize();
                ultrasonicController.UpdateSensorReceived += UltrasonicController_UpdateSensorReceived;
            }
            catch (MonitorInitializationException e)
            {
                RefreshState?.Invoke("Ocurrió un error al iniciar el monitor ultrasónico.");
                ExitState?.Invoke(FSMState.ERROR);
                return;
            }
      
            Thread th = new Thread(new ThreadStart(excecuteTh));
            th.Start();    
        }

        public void ContinueToValidatingState()
        {
            ContinueToNextState = true;
        }

        private void UltrasonicController_UpdateSensorReceived(monitor.enums.MonitorType monitorType, object value)
        {
            ultrasonicController.UpdateSensorReceived -= UltrasonicController_UpdateSensorReceived;
            ultrasonicModel = ((UltrasonicController)ultrasonicController).UltrasonicModel;

            SampleNumber = 1;
            ElapsedTimeControl?.Invoke(SampleNumber.ToString());

            InitTimerControl();
        }

        private void excecuteTh()
        {
            AllSamplesObtained = false;
            ContinueToNextState = false;

            do
            {
                if (AllSamplesObtained)
                {
                    AllSamplesObtained = false;
                    ObtainingSampleFinished?.Invoke();
                }

                if (ContinueToNextState)
                {
                    Thread.Sleep(1000);
                    ExitState?.Invoke(FSMState.VALIDATING);
                    break;
                }

                Thread.Sleep(200);
            } while (!token.IsCancellationRequested);
        }

        private void CalculateAverages()
        {
            Averages = new Sample();


            Averages.CalibrationTemperature.Difference = Samples.Average(d => d.CalibrationTemperature.Difference);
            Averages.CalibrationTemperature.Value = Samples.Average(t => t.CalibrationTemperature.Value);
            //Averages.CalibrationTemperature.Uncertainty = Utils.CalculateUncertainty(
            //    Utils.CalculateStandardDeviation(Samples.Select(s => s.CalibrationTemperature.Value).ToList()), (1d / 100d), (rtdCount * 10), UP_TEMP);

            var resultadoMinMax = Utils.CalculateMinMaxRtd(Samples); // Obtenemos matriz min max por rtd
            var udif = Utils.CalculateUDif(resultadoMinMax);

            List<double> temperaturas = new List<double>();
            foreach (var item in Samples)
            {
                int contador = 1;
                foreach (var temperatura in item.TemperatureDetail)
                {
                    if (temperatura.TempValue != 0 && contador <= rtdCount)
                    {
                        temperaturas.Add(temperatura.TempValue);
                    }
                    contador = contador + 1;
                }
            }

            //Averages.Uat = Utils.CalculateStandardDeviation(Samples.Select(s => s.CalibrationTemperature.Value).ToList()) / Math.Sqrt((rtdCount * 10));
            //Averages.Uat = Utils.CalculateStandardDeviation(temperaturas) / Math.Sqrt((rtdCount * 10));
            Averages.CalibrationTemperature.Uncertainty = Utils.CalculateUncertainty(Utils.CalculateStandardDeviation(temperaturas), Utils.CalculateUncertaintyRes(rtdCount), (rtdCount * 10), Utils.CalculateUncertaintyUP(rtdCount), udif);
            //Averages.CalibrationTemperature.Uncertainty = Utils.CalculateUncertainty(
            //    Utils.CalculateStandardDeviation(Samples.Select(s => s.CalibrationTemperature.Value).ToList()), Utils.CalculateUncertaintyRes(rtdCount), (rtdCount * 10), Utils.CalculateUncertaintyUP(rtdCount), udif);



            Averages.EnvirontmentTemperature.Difference = Samples.Average(d => d.EnvirontmentTemperature.Difference);
            Averages.EnvirontmentTemperature.Value = Samples.Average(t => t.EnvirontmentTemperature.Value);
            Averages.EnvirontmentTemperature.Uncertainty = Utils.CalculateUncertainty(
                Utils.CalculateStandardDeviation(Samples.Select(s => s.EnvirontmentTemperature.Value).ToList()), (1d / 100d), (rtdEnvCount * 10), UP_TEMP);

            Averages.Uap = Utils.CalculateStandardDeviation(Samples.Select(s => s.PressureValue).ToList()) / Math.Sqrt((10));
            Averages.PressureValue = Samples.Average(p => p.PressureValue);
            Averages.PressureUncertainty = Utils.CalculateUncertainty(
                Utils.CalculateStandardDeviation(Samples.Select(s => s.PressureValue).ToList()), (1d / 100d), 10, UP_PRESS);

            RopeValue ropeAvg = new RopeValue();

            switch (ultrasonicModel)
            {
                case UltrasonicModel.Daniel:
                case UltrasonicModel.Sick:
                case UltrasonicModel.FMU:
                    ropeAvg = calculateRopeAvg("A");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("B");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("C");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("D");
                    Averages.Ropes.Add(ropeAvg);
                    break;
                case UltrasonicModel.DanielJunior1R:
                    ropeAvg = calculateRopeAvg("A");
                    Averages.Ropes.Add(ropeAvg);
                    break;
                case UltrasonicModel.DanielJunior2R:
                    ropeAvg = calculateRopeAvg("A");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("B");
                    Averages.Ropes.Add(ropeAvg);
                    break;
                case UltrasonicModel.InstrometS5:
                    ropeAvg = calculateRopeAvg("A");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("B");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("C");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("D");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("E");
                    Averages.Ropes.Add(ropeAvg);
                    //ropeAvg = calculateRopeAvg("F");
                    //Averages.Ropes.Add(ropeAvg);
                    break;
                case UltrasonicModel.InstrometS6:
                    ropeAvg = calculateRopeAvg("A");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("B");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("C");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("D");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("E");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("F");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("G");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("H");
                    Averages.Ropes.Add(ropeAvg);
                    break;
                case UltrasonicModel.KrohneAltosonicV12:
                    ropeAvg = calculateRopeAvg("A");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("B");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("C");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("D");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("E");
                    Averages.Ropes.Add(ropeAvg);
                    ropeAvg = calculateRopeAvg("F");
                    Averages.Ropes.Add(ropeAvg);
                    break;
            }
        }

        private RopeValue calculateRopeAvg(string name)
        {
            double FlowAvg = Samples.Average(r => r.Ropes.Find(f => f.Name == name).FlowSpeedValue);
            double DeviationFlow = Utils.CalculateStandardDeviation(Samples.Select(s => s.Ropes.Find( f => f.Name == name).FlowSpeedValue).ToList());
            double SoundAvg = Samples.Average(r => r.Ropes.Find(f => f.Name == name).SoundSpeedValue);
            double DeviationSound = Utils.CalculateStandardDeviation(Samples.Select(s => s.Ropes.Find( f => f.Name == name).SoundSpeedValue).ToList());
          
            List<double> gains = Samples.Select(s => s.Ropes.Find(f => f.Name == name).GainValues.T1).ToList();
            gains.AddRange(Samples.Select(s => s.Ropes.Find(f => f.Name == name).GainValues.T2).ToList());
                    
            double GainAvg = gains.Average();       
            double DeviationGain = Utils.CalculateStandardDeviation(gains);

            double EffAvg = Samples.Average(r => r.Ropes.Find(f => f.Name == name).EfficiencyValue);

            RopeValue ropeAvg = new RopeValue()
            {
                Name = name,              
                FlowSpeedValue = FlowAvg,
                DeviationFlowSpeed = DeviationFlow,               
                SoundSpeedValue = SoundAvg,
                DeviationSoundSpeed = DeviationSound,               
                GainValue = GainAvg,
                DeviationGain = DeviationGain,               
                EfficiencyValue = (int)EffAvg
            };

            return ropeAvg;
        }

        public void Dispose()
        {
            token.Cancel();

            //Dispose controllers
            rtdController.Monitor.StopMonitor();
            pressureController.Monitor.StopMonitor();

            if (ultrasonicController.Monitor != null)
            {
                ultrasonicController.Monitor.StopMonitor();
            }

            //Dispose timer
            TimerControl.Close();
            TimerControl.Dispose();
        }
     
        public void InitTimerControl()
        {
            TimerControl.Start();
        }

        public void StopTimerControl()
        {
            TimerControl.Enabled = false;
        }

        public void AddCurrentSample(Sample sample)
        {

            lock (objLock)
            {
                var match = Samples.Find(f => f.Number == sample.Number);

                if (match == null)
                {
                    var clonedDetails = sample.TemperatureDetail
                        .Select(detail => new Rtd(detail.Number, detail.ResPoints, 100)
                        {
                            TempValue = detail.TempValue,
                            ValueObtained = detail.ValueObtained
                        }).ToList();

                    StaticTemperatureDetails[sample.Number] = clonedDetails;

                    Sample s = new Sample()
                    {
                        CalibrationTemperature = sample.CalibrationTemperature,
                        EnvirontmentTemperature = sample.EnvirontmentTemperature,
                        Number = sample.Number,
                        PressureValue = sample.PressureValue,
                        TemperatureDetail = clonedDetails
                    };


                    //s.TemperatureDetail.AddRange(sample.TemperatureDetail);
//                    foreach (var detail in sample.TemperatureDetail)
//                    {
//#if DEBUG
//                        s.TemperatureDetail.Add(new Rtd(detail.Number, detail.TempValue)
//                        {
//                            ResPoints = detail.ResPoints,
//                            ValueObtained = detail.ValueObtained
//                        });
//#else
//                        s.TemperatureDetail.Add(new Rtd(detail.Number, detail.ResPoints, 100)
//                        {
//                            TempValue = detail.TempValue,
//                            ValueObtained = detail.ValueObtained
//                        });
//#endif
//                    }


                    foreach (RopeValue r in sample.Ropes)
                    {
                        RopeValue rv = new RopeValue()
                        {
                            DeviationFlowSpeed = r.DeviationFlowSpeed,
                            DeviationSoundSpeed = r.DeviationSoundSpeed,
                            EfficiencyValue = r.EfficiencyValue,
                            GainValues = new GainValue() { T1 = r.GainValues.T1, T2 = r.GainValues.T2 },
                            FlowSpeedValue = r.FlowSpeedValue,
                            Name = r.Name,
                            SoundSpeedValue = r.SoundSpeedValue
                        };

                        s.Ropes.Add(rv);
                    }

                    Samples.Add(s);
                }
            }
        }

        private static Mutex mutex = new Mutex();
      
        private void TimerControl_Elapsed(object sender, ElapsedEventArgs e)
        {
            mutex.WaitOne();

            SampleNumber++;

            if(SampleNumber == 10)
            {
                ElapsedTimeControl?.Invoke(SampleNumber.ToString());

                TimerControl.Enabled = false;
                TimerControl.Interval = 3000;
                TimerControl.Start();

                mutex.ReleaseMutex();
                return;
            }

            if (SampleNumber == 11)
            {
                //calcular promedios
                RefreshState?.Invoke("Calculando promedios...");

                CalculateAverages();
         
                ElapsedTimeControl?.Invoke(SampleNumber.ToString());

                mutex.ReleaseMutex();
                return;
            }

            if (SampleNumber == 12)
            {
                StopTimerControl();

                //generar csv
                GenerateCsv?.Invoke(Samples, Averages);

                RefreshState?.Invoke("Todas las muestras fueron obtenidas.");

                AllSamplesObtained = true;

                mutex.ReleaseMutex();
                return;
            }

            ElapsedTimeControl?.Invoke(SampleNumber.ToString());

            mutex.ReleaseMutex();

        }
    }



    public class Sample
    {
        public int Number { get; set; }

        public SampleTemperature CalibrationTemperature { get; set; }
        public SampleTemperature EnvirontmentTemperature { get; set; }
        
        public double PressureValue { get; set; }
        public double PressureUncertainty { get; set; }

        public List<Rtd> TemperatureDetail { get; set; }
        public List<RopeValue> Ropes { get; set; }
        public double Uat { get; set; }
        public double Uap { get; set; }

        public Sample()
        {
            TemperatureDetail = new List<Rtd>();
            Ropes = new List<RopeValue>();
            CalibrationTemperature = new SampleTemperature();
            EnvirontmentTemperature = new SampleTemperature();
        }

        public override string ToString()
        {
            string result = "";

            foreach (RopeValue item in Ropes)
            {
                result += string.Format("Cuerda={0}, FlowSpeed={1}, SoundSpeed={2}" + Environment.NewLine, item.Name, item.FlowSpeedValue.ToString(), item.SoundSpeedValue.ToString());
            }

            return result;
        }
    }

    public class SampleTemperature
    {
        public double Value { get; set; }
        public double Difference { get; set; }
        public double Uncertainty { get; set; }
    }


   

}
