using System;
using System.IO;
using System.Timers;
using System.Windows.Controls;
using Proser.DryCalibration.controller.enums;
using Proser.DryCalibration.controller.pressure;
using Proser.DryCalibration.controller.rtd;
using Proser.DryCalibration.controller.ultrasonic;
using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.fsm.interfaces;
using Proser.DryCalibration.monitor.exceptions;
using Proser.DryCalibration.sensor.rtd;
using Proser.DryCalibration.sensor.rtd.calibration;
using Proser.DryCalibration.util;

namespace Proser.DryCalibration.App.mainapp
{
    public class DryCalibration : DryCalibrationBase, ITimerControl
    {   
        public event Action<string> ElapsedTimeControl;
        public event Action StartUpAborted;
        public event Action ModuleAdjustamentAborted;

        public Timer TimerControl { get; set; }
       
        public DryCalibration()
        {
            ProcessingStartUp = false;
            ProcessingDaqModuleAdjustament = false;
            ProcessingDryCalibration = false;

            TimerControl = new Timer(1000);
            TimerControl.Elapsed += TimerControl_Elapsed;
        }

        public void InitStartUp()
        {
            ProcessingStartUp = true;
            bool isAutomatic = CurrentModbusConfiguration.UltrasonicSampleMode == (int)UltSampMode.Automatic;

            rtdController = new RtdController();
            pressureController = new PressureController();
            ultrasonicController = new UltrasonicController();

            rtdController.UpdateSensorReceived += DryCalibration_UpdateSensorReceived;
            rtdController.RefreshState += DryCalProcess_RefreshState;

            pressureController.UpdateSensorReceived += DryCalibration_UpdateSensorReceived;
            pressureController.RefreshState += DryCalProcess_RefreshState;

            ultrasonicController.UpdateSensorReceived += DryCalibration_UpdateSensorReceived;
            ultrasonicController.StatisticReceived += DryCalibration_StatisticReceived;
            ultrasonicController.RefreshState += DryCalProcess_RefreshState;

            StatusMessage("Iniciando dispositivos...");

            try
            {
                rtdController.Initialize();
            }
            catch (MonitorInitializationException e)
            {
                StatusMessage("Ocurrió un error al iniciar el monitor de temperatura." + e.Message);

                log.Log.WriteIfExists("Ocurrió un error al iniciar el monitor de temperatura.", e);

                StartUpAborted?.Invoke();
                return;
            }

            try
            {
                pressureController.Initialize();
            }
            catch (MonitorInitializationException e)
            {
                StatusMessage("Ocurrió un error al iniciar el monitor de presión.");

                log.Log.WriteIfExists("Ocurrió un error al iniciar el monitor de presión.", e);

                StartUpAborted?.Invoke();
                return;
            }


            try
            {
                ultrasonicController.Initialize();
            }
            catch (MonitorInitializationException e)
            {
                log.Log.WriteIfExists("Ocurrió un error al iniciar el monitor ultrasónico.", e);

                if (isAutomatic) 
                {
                    StatusMessage("Ocurrió un error al iniciar el monitor ultrasónico.");

                    StartUpAborted?.Invoke();
                    return;
                }
            }

            StatusMessage("Todos los dispositivos están iniciados.");
           
        }

        public bool InitDaqModuleAdjustament()
        {
          
            rtdController = new RtdController();
            pressureController = new PressureController();

            rtdController.UpdateSensorReceived += DryCalibration_UpdateSensorReceived;
            pressureController.UpdateSensorReceived += DryCalibration_UpdateSensorReceived;

            StatusMessage("Iniciando dispositivos...");

            try
            {
                rtdController.Initialize();
            }
            catch (MonitorInitializationException e)
            {
                StatusMessage("Ocurrió un error al iniciar el monitor de temperatura.");

                ModuleAdjustamentAborted?.Invoke();
                return false;
            }

            try
            {
                pressureController.Initialize();
            }
            catch (MonitorInitializationException e)
            {
                StatusMessage("Ocurrió un error al iniciar el monitor de presión.");

                ModuleAdjustamentAborted?.Invoke();
                return false;
            }

            ProcessingDaqModuleAdjustament = true;
            StatusMessage("Todos los dispositivos están iniciados.");
            return true;
        }

        public void StopMonitorContollers()
        {
            ProcessingStartUp = false;
            ProcessingDaqModuleAdjustament = false;

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

        public void StartUpRTDActiveStateChange(int number, bool active)
        {
            try
            {
                string path = Path.Combine(Utils.ConfigurationPath, "RtdCalibration.xml");
                RtdTable calibration = RtdTable.Read(path);

                if (calibration != null)
                {
                    calibration.RtdSensors.Find(r => r.Number == number).Active = active ? 1 : 0;
                }

                RtdTable.Generate(path, calibration);

                StatusMessage("La configuración se guardó con exito.");

                System.Threading.Thread.Sleep(1000);

                StatusMessage("Listo.");

            }
            catch (Exception e)
            {
                StatusMessage("Ocurrió un error con la configuración.");
            }

        }

        public void UpdateRTDLayout(params TextBox[] txtRDT)
        {    
            int index = 0;

            foreach (Rtd item in this.RtdVal.Rtds)
            {
                if (item.Number == index)
                {
                    if (item.TempValue == -99)
                    {
                        SetText(txtRDT[index], "¡Error!");
                    }
                    else
                    {
                        SetText(txtRDT[index], item.TempValue);
                    }

                    txtRDT[index].Refresh();
                }

                index++;
            }

            if (RtdVal.CalibrationRTD.Average != -99)
            {
                SetText(txtRDT[12], RtdVal.CalibrationRTD.Average);
            }
            else
            {
                SetText(txtRDT[12], "¡Error!");
            }

            if (RtdVal.CalibrationRTD.Difference != -99) //(RtdVal.CalibrationRTD.Uncertainty != -99)
            {
                SetText(txtRDT[13], RtdVal.CalibrationRTD.Difference);
                //SetText(txtRDT[13], RtdVal.CalibrationRTD.Uncertainty);
            }
            else
            {
                SetText(txtRDT[13], "¡Error!");
            }
            
        }

        public void UpdateAdjPointRTDLayout(params TextBox[] txtRDT)
        {
            int index = 0;

            foreach (Rtd item in this.RtdVal.Rtds)
            {
                if (item.Number == index)
                {
                    if (item.RealResValue == -99)
                    {
                        SetText(txtRDT[index], "¡Error!");
                    }
                    else
                    {
                        SetText(txtRDT[index], item.RealResValue);
                    }

                    txtRDT[index].Refresh();
                }
                
                index++;
            }
        }

        public void UpdatePressureLayout(TextBox txtPress)
        {
            // Pressure monitor

            if (PressureVal.Value == -99)
            {
                SetText(txtPress, "¡Error!");
            }
            else
            {
                SetText(txtPress, PressureVal.Value);
            }
            
        }

        public void UpdateRTDConfiguration(params CheckBox[] activeCheck)
        {
            string path = Path.Combine(Utils.ConfigurationPath, "RtdCalibration.xml");
            RtdTable calibration = RtdTable.Read(path);

            if (calibration != null)
            {
                for (int i = 0; i < activeCheck.Length; i++)
                {
                    activeCheck[i].IsChecked = Convert.ToBoolean(calibration.RtdSensors.Find(r => r.Number == i).Active);
                }
            }

            StatusMessage("Listo.");
        }


        #region Timer Control

        private DateTime beginTime;

        private TimerControlState tiContSt;

        private void TimerControl_Elapsed(object sender, ElapsedEventArgs e)
        {

            if (CurrentModbusConfiguration.UltrasonicSampleMode == (int)UltSampMode.Automatic)
            {
                TimeSpan difference = DateTime.Now - beginTime;

                string minutes = difference.Minutes.ToString().PadLeft(2, '0');
                string seconds = difference.Seconds.ToString().PadLeft(2, '0');

                // tiempo total en segundos
                CurrentSecondsTimeProcess = (difference.Minutes * 60) + difference.Seconds;

                string timeElapsedStr = string.Format("{0}:{1}", minutes, seconds);

                ElapsedTimeControl?.Invoke(timeElapsedStr);
            }
            else
            {
                if (tiContSt == TimerControlState.Started) 
                {
                    // tiempo total en segundos
                    CurrentSecondsTimeProcess++;

                    TimeSpan total = TimeSpan.FromSeconds(CurrentSecondsTimeProcess);

                    string minutes = total.Minutes.ToString().PadLeft(2, '0');
                    string seconds = total.Seconds.ToString().PadLeft(2, '0');

                    string timeElapsedStr = string.Format("{0}:{1}", minutes, seconds);

                    ElapsedTimeControl?.Invoke(timeElapsedStr);
                }         

            }          
            
        }

        public void InitTimerControl()
        {
            beginTime = DateTime.Now;

            ElapsedTimeControl?.Invoke("00:00");
            CurrentSecondsTimeProcess = 0;
            TimerControl.Start();

            tiContSt = TimerControlState.Started;
        }

        public void StopTimerControl()
        {
            ElapsedTimeControl?.Invoke("00:00");
            CurrentSecondsTimeProcess = 0;
            TimerControl.Enabled = false;

            tiContSt = TimerControlState.Stopped;
        }

        public void PauseTimerControl() 
        {
            tiContSt = TimerControlState.Paused;
        }

        public void StartTimerControl() 
        {
            tiContSt = TimerControlState.Started;
        }

        public void Dispose()
        {
            TimerControl.Close();
            TimerControl.Dispose();
        }

       
        #endregion
    }
}
