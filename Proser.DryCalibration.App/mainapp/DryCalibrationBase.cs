using log4net;
using Proser.DryCalibration.controller.interfaces;
using Proser.DryCalibration.fsm;
using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.fsm.interfaces;
using Proser.DryCalibration.fsm.states;
using Proser.DryCalibration.monitor;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.statistic;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.configuration;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Proser.DryCalibration.Report;
using Proser.DryCalibration.measuring;
using System.IO;
using Proser.DryCalibration.fsm.exceptions;
using System.Text.RegularExpressions;
using Proser.DryCalibration.sensor.pressure.calibration;
using Proser.DryCalibration.util;
using Proser.DryCalibration.controller.enums;
using System.Text;

namespace Proser.DryCalibration.App.mainapp
{
    public class DryCalibrationBase : IDryCalibrationProcess
    {
        public event Action<MonitorType, StatisticValue> StatisticReceived;
        public event Action<MonitorType, object> UpdateSensorReceived;
        public event Action<string> RefreshState;
        public event Action DryCalibrationAborted;
        public event Action<FSMState, UltrasonicModel> DryCalibrationStateChange;
        public event Action<int, UltrasonicModel> SampleObtained;
        public event Action ObtainingSampleFinished;
        public event Action<ValidatedResult, UltrasonicModel> ValidationFinished;

        protected DryCalibrationProcess dryCalProcess;

        public IController rtdController;
        protected IController pressureController;
        protected IController ultrasonicController;

        public bool ProcessingStartUp { get; set; }
        public bool ProcessingDaqModuleAdjustament { get; set; }
        public bool ProcessingDryCalibration { get; set; }

        public ModbusConfiguration CurrentModbusConfiguration { get; private set; }
        public PressureCalibration CurrentPressureCalibration { get; private set; }

        public RTDValue RtdVal { get; private set; }
        protected PressureValue PressureVal { get; private set; }
        public UltrasonicValue UltrasonicVal { get; private set; }

        public IState CurrentState { get { return dryCalProcess.CurrentState; } }

        public int CurrentSecondsTimeProcess { get; internal set; }
        public string CurrentFullReportPath { get; private set; }
        public ReportModel CurrentReportModel { get; private set; }

        public DryCalibrationBase()
        {
            SetCurrentConfiguration();
        }

        public void Initialize()
        {
           
            UltSampMode ultSampMode = UltSampMode.Automatic;

            try
            {
                ultSampMode = (UltSampMode)CurrentModbusConfiguration.UltrasonicSampleMode;
            }
            catch { }

            dryCalProcess = new DryCalibrationProcess(ultSampMode);

            dryCalProcess.UpdateSensorReceived += DryCalibration_UpdateSensorReceived;
            dryCalProcess.StatisticReceived += DryCalibration_StatisticReceived;
            dryCalProcess.RefreshState += DryCalProcess_RefreshState;
            dryCalProcess.DryCalibrationAborted += DryCalProcess_DryCalibrationAborted;
            dryCalProcess.DryCalibrationStateChange += DryCalProcess_DryCalibrationStateChange;
            dryCalProcess.SampleObtained += DryCalProcess_SampleObtained;
            dryCalProcess.ObtainingSampleFinished += DryCalProcess_ObtainingSampleFinished;
            dryCalProcess.ValidationFinished += DryCalProcess_ValidationFinished;
            dryCalProcess.GenerateReportSucceeded += DryCalProcess_GenerateReportSucceeded;
            dryCalProcess.GenerateCsv += DryCalProcess_GenerateCsv;

            dryCalProcess.CurrentReportModel = CurrentReportModel;
            dryCalProcess.CurrentPressureCalibration = CurrentPressureCalibration;


            this.dryCalProcess.Initialize();
        }

       
        public void SetCurrentConfiguration()
        {
            // crear la ruta de configuración
            if (!Directory.Exists(Utils.ConfigurationPath))
            {
                Directory.CreateDirectory(Utils.ConfigurationPath);
            }

            // sensor ultrasónico
            string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");

            if (!File.Exists(path))
            {
                ModbusConfiguration.Generate(path);
            }

            CurrentModbusConfiguration = ModbusConfiguration.Read(path);

            // mapas modbus
            MapUltranicModel();

            // sensor de presión
            path = Path.Combine(Utils.ConfigurationPath, "PressureCalibration.xml");

            if (!File.Exists(path))
            {
                PressureCalibration.Generate(path, "0.0.0.0");
            }

            CurrentPressureCalibration = PressureCalibration.Read(path);
        }

        private void MapUltranicModel()
        {
            // crear la ruta de configuración

            if (!Directory.Exists(Utils.ModbusMapPath))
            {
                Directory.CreateDirectory(Utils.ModbusMapPath);
            }

            foreach (UltrasonicModel model in (UltrasonicModel[])Enum.GetValues(typeof(UltrasonicModel)))
            {
                string file = model.ToString() + ".xml";
                string path = Path.Combine(Utils.ModbusMapPath, file);

                if (!File.Exists(path))
                {
                    ModbusMapConfig.Generate(path, model);
                }
            }
        }

        public void InitDryCalibration()
        {
            ProcessingDryCalibration = true;
            this.dryCalProcess.InitDryCalibration();
        }

        public void CancelDryCalibration()
        {
            ProcessingDryCalibration = false;
            this.dryCalProcess.CancelDryCalibration();
        }

        public void AbortDryCalibration()
        {
            ProcessingDryCalibration = false;
            this.dryCalProcess.AbortDryCalibration();
        }

        public void InitDryCalibrationValidation()
        {
            if (CurrentModbusConfiguration.UltrasonicSampleMode == (int)UltSampMode.Automatic)
            {
                ((ObtainingSampleState)CurrentState).ContinueToValidatingState();
            }
            else 
            {
                ((ObtainingManualSampleState)CurrentState).ContinueToValidatingState();
            }
        }

        public void InitReportGeneration()
        {
            ((ValidatingState)CurrentState).ContinueToGeneratingReportState();
        }

        public void GenerateReport(params TextBox[] calibMeasValues)
        {
            CalibrationMeasuring calibrationMeasuring = new CalibrationMeasuring();

            List<ReportMeasuringInstrument> measuringInstruments = new List<ReportMeasuringInstrument>();

            for (int i = 1; i <= 13; i+=3)
            {
                if (!string.IsNullOrEmpty(calibMeasValues[i].Text)
                    && !string.IsNullOrEmpty(calibMeasValues[i + 1].Text)
                    && !string.IsNullOrEmpty(calibMeasValues[i + 2].Text))
                {
                    measuringInstruments.Add(new ReportMeasuringInstrument()
                    {
                        BrandName = calibMeasValues[i].Text,
                        InternalIdentification = calibMeasValues[i + 1].Text,
                        CalibrationCode = calibMeasValues[i + 2].Text
                    });
                }
            }

            calibrationMeasuring.MeasuringInstruments.AddRange(measuringInstruments);
            calibrationMeasuring.Observations = calibMeasValues[0].Text;

            string currentPressureSensorType = CurrentPressureCalibration.SensorType == 0 ? "A" : "";

            ((GeneratingReportState)CurrentState).GenerateReport(calibrationMeasuring, CurrentSecondsTimeProcess, currentPressureSensorType);
        }

        public void SetSampleTableConfiguration(params Grid[] sampleTables)
        {
            string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");
            ModbusConfiguration configuration = ModbusConfiguration.Read(path);

            //cuerdas según modelo del ultrasónico
            SetControlVisibility(sampleTables[0], Visibility.Hidden);
            SetControlVisibility(sampleTables[1], Visibility.Hidden);
            SetControlVisibility(sampleTables[2], Visibility.Hidden);
            SetControlVisibility(sampleTables[3], Visibility.Hidden);
            SetControlVisibility(sampleTables[4], Visibility.Hidden);
            SetControlVisibility(sampleTables[5], Visibility.Hidden);
            SetControlVisibility(sampleTables[6], Visibility.Hidden);
            SetControlVisibility(sampleTables[7], Visibility.Hidden);

            //promedios según modelo del ultrasónico
            SetControlVisibility(sampleTables[8], Visibility.Hidden);
            SetControlVisibility(sampleTables[9], Visibility.Hidden);
            SetControlVisibility(sampleTables[10], Visibility.Hidden);
            SetControlVisibility(sampleTables[11], Visibility.Hidden);
            SetControlVisibility(sampleTables[12], Visibility.Hidden);
            SetControlVisibility(sampleTables[13], Visibility.Hidden);
            SetControlVisibility(sampleTables[14], Visibility.Hidden);
            SetControlVisibility(sampleTables[15], Visibility.Hidden);

            //eficiencia según modelo del ultrasónico
            SetControlVisibility(sampleTables[16], Visibility.Hidden);
            SetControlVisibility(sampleTables[17], Visibility.Hidden);
            SetControlVisibility(sampleTables[18], Visibility.Hidden);
            SetControlVisibility(sampleTables[19], Visibility.Hidden);
            SetControlVisibility(sampleTables[20], Visibility.Hidden);
            SetControlVisibility(sampleTables[21], Visibility.Hidden);
            SetControlVisibility(sampleTables[22], Visibility.Hidden);
            SetControlVisibility(sampleTables[23], Visibility.Hidden);

            //nivel de ganancia según modelo del ultrasónico
            SetControlVisibility(sampleTables[24], Visibility.Hidden);
            SetControlVisibility(sampleTables[25], Visibility.Hidden);
            SetControlVisibility(sampleTables[26], Visibility.Hidden);
            SetControlVisibility(sampleTables[27], Visibility.Hidden);
            SetControlVisibility(sampleTables[28], Visibility.Hidden);
            SetControlVisibility(sampleTables[29], Visibility.Hidden);
            SetControlVisibility(sampleTables[30], Visibility.Hidden);
            SetControlVisibility(sampleTables[31], Visibility.Hidden);

            switch (configuration.SlaveConfig.Model)
            {
                case (int)UltrasonicModel.Daniel:
                    SetControlVisibility(sampleTables[0], Visibility.Visible);
                    SetControlVisibility(sampleTables[8], Visibility.Visible);
                    SetControlVisibility(sampleTables[16], Visibility.Visible);
                    SetControlVisibility(sampleTables[24], Visibility.Visible);
                    break;
                case (int)UltrasonicModel.DanielJunior1R:
                    SetControlVisibility(sampleTables[1], Visibility.Visible);
                    SetControlVisibility(sampleTables[9], Visibility.Visible);
                    SetControlVisibility(sampleTables[17], Visibility.Visible);
                    SetControlVisibility(sampleTables[25], Visibility.Visible);
                    break;
                case (int)UltrasonicModel.DanielJunior2R:
                    SetControlVisibility(sampleTables[2], Visibility.Visible);
                    SetControlVisibility(sampleTables[10], Visibility.Visible);
                    SetControlVisibility(sampleTables[18], Visibility.Visible);
                    SetControlVisibility(sampleTables[26], Visibility.Visible);
                    break;
                case (int)UltrasonicModel.FMU:
                    SetControlVisibility(sampleTables[3], Visibility.Visible);
                    SetControlVisibility(sampleTables[11], Visibility.Visible);
                    SetControlVisibility(sampleTables[19], Visibility.Visible);
                    SetControlVisibility(sampleTables[27], Visibility.Visible);
                    break;
                case (int)UltrasonicModel.Sick:
                    SetControlVisibility(sampleTables[4], Visibility.Visible);
                    SetControlVisibility(sampleTables[12], Visibility.Visible);
                    SetControlVisibility(sampleTables[20], Visibility.Visible);
                    SetControlVisibility(sampleTables[28], Visibility.Visible);
                    break;
                case (int)UltrasonicModel.InstrometS5:
                    SetControlVisibility(sampleTables[5], Visibility.Visible);
                    SetControlVisibility(sampleTables[13], Visibility.Visible);
                    SetControlVisibility(sampleTables[21], Visibility.Visible);
                    SetControlVisibility(sampleTables[29], Visibility.Visible);
                    break;
                case (int)UltrasonicModel.InstrometS6:
                    SetControlVisibility(sampleTables[6], Visibility.Visible);
                    SetControlVisibility(sampleTables[14], Visibility.Visible);
                    SetControlVisibility(sampleTables[22], Visibility.Visible);
                    SetControlVisibility(sampleTables[30], Visibility.Visible);
                    break;
                case (int)UltrasonicModel.KrohneAltosonicV12:
                    SetControlVisibility(sampleTables[7], Visibility.Visible);
                    SetControlVisibility(sampleTables[15], Visibility.Visible);
                    SetControlVisibility(sampleTables[23], Visibility.Visible);
                    SetControlVisibility(sampleTables[31], Visibility.Visible);
                    break;
                default:
                    SetControlVisibility(sampleTables[0], Visibility.Visible);
                    SetControlVisibility(sampleTables[8], Visibility.Visible);
                    SetControlVisibility(sampleTables[16], Visibility.Visible);
                    SetControlVisibility(sampleTables[24], Visibility.Visible);
                    break;
            }         

        }

        public void SetValidatingTableConfiguration(params Grid[] validatingTables)
        {
            string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");
            ModbusConfiguration configuration = ModbusConfiguration.Read(path);

            //promedios según modelo del ultrasónico
            SetControlVisibility(validatingTables[0], Visibility.Hidden);
            SetControlVisibility(validatingTables[1], Visibility.Hidden);
            SetControlVisibility(validatingTables[2], Visibility.Hidden);
            SetControlVisibility(validatingTables[3], Visibility.Hidden);
            SetControlVisibility(validatingTables[4], Visibility.Hidden);
            SetControlVisibility(validatingTables[5], Visibility.Hidden);
            SetControlVisibility(validatingTables[6], Visibility.Hidden);
            SetControlVisibility(validatingTables[7], Visibility.Hidden);

            //error porcentual según modelo del ultrasónico
            SetControlVisibility(validatingTables[8], Visibility.Hidden);
            SetControlVisibility(validatingTables[9], Visibility.Hidden);
            SetControlVisibility(validatingTables[10], Visibility.Hidden);
            SetControlVisibility(validatingTables[11], Visibility.Hidden);
            SetControlVisibility(validatingTables[12], Visibility.Hidden);
            SetControlVisibility(validatingTables[13], Visibility.Hidden);
            SetControlVisibility(validatingTables[14], Visibility.Hidden);
            SetControlVisibility(validatingTables[15], Visibility.Hidden);

            //eficiencia por cuerda según modelo del ultrasónico
            SetControlVisibility(validatingTables[16], Visibility.Hidden);
            SetControlVisibility(validatingTables[17], Visibility.Hidden);
            SetControlVisibility(validatingTables[18], Visibility.Hidden);
            SetControlVisibility(validatingTables[19], Visibility.Hidden);
            SetControlVisibility(validatingTables[20], Visibility.Hidden);
            SetControlVisibility(validatingTables[21], Visibility.Hidden);
            SetControlVisibility(validatingTables[22], Visibility.Hidden);
            SetControlVisibility(validatingTables[23], Visibility.Hidden);

            //nivel de ganancia por cuerda según modelo del ultrasónico
            SetControlVisibility(validatingTables[24], Visibility.Hidden);
            SetControlVisibility(validatingTables[25], Visibility.Hidden);
            SetControlVisibility(validatingTables[26], Visibility.Hidden);
            SetControlVisibility(validatingTables[27], Visibility.Hidden);
            SetControlVisibility(validatingTables[28], Visibility.Hidden);
            SetControlVisibility(validatingTables[29], Visibility.Hidden);
            SetControlVisibility(validatingTables[30], Visibility.Hidden);
            SetControlVisibility(validatingTables[31], Visibility.Hidden);
    
            switch (configuration.SlaveConfig.Model)
            {
                case (int)UltrasonicModel.Daniel:
                    SetControlVisibility(validatingTables[0], Visibility.Visible);
                    SetControlVisibility(validatingTables[8], Visibility.Visible);
                    SetControlVisibility(validatingTables[16], Visibility.Visible);
                    SetControlVisibility(validatingTables[24], Visibility.Visible);

                    break;
                case (int)UltrasonicModel.DanielJunior1R:
                    SetControlVisibility(validatingTables[1], Visibility.Visible);
                    SetControlVisibility(validatingTables[9], Visibility.Visible);
                    SetControlVisibility(validatingTables[17], Visibility.Visible);
                    SetControlVisibility(validatingTables[25], Visibility.Visible);

                    break;
                case (int)UltrasonicModel.DanielJunior2R:
                    SetControlVisibility(validatingTables[2], Visibility.Visible);
                    SetControlVisibility(validatingTables[10], Visibility.Visible);
                    SetControlVisibility(validatingTables[18], Visibility.Visible);
                    SetControlVisibility(validatingTables[26], Visibility.Visible);

                    break;
                case (int)UltrasonicModel.FMU:
                    SetControlVisibility(validatingTables[3], Visibility.Visible);
                    SetControlVisibility(validatingTables[11], Visibility.Visible);
                    SetControlVisibility(validatingTables[19], Visibility.Visible);
                    SetControlVisibility(validatingTables[27], Visibility.Visible);
                    break;
                case (int)UltrasonicModel.Sick:
                    SetControlVisibility(validatingTables[4], Visibility.Visible);
                    SetControlVisibility(validatingTables[12], Visibility.Visible);
                    SetControlVisibility(validatingTables[20], Visibility.Visible);
                    SetControlVisibility(validatingTables[28], Visibility.Visible);

                    break;
                case (int)UltrasonicModel.InstrometS5:
                    SetControlVisibility(validatingTables[5], Visibility.Visible);
                    SetControlVisibility(validatingTables[13], Visibility.Visible);
                    SetControlVisibility(validatingTables[21], Visibility.Visible);
                    SetControlVisibility(validatingTables[29], Visibility.Visible);

                    break;
                case (int)UltrasonicModel.InstrometS6:
                    SetControlVisibility(validatingTables[6], Visibility.Visible);
                    SetControlVisibility(validatingTables[14], Visibility.Visible);
                    SetControlVisibility(validatingTables[22], Visibility.Visible);
                    SetControlVisibility(validatingTables[30], Visibility.Visible);

                    break;
                case (int)UltrasonicModel.KrohneAltosonicV12:
                    SetControlVisibility(validatingTables[7], Visibility.Visible);
                    SetControlVisibility(validatingTables[15], Visibility.Visible);
                    SetControlVisibility(validatingTables[23], Visibility.Visible);
                    SetControlVisibility(validatingTables[31], Visibility.Visible);

                    break;
                default:
                    SetControlVisibility(validatingTables[0], Visibility.Visible);
                    SetControlVisibility(validatingTables[8], Visibility.Visible);
                    SetControlVisibility(validatingTables[16], Visibility.Visible);
                    SetControlVisibility(validatingTables[24], Visibility.Visible);

                    break;
            }

        }

        public void SetReportMeasurersTableConfiguration(params TextBox[] txtConfig)
        {
            string path = Path.Combine(Utils.ConfigurationPath, "MeasuringConfiguration.xml");

            if (!File.Exists(path))
            {
                MeasuringConfiguration.Generate(path);
            }

            MeasuringConfiguration configuration = MeasuringConfiguration.Read(path);

            // limpiar tabla
            foreach (TextBox textBox in txtConfig)
            {
                SetText(textBox, "");
            }

            int row = 0;
    
            foreach (MeasuringInstrument instrument in configuration.MeasuringInstruments)
            {
                string brandName = !string.IsNullOrEmpty(instrument.BrandName) ? instrument.BrandName : "";
                string internalIdentification = !string.IsNullOrEmpty(instrument.InternalIdentification) ? instrument.InternalIdentification : "";
                string calibrationCode = !string.IsNullOrEmpty(instrument.CalibrationCode) ? instrument.CalibrationCode : "";

                SetText(txtConfig[row], brandName);
                SetText(txtConfig[row + 1], internalIdentification);
                SetText(txtConfig[row + 2], calibrationCode);

                row += 3;

                if (row > 12)
                {
                    break; // desborde
                }
            }

            
        }

        private static Mutex mutex = new Mutex();
        public void UpdateObtainedSampleLayout(int sampleNumber, UltrasonicModel ultrasonicModel, params TextBox[] sampleValues)
        {

            //mutex.WaitOne();
            Sample curretSample = new Sample();
            curretSample.Number = sampleNumber;

            List<sensor.rtd.Rtd> tempRtds = new List<sensor.rtd.Rtd>();
            tempRtds.AddRange(RtdVal.Rtds);

            //double tempDiffValue = RtdVal.CalibrationRTD.Uncertainty;
            double tempDiffValue = RtdVal.CalibrationRTD.Difference;
            double tempValue = RtdVal.CalibrationRTD.Average;
            double tempEnvDiffValue = RtdVal.EnvironmentRTD.Difference;
            //double tempEnvDiffValue = RtdVal.EnvironmentRTD.Uncertainty;
            double tempEnvValue = RtdVal.EnvironmentRTD.Average;
            double pressValue = PressureVal.Value;

            SolidColorBrush succColor = Brushes.LightGreen;
            SolidColorBrush errColor = Brushes.LightCoral;

            // temperatura de calibaración
            curretSample.TemperatureDetail = tempRtds;
            curretSample.CalibrationTemperature.Difference = tempDiffValue;
            curretSample.CalibrationTemperature.Value = tempValue;

            // temperatura ambiente
            curretSample.EnvirontmentTemperature.Difference = tempEnvDiffValue;
            curretSample.EnvirontmentTemperature.Value = tempEnvValue;

            curretSample.PressureValue = pressValue;

            if (tempValue != -99)
            {
                SetText(sampleValues[0], tempValue);
            }
            else
            {
                SetText(sampleValues[0], "¡Error!");
                SetColor(sampleValues[0], errColor);
                throw new FSMStateProcessException("Ocurrió un error con los sensores de temperatura.");
            }

            if (pressValue != -99)
            {
                SetText(sampleValues[1], pressValue);
            }
            else
            {
                SetText(sampleValues[1], "¡Error!");
                SetColor(sampleValues[1], errColor);
                throw new FSMStateProcessException("Ocurrió un error con el sensor de presión.");
            }


            RopeValue ropeA = new RopeValue("A");
            RopeValue ropeB = new RopeValue("B");
            RopeValue ropeC = new RopeValue("C");
            RopeValue ropeD = new RopeValue("D");
            RopeValue ropeE = new RopeValue("E");
            RopeValue ropeF = new RopeValue("F");
            RopeValue ropeG = new RopeValue("G");
            RopeValue ropeH = new RopeValue("H");


            if (dryCalProcess.UltrasonicSampleMode == UltSampMode.Automatic) 
            {
                ropeA = UltrasonicVal.Ropes.Find(r => r.Name == "A");
                ropeB = UltrasonicVal.Ropes.Find(r => r.Name == "B");
                ropeC = UltrasonicVal.Ropes.Find(r => r.Name == "C");
                ropeD = UltrasonicVal.Ropes.Find(r => r.Name == "D");
                ropeE = UltrasonicVal.Ropes.Find(r => r.Name == "E");
                ropeF = UltrasonicVal.Ropes.Find(r => r.Name == "F");
                ropeG = UltrasonicVal.Ropes.Find(r => r.Name == "G");
                ropeH = UltrasonicVal.Ropes.Find(r => r.Name == "H");
            }


            int diffBySampIndex = 0;

            switch (ultrasonicModel)
            {
                case UltrasonicModel.Daniel:
                case UltrasonicModel.FMU:
                case UltrasonicModel.Sick:

                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                    curretSample.Ropes.Add(ropeC);
                    curretSample.Ropes.Add(ropeD);

                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);
                    SetText(sampleValues[4], ropeC.FlowSpeedValue);
                    SetText(sampleValues[5], ropeD.FlowSpeedValue);

                    SetText(sampleValues[6], ropeA.SoundSpeedValue);
                    SetText(sampleValues[7], ropeB.SoundSpeedValue);
                    SetText(sampleValues[8], ropeC.SoundSpeedValue);
                    SetText(sampleValues[9], ropeD.SoundSpeedValue);

                    diffBySampIndex = 10;

                    break;
                case UltrasonicModel.DanielJunior1R:
                    curretSample.Ropes.Add(ropeA);
             
                    SetText(sampleValues[2], ropeA.FlowSpeedValue);         
                    SetText(sampleValues[3], ropeA.SoundSpeedValue);
                   
                    diffBySampIndex = 4;
                    break;
                case UltrasonicModel.DanielJunior2R:
                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                
                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);
                   
                    SetText(sampleValues[4], ropeA.SoundSpeedValue);
                    SetText(sampleValues[5], ropeB.SoundSpeedValue);
                   
                    diffBySampIndex = 6;
                    break;
                case UltrasonicModel.InstrometS5:

                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                    curretSample.Ropes.Add(ropeC);
                    curretSample.Ropes.Add(ropeD);
                    curretSample.Ropes.Add(ropeE);
                    //curretSample.Ropes.Add(ropeF);

                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);
                    SetText(sampleValues[4], ropeC.FlowSpeedValue);
                    SetText(sampleValues[5], ropeD.FlowSpeedValue);
                    SetText(sampleValues[6], ropeE.FlowSpeedValue);
                    //SetText(stUpUltInst5FlowF, ropeF.FlowSpeedValue);

                    SetText(sampleValues[7], ropeA.SoundSpeedValue);
                    SetText(sampleValues[8], ropeB.SoundSpeedValue);
                    SetText(sampleValues[9], ropeC.SoundSpeedValue);
                    SetText(sampleValues[10], ropeD.SoundSpeedValue);
                    SetText(sampleValues[11], ropeE.SoundSpeedValue);
                    //SetText(stUpUltInst5SoundF, ropeF.SoundSpeedValue);

                    diffBySampIndex = 12;

                    break;
                case UltrasonicModel.InstrometS6:
                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                    curretSample.Ropes.Add(ropeC);
                    curretSample.Ropes.Add(ropeD);
                    curretSample.Ropes.Add(ropeE);
                    curretSample.Ropes.Add(ropeF);
                    curretSample.Ropes.Add(ropeG);
                    curretSample.Ropes.Add(ropeH);

                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);
                    SetText(sampleValues[4], ropeC.FlowSpeedValue);
                    SetText(sampleValues[5], ropeD.FlowSpeedValue);
                    SetText(sampleValues[6], ropeE.FlowSpeedValue);
                    SetText(sampleValues[7], ropeF.FlowSpeedValue);
                    SetText(sampleValues[8], ropeG.FlowSpeedValue);
                    SetText(sampleValues[9], ropeH.FlowSpeedValue);

                    SetText(sampleValues[10], ropeA.SoundSpeedValue);
                    SetText(sampleValues[11], ropeB.SoundSpeedValue);
                    SetText(sampleValues[12], ropeC.SoundSpeedValue);
                    SetText(sampleValues[13], ropeD.SoundSpeedValue);
                    SetText(sampleValues[14], ropeE.SoundSpeedValue);
                    SetText(sampleValues[15], ropeF.SoundSpeedValue);
                    SetText(sampleValues[16], ropeG.SoundSpeedValue);
                    SetText(sampleValues[17], ropeH.SoundSpeedValue);

                    diffBySampIndex = 18;

                    break;
                case UltrasonicModel.KrohneAltosonicV12:
                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                    curretSample.Ropes.Add(ropeC);
                    curretSample.Ropes.Add(ropeD);
                    curretSample.Ropes.Add(ropeE);
                    curretSample.Ropes.Add(ropeF);
                    
                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);
                    SetText(sampleValues[4], ropeC.FlowSpeedValue);
                    SetText(sampleValues[5], ropeD.FlowSpeedValue);
                    SetText(sampleValues[6], ropeE.FlowSpeedValue);
                    SetText(sampleValues[7], ropeF.FlowSpeedValue);
                   
                    SetText(sampleValues[8], ropeA.SoundSpeedValue);
                    SetText(sampleValues[9], ropeB.SoundSpeedValue);
                    SetText(sampleValues[10], ropeC.SoundSpeedValue);
                    SetText(sampleValues[11], ropeD.SoundSpeedValue);
                    SetText(sampleValues[12], ropeE.SoundSpeedValue);
                    SetText(sampleValues[13], ropeF.SoundSpeedValue);
                   
                    diffBySampIndex = 14;

                    break;
            }

            // guardar muestra
            if (dryCalProcess.UltrasonicSampleMode == UltSampMode.Automatic)
            {
                ((ObtainingSampleState)CurrentState).AddCurrentSample(curretSample);
            }
            
            // diferecia de temperatura entre muestras
            SetText(sampleValues[diffBySampIndex], CalculateDifferenceBySample());

            //mutex.ReleaseMutex();
        }

        public void UpdateSampleValuesFromSampleLayout(int sampleNumber, UltrasonicModel ultrasonicModel, params TextBox[] sampleValues)
        {

            Sample curretSample = new Sample();
            curretSample.Number = sampleNumber;

            double tempDiffValue = RtdVal.CalibrationRTD.Difference;
            double tempValue = RtdVal.CalibrationRTD.Average;
            double tempEnvDiffValue = RtdVal.EnvironmentRTD.Difference;
            double tempEnvValue = RtdVal.EnvironmentRTD.Average;
            double pressValue = PressureVal.Value;

            // temperatura de calibaración
            curretSample.CalibrationTemperature.Difference = tempDiffValue;
            curretSample.CalibrationTemperature.Value = tempValue;

            List<sensor.rtd.Rtd> tempRtds = new List<sensor.rtd.Rtd>();
            tempRtds.AddRange(RtdVal.Rtds);

            curretSample.TemperatureDetail = tempRtds;

            // temperatura ambiente
            curretSample.EnvirontmentTemperature.Difference = tempEnvDiffValue;
            curretSample.EnvirontmentTemperature.Value = tempEnvValue;

            curretSample.PressureValue = pressValue;

         
            RopeValue ropeA = new RopeValue("A");
            RopeValue ropeB = new RopeValue("B");
            RopeValue ropeC = new RopeValue("C");
            RopeValue ropeD = new RopeValue("D");
            RopeValue ropeE = new RopeValue("E");
            RopeValue ropeF = new RopeValue("F");
            RopeValue ropeG = new RopeValue("G");
            RopeValue ropeH = new RopeValue("H");

           
            switch (ultrasonicModel)
            {
                case UltrasonicModel.Daniel:
                case UltrasonicModel.FMU:
                case UltrasonicModel.Sick:

                    ropeA.FlowSpeedValue = SetValue(sampleValues[0]);
                    ropeB.FlowSpeedValue = SetValue(sampleValues[1]);
                    ropeC.FlowSpeedValue = SetValue(sampleValues[2]);
                    ropeD.FlowSpeedValue = SetValue(sampleValues[3]);

                    ropeA.SoundSpeedValue = SetValue(sampleValues[4]);
                    ropeB.SoundSpeedValue = SetValue(sampleValues[5]);
                    ropeC.SoundSpeedValue = SetValue(sampleValues[6]);
                    ropeD.SoundSpeedValue = SetValue(sampleValues[7]);

                    ropeA.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[8]));
                    ropeB.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[9]));
                    ropeC.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[10]));
                    ropeD.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[11]));

                    ropeA.GainValues.T1 = SetValue(sampleValues[12]);
                    ropeA.GainValues.T2 = SetValue(sampleValues[13]);
                    ropeB.GainValues.T1 = SetValue(sampleValues[14]);
                    ropeB.GainValues.T2 = SetValue(sampleValues[15]);
                    ropeC.GainValues.T1 = SetValue(sampleValues[16]);
                    ropeC.GainValues.T2 = SetValue(sampleValues[17]);
                    ropeD.GainValues.T1 = SetValue(sampleValues[18]);
                    ropeD.GainValues.T2 = SetValue(sampleValues[19]);

                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                    curretSample.Ropes.Add(ropeC);
                    curretSample.Ropes.Add(ropeD);

                    break;
                case UltrasonicModel.DanielJunior1R:
                    ropeA.FlowSpeedValue = SetValue(sampleValues[0]);  
                   
                    ropeA.SoundSpeedValue = SetValue(sampleValues[1]);
                  
                    ropeA.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[2])); 
                  
                    ropeA.GainValues.T1 = SetValue(sampleValues[3]); 
                    ropeA.GainValues.T2 = SetValue(sampleValues[4]);
                
                    curretSample.Ropes.Add(ropeA);
                  
                    break;
                case UltrasonicModel.DanielJunior2R:
                    ropeA.FlowSpeedValue = SetValue(sampleValues[0]);
                    ropeB.FlowSpeedValue = SetValue(sampleValues[1]);
             
                    ropeA.SoundSpeedValue = SetValue(sampleValues[2]);
                    ropeB.SoundSpeedValue = SetValue(sampleValues[3]);
                  
                    ropeA.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[4]));
                    ropeB.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[5]));
                    
                    ropeA.GainValues.T1 = SetValue(sampleValues[6]);
                    ropeA.GainValues.T2 = SetValue(sampleValues[7]);
                    ropeB.GainValues.T1 = SetValue(sampleValues[8]);
                    ropeB.GainValues.T2 = SetValue(sampleValues[9]);
                  
                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                
                    break;
                case UltrasonicModel.InstrometS5:

                    ropeA.FlowSpeedValue = SetValue(sampleValues[0]);
                    ropeB.FlowSpeedValue = SetValue(sampleValues[1]);
                    ropeC.FlowSpeedValue = SetValue(sampleValues[2]);
                    ropeD.FlowSpeedValue = SetValue(sampleValues[3]);
                    ropeE.FlowSpeedValue = SetValue(sampleValues[4]);
                    
                    ropeA.SoundSpeedValue = SetValue(sampleValues[5]);
                    ropeB.SoundSpeedValue = SetValue(sampleValues[6]);
                    ropeC.SoundSpeedValue = SetValue(sampleValues[7]);
                    ropeD.SoundSpeedValue = SetValue(sampleValues[8]);
                    ropeE.SoundSpeedValue = SetValue(sampleValues[9]);

                    ropeA.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[10]));
                    ropeB.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[11]));
                    ropeC.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[12]));
                    ropeD.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[13]));
                    ropeE.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[14]));

                    ropeA.GainValues.T1 = SetValue(sampleValues[15]);
                    ropeA.GainValues.T2 = SetValue(sampleValues[16]);
                    ropeB.GainValues.T1 = SetValue(sampleValues[17]);
                    ropeB.GainValues.T2 = SetValue(sampleValues[18]);
                    ropeC.GainValues.T1 = SetValue(sampleValues[19]);
                    ropeC.GainValues.T2 = SetValue(sampleValues[20]);
                    ropeD.GainValues.T1 = SetValue(sampleValues[21]);
                    ropeD.GainValues.T2 = SetValue(sampleValues[22]);
                    ropeE.GainValues.T1 = SetValue(sampleValues[23]);
                    ropeE.GainValues.T2 = SetValue(sampleValues[24]);

                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                    curretSample.Ropes.Add(ropeC);
                    curretSample.Ropes.Add(ropeD);
                    curretSample.Ropes.Add(ropeE);
          
                    break;
                case UltrasonicModel.InstrometS6:

                    ropeA.FlowSpeedValue = SetValue(sampleValues[0]);
                    ropeB.FlowSpeedValue = SetValue(sampleValues[1]);
                    ropeC.FlowSpeedValue = SetValue(sampleValues[2]);
                    ropeD.FlowSpeedValue = SetValue(sampleValues[3]);
                    ropeE.FlowSpeedValue = SetValue(sampleValues[4]);
                    ropeF.FlowSpeedValue = SetValue(sampleValues[5]);
                    ropeG.FlowSpeedValue = SetValue(sampleValues[6]);
                    ropeH.FlowSpeedValue = SetValue(sampleValues[7]);

                    ropeA.SoundSpeedValue = SetValue(sampleValues[8]);
                    ropeB.SoundSpeedValue = SetValue(sampleValues[9]);
                    ropeC.SoundSpeedValue = SetValue(sampleValues[10]);
                    ropeD.SoundSpeedValue = SetValue(sampleValues[11]);
                    ropeE.SoundSpeedValue = SetValue(sampleValues[12]);
                    ropeF.SoundSpeedValue = SetValue(sampleValues[13]);
                    ropeG.SoundSpeedValue = SetValue(sampleValues[14]);
                    ropeH.SoundSpeedValue = SetValue(sampleValues[15]);

                    ropeA.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[16]));
                    ropeB.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[17]));
                    ropeC.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[18]));
                    ropeD.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[19]));
                    ropeE.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[20]));
                    ropeF.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[21]));
                    ropeG.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[22]));
                    ropeH.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[23]));

                    ropeA.GainValues.T1 = SetValue(sampleValues[24]);
                    ropeA.GainValues.T2 = SetValue(sampleValues[25]);
                    ropeB.GainValues.T1 = SetValue(sampleValues[26]);
                    ropeB.GainValues.T2 = SetValue(sampleValues[27]);
                    ropeC.GainValues.T1 = SetValue(sampleValues[28]);
                    ropeC.GainValues.T2 = SetValue(sampleValues[29]);
                    ropeD.GainValues.T1 = SetValue(sampleValues[30]);
                    ropeD.GainValues.T2 = SetValue(sampleValues[31]);
                    ropeE.GainValues.T1 = SetValue(sampleValues[32]);
                    ropeE.GainValues.T2 = SetValue(sampleValues[33]);
                    ropeF.GainValues.T1 = SetValue(sampleValues[34]);
                    ropeF.GainValues.T2 = SetValue(sampleValues[35]);
                    ropeG.GainValues.T1 = SetValue(sampleValues[36]);
                    ropeG.GainValues.T2 = SetValue(sampleValues[37]);
                    ropeH.GainValues.T1 = SetValue(sampleValues[38]);
                    ropeH.GainValues.T2 = SetValue(sampleValues[39]);

                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                    curretSample.Ropes.Add(ropeC);
                    curretSample.Ropes.Add(ropeD);
                    curretSample.Ropes.Add(ropeE);
                    curretSample.Ropes.Add(ropeF);
                    curretSample.Ropes.Add(ropeG);
                    curretSample.Ropes.Add(ropeH);

                    break;
                case UltrasonicModel.KrohneAltosonicV12:

                    ropeA.FlowSpeedValue = SetValue(sampleValues[0]);
                    ropeB.FlowSpeedValue = SetValue(sampleValues[1]);
                    ropeC.FlowSpeedValue = SetValue(sampleValues[2]);
                    ropeD.FlowSpeedValue = SetValue(sampleValues[3]);
                    ropeE.FlowSpeedValue = SetValue(sampleValues[4]);
                    ropeF.FlowSpeedValue = SetValue(sampleValues[5]);
                   
                    ropeA.SoundSpeedValue = SetValue(sampleValues[6]);
                    ropeB.SoundSpeedValue = SetValue(sampleValues[7]);
                    ropeC.SoundSpeedValue = SetValue(sampleValues[8]);
                    ropeD.SoundSpeedValue = SetValue(sampleValues[9]);
                    ropeE.SoundSpeedValue = SetValue(sampleValues[10]);
                    ropeF.SoundSpeedValue = SetValue(sampleValues[11]);

                    ropeA.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[12]));
                    ropeB.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[13]));
                    ropeC.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[14]));
                    ropeD.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[15]));
                    ropeE.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[16]));
                    ropeF.EfficiencyValue = Convert.ToInt32(SetValue(sampleValues[17]));

                    ropeA.GainValues.T1 = SetValue(sampleValues[18]);
                    ropeA.GainValues.T2 = SetValue(sampleValues[19]);
                    ropeB.GainValues.T1 = SetValue(sampleValues[20]);
                    ropeB.GainValues.T2 = SetValue(sampleValues[21]);
                    ropeC.GainValues.T1 = SetValue(sampleValues[22]);
                    ropeC.GainValues.T2 = SetValue(sampleValues[23]);
                    ropeD.GainValues.T1 = SetValue(sampleValues[24]);
                    ropeD.GainValues.T2 = SetValue(sampleValues[25]);
                    ropeE.GainValues.T1 = SetValue(sampleValues[26]);
                    ropeE.GainValues.T2 = SetValue(sampleValues[27]);
                    ropeF.GainValues.T1 = SetValue(sampleValues[28]);
                    ropeF.GainValues.T2 = SetValue(sampleValues[29]);

                    curretSample.Ropes.Add(ropeA);
                    curretSample.Ropes.Add(ropeB);
                    curretSample.Ropes.Add(ropeC);
                    curretSample.Ropes.Add(ropeD);
                    curretSample.Ropes.Add(ropeE);
                    curretSample.Ropes.Add(ropeF);


                    break;
            }
            
            // guardar muestra
            ((ObtainingManualSampleState)CurrentState).AddCurrentSample(curretSample);
            
        }

        private double CalculateDifferenceBySample()
        {

            List<Sample> sampleList = new List<Sample>();

            if (dryCalProcess.UltrasonicSampleMode == UltSampMode.Automatic)
            {
                sampleList = ((ObtainingSampleState)CurrentState).Samples;
            }
            else
            {
                sampleList = ((ObtainingManualSampleState)CurrentState).Samples;
            }

            double minimum = (sampleList.Count > 0) ? sampleList.Min(r => r.CalibrationTemperature.Value) : 0;
            double maximum = (sampleList.Count > 0) ? sampleList.Max(r => r.CalibrationTemperature.Value) : 0;
            double difference = (sampleList.Count > 0) ? Math.Abs(maximum - minimum) : 0;

            return difference;    
        }

        public void UpdateObtainedSampleAverageLayout(UltrasonicModel ultrasonicModel, params TextBox[] sampleValues)
        {
            // obtener promedios
            Sample averages = new Sample();

            if (dryCalProcess.UltrasonicSampleMode == UltSampMode.Automatic)
            {
                averages = ((ObtainingSampleState)CurrentState).Averages;
            }
            else
            {
                averages = ((ObtainingManualSampleState)CurrentState).Averages;
            }

            mutex.WaitOne();
      
            double tempValue = averages.CalibrationTemperature.Value;
            double pressValue = averages.PressureValue;

            SolidColorBrush succColor = Brushes.LightGreen;
            SolidColorBrush errColor = Brushes.LightCoral;

            if (tempValue != -99)
            {
                SetText(sampleValues[0], tempValue);
            }
            else
            {
                SetText(sampleValues[0], "¡Error!");
                SetColor(sampleValues[0], errColor);
                throw new FSMStateProcessException("Ocurrió un error con los sensores de temperatura.");
            }

            if (pressValue != -99)
            {
                SetText(sampleValues[1], pressValue);
            }
            else
            {
                SetText(sampleValues[1], "¡Error!");
                SetColor(sampleValues[1], errColor);
                throw new FSMStateProcessException("Ocurrió un error con el sensor de presión.");
            }

            RopeValue ropeA = averages.Ropes.Find(r => r.Name == "A");
            RopeValue ropeB = averages.Ropes.Find(r => r.Name == "B");
            RopeValue ropeC = averages.Ropes.Find(r => r.Name == "C");
            RopeValue ropeD = averages.Ropes.Find(r => r.Name == "D");
            RopeValue ropeE = averages.Ropes.Find(r => r.Name == "E");
            RopeValue ropeF = averages.Ropes.Find(r => r.Name == "F");
            RopeValue ropeG = averages.Ropes.Find(r => r.Name == "G");
            RopeValue ropeH = averages.Ropes.Find(r => r.Name == "H");

            switch (ultrasonicModel)
            {
                case UltrasonicModel.Daniel:
                case UltrasonicModel.FMU:
                case UltrasonicModel.Sick:
                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);
                    SetText(sampleValues[4], ropeC.FlowSpeedValue);
                    SetText(sampleValues[5], ropeD.FlowSpeedValue);

                    SetText(sampleValues[6], ropeA.SoundSpeedValue);
                    SetText(sampleValues[7], ropeB.SoundSpeedValue);
                    SetText(sampleValues[8], ropeC.SoundSpeedValue);
                    SetText(sampleValues[9], ropeD.SoundSpeedValue);

                    SetText(sampleValues[10], ropeA.EfficiencyValue);
                    SetText(sampleValues[11], ropeB.EfficiencyValue);
                    SetText(sampleValues[12], ropeC.EfficiencyValue);
                    SetText(sampleValues[13], ropeD.EfficiencyValue);

                    break;
                case UltrasonicModel.DanielJunior1R:
                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeA.SoundSpeedValue);
                    SetText(sampleValues[4], ropeA.EfficiencyValue);
                   
                    break;
                case UltrasonicModel.DanielJunior2R:
                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);               
                    SetText(sampleValues[4], ropeA.SoundSpeedValue);
                    SetText(sampleValues[5], ropeB.SoundSpeedValue);          
                    SetText(sampleValues[6], ropeA.EfficiencyValue);
                    SetText(sampleValues[7], ropeB.EfficiencyValue);
              
                    break;
                case UltrasonicModel.InstrometS5:
                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);
                    SetText(sampleValues[4], ropeC.FlowSpeedValue);
                    SetText(sampleValues[5], ropeD.FlowSpeedValue);
                    SetText(sampleValues[6], ropeE.FlowSpeedValue);

                    SetText(sampleValues[7], ropeA.SoundSpeedValue);
                    SetText(sampleValues[8], ropeB.SoundSpeedValue);
                    SetText(sampleValues[9], ropeC.SoundSpeedValue);
                    SetText(sampleValues[10], ropeD.SoundSpeedValue);
                    SetText(sampleValues[11], ropeE.SoundSpeedValue);

                    SetText(sampleValues[12], ropeA.EfficiencyValue);
                    SetText(sampleValues[13], ropeB.EfficiencyValue);
                    SetText(sampleValues[14], ropeC.EfficiencyValue);
                    SetText(sampleValues[15], ropeD.EfficiencyValue);
                    SetText(sampleValues[16], ropeE.EfficiencyValue);
                    break;
                case UltrasonicModel.InstrometS6:
                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);
                    SetText(sampleValues[4], ropeC.FlowSpeedValue);
                    SetText(sampleValues[5], ropeD.FlowSpeedValue);
                    SetText(sampleValues[6], ropeE.FlowSpeedValue);
                    SetText(sampleValues[7], ropeF.FlowSpeedValue);
                    SetText(sampleValues[8], ropeG.FlowSpeedValue);
                    SetText(sampleValues[9], ropeH.FlowSpeedValue);

                    SetText(sampleValues[10], ropeA.SoundSpeedValue);
                    SetText(sampleValues[11], ropeB.SoundSpeedValue);
                    SetText(sampleValues[12], ropeC.SoundSpeedValue);
                    SetText(sampleValues[13], ropeD.SoundSpeedValue);
                    SetText(sampleValues[14], ropeE.SoundSpeedValue);
                    SetText(sampleValues[15], ropeF.SoundSpeedValue);
                    SetText(sampleValues[16], ropeG.SoundSpeedValue);
                    SetText(sampleValues[17], ropeH.SoundSpeedValue);

                    SetText(sampleValues[18], ropeA.EfficiencyValue);
                    SetText(sampleValues[19], ropeB.EfficiencyValue);
                    SetText(sampleValues[20], ropeC.EfficiencyValue);
                    SetText(sampleValues[21], ropeD.EfficiencyValue);
                    SetText(sampleValues[22], ropeE.EfficiencyValue);
                    SetText(sampleValues[23], ropeF.EfficiencyValue);
                    SetText(sampleValues[24], ropeG.EfficiencyValue);
                    SetText(sampleValues[25], ropeH.EfficiencyValue);
                    break;
                case UltrasonicModel.KrohneAltosonicV12:
                    SetText(sampleValues[2], ropeA.FlowSpeedValue);
                    SetText(sampleValues[3], ropeB.FlowSpeedValue);
                    SetText(sampleValues[4], ropeC.FlowSpeedValue);
                    SetText(sampleValues[5], ropeD.FlowSpeedValue);
                    SetText(sampleValues[6], ropeE.FlowSpeedValue);
                    SetText(sampleValues[7], ropeF.FlowSpeedValue);
                   
                    SetText(sampleValues[8], ropeA.SoundSpeedValue);
                    SetText(sampleValues[9], ropeB.SoundSpeedValue);
                    SetText(sampleValues[10], ropeC.SoundSpeedValue);
                    SetText(sampleValues[11], ropeD.SoundSpeedValue);
                    SetText(sampleValues[12], ropeE.SoundSpeedValue);
                    SetText(sampleValues[13], ropeF.SoundSpeedValue);
                   
                    SetText(sampleValues[14], ropeA.EfficiencyValue);
                    SetText(sampleValues[15], ropeB.EfficiencyValue);
                    SetText(sampleValues[16], ropeC.EfficiencyValue);
                    SetText(sampleValues[17], ropeD.EfficiencyValue);
                    SetText(sampleValues[18], ropeE.EfficiencyValue);
                    SetText(sampleValues[19], ropeF.EfficiencyValue);
                   
                    break;
            }

            mutex.ReleaseMutex();
        }

        public void UpdateValidatedSampleLayout(ValidatedResult validationResult, UltrasonicModel ultrasonicModel, params TextBox[] sampleValues)
        {

            log.Log.WriteIfExists("UpdateValidatedSampleLayout Ultranic Model: " + ultrasonicModel.ToString());

            mutex.WaitOne();

            SolidColorBrush succColor = Brushes.LightGreen;
            SolidColorBrush errColor = Brushes.LightCoral;

            ValTempDifference tempAvg = (ValTempDifference)validationResult.Averages.Find(t => t.ValidationType == ValidationType.TempAVG);

            ValResValue pressAvgValue = validationResult.Averages.Find(t => t.ValidationType == ValidationType.PressAVG);
            List<ValResValue> flowSpeedAvgValues = validationResult.Averages.FindAll(f => f.ValidationType == ValidationType.FlowAvg);
            List<ValResValue> efficiencyValues = validationResult.Averages.FindAll(f => f.ValidationType == ValidationType.EffAvg);
            List<ValResValue> gainValues = validationResult.Averages.FindAll(f => f.ValidationType == ValidationType.GainAvg);

            if (tempAvg.Value != -99)
            {
                SetText(sampleValues[0], tempAvg.Value);
                //SetColor(sampleValues[0], tempAvg.Success ? succColor : errColor);
            }
            else
            {
                SetText(sampleValues[0], "¡Error!");
                SetColor(sampleValues[0], errColor);
            }


            if (tempAvg.TempDifference != -99)
            {
                SetText(sampleValues[1], tempAvg.TempDifference);
                SetColor(sampleValues[1], tempAvg.Success ? succColor : errColor);
            }
            else
            {
                SetText(sampleValues[1], "¡Error!");
                SetColor(sampleValues[1], errColor);
            }


            if (pressAvgValue.Value != -99)
            {
                SetText(sampleValues[2], pressAvgValue.Value);
                //SetColor(sampleValues[2], pressAvgValue.Success ? succColor : errColor);
            }
            else
            {
                SetText(sampleValues[2], "¡Error!");
                SetColor(sampleValues[2], errColor);
            }


            // velocidad del sonido teórica

            if (validationResult.TheoreticalSoundSpeed is double)
            {
                SetText(sampleValues[3], validationResult.TheoreticalSoundSpeed);
            }
            else 
            {
                SetText(sampleValues[2], "¡Error!");
                SetColor(sampleValues[2], errColor);
            }

            // promedios por cuerda
            ValResValue ropeFlowA = flowSpeedAvgValues.Find(r => r.Name == "A");
            ValResValue ropeFlowB = flowSpeedAvgValues.Find(r => r.Name == "B");
            ValResValue ropeFlowC = flowSpeedAvgValues.Find(r => r.Name == "C");
            ValResValue ropeFlowD = flowSpeedAvgValues.Find(r => r.Name == "D");
            ValResValue ropeFlowE = flowSpeedAvgValues.Find(r => r.Name == "E");
            ValResValue ropeFlowF = flowSpeedAvgValues.Find(r => r.Name == "F");
            ValResValue ropeFlowG = flowSpeedAvgValues.Find(r => r.Name == "G");
            ValResValue ropeFlowH = flowSpeedAvgValues.Find(r => r.Name == "H");

            // errores porcentuales por cuerda
            ValPercentErrorValue ropeErrA = validationResult.PercentErrors.Find(r => r.Name == "A");
            ValPercentErrorValue ropeErrB = validationResult.PercentErrors.Find(r => r.Name == "B");
            ValPercentErrorValue ropeErrC = validationResult.PercentErrors.Find(r => r.Name == "C");
            ValPercentErrorValue ropeErrD = validationResult.PercentErrors.Find(r => r.Name == "D");
            ValPercentErrorValue ropeErrE = validationResult.PercentErrors.Find(r => r.Name == "E");
            ValPercentErrorValue ropeErrF = validationResult.PercentErrors.Find(r => r.Name == "F");
            ValPercentErrorValue ropeErrG = validationResult.PercentErrors.Find(r => r.Name == "G");
            ValPercentErrorValue ropeErrH = validationResult.PercentErrors.Find(r => r.Name == "H");

            // eficiencia por cuerda
            ValResValue ropeEffA = efficiencyValues.Find(r => r.Name == "A");
            ValResValue ropeEffB = efficiencyValues.Find(r => r.Name == "B");
            ValResValue ropeEffC = efficiencyValues.Find(r => r.Name == "C");
            ValResValue ropeEffD = efficiencyValues.Find(r => r.Name == "D");
            ValResValue ropeEffE = efficiencyValues.Find(r => r.Name == "E");
            ValResValue ropeEffF = efficiencyValues.Find(r => r.Name == "F");
            ValResValue ropeEffG = efficiencyValues.Find(r => r.Name == "G");
            ValResValue ropeEffH = efficiencyValues.Find(r => r.Name == "H");

            // gamancia por cuerda
            ValResValue ropeGainA = gainValues.Find(r => r.Name == "A");
            ValResValue ropeGainB = gainValues.Find(r => r.Name == "B");
            ValResValue ropeGainC = gainValues.Find(r => r.Name == "C");
            ValResValue ropeGainD = gainValues.Find(r => r.Name == "D");
            ValResValue ropeGainE = gainValues.Find(r => r.Name == "E");
            ValResValue ropeGainF = gainValues.Find(r => r.Name == "F");
            ValResValue ropeGainG = gainValues.Find(r => r.Name == "G");
            ValResValue ropeGainH = gainValues.Find(r => r.Name == "H");

            switch (ultrasonicModel)
            {
                case UltrasonicModel.Daniel:
                case UltrasonicModel.FMU:
                case UltrasonicModel.Sick:
                    // velocidad de flujo
                    SetText(sampleValues[4], ropeFlowA.Value);
                    SetText(sampleValues[5], ropeFlowB.Value);
                    SetText(sampleValues[6], ropeFlowC.Value);
                    SetText(sampleValues[7], ropeFlowD.Value);
                    // colores
                    SetColor(sampleValues[4], ropeFlowA.Success ? succColor : errColor);
                    SetColor(sampleValues[5], ropeFlowB.Success ? succColor : errColor);
                    SetColor(sampleValues[6], ropeFlowC.Success ? succColor : errColor);
                    SetColor(sampleValues[7], ropeFlowD.Success ? succColor : errColor);

                    // velocidad del sonido
                    SetText(sampleValues[8], ropeErrA.Value);
                    SetText(sampleValues[9], ropeErrB.Value);
                    SetText(sampleValues[10], ropeErrC.Value);
                    SetText(sampleValues[11], ropeErrD.Value);
                    //error porcentual
                    SetText(sampleValues[12], ropeErrA.PercentError);
                    SetText(sampleValues[13], ropeErrB.PercentError);
                    SetText(sampleValues[14], ropeErrC.PercentError);
                    SetText(sampleValues[15], ropeErrD.PercentError);
                    // colores
                    SetColor(sampleValues[12], ropeErrA.Success ? succColor : errColor);
                    SetColor(sampleValues[13], ropeErrB.Success ? succColor : errColor);
                    SetColor(sampleValues[14], ropeErrC.Success ? succColor : errColor);
                    SetColor(sampleValues[15], ropeErrD.Success ? succColor : errColor);

                    //diferencia de la velocidad del sonido entre cuerdas
                    SetText(sampleValues[16], validationResult.SoundDifference.Max);
                    SetText(sampleValues[17], validationResult.SoundDifference.Min);
                    SetText(sampleValues[18], validationResult.SoundDifference.Value);
                    //color
                    SetColor(sampleValues[18], validationResult.SoundDifference.Success ? succColor : errColor);

                    //eficiencia por cuerda
                    SetText(sampleValues[19], (int)ropeEffA.Value);
                    SetText(sampleValues[20], (int)ropeEffB.Value);
                    SetText(sampleValues[21], (int)ropeEffC.Value);
                    SetText(sampleValues[22], (int)ropeEffD.Value);

                    //colores
                    SetColor(sampleValues[19], ropeEffA.Success ? succColor : errColor);
                    SetColor(sampleValues[20], ropeEffB.Success ? succColor : errColor);
                    SetColor(sampleValues[21], ropeEffC.Success ? succColor : errColor);
                    SetColor(sampleValues[22], ropeEffD.Success ? succColor : errColor);

                    // ganancia por cuerda
                    SetText(sampleValues[23], ropeGainA.Value,1);
                    SetText(sampleValues[24], ropeGainB.Value,1);
                    SetText(sampleValues[25], ropeGainC.Value,1);
                    SetText(sampleValues[26], ropeGainD.Value,1);

                    // colores
                    SetColor(sampleValues[23], ropeGainA.Success ? succColor : errColor);
                    SetColor(sampleValues[24], ropeGainB.Success ? succColor : errColor);
                    SetColor(sampleValues[25], ropeGainC.Success ? succColor : errColor);
                    SetColor(sampleValues[26], ropeGainD.Success ? succColor : errColor);


                    break;
                case UltrasonicModel.DanielJunior1R:
                    // velocidad de flujo
                    SetText(sampleValues[4], ropeFlowA.Value);
                    // colores
                    SetColor(sampleValues[4], ropeFlowA.Success ? succColor : errColor);
                
                    // velocidad del sonido
                    SetText(sampleValues[5], ropeErrA.Value);
                  
                    //error porcentual
                    SetText(sampleValues[6], ropeErrA.PercentError);
                    // colores
                    SetColor(sampleValues[6], ropeErrA.Success ? succColor : errColor);
                 
                    //diferencia de la velocidad del sonido entre cuerdas
                    SetText(sampleValues[7], validationResult.SoundDifference.Max);
                    SetText(sampleValues[8], validationResult.SoundDifference.Min);
                    SetText(sampleValues[9], validationResult.SoundDifference.Value);
                    //color
                    SetColor(sampleValues[9], validationResult.SoundDifference.Success ? succColor : errColor);

                    //eficiencia por cuerda
                    SetText(sampleValues[10], (int)ropeEffA.Value);   
                    //colores
                    SetColor(sampleValues[10], ropeEffA.Success ? succColor : errColor);
                  
                    // ganancia por cuerda
                    SetText(sampleValues[11], ropeGainA.Value, 1);
                    // colores
                    SetColor(sampleValues[11], ropeGainA.Success ? succColor : errColor);
             
                    break;
                case UltrasonicModel.DanielJunior2R:
                    // velocidad de flujo
                    SetText(sampleValues[4], ropeFlowA.Value);
                    SetText(sampleValues[5], ropeFlowB.Value);
                    // colores
                    SetColor(sampleValues[4], ropeFlowA.Success ? succColor : errColor);
                    SetColor(sampleValues[5], ropeFlowB.Success ? succColor : errColor);
            
                    // velocidad del sonido
                    SetText(sampleValues[6], ropeErrA.Value);
                    SetText(sampleValues[7], ropeErrB.Value);
                
                    //error porcentual
                    SetText(sampleValues[8], ropeErrA.PercentError);
                    SetText(sampleValues[9], ropeErrB.PercentError);
                    // colores
                    SetColor(sampleValues[8], ropeErrA.Success ? succColor : errColor);
                    SetColor(sampleValues[9], ropeErrB.Success ? succColor : errColor);
               
                    //diferencia de la velocidad del sonido entre cuerdas
                    SetText(sampleValues[10], validationResult.SoundDifference.Max);
                    SetText(sampleValues[11], validationResult.SoundDifference.Min);
                    SetText(sampleValues[12], validationResult.SoundDifference.Value);
                    //color
                    SetColor(sampleValues[12], validationResult.SoundDifference.Success ? succColor : errColor);

                    //eficiencia por cuerda
                    SetText(sampleValues[13], (int)ropeEffA.Value);
                    SetText(sampleValues[14], (int)ropeEffB.Value);
               
                    //colores
                    SetColor(sampleValues[13], ropeEffA.Success ? succColor : errColor);
                    SetColor(sampleValues[14], ropeEffB.Success ? succColor : errColor);
                
                    // ganancia por cuerda
                    SetText(sampleValues[15], ropeGainA.Value, 1);
                    SetText(sampleValues[16], ropeGainB.Value, 1);
              
                    // colores
                    SetColor(sampleValues[15], ropeGainA.Success ? succColor : errColor);
                    SetColor(sampleValues[16], ropeGainB.Success ? succColor : errColor);
                 
                    break;
                case UltrasonicModel.InstrometS5:
                    // velocidad de flujo
                    SetText(sampleValues[4], ropeFlowA.Value);
                    SetText(sampleValues[5], ropeFlowB.Value);
                    SetText(sampleValues[6], ropeFlowC.Value);
                    SetText(sampleValues[7], ropeFlowD.Value);
                    SetText(sampleValues[8], ropeFlowE.Value);
                    // colores
                    SetColor(sampleValues[4], ropeFlowA.Success ? succColor : errColor);
                    SetColor(sampleValues[5], ropeFlowB.Success ? succColor : errColor);
                    SetColor(sampleValues[6], ropeFlowC.Success ? succColor : errColor);
                    SetColor(sampleValues[7], ropeFlowD.Success ? succColor : errColor);
                    SetColor(sampleValues[8], ropeFlowE.Success ? succColor : errColor);
                    // velocidad del sonido
                    SetText(sampleValues[9], ropeErrA.Value);
                    SetText(sampleValues[10], ropeErrB.Value);
                    SetText(sampleValues[11], ropeErrC.Value);
                    SetText(sampleValues[12], ropeErrD.Value);
                    SetText(sampleValues[13], ropeErrE.Value);
                    //error porcentual
                    SetText(sampleValues[14], ropeErrA.PercentError);
                    SetText(sampleValues[15], ropeErrB.PercentError);
                    SetText(sampleValues[16], ropeErrC.PercentError);
                    SetText(sampleValues[17], ropeErrD.PercentError);
                    SetText(sampleValues[18], ropeErrE.PercentError);
                    // colores
                    SetColor(sampleValues[14], ropeErrA.Success ? succColor : errColor);
                    SetColor(sampleValues[15], ropeErrB.Success ? succColor : errColor);
                    SetColor(sampleValues[16], ropeErrC.Success ? succColor : errColor);
                    SetColor(sampleValues[17], ropeErrD.Success ? succColor : errColor);
                    SetColor(sampleValues[18], ropeErrE.Success ? succColor : errColor);
                    
                    //diferencia de la velocidad del sonido entre cuerdas
                    SetText(sampleValues[19], validationResult.SoundDifference.Max);
                    SetText(sampleValues[20], validationResult.SoundDifference.Min);
                    SetText(sampleValues[21], validationResult.SoundDifference.Value);
                    //color
                    SetColor(sampleValues[21], validationResult.SoundDifference.Success ? succColor : errColor);

                    //eficiencia por cuerda
                    SetText(sampleValues[22], (int)ropeEffA.Value);
                    SetText(sampleValues[23], (int)ropeEffB.Value);
                    SetText(sampleValues[24], (int)ropeEffC.Value);
                    SetText(sampleValues[25], (int)ropeEffD.Value);
                    SetText(sampleValues[26], (int)ropeEffE.Value);

                    //colores
                    SetColor(sampleValues[22], ropeEffA.Success ? succColor : errColor);
                    SetColor(sampleValues[23], ropeEffB.Success ? succColor : errColor);
                    SetColor(sampleValues[24], ropeEffC.Success ? succColor : errColor);
                    SetColor(sampleValues[25], ropeEffD.Success ? succColor : errColor);
                    SetColor(sampleValues[26], ropeEffE.Success ? succColor : errColor);

                    // ganancia por cuerda
                    SetText(sampleValues[27], ropeGainA.Value,1);
                    SetText(sampleValues[28], ropeGainB.Value,1);
                    SetText(sampleValues[29], ropeGainC.Value,1);
                    SetText(sampleValues[30], ropeGainD.Value,1);
                    SetText(sampleValues[31], ropeGainE.Value,1);

                    // colores
                    SetColor(sampleValues[27], ropeGainA.Success ? succColor : errColor);
                    SetColor(sampleValues[28], ropeGainB.Success ? succColor : errColor);
                    SetColor(sampleValues[29], ropeGainC.Success ? succColor : errColor);
                    SetColor(sampleValues[30], ropeGainD.Success ? succColor : errColor);
                    SetColor(sampleValues[31], ropeGainE.Success ? succColor : errColor);

                    break;
                case UltrasonicModel.InstrometS6:
                    // velocidad de flujo
                    SetText(sampleValues[4], ropeFlowA.Value);
                    SetText(sampleValues[5], ropeFlowB.Value);
                    SetText(sampleValues[6], ropeFlowC.Value);
                    SetText(sampleValues[7], ropeFlowD.Value);
                    SetText(sampleValues[8], ropeFlowE.Value);
                    SetText(sampleValues[9], ropeFlowF.Value);
                    SetText(sampleValues[10], ropeFlowG.Value);
                    SetText(sampleValues[11], ropeFlowH.Value);
                    // colores
                    SetColor(sampleValues[4], ropeFlowA.Success ? succColor : errColor);
                    SetColor(sampleValues[5], ropeFlowB.Success ? succColor : errColor);
                    SetColor(sampleValues[6], ropeFlowC.Success ? succColor : errColor);
                    SetColor(sampleValues[7], ropeFlowD.Success ? succColor : errColor);
                    SetColor(sampleValues[8], ropeFlowE.Success ? succColor : errColor);
                    SetColor(sampleValues[9], ropeFlowF.Success ? succColor : errColor);
                    SetColor(sampleValues[10], ropeFlowG.Success ? succColor : errColor);
                    SetColor(sampleValues[11], ropeFlowH.Success ? succColor : errColor);
                    // velocidad del sonido
                    SetText(sampleValues[12], ropeErrA.Value);
                    SetText(sampleValues[13], ropeErrB.Value);
                    SetText(sampleValues[14], ropeErrC.Value);
                    SetText(sampleValues[15], ropeErrD.Value);
                    SetText(sampleValues[16], ropeErrE.Value);
                    SetText(sampleValues[17], ropeErrF.Value);
                    SetText(sampleValues[18], ropeErrG.Value);
                    SetText(sampleValues[19], ropeErrH.Value);
                    //error porcentual
                    SetText(sampleValues[20], ropeErrA.PercentError);
                    SetText(sampleValues[21], ropeErrB.PercentError);
                    SetText(sampleValues[22], ropeErrC.PercentError);
                    SetText(sampleValues[23], ropeErrD.PercentError);
                    SetText(sampleValues[24], ropeErrE.PercentError);
                    SetText(sampleValues[25], ropeErrF.PercentError);
                    SetText(sampleValues[26], ropeErrG.PercentError);
                    SetText(sampleValues[27], ropeErrH.PercentError);
                    // colores
                    SetColor(sampleValues[20], ropeErrA.Success ? succColor : errColor);
                    SetColor(sampleValues[21], ropeErrB.Success ? succColor : errColor);
                    SetColor(sampleValues[22], ropeErrC.Success ? succColor : errColor);
                    SetColor(sampleValues[23], ropeErrD.Success ? succColor : errColor);
                    SetColor(sampleValues[24], ropeErrE.Success ? succColor : errColor);
                    SetColor(sampleValues[25], ropeErrF.Success ? succColor : errColor);
                    SetColor(sampleValues[26], ropeErrG.Success ? succColor : errColor);
                    SetColor(sampleValues[27], ropeErrH.Success ? succColor : errColor);

                    //diferencia de la velocidad del sonido entre cuerdas
                    SetText(sampleValues[28], validationResult.SoundDifference.Max);
                    SetText(sampleValues[29], validationResult.SoundDifference.Min);
                    SetText(sampleValues[30], validationResult.SoundDifference.Value);
                    //color
                    SetColor(sampleValues[30], validationResult.SoundDifference.Success ? succColor : errColor);

                    //eficiencia por cuerda
                    SetText(sampleValues[31], (int)ropeEffA.Value);
                    SetText(sampleValues[32], (int)ropeEffB.Value);
                    SetText(sampleValues[33], (int)ropeEffC.Value);
                    SetText(sampleValues[34], (int)ropeEffD.Value);
                    SetText(sampleValues[35], (int)ropeEffE.Value);
                    SetText(sampleValues[36], (int)ropeEffF.Value);
                    SetText(sampleValues[37], (int)ropeEffG.Value);
                    SetText(sampleValues[38], (int)ropeEffH.Value);

                    //colores
                    SetColor(sampleValues[31], ropeEffA.Success ? succColor : errColor);
                    SetColor(sampleValues[32], ropeEffB.Success ? succColor : errColor);
                    SetColor(sampleValues[33], ropeEffC.Success ? succColor : errColor);
                    SetColor(sampleValues[34], ropeEffD.Success ? succColor : errColor);
                    SetColor(sampleValues[35], ropeEffE.Success ? succColor : errColor);
                    SetColor(sampleValues[36], ropeEffF.Success ? succColor : errColor);
                    SetColor(sampleValues[37], ropeEffG.Success ? succColor : errColor);
                    SetColor(sampleValues[38], ropeEffH.Success ? succColor : errColor);

                    // ganancia por cuerda
                    SetText(sampleValues[39], ropeGainA.Value,1);
                    SetText(sampleValues[40], ropeGainB.Value,1);
                    SetText(sampleValues[41], ropeGainC.Value,1);
                    SetText(sampleValues[42], ropeGainD.Value,1);
                    SetText(sampleValues[43], ropeGainE.Value,1);
                    SetText(sampleValues[44], ropeGainF.Value,1);
                    SetText(sampleValues[45], ropeGainG.Value,1);
                    SetText(sampleValues[46], ropeGainH.Value,1);

                    // colores
                    SetColor(sampleValues[39], ropeGainA.Success ? succColor : errColor);
                    SetColor(sampleValues[40], ropeGainB.Success ? succColor : errColor);
                    SetColor(sampleValues[41], ropeGainC.Success ? succColor : errColor);
                    SetColor(sampleValues[42], ropeGainD.Success ? succColor : errColor);
                    SetColor(sampleValues[43], ropeGainE.Success ? succColor : errColor);
                    SetColor(sampleValues[44], ropeGainF.Success ? succColor : errColor);
                    SetColor(sampleValues[45], ropeGainG.Success ? succColor : errColor);
                    SetColor(sampleValues[46], ropeGainH.Success ? succColor : errColor);

                    break;
                case UltrasonicModel.KrohneAltosonicV12:
                    // velocidad de flujo
                    SetText(sampleValues[4], ropeFlowA.Value);
                    SetText(sampleValues[5], ropeFlowB.Value);
                    SetText(sampleValues[6], ropeFlowC.Value);
                    SetText(sampleValues[7], ropeFlowD.Value);
                    SetText(sampleValues[8], ropeFlowE.Value);
                    SetText(sampleValues[9], ropeFlowF.Value);
                    // colores
                    SetColor(sampleValues[4], ropeFlowA.Success ? succColor : errColor);
                    SetColor(sampleValues[5], ropeFlowB.Success ? succColor : errColor);
                    SetColor(sampleValues[6], ropeFlowC.Success ? succColor : errColor);
                    SetColor(sampleValues[7], ropeFlowD.Success ? succColor : errColor);
                    SetColor(sampleValues[8], ropeFlowE.Success ? succColor : errColor);
                    SetColor(sampleValues[9], ropeFlowF.Success ? succColor : errColor);
                    // velocidad del sonido
                    SetText(sampleValues[10], ropeErrA.Value);
                    SetText(sampleValues[11], ropeErrB.Value);
                    SetText(sampleValues[12], ropeErrC.Value);
                    SetText(sampleValues[13], ropeErrD.Value);
                    SetText(sampleValues[14], ropeErrE.Value);
                    SetText(sampleValues[15], ropeErrF.Value);
                    //error porcentual
                    SetText(sampleValues[16], ropeErrA.PercentError);
                    SetText(sampleValues[17], ropeErrB.PercentError);
                    SetText(sampleValues[18], ropeErrC.PercentError);
                    SetText(sampleValues[19], ropeErrD.PercentError);
                    SetText(sampleValues[20], ropeErrE.PercentError);
                    SetText(sampleValues[21], ropeErrF.PercentError);
                    // colores
                    SetColor(sampleValues[16], ropeErrA.Success ? succColor : errColor);
                    SetColor(sampleValues[17], ropeErrB.Success ? succColor : errColor);
                    SetColor(sampleValues[18], ropeErrC.Success ? succColor : errColor);
                    SetColor(sampleValues[19], ropeErrD.Success ? succColor : errColor);
                    SetColor(sampleValues[20], ropeErrE.Success ? succColor : errColor);
                    SetColor(sampleValues[21], ropeErrF.Success ? succColor : errColor);
                    
                    //diferencia de la velocidad del sonido entre cuerdas
                    SetText(sampleValues[22], validationResult.SoundDifference.Max);
                    SetText(sampleValues[23], validationResult.SoundDifference.Min);
                    SetText(sampleValues[24], validationResult.SoundDifference.Value);
                    //color
                    SetColor(sampleValues[24], validationResult.SoundDifference.Success ? succColor : errColor);

                    //eficiencia por cuerda
                    SetText(sampleValues[25], (int)ropeEffA.Value);
                    SetText(sampleValues[26], (int)ropeEffB.Value);
                    SetText(sampleValues[27], (int)ropeEffC.Value);
                    SetText(sampleValues[28], (int)ropeEffD.Value);
                    SetText(sampleValues[29], (int)ropeEffE.Value);
                    SetText(sampleValues[30], (int)ropeEffF.Value);
                    //colores
                    SetColor(sampleValues[25], ropeEffA.Success ? succColor : errColor);
                    SetColor(sampleValues[26], ropeEffB.Success ? succColor : errColor);
                    SetColor(sampleValues[27], ropeEffC.Success ? succColor : errColor);
                    SetColor(sampleValues[28], ropeEffD.Success ? succColor : errColor);
                    SetColor(sampleValues[29], ropeEffE.Success ? succColor : errColor);
                    SetColor(sampleValues[30], ropeEffF.Success ? succColor : errColor);

                    // ganancia por cuerda
                    SetText(sampleValues[31], ropeGainA.Value,1);
                    SetText(sampleValues[32], ropeGainB.Value,1);
                    SetText(sampleValues[33], ropeGainC.Value,1);
                    SetText(sampleValues[34], ropeGainD.Value,1);
                    SetText(sampleValues[35], ropeGainE.Value,1);
                    SetText(sampleValues[36], ropeGainF.Value,1);

                    // colores
                    SetColor(sampleValues[31], ropeGainA.Success ? succColor : errColor);
                    SetColor(sampleValues[32], ropeGainB.Success ? succColor : errColor);
                    SetColor(sampleValues[33], ropeGainC.Success ? succColor : errColor);
                    SetColor(sampleValues[34], ropeGainD.Success ? succColor : errColor);
                    SetColor(sampleValues[35], ropeGainE.Success ? succColor : errColor);
                    SetColor(sampleValues[36], ropeGainF.Success ? succColor : errColor);

                    break;
                default:
                    break;
            }

            // condición de salida
            ((ValidatingState)CurrentState).ValidationComplete = true;

            mutex.ReleaseMutex();
        }

        public void UpdateObtainedSampleRTDDiffLayout(params TextBox[] sampleValues)
        {
            if (RtdVal.CalibrationRTD.Average != -99)
            {
                SetText(sampleValues[0], RtdVal.CalibrationRTD.Average);
            }
            else
            {
                SetText(sampleValues[0], "¡Error!");
            }

            if (RtdVal.CalibrationRTD.Difference != -99) //(RtdVal.CalibrationRTD.Uncertainty != -99)
            {
                //SetText(sampleValues[1], RtdVal.CalibrationRTD.Uncertainty);
                SetText(sampleValues[1], RtdVal.CalibrationRTD.Difference);
            }
            else
            {
                SetText(sampleValues[1], "¡Error!");
            }
        }

        public bool SaveGeneralConfiguration(params TextBox[] txtConfig)
        {
            bool result = true;

            for (int i = 0; i < txtConfig.Length; i++)
            {    
                if (string.IsNullOrEmpty(txtConfig[i].Text))
                {
                    SetColor(txtConfig[i], Brushes.LightCoral);
                    result = false;
                }
                else
                {
                    SetColor(txtConfig[i], Brushes.White);
                }
            }

            //comprobar máscara
            string pressureValue = ((txtConfig[16].Text).Replace("_", "") == "") ? "" : txtConfig[16].Text;

            if (string.IsNullOrEmpty(pressureValue))
            {
                SetColor(txtConfig[16], Brushes.LightCoral);
                result = false;
            }

            string reportNumber = txtConfig[14].Text;
            // formato DC-NN-NNNN-NN (DC-YY-NNNN-MM)
            string pattern = @"^DC-([0-9])([0-9])-([0-9])([0-9])([0-9])([0-9])-([0-9])([0-9])$"; 
   
            Regex rgx = new Regex(pattern);
            if (!rgx.IsMatch(reportNumber))
            {
                SetColor(txtConfig[14], Brushes.LightCoral);
                result = false;
            }

            if (!result)
            {
                return false;
            }

            ReportModel report = new ReportModel();

            // objeto del reporte
            report.Header.CalibrationInformation.CalibrationObject = "1 (un) medidor ultrasónico";

            // Ultrasónico
            report.Header.Measurer.Brand = txtConfig[0].Text;
            report.Header.Measurer.Maker = txtConfig[1].Text;

            report.Header.Measurer.Model = txtConfig[2].Text;
            report.Header.Measurer.DN = txtConfig[3].Text;
            report.Header.Measurer.Sch = txtConfig[4].Text;
            report.Header.Measurer.Serie = txtConfig[5].Text;

            report.Header.Measurer.SerieNumber = txtConfig[6].Text;
            report.Header.Measurer.Identification = txtConfig[7].Text;

            report.Header.Measurer.FirmwareVersion = txtConfig[8].Text;
            report.Header.CalibrationInformation.RequiredDetermination = "Ensayo de verificación de flujo cero y velocidad de sonido";
            
            // Solicitante
            report.Header.Petitioner.BusinessName = txtConfig[9].Text;
            report.Header.Petitioner.BusinessAddress = txtConfig[10].Text;
            report.Header.Petitioner.RealizationPlace = txtConfig[11].Text;
            report.Header.Petitioner.RealizationAddress = txtConfig[12].Text;

            // Información del ensayo
            report.Header.CalibrationInformation.Responsible = txtConfig[13].Text;
            report.Header.CalibrationInformation.ReportNumber = txtConfig[14].Text;    
            report.Header.CalibrationInformation.CalibrationDate = txtConfig[15].Text;
            report.Header.CalibrationInformation.EmitionDate = txtConfig[15].Text;

            // Condición ambiental
            try
            {
                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

                decimal aga10atmPres = Convert.ToDecimal(pressureValue);
                CurrentPressureCalibration.AtmosphericPresssure = (aga10atmPres / 1000);

                report.Header.EnvironmentalCondition.AtmosphericPressure = Convert.ToString(aga10atmPres);

            }
            catch
            {
                SetColor(txtConfig[16], Brushes.LightCoral);
                return false;
            }
 
            //guardar reporte
            CurrentReportModel  = report;

            return true;
        }

        public void SaveReportMeasuringConfiguration(params TextBox[] txtConfig)
        {
            try
            {
                MeasuringConfiguration configuration = new MeasuringConfiguration();

                for (int row = 0; row <= txtConfig.Length - 3; row += 3)
                {
                    string brandName = txtConfig[row].Text;
                    string internalIdentification = txtConfig[row + 1].Text;
                    string calibrationCode = txtConfig[row + 2].Text;

                    MeasuringInstrument instrument = new MeasuringInstrument()
                    {
                        BrandName = brandName,
                        InternalIdentification = internalIdentification,
                        CalibrationCode = calibrationCode,
                    };

                    configuration.MeasuringInstruments.Add(instrument);
                }

                string path = Path.Combine(Utils.ConfigurationPath, "MeasuringConfiguration.xml");
                MeasuringConfiguration.Generate(path, configuration);
             
            }
            catch (Exception e)
            {

                log.Log.WriteIfExists("Ocurrió un error al guardar la configuración de los instrumentos de medición del ensayo.", e);
            }
        }

        private void DryCalProcess_DryCalibrationAborted()
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch { }
       
            DryCalibrationAborted?.Invoke();
        }

        private void DryCalProcess_DryCalibrationStateChange(FSMState current, UltrasonicModel ultrasonicModel)
        {
            DryCalibrationStateChange?.Invoke(current, ultrasonicModel);
        }
  
        private void DryCalProcess_SampleObtained(int sampleNumber, UltrasonicModel ultrasonicModel)
        {
            SampleObtained?.Invoke(sampleNumber, ultrasonicModel);
        }

        private void DryCalProcess_ObtainingSampleFinished()
        {
            ObtainingSampleFinished?.Invoke();
        }

        private void DryCalProcess_ValidationFinished(ValidatedResult validatedResult)
        {
            // eliminar la ruta al reporte anterior si existe
            CurrentFullReportPath = "";
            
            UltrasonicModel ultrasonicModel = (UltrasonicModel)CurrentModbusConfiguration.SlaveConfig.Model;
            ValidationFinished?.Invoke(validatedResult, ultrasonicModel);
        }

        private void DryCalProcess_GenerateReportSucceeded(string fullReportPath)
        {
            this.CurrentFullReportPath = fullReportPath;
        }

        protected void DryCalProcess_RefreshState(string description)
        {
            RefreshState?.Invoke(description);
        }

        private void DryCalProcess_GenerateCsv(List<Sample> samples, Sample averages)
        {
            EnvironmentalCondition environmentalCondition = dryCalProcess.CurrentReportModel.Header.EnvironmentalCondition;
            string reportNumber = dryCalProcess.CurrentReportModel.Header.CalibrationInformation.ReportNumber;
            UltrasonicModel ultrasonicModel = (UltrasonicModel)CurrentModbusConfiguration.SlaveConfig.Model;

            try
            {
                string csv = Utils.MakeCsv(samples, averages, environmentalCondition, ultrasonicModel, CurrentSecondsTimeProcess);

                string csvPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dry Calibration\\Certificados");
                string csvName = string.Format("IT_{0}.csv", reportNumber);
                csvPath = Path.Combine(csvPath, csvName);


                using (StreamWriter sw = new StreamWriter(csvPath, false, Encoding.UTF8))
                {
                    sw.Write(csv);
                    sw.Flush();
                }

            }
            catch (Exception e)
            {
                // log

            }
        }

        protected void StatusMessage(string message)
        {
            RefreshState?.Invoke(message);
        }
        
        protected void DryCalibration_UpdateSensorReceived(MonitorType type, object value)
        {
            switch (type)
            {
                case MonitorType.RTD:
                    RtdVal = (RTDValue)value;
                    break;
                case MonitorType.Pressure:
                    PressureVal = (PressureValue)value;
                    break;
                case MonitorType.Ultrasonic:
                    UltrasonicVal = (UltrasonicValue)value;
                    break;
                default:
                    break;
            }

            UpdateSensorReceived?.Invoke(type, value);
        }

        protected void DryCalibration_StatisticReceived(MonitorType type, StatisticValue value)
        {
            StatisticReceived?.Invoke(type, value);
        }

        public void SetText(TextBox textBox, object arg, int decQ = 3)
        {
            if (!textBox.Dispatcher.CheckAccess())
            {
                textBox.Dispatcher.Invoke(new Action<TextBox, object, int>(SetText), textBox, arg, decQ);
            }
            else
            {
                if (arg is double)
                {
                    string strValue = "";

                    double num = Convert.ToDouble(arg);

                    if (Double.IsNaN(num))
                    {
                        strValue = "¡Error!";
                    }
                    else 
                    {
                        strValue = String.Format("{0:0.000}", num);

                        if (decQ == 1)
                        {
                            strValue = String.Format("{0:0.0}", num);
                        }
                        else if (decQ == 2)
                        {
                            strValue = String.Format("{0:0.00}", num);
                        }
                    }          
                  
                    textBox.Text = strValue;
                    
                    return;
                }

                textBox.Text = arg.ToString();
            }
        }

        private double SetValue(TextBox textBox)
        {
            try
            {
                string strValue = String.Format("{0:0.000}", textBox.Text);

                return Convert.ToDouble(strValue);
            }
            catch 
            {
                return 0d;
            }       
        }

        public void SetColor(TextBox textBox, SolidColorBrush color)
        {
            if (!textBox.Dispatcher.CheckAccess())
            {
                textBox.Dispatcher.Invoke(new Action<TextBox, SolidColorBrush>(SetColor), textBox, color);
            }
            else
            {
                textBox.Background = color;
            }
        }

        public void SetControlVisibility(UIElement control, Visibility arg)
        {
            if (!control.Dispatcher.CheckAccess())
            {
                control.Dispatcher.Invoke(new Action<UIElement, Visibility>(SetControlVisibility), control, arg);
            }
            else
            {
                control.Visibility = arg;
            }
        }

        public void SetControlEnabled(UIElement control, bool enabled)
        {
            if (!control.Dispatcher.CheckAccess())
            {
                control.Dispatcher.Invoke(new Action<UIElement, bool>(SetControlEnabled), control, enabled);
            }
            else
            {
                control.IsEnabled = enabled;
            }
        }

    }
}