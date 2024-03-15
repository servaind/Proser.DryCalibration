using Proser.DryCalibration.controller.enums;
using Proser.DryCalibration.controller.interfaces;
using Proser.DryCalibration.controller.pressure;
using Proser.DryCalibration.controller.rtd;
using Proser.DryCalibration.controller.ultrasonic;
using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.fsm.interfaces;
using Proser.DryCalibration.fsm.states;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.statistic;
using Proser.DryCalibration.Report;
using Proser.DryCalibration.sensor.pressure.calibration;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;

namespace Proser.DryCalibration.fsm
{
    public class DryCalibrationProcess : IDryCalibrationProcess
    {
        public event Action<MonitorType, StatisticValue> StatisticReceived;
        public event Action<MonitorType, object> UpdateSensorReceived;
        public event Action<string> RefreshState;
        public event Action DryCalibrationAborted;
        public event Action<FSMState, UltrasonicModel> DryCalibrationStateChange;
        public event Action<int, UltrasonicModel> SampleObtained;
        public event Action ObtainingSampleFinished;
        public event Action<ValidatedResult> ValidationFinished;
        public event Action<string> GenerateReportSucceeded;
        public event Action<List<Sample>, Sample> GenerateCsv;

        public IState CurrentState { get; private set; }
        public ValidatedResult CurrentValidatedResults { get; private set; }
        public ReportModel CurrentReportModel { get; set; }
        public Sample CurrentAverages { get; private set; }

        public UltSampMode UltrasonicSampleMode { get; set; }
        public PressureCalibration CurrentPressureCalibration { get; set; }

        private IController rtdController;
        private IController pressureController;
        private IController ultrasonicController;

        private UltrasonicModel ultrasonicModel;
        private bool DryCalibrationIsAborted;

        public DryCalibrationProcess(UltSampMode ultSampMode)
        {
          
            // rtd monitor
            rtdController = new RtdController();
            rtdController.StatisticReceived += DryCalibrationProcess_StatisticReceived;
            rtdController.UpdateSensorReceived += DryCalibrationProcess_UpdateSensorReceived;

            // pressure monitor
            pressureController = new PressureController();
            pressureController.StatisticReceived += DryCalibrationProcess_StatisticReceived;
            pressureController.UpdateSensorReceived += DryCalibrationProcess_UpdateSensorReceived;

            // ultrasonic monitor
            ultrasonicController = new UltrasonicController();
            ultrasonicModel = ((UltrasonicController)ultrasonicController).UltrasonicModel;

            if (ultSampMode == UltSampMode.Automatic) 
            {      
                ultrasonicController.StatisticReceived += DryCalibrationProcess_StatisticReceived;
                ultrasonicController.UpdateSensorReceived += DryCalibrationProcess_UpdateSensorReceived;
            }

            UltrasonicSampleMode = ultSampMode;
            DryCalibrationIsAborted = false;
        }

        private void ExecuteNextState(FSMState next)
        {         
            //dispose del estado anterior
            this.CurrentState?.Dispose();

            switch (next)
            {
                case FSMState.INITIALIZED: 
                    this.CurrentState = new ReadyState(rtdController,
                                                       pressureController);
                    break;
                case FSMState.INITIALIZING:
                    this.CurrentState = new InitializingState(rtdController, 
                                                              pressureController);
                    break;
                case FSMState.STABILIZING:
                    this.CurrentState = new StabilizingState(rtdController,
                                                             pressureController);
                    break;
                case FSMState.OBTAINING_SAMPLES:

                    if (UltrasonicSampleMode == UltSampMode.Automatic)
                    {
                        ObtainingSampleState obtSampState = new ObtainingSampleState(rtdController,
                                                                                     pressureController,
                                                                                     ultrasonicController); // CurrentReportModel.Header);

                        obtSampState.ElapsedTimeControl += ObtainingSampleState_ElapsedTimeControl;
                        obtSampState.ObtainingSampleFinished += ObtSampState_ObtainingSampleFinished;
                        obtSampState.GenerateCsv += ObtSampState_GenerateCsv;

                        this.CurrentState = obtSampState;
                    }
                    else
                    {
                        ObtainingManualSampleState obtSampState = new ObtainingManualSampleState(rtdController, pressureController, ultrasonicModel);

                        obtSampState.ElapsedTimeControl += ObtainingSampleState_ElapsedTimeControl;
                        obtSampState.ObtainingSampleFinished += ObtSampState_ObtainingSampleFinished;
                        obtSampState.GenerateCsv += ObtSampState_GenerateCsv;


                        this.CurrentState = obtSampState;

                    }
                    
                    break;
                case FSMState.VALIDATING:
                    if (UltrasonicSampleMode == UltSampMode.Automatic)
                    {
                        CurrentAverages = ((ObtainingSampleState)CurrentState).Averages;
                    }
                    else 
                    {
                        CurrentAverages = ((ObtainingManualSampleState)CurrentState).Averages;
                    }
                    
                    ValidatingState validatingState = new ValidatingState(CurrentAverages, CurrentPressureCalibration, ultrasonicModel);
             
                    validatingState.ValidatingStateReady += ValidatingState_ValidatingStateReady;
                    validatingState.ValidationFinished += ValidatingState_ValidationFinished;
                
                    this.CurrentState = validatingState;
                    break;
                case FSMState.GENERATING_REPORT:
                    GeneratingReportState generatingReportState = new GeneratingReportState(CurrentReportModel,
                                                                                           CurrentValidatedResults,
                                                                                           CurrentAverages,
                                                                                           ultrasonicModel);

                    generatingReportState.GenerateReportSucceeded += GeneratingReportState_GenerateReportSucceeded;
                    this.CurrentState = generatingReportState;

                    break;
                case FSMState.ENDING:
                    this.CurrentState = new EndingState();
                    break;
                case FSMState.ERROR:
                    AbortDryCalibration();
                    return;
                case FSMState.REPOSE: //estado de reposo
                    return;
                default:
                    break;
            }

            RefreshState?.Invoke(CurrentState.Description);
            DryCalibrationStateChange?.Invoke(CurrentState.Name, ultrasonicModel);

            this.CurrentState.ExitState += CurrentState_ExitState;
            this.CurrentState.RefreshState += CurrentState_RefreshState;
            this.CurrentState.Execute();         
        }

        private void ObtSampState_GenerateCsv(List<Sample> samples, Sample averages)
        {
            GenerateCsv?.Invoke(samples, averages);   
        }

        public void Initialize()
        {
            this.ExecuteNextState(FSMState.INITIALIZING);
        }

        public void InitDryCalibration()
        {
            DryCalibrationIsAborted = false;
            this.ExecuteNextState(FSMState.STABILIZING);
        }

        public void CancelDryCalibration()
        {
            StopMonitorContollers();

            if (CurrentState != null)
            {
                this.CurrentState.ExitState -= CurrentState_ExitState;
                this.CurrentState.Dispose();
            }

  
            RefreshState?.Invoke("");
            this.ExecuteNextState(FSMState.ERROR);  
        }

        public void AbortDryCalibration()
        {
            DryCalibrationIsAborted = true;

            StopMonitorContollers();

            this.CurrentState.ExitState -= CurrentState_ExitState;
            this.CurrentState.Dispose();

            DryCalibrationAborted?.Invoke();
        }

        private void CurrentState_ExitState(FSMState next)
        {
            if (CurrentState.Name == next)
            {
                return;
            }

            this.CurrentState.ExitState -= CurrentState_ExitState;
            this.CurrentState.RefreshState -= CurrentState_RefreshState;

            this.ExecuteNextState(next);
        }

        private void CurrentState_RefreshState(string description)
        {
            RefreshState?.Invoke(description);
        }

        private void DryCalibrationProcess_UpdateSensorReceived(MonitorType type, object value)
        {
            UpdateSensorReceived?.Invoke(type, value);
        }

        private void DryCalibrationProcess_StatisticReceived(MonitorType type, StatisticValue value)
        {
            StatisticReceived?.Invoke(type, value);
        }

        private void ObtainingSampleState_ElapsedTimeControl(string strSampleNumber)
        {
           
            if (!DryCalibrationIsAborted)
            {
                UltrasonicModel ultrasonicModel = ((UltrasonicController)ultrasonicController).UltrasonicModel;

                int sampleNumber = Convert.ToInt32(strSampleNumber);

                if (sampleNumber <= 11)
                {
                    SampleObtained?.Invoke(sampleNumber, ultrasonicModel);

                    if (sampleNumber <= 10)
                    {
                        if (!DryCalibrationIsAborted)
                        {
                            RefreshState?.Invoke(String.Format("Muestra obtenida ({0}/10) ", sampleNumber.ToString()));
                        }             
                    }   
                }    
            }       
        }

        private void ObtSampState_ObtainingSampleFinished()
        {
            StopMonitorContollers();
            ObtainingSampleFinished?.Invoke();
        }

        private void ValidatingState_ValidatingStateReady()
        {

        }

        private void ValidatingState_ValidationFinished(ValidatedResult validationResult)
        {
            CurrentValidatedResults = validationResult;// guardar validaciones para el reporte

            ValidationFinished?.Invoke(validationResult);
        }

        private void GeneratingReportState_GenerateReportSucceeded(string fullReportPath)
        {
            GenerateReportSucceeded?.Invoke(fullReportPath);
        }

        private void StopMonitorContollers()
        {
           
            if (rtdController != null)
            {
                if (rtdController.Monitor != null)
                {
                    rtdController.Monitor.StopMonitor();
                }
            }

            if (pressureController != null)
            {
                if (pressureController.Monitor != null)
                {
                    pressureController.Monitor.StopMonitor();
                }
            }

            if (ultrasonicController != null)
            {
                if (ultrasonicController.Monitor != null)
                {
                    ultrasonicController.Monitor.StopMonitor();
                }
            }

        }

    }
}
