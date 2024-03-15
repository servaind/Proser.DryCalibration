using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.statistic;
using Proser.DryCalibration.sensor.ultrasonic.modbus.configuration;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Linq;
using Proser.DryCalibration.sensor.rtd.calibration;
using Proser.DryCalibration.sensor.pressure.calibration;
using System.Collections.Generic;
using System.Threading;
using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.fsm.states;
using System.Windows.Media;
using System.Diagnostics;
using System.IO;
using Proser.DryCalibration.fsm.exceptions;
using Proser.DryCalibration.util;
using Proser.DryCalibration.controller.enums;

namespace Proser.DryCalibration.App
{
    public partial class MainWindow : Window
    {
        private mainapp.DryCalibration dryCalibration;
        private bool saveRTDAdjustament;
        private bool savePressAdjustament;
        private FSMState currenState;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (Mouse.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void ButtomPopUpSalir_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
            Process.GetCurrentProcess().Kill();
        }

        private void ButtonCloseMenu_Click(object sender, RoutedEventArgs e)
        {
            ButtonCloseMenu.Visibility = Visibility.Collapsed;
            ButtonOpenMenu.Visibility = Visibility.Visible;
        }
         
        private void ButtonOpenMenu_Click(object sender, RoutedEventArgs e)
        {
            ButtonCloseMenu.Visibility = Visibility.Visible;
            ButtonOpenMenu.Visibility = Visibility.Collapsed;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //instance
            dryCalibration = new mainapp.DryCalibration();
            Initialize();
        }

        private void Initialize()
        {
            //initial status
            SetTextBlock(Status, "");
            confGenCalibrationInformationCalibrationDate.Text = DateTime.Now.ToLongDateString();

            //events
            dryCalibration.RefreshState += DryCalibration_RefreshState;
            dryCalibration.StatisticReceived += DryCalibration_StatisticReceived;
            dryCalibration.UpdateSensorReceived += DryCalibration_UpdateSensorReceived;
            dryCalibration.ElapsedTimeControl += DryCalibration_ElapsedTimeControl;
            dryCalibration.StartUpAborted += DryCalibration_StartUpAborted;
            dryCalibration.ModuleAdjustamentAborted += DryCalibration_ModuleAdjustamentAborted;
            dryCalibration.DryCalibrationAborted += DryCalibration_DryCalibrationAborted;
            dryCalibration.DryCalibrationStateChange += DryCalibration_DryCalibrationStateChange;
            dryCalibration.SampleObtained += DryCalibration_SampleObtained;
            dryCalibration.ObtainingSampleFinished += DryCalibration_ObtainingSampleFinished;
            dryCalibration.ValidationFinished += DryCalibration_ValidationFinished;

            //visibility
            GridConfigurationUser.Visibility = Visibility.Visible;
            GrigConfigurationDevice.Visibility = Visibility.Hidden;

            //security
            DisableMainMenu();

            //communication
            LoadPortNames();

            //flags
            saveRTDAdjustament = false;
            savePressAdjustament = false;

            //focus 
            txtUserName.Focus();
        }

        private void DisableMainMenu()
        {
            this.mnuConfiguration.IsEnabled = false;
            this.mnuInit.IsEnabled = false;
            this.mnuCancel.IsEnabled = false;
            this.mnuReport.IsEnabled = false;
        }

        private void LoadPortNames()
        {
            string[] portNames = System.IO.Ports.SerialPort.GetPortNames();

            if (cbConfigSerialPortNames != null)
            {
                for (int i = 0; i < portNames.Length; i++)
                {
                    ComboBoxItem newItem = new ComboBoxItem()
                    {
                        Uid = i.ToString(),
                        Content = portNames[i],
                    };

                    cbConfigSerialPortNames.Items.Add(newItem);
                }

                if (portNames.Length > 0)
                {
                    cbConfigSerialPortNames.SelectedIndex = 0;
                }

            }
        }

        private void DryCalibration_RefreshState(string description)
        {
            SetTextBlock(Status, description);
            Status.Refresh();
        }

        private void BtnUserAccept_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtUserName.Text))
            {
                GridConfigurationUser.Visibility = Visibility.Hidden;
                GrigConfigurationDevice.Visibility = Visibility.Visible;

                SetTextBlock(Status, "Ingrese los datos requeridos para continuar con el ensayo.");
                confGenCalibrationInformationResponsible.Text = txtUserName.Text;
                mnuConfiguration.IsEnabled = true;
            }
        }

        private void ButtonPopUpUserConfig_Click(object sender, RoutedEventArgs e)
        {
            SetTextBlock(Status, "");

            DisableMainMenu();

            GrigConfigurationDevice.Visibility = Visibility.Hidden;
            GridConfigurationUser.Visibility = Visibility.Visible;
        }

        private void ConfigItemTabButton_Click(object sender, RoutedEventArgs e)
        {
            int index = int.Parse(((Button)e.Source).Uid);
            ChangeTabConfiguration(index);
        }

        private void MnuMain_MouseUp(object sender, MouseButtonEventArgs e)
        {
            int index = int.Parse(((ListViewItem)sender).Uid);
            switch (index)
            {
                case 0: // configuración               
                    // deshabilitar ensayo
                    SetInitCalibrationState();       
                    break;
                case 1: // iniciar ensayo
                    SetControlEnabled(mnuInit, false);
                    SetControlEnabled(mnuCancel, false);
                    SetControlEnabled(mnuConfiguration, false);
                    SetControlEnabled(mnuReport, false);
                    InitDryCalibration();
                    break;
                case 2: // cancelar ensayo
                    CancelDryCalibration();
                    break;
                case 3: // ver reporte
                    if (File.Exists(dryCalibration.CurrentFullReportPath))
                    {
                        Process.Start(new ProcessStartInfo(dryCalibration.CurrentFullReportPath));
                    }
                    break;
            }
        }

        private void SetInitCalibrationState()
        {
            // seleccionar configuración general
            ChangeTabConfiguration(0);

            SetControlEnabled(btnUserConfig, true);
            SetControlEnabled(mnuInit, false);
            SetControlEnabled(mnuCancel, false);
            
            dryCalibration.SetControlVisibility(GridDryCalibration, Visibility.Hidden);
            dryCalibration.SetControlVisibility(StatusTimeControl, Visibility.Hidden);
            dryCalibration.SetControlVisibility(GrigConfigurationDevice, Visibility.Visible);
        }

        private void CbConfigComunication_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            ComboBoxItem item = e.AddedItems[0] as ComboBoxItem;

            if (ConfigCommTCP != null && ConfigCommTCP != null)
            {
                switch (item.Content)
                {
                    case "Serial":
                        ConfigCommTCP.Visibility = Visibility.Hidden;
                        ConfigCommSerial.Visibility = Visibility.Visible;
                        break;
                    case "TCP":
                        ConfigCommSerial.Visibility = Visibility.Hidden;
                        ConfigCommTCP.Visibility = Visibility.Visible;
                        break;
                }
            }
        }

        private void CbPressureSensorTypeAdjustment_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            savePressAdjustament = true;
            ComboBoxItem item = e.AddedItems[0] as ComboBoxItem;
            int index = int.Parse(item.Uid);

            //if (txtAtmPressureAdjustment != null)
            //{
                switch (index)
                {
                    case 0: // Absoluto
                        //txtAtmPressureAdjustment.Visibility = Visibility.Hidden;
                        lblAdjModIndic.Content = "Indicación (barA):";
                        lblAdjModSensorRange.Content = "Rango del Sensor (barA):";
                        //lblStartUpInidicPressure.Content = "Indicación (barA):";
                        break;
                    case 1: // Relativo
                        //txtAtmPressureAdjustment.Visibility = Visibility.Visible;
                        lblAdjModIndic.Content = "Indicación (barg):";
                        lblAdjModSensorRange.Content = "Rango del Sensor (barg):";
                        //lblStartUpInidicPressure.Content = "Indicación (barg):";
                        break;
                }
            //}
        }

        private void BtnSaveCommUltrasonic_Click(object sender, RoutedEventArgs e)
        {
            string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");

            if (!File.Exists(path))
            {
                ModbusConfiguration.Generate(path);
            }

            ModbusConfiguration curModbusConfiguration = ModbusConfiguration.Read(path);


            ModbusConfiguration newModbusConfiguration = new ModbusConfiguration();
            newModbusConfiguration.UltGainConfig = new List<GainConfig>(curModbusConfiguration.UltGainConfig);

            try
            {
                // modelo del ultrasónico
                newModbusConfiguration.SlaveConfig.Model = cbCommUltrasonicModel.SelectedIndex;

                // comunicación
                bool isSerial = cbConfigComunication.SelectedIndex == 0;

                if (isSerial)
                {
                    //serial
                    newModbusConfiguration.MasterSerial.PortName = ((ComboBoxItem)cbConfigSerialPortNames.SelectedItem).Content.ToString();
                    newModbusConfiguration.MasterSerial.BaudRate = cbConfigSerialBaudRate.SelectedIndex;
                    newModbusConfiguration.MasterSerial.DataBits = cbConfigSerialDataBits.SelectedIndex;
                    newModbusConfiguration.MasterSerial.Parity = cbConfigSerialParity.SelectedIndex;
                    newModbusConfiguration.MasterSerial.StopBits = cbConfigSerialStopBits.SelectedIndex;
                }
                else
                {
                    //tcp
                    newModbusConfiguration.Tcp.IPAddress = txtConfigUltrasonicTcpIP.Text;
                    newModbusConfiguration.Tcp.PortNumber = Int32.Parse(txtConfigUltrasonicTcpPort.Text);
                }

                // modbus
                newModbusConfiguration.SlaveConfig.SlaveId = byte.Parse(txtConfigUltrasonicSlaveId.Text);
                newModbusConfiguration.SlaveConfig.ModbusFrameFormat = cbConfigModbusFrameFormat.SelectedIndex;

                // timing
                newModbusConfiguration.SlaveConfig.SampleInterval = Int32.Parse(txtConfigUltrasonicSampleInterval.Text);
                newModbusConfiguration.SlaveConfig.SampleTimeOut = Int32.Parse(txtConfigUltrasonicSampleTimeOut.Text);


                // límites de ganancia por cuerda del ultrasónico

                UltrasonicModel model = (UltrasonicModel)cbCommUltrasonicModel.SelectedIndex;

                foreach (GainConfig gain in newModbusConfiguration.UltGainConfig)
                {
                    if (gain.UltModel.Equals(model))
                    {
                        gain.Min = Convert.ToDouble(txtConfigUltrasonicGainMin.Text);
                        gain.Max = Convert.ToDouble(txtConfigUltrasonicGainMax.Text);

                        break;
                    }
                }

                // modo de obtención de muestras
                bool isAutomatic = cbUltSampleMode.SelectedIndex == 0;

                if (isAutomatic)
                {
                    newModbusConfiguration.UltrasonicSampleMode = (int)UltSampMode.Automatic;
                }
                else 
                {
                    newModbusConfiguration.UltrasonicSampleMode = (int)UltSampMode.Manual;
                }

                string configPath = ModbusConfiguration.Generate(path, newModbusConfiguration);

                if (!string.IsNullOrEmpty(configPath))
                {
                    log.Log.WriteIfExists("La configuración del dispositivo ultrasónico se guardó correctamente.");

                    SetTextBlock(Status, "La configuración se guardó con exito.");

                }
            }
            catch (Exception ex)
            {
                //log
                log.Log.WriteIfExists("Ocurrió un error al configurar el dispositivo ultrasónico. Se guardará la configuración por defecto.");
                log.Log.WriteIfExists(ex.Message, ex);

            }

            dryCalibration.SetCurrentConfiguration();
        }

        private void UltrasonicModel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (txtConfigUltrasonicGainMax != null) // se cargaron los controles
            {
                string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");

                ModbusConfiguration configuration = ModbusConfiguration.Read(path);

                if (configuration != null)
                {
                    UltrasonicModel model = (UltrasonicModel)cbCommUltrasonicModel.SelectedIndex;

                    GainConfig gainConfig = configuration.UltGainConfig.FirstOrDefault(g => g.UltModel.Equals(model));

                    if (gainConfig != null)
                    {
                        txtConfigUltrasonicGainMin.Text = Convert.ToString(gainConfig.Min);
                        txtConfigUltrasonicGainMax.Text = Convert.ToString(gainConfig.Max);
                    }
                }

            }
        }

        private void BtnSaveAdjDaqModule_Click(object sender, RoutedEventArgs e)
        {
            // temperatura
            SaveRTDCalibration();

            // presión
            SavePressureCalibration();
        }

        private void SavePressureCalibration()
        {
            if (!savePressAdjustament)
            {
                return;
            }

            savePressAdjustament = false;

            try
            {
                string path = Path.Combine(Utils.ConfigurationPath, "PressureCalibration.xml");

                PressureCalibration prevCalibration = PressureCalibration.Read(path);

                Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-AR");

                PressureCalibration pressureCalibration = new PressureCalibration()
                {
                    DaqModuleIPAddress = adjModIPAddress.Text,
                    Error = CalculteError(adjModPress1, adjModReferencePress, prevCalibration.Error),
                    Zero = Convert.ToInt32(adjModPressZero.Text),
                    Span = Convert.ToInt32(adjModPressSpan.Text),
                    SensorType = cbPressSensorTypeAdj.SelectedIndex
                };

                string configPath = PressureCalibration.Generate(path, pressureCalibration);

                if (!string.IsNullOrEmpty(configPath))
                {
                    log.Log.WriteIfExists("La configuración del sensor de presión se guardó correctamente.");
                }

                SetTextBlock(Status, "La configuración se guardó con exito.");

            }
            catch (Exception ex)
            {
                //log
                log.Log.WriteIfExists("Ocurrió un error al configurar el sensor de presión. Se guardará la configuración por defecto.");
                log.Log.WriteIfExists(ex.Message, ex);
            }

            savePressAdjustament = true;

        }

        private void SaveRTDCalibration()
        {
            if (!saveRTDAdjustament)
            {
                return;
            }

            saveRTDAdjustament = false;

            try
            {
                string path = Path.Combine(Utils.ConfigurationPath, "RtdCalibration.xml");
                RtdTable prevCalibration = RtdTable.Read(path);

                RtdCalibration prevCal1 = prevCalibration.RtdSensors.Find(r => r.Number == 0);
                RtdCalibration prevCal2 = prevCalibration.RtdSensors.Find(r => r.Number == 1);
                RtdCalibration prevCal3 = prevCalibration.RtdSensors.Find(r => r.Number == 2);
                RtdCalibration prevCal4 = prevCalibration.RtdSensors.Find(r => r.Number == 3);
                RtdCalibration prevCal5 = prevCalibration.RtdSensors.Find(r => r.Number == 4);
                RtdCalibration prevCal6 = prevCalibration.RtdSensors.Find(r => r.Number == 5);
                RtdCalibration prevCal7 = prevCalibration.RtdSensors.Find(r => r.Number == 6);
                RtdCalibration prevCal8 = prevCalibration.RtdSensors.Find(r => r.Number == 7);
                RtdCalibration prevCal9 = prevCalibration.RtdSensors.Find(r => r.Number == 8);
                RtdCalibration prevCal10 = prevCalibration.RtdSensors.Find(r => r.Number == 9);
                RtdCalibration prevCal11 = prevCalibration.RtdSensors.Find(r => r.Number == 10);
                RtdCalibration prevCal12 = prevCalibration.RtdSensors.Find(r => r.Number == 11);

                RtdTable rtdConfiguration = new RtdTable();
                rtdConfiguration.DaqModuleIPAddress = adjModIPAddress.Text;

                rtdConfiguration.TempPoints = new List<TempPoint>();
                rtdConfiguration.RtdSensors = new List<RtdCalibration>();

                // puntos de temperatura
                rtdConfiguration.TempPoints.Add(new TempPoint()
                {
                    Number = 1,
                    Value = adjLblBtnPoint1.Text != "Punto 1" ? Convert.ToDouble(adjLblBtnPoint1.Text) : 0
                });
                rtdConfiguration.TempPoints.Add(new TempPoint()
                {
                    Number = 2,
                    Value = adjLblBtnPoint2.Text != "Punto 2" ? Convert.ToDouble(adjLblBtnPoint2.Text) : 0
                });
                rtdConfiguration.TempPoints.Add(new TempPoint()
                {
                    Number = 3,
                    Value = adjLblBtnPoint3.Text != "Punto 3" ? Convert.ToDouble(adjLblBtnPoint3.Text) : 0
                });
                rtdConfiguration.TempPoints.Add(new TempPoint()
                {
                    Number = 4,
                    Value = adjLblBtnPoint4.Text != "Punto 4" ? Convert.ToDouble(adjLblBtnPoint4.Text) : 0
                });
                rtdConfiguration.TempPoints.Add(new TempPoint()
                {
                    Number = 5,
                    Value = adjLblBtnPoint5.Text != "Punto 5" ? Convert.ToDouble(adjLblBtnPoint5.Text) : 0
                });

                // RTD1
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk1.IsChecked,
                                         0,
                                         prevCal1,
                                         adjResPoint1RTD1,
                                         adjResPoint2RTD1,
                                         adjResPoint3RTD1,
                                         adjResPoint4RTD1,
                                         adjResPoint5RTD1)
                );

                // RTD2
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk2.IsChecked,
                                         1,
                                         prevCal2,
                                         adjResPoint1RTD2,
                                         adjResPoint2RTD2,
                                         adjResPoint3RTD2,
                                         adjResPoint4RTD2,
                                         adjResPoint5RTD2)
                );

                // RTD3
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk3.IsChecked,
                                         2,
                                         prevCal3,
                                         adjResPoint1RTD3,
                                         adjResPoint2RTD3,
                                         adjResPoint3RTD3,
                                         adjResPoint4RTD3,
                                         adjResPoint5RTD3)
                );

                // RTD4
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk4.IsChecked,
                                         3,
                                         prevCal4,
                                         adjResPoint1RTD4,
                                         adjResPoint2RTD4,
                                         adjResPoint3RTD4,
                                         adjResPoint4RTD4,
                                         adjResPoint5RTD4)
                );

                // RTD5
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk5.IsChecked,
                                         4,
                                         prevCal5,
                                         adjResPoint1RTD5,
                                         adjResPoint2RTD5,
                                         adjResPoint3RTD5,
                                         adjResPoint4RTD5,
                                         adjResPoint5RTD5)
                );

                // RTD6 
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk6.IsChecked,
                                         5,
                                         prevCal6,
                                         adjResPoint1RTD6,
                                         adjResPoint2RTD6,
                                         adjResPoint3RTD6,
                                         adjResPoint4RTD6,
                                         adjResPoint5RTD6)
                );

                // RTD7
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk7.IsChecked,
                                         6,
                                         prevCal7,
                                         adjResPoint1RTD7,
                                         adjResPoint2RTD7,
                                         adjResPoint3RTD7,
                                         adjResPoint4RTD7,
                                         adjResPoint5RTD7)
                );

                // RTD8
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk8.IsChecked,
                                         7,
                                         prevCal8,
                                         adjResPoint1RTD8,
                                         adjResPoint2RTD8,
                                         adjResPoint3RTD8,
                                         adjResPoint4RTD8,
                                         adjResPoint5RTD8)
                );
                // RTD9
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk9.IsChecked,
                                         8,
                                         prevCal9,
                                         adjResPoint1RTD9,
                                         adjResPoint2RTD9,
                                         adjResPoint3RTD9,
                                         adjResPoint4RTD9,
                                         adjResPoint5RTD9)
                );
                // RTD10
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk10.IsChecked,
                                         9,
                                         prevCal10,
                                         adjResPoint1RTD10,
                                         adjResPoint2RTD10,
                                         adjResPoint3RTD10,
                                         adjResPoint4RTD10,
                                         adjResPoint5RTD10)
                );
                // RTD11
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk11.IsChecked,
                                         10,
                                         prevCal11,
                                         adjResPoint1RTD11,
                                         adjResPoint2RTD11,
                                         adjResPoint3RTD11,
                                         adjResPoint4RTD11,
                                         adjResPoint5RTD11)
                );
                // RTD12
                rtdConfiguration.RtdSensors.Add(
                    SetResistancePoints((bool)adjModRTDActChk12.IsChecked,
                                         11,
                                         prevCal12,
                                         adjResPoint1RTD12,
                                         adjResPoint2RTD12,
                                         adjResPoint3RTD12,
                                         adjResPoint4RTD12,
                                         adjResPoint5RTD12)
                );

                path = Path.Combine(Utils.ConfigurationPath, "RtdCalibration.xml");
                string configPath = RtdTable.Generate(path, rtdConfiguration);

                if (!string.IsNullOrEmpty(configPath))
                {
                    SetTextBlock(Status, "La configuración se guardó correctamente");
                    log.Log.WriteIfExists("La configuración del los sensores de temperatura se guardó correctamente.");
                }
            }
            catch (Exception ex)
            {
                SetTextBlock(Status, "Ocurrió un error al configurar los sensores de temperatura");

                //log
                log.Log.WriteIfExists("Ocurrió un error al configurar los sensores de temperatura.");
                log.Log.WriteIfExists(ex.Message, ex);
            }

            saveRTDAdjustament = true;
        }

        private RtdCalibration SetResistancePoints(bool included,
                                                    int number,
                                                    RtdCalibration prevCalibration,
                                                    params TextBox[] values)
        {

            if (included)
            {
                List<ResPoint> resPoints = new List<ResPoint>();

                for (int i = 0; i < 5; i++)
                {
                    resPoints.Add(new ResPoint()
                    {
                        Number = i + 1,
                        Value = Convert.ToDouble(values[i].Text)
                    });
                }

                RtdCalibration calibration = new RtdCalibration
                {
                    Number = number,
                    ResPoints = resPoints,
                    Active = 1
                };

                return calibration;
            }

            return prevCalibration;
        }

        private decimal CalculteError(TextBox indicator, TextBox reference, decimal error)
        {
            try
            {
                if (indicator.Text != "¡Error!")
                {
                    Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-AR");

                    decimal indicatorValue = Convert.ToDecimal(indicator.Text);
                    decimal referenceValue = Convert.ToDecimal(reference.Text);

                    if (indicatorValue != 0 && referenceValue != 0)
                    {
                        return indicatorValue + error - referenceValue;
                    }
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private double CalculteError(TextBox indicator, TextBox reference)
        {
            try
            {
                if (indicator.Text != "¡Error!")
                {
                    Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-AR");

                    double indicatorValue = Convert.ToDouble(indicator.Text);
                    double referenceValue = Convert.ToDouble(reference.Text.Replace("_", ""));

                    if (indicatorValue != 0 && referenceValue != 0)
                    {
                        return indicatorValue - referenceValue;
                    }
                }

                return 0d;
            }
            catch
            {
                return 0d;
            }
        }

        private void BtnInitStartUp_Click(object sender, RoutedEventArgs e)
        {
            // iniciar controles
            CleanStartUp();

            btnInitStartUp.Visibility = Visibility.Hidden;
            btnStopStartUp.Visibility = Visibility.Visible;

            dryCalibration.InitStartUp();

        }

        private void BtnStopStartUp_Click(object sender, RoutedEventArgs e)
        {
            btnStopStartUp.Visibility = Visibility.Hidden;
            btnInitStartUp.Visibility = Visibility.Visible;

            SetTextBlock(Status, "");
            dryCalibration.StopMonitorContollers();
        }

        private void BtnInitDaqModuleAdjustament_Click(object sender, RoutedEventArgs e)
        {
            // iniciar controles
            CleanAdjustment();

            // inicio
            btnInitDaqModuleAdjustament.Visibility = Visibility.Hidden;
            btnStopDaqModuleAdjustament.Visibility = Visibility.Visible;

            bool initOk = dryCalibration.InitDaqModuleAdjustament();

            // habilitar botones de ajuste
            EnabledAdjustament(initOk);
        }

        private void BtnStopDaqModuleAdjustament_Click(object sender, RoutedEventArgs e)
        {
            // inicio
            btnStopDaqModuleAdjustament.Visibility = Visibility.Hidden;
            btnInitDaqModuleAdjustament.Visibility = Visibility.Visible;

            // deshabilitar botones de ajuste
            EnabledAdjustament(false);

            SetTextBlock(Status, "");
            dryCalibration.StopMonitorContollers();
        }

        private void BtnAdjPoint1_Click(object sender, RoutedEventArgs e)
        {
            try
            {

                saveRTDAdjustament = true;

                string strAdj = adjModRefTemp.Text.TrimStart().Replace("_", "");
                double reference = Convert.ToDouble(strAdj);
                SetTextBlock(adjLblBtnPoint1, reference);

                dryCalibration.UpdateAdjPointRTDLayout(adjResPoint1RTD1, adjResPoint1RTD2, adjResPoint1RTD3, adjResPoint1RTD4,
                                                       adjResPoint1RTD5, adjResPoint1RTD6, adjResPoint1RTD7, adjResPoint1RTD8,
                                                       adjResPoint1RTD9, adjResPoint1RTD10, adjResPoint1RTD11, adjResPoint1RTD12);
                SetTextBlock(Status, "");
            }
            catch
            {
                SetTextBlock(Status, "Verifique el valor de referencia ingresado.");
            }
        }

        private void BtnAdjPoint2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveRTDAdjustament = true;

                string strAdj = adjModRefTemp.Text.TrimStart().Replace("_", "");
                double reference = Convert.ToDouble(strAdj);
                SetTextBlock(adjLblBtnPoint2, reference);

                dryCalibration.UpdateAdjPointRTDLayout(adjResPoint2RTD1, adjResPoint2RTD2, adjResPoint2RTD3, adjResPoint2RTD4,
                                                       adjResPoint2RTD5, adjResPoint2RTD6, adjResPoint2RTD7, adjResPoint2RTD8,
                                                       adjResPoint2RTD9, adjResPoint2RTD10, adjResPoint2RTD11, adjResPoint2RTD12);
                SetTextBlock(Status, "");
            }
            catch
            {
                SetTextBlock(Status, "Verifique el valor de referencia ingresado");
            }
        }

        private void BtnAdjPoint3_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveRTDAdjustament = true;

                string strAdj = adjModRefTemp.Text.TrimStart().Replace("_", "");
                double reference = Convert.ToDouble(strAdj);
                SetTextBlock(adjLblBtnPoint3, reference);

                dryCalibration.UpdateAdjPointRTDLayout(adjResPoint3RTD1, adjResPoint3RTD2, adjResPoint3RTD3, adjResPoint3RTD4,
                                                       adjResPoint3RTD5, adjResPoint3RTD6, adjResPoint3RTD7, adjResPoint3RTD8,
                                                       adjResPoint3RTD9, adjResPoint3RTD10, adjResPoint3RTD11, adjResPoint3RTD12);
                SetTextBlock(Status, "");
            }
            catch
            {
                SetTextBlock(Status, "Verifique el valor de referencia ingresado");
            }
        }

        private void BtnAdjPoint4_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveRTDAdjustament = true;

                string strAdj = adjModRefTemp.Text.TrimStart().Replace("_", "");
                double reference = Convert.ToDouble(strAdj);
                SetTextBlock(adjLblBtnPoint4, reference);

                dryCalibration.UpdateAdjPointRTDLayout(adjResPoint4RTD1, adjResPoint4RTD2, adjResPoint4RTD3, adjResPoint4RTD4,
                                                       adjResPoint4RTD5, adjResPoint4RTD6, adjResPoint4RTD7, adjResPoint4RTD8,
                                                       adjResPoint4RTD9, adjResPoint4RTD10, adjResPoint4RTD11, adjResPoint4RTD12);
                SetTextBlock(Status, "");
            }
            catch
            {
                SetTextBlock(Status, "Verifique el valor de referencia ingresado");
            }
        }

        private void BtnAdjPoint5_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                saveRTDAdjustament = true;

                string strAdj = adjModRefTemp.Text.TrimStart().Replace("_", "");
                double reference = Convert.ToDouble(strAdj);
                SetTextBlock(adjLblBtnPoint5, reference);

                dryCalibration.UpdateAdjPointRTDLayout(adjResPoint5RTD1, adjResPoint5RTD2, adjResPoint5RTD3, adjResPoint5RTD4,
                                                       adjResPoint5RTD5, adjResPoint5RTD6, adjResPoint5RTD7, adjResPoint5RTD8,
                                                       adjResPoint5RTD9, adjResPoint5RTD10, adjResPoint5RTD11, adjResPoint5RTD12);
                SetTextBlock(Status, "");
            }
            catch
            {
                SetTextBlock(Status, "Verifique el valor de referencia ingresado");
            }
        }

        private void AdjModRefTemp_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Status.Text != "")
            {
                SetTextBlock(Status, "");
            }
        }

        private void BtnAdjPressCalculate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                savePressAdjustament = true;

                dryCalibration.UpdatePressureLayout(adjModPress1);

                SetText(adjModPressDif1, CalculteError(adjModPress1, adjModReferencePress));

            }
            catch
            {
                SetText(adjModReferencePress, 0d);

            }

        }

        private void BtnSaveGeneralConfiguration_Click(object sender, RoutedEventArgs e)
        {
            //guardar configuración general
            bool result = dryCalibration.SaveGeneralConfiguration(confGenUltrasBrand, confGenUltrasMaker, confGenUltrasModel, confGenUltrasDN, confGenUltrasSch,
                                                                  confGenUltrasSerie, confGenUltrasSerieNumber, confGenUltrasIdentification, confGenUltrasFirmwareVersion,
                                                                  confGenPetitionerBusinessName, confGenPetitionerBusinessAddress, confGenPetitionerRealizationPlace, confGenPetitionerRealizationAddress,
                                                                  confGenCalibrationInformationResponsible, confGenCalibrationInformationReportNumber, confGenCalibrationInformationCalibrationDate,
                                                                  confGenEnvironmentalConditionAtmosphericPressure);

            if (!result)
            {
                return;
            }

            // deshabilitar configuración
            //mnuConfiguration.IsEnabled = true;
            GridConfigurationUser.Visibility = Visibility.Hidden;
            GrigConfigurationDevice.Visibility = Visibility.Hidden;

            // habilitar ensayo
            btnUserConfig.IsEnabled = false;
            GridDryCalibration.Visibility = Visibility.Visible;
            ObtainingSampleState.Visibility = Visibility.Hidden;
            ValidatingState.Visibility = Visibility.Hidden;
            StabilizingState.Visibility = Visibility.Visible;

            mnuInit.IsEnabled = true;
            mnuCancel.IsEnabled = false;
            mnuReport.IsEnabled = false;

            saveRTDAdjustament = true;

            dryCalibration.UpdateRTDConfiguration(stateStabRTDActChk1, stateStabRTDActChk2, stateStabRTDActChk3, stateStabRTDActChk4,
                                                  stateStabRTDActChk5, stateStabRTDActChk6, stateStabRTDActChk7, stateStabRTDActChk8,
                                                  stateStabRTDActChk9, stateStabRTDActChk10, stateStabRTDActChk11, stateStabRTDActChk12);
            saveRTDAdjustament = false;



            dryCalibration.StopTimerControl();
        }

        private void ConfigurationGeneralItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            dryCalibration.SetColor(textBox, Brushes.White);          
        }

        private void BtnObtSampNext_Click(object sender, RoutedEventArgs e)
        {
            dryCalibration.InitDryCalibrationValidation();
        }

        private void BtnNextManSample_Click(object sender, RoutedEventArgs e)
        {            

            ((ObtainingManualSampleState)dryCalibration.CurrentState).InitTimerControl();

            dryCalibration.SetControlEnabled(btnNextManSample, false);

            int sampleNumber = ((ObtainingManualSampleState)dryCalibration.CurrentState).SampleNumber;
            UltrasonicModel ultrasonicModel = (UltrasonicModel)dryCalibration.CurrentModbusConfiguration.SlaveConfig.Model;

            if (sampleNumber == 10) { 

                ((ObtainingManualSampleState)dryCalibration.CurrentState).SampleNumber = 11; 

                SetSampleModeEdition(sampleNumber, ultrasonicModel, true);

                UpdateSampleValuesFromSampleLayout(sampleNumber, ultrasonicModel);

                ((ObtainingManualSampleState)dryCalibration.CurrentState).CalculateManualAverages();

                UpdateSampleValues(11, ultrasonicModel);

                ((ObtainingManualSampleState)dryCalibration.CurrentState).FinishProcess();

                dryCalibration.SetControlVisibility(btnNextManSample, Visibility.Hidden);     
            }
            else 
            {

                SetSampleModeEdition(sampleNumber, ultrasonicModel, true);

                UpdateSampleValuesFromSampleLayout(sampleNumber, ultrasonicModel);

                dryCalibration.StartTimerControl();
            }
       
        }

        private void BtnValidStateNext_Click(object sender, RoutedEventArgs e)
        {
            dryCalibration.InitReportGeneration();
        }

        private void BtnEditGenReportMeasurers_Click(object sender, RoutedEventArgs e)
        {
            btnEditGenReportMeasurers.Visibility = Visibility.Hidden;
            btnSaveGenReportMeasurers.Visibility = Visibility.Visible;

            EditMeasurersReportTables(true);

        }

        private void BtnSaveGenReportMeasurers_Click(object sender, RoutedEventArgs e)
        {
            btnSaveGenReportMeasurers.Visibility = Visibility.Hidden;
            btnEditGenReportMeasurers.Visibility = Visibility.Visible;

            dryCalibration.SaveReportMeasuringConfiguration(GenRepMeasTable1, GenRepMeasTable2, GenRepMeasTable3,
                                                            GenRepMeasTable4, GenRepMeasTable5, GenRepMeasTable6,
                                                            GenRepMeasTable7, GenRepMeasTable8, GenRepMeasTable9,
                                                            GenRepMeasTable10, GenRepMeasTable11, GenRepMeasTable12,
                                                            GenRepMeasTable13, GenRepMeasTable14, GenRepMeasTable15);

            EditMeasurersReportTables(false);
        }

        private void EditMeasurersReportTables(bool editable)
        {
            GenRepMeasTable1.IsReadOnly = !editable;
            GenRepMeasTable2.IsReadOnly = !editable;
            GenRepMeasTable3.IsReadOnly = !editable;
            GenRepMeasTable4.IsReadOnly = !editable;
            GenRepMeasTable5.IsReadOnly = !editable;
            GenRepMeasTable6.IsReadOnly = !editable;
            GenRepMeasTable7.IsReadOnly = !editable;
            GenRepMeasTable8.IsReadOnly = !editable;
            GenRepMeasTable9.IsReadOnly = !editable;
            GenRepMeasTable10.IsReadOnly = !editable;
            GenRepMeasTable11.IsReadOnly = !editable;
            GenRepMeasTable12.IsReadOnly = !editable;
            GenRepMeasTable13.IsReadOnly = !editable;
            GenRepMeasTable14.IsReadOnly = !editable;
            GenRepMeasTable15.IsReadOnly = !editable;
        }

        private void BtnGenReportNext_Click(object sender, RoutedEventArgs e)
        {
            SetControlEnabled(mnuCancel, false);
            SetControlEnabled(mnuConfiguration, false);

            SetControlEnabled(BtnGenReportNext, false);
            SetControlEnabled(btnEditGenReportMeasurers, false);
            SetControlEnabled(btnSaveGenReportMeasurers, false);
            
            dryCalibration.GenerateReport(GenRepMeasObservation, GenRepMeasTable1, GenRepMeasTable2, GenRepMeasTable3, GenRepMeasTable4,
                                          GenRepMeasTable5, GenRepMeasTable6, GenRepMeasTable7, GenRepMeasTable8,
                                          GenRepMeasTable9, GenRepMeasTable10, GenRepMeasTable11, GenRepMeasTable12,
                                          GenRepMeasTable13, GenRepMeasTable14, GenRepMeasTable15);
        }

        private void DryCalibration_ModuleAdjustamentAborted()
        {
            // inicio
            btnStopDaqModuleAdjustament.Visibility = Visibility.Hidden;
            btnInitDaqModuleAdjustament.Visibility = Visibility.Visible;

            dryCalibration.StopMonitorContollers();
        }

        private void DryCalibration_StartUpAborted()
        {
            btnStopStartUp.Visibility = Visibility.Hidden;
            btnInitStartUp.Visibility = Visibility.Visible;

            dryCalibration.StopMonitorContollers();
        }

        private void DryCalibration_DryCalibrationAborted()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action(DryCalibration_DryCalibrationAborted));
            }
            else
            {
                //mnuCancel.IsEnabled = false;
                mnuConfiguration.IsEnabled = true;

                //StatusTimeControl.Visibility = Visibility.Hidden;
                dryCalibration.StopTimerControl();

            }    
        }

        private void DryCalibration_DryCalibrationStateChange(FSMState current, UltrasonicModel ultrasonicModel)
        {
            this.currenState = current;

            if (current != FSMState.ERROR)
            {
                dryCalibration.SetControlVisibility(StabilizingState, Visibility.Hidden);
                dryCalibration.SetControlVisibility(ObtainingSampleState, Visibility.Hidden);
                dryCalibration.SetControlVisibility(ValidatingState, Visibility.Hidden);
                dryCalibration.SetControlVisibility(GeneratingReportState, Visibility.Hidden);

                //....

            }

            switch (current)
            {
                case FSMState.REPOSE:
                    // estado de reposo
                    break;

                case FSMState.INITIALIZED:
                    dryCalibration.SetControlVisibility(StabilizingState, Visibility.Visible);

                    dryCalibration.UpdateRTDLayout(stateStabRtd1, stateStabRtd2, stateStabRtd3, stateStabRtd4,
                    stateStabRtd5, stateStabRtd6, stateStabRtd7, stateStabRtd8, stateStabRtd9, stateStabRtd10, stateStabRtd11,
                    stateStabRtd12, stateStabRtdAVG, stateStabRtdDifference);

                    SetControlEnabled(mnuCancel, true);
                    SetControlEnabled(mnuConfiguration, false);

                    dryCalibration.InitDryCalibration();
                    dryCalibration.InitTimerControl();

                    break;
                case FSMState.ERROR:
             
                    break;
                case FSMState.INITIALIZING:
                    dryCalibration.SetControlVisibility(StabilizingState, Visibility.Visible);
                    StatusTimeControl.Visibility = Visibility.Visible;
                    break;
                case FSMState.STABILIZING:
                    dryCalibration.SetControlVisibility(StabilizingState, Visibility.Visible);
                    break;
                case FSMState.OBTAINING_SAMPLES:

                    SetControlEnabled(mnuCancel, true);
                    SetControlEnabled(mnuConfiguration, false);

                    CleanPreviusState(current, ultrasonicModel);
                    dryCalibration.SetControlVisibility(btnObtSampNext, Visibility.Hidden);
                    dryCalibration.SetControlVisibility(ObtainingSampleState, Visibility.Visible);

                    UltSampMode mode = (UltSampMode)dryCalibration.CurrentModbusConfiguration.UltrasonicSampleMode;

                    if (mode == UltSampMode.Manual)
                    {
                        dryCalibration.SetControlVisibility(btnNextManSample, Visibility.Visible);
                    }
                    else 
                    {
                        dryCalibration.SetControlVisibility(btnNextManSample, Visibility.Hidden);
                    }
                  
                    dryCalibration.SetSampleTableConfiguration(ObtSampStateRopeTableDaniel,
                                                               ObtSampStateRopeTableDaniel1R,
                                                               ObtSampStateRopeTableDaniel2R,
                                                               ObtSampStateRopeTableFmu,
                                                               ObtSampStateRopeTableSick,
                                                               ObtSampStateRopeTableInstrometS5,
                                                               ObtSampStateRopeTableInstrometS6,
                                                               ObtSampStateRopeTableKrohneAltV12,
                                                               
                                                               ObtSampAverageUltDaniel,
                                                               ObtSampAverageUltDaniel1R,
                                                               ObtSampAverageUltDaniel2R,
                                                               ObtSampAverageUltFmu,
                                                               ObtSampAverageUltSick,
                                                               ObtSampAverageUltInstS5,
                                                               ObtSampAverageUltInstS6,
                                                               ObtSampAverageUltKrohneAltV12,
                                                               
                                                               ObtSampEffUltDaniel,
                                                               ObtSampEffUltDaniel1R,
                                                               ObtSampEffUltDaniel2R,
                                                               ObtSampEffUltFmu,
                                                               ObtSampEffUltSick,
                                                               ObtSampEffUltInstS5,
                                                               ObtSampEffUltInstS6,
                                                               ObtSampEffUltKrohneAltV12,
                                                               
                                                               ObtSampGainUltDaniel,
                                                               ObtSampGainUltDaniel1R,
                                                               ObtSampGainUltDaniel2R,
                                                               ObtSampGainUltFMU,
                                                               ObtSampGainUltSick,
                                                               ObtSampGainUltInstS5,
                                                               ObtSampGainUltInstS6,
                                                               ObtSampGainUltKrohneAltV12);

                    dryCalibration.UpdateObtainedSampleRTDDiffLayout(ObtSampRtdAVG, ObtSampRtdDifference);
                    
                    break;
                case FSMState.VALIDATING:

                    SetControlEnabled(mnuCancel, true);
                    SetControlEnabled(mnuConfiguration, false);

                    CleanPreviusState(current, ultrasonicModel);

                    dryCalibration.SetControlVisibility(StatusTimeControl, Visibility.Hidden);
                    dryCalibration.SetControlVisibility(btnValidStateNext, Visibility.Hidden);
                    dryCalibration.SetControlVisibility(ValidatingState, Visibility.Visible);

                    string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");
                    ModbusConfiguration configuration = ModbusConfiguration.Read(path);

                    //límites en el nivel de ganancia según modelo del ultrasónico
                    GainConfig config = configuration.UltGainConfig.FirstOrDefault(c => (int)c.UltModel == (int)configuration.SlaveConfig.Model);

                    string content = string.Format("Admisible [dB]:  Mín. {0} Máx. {1} ",
                        Utils.DecimalComplete(config.Min, 1),
                        Utils.DecimalComplete(config.Max, 1));

                    SetContent(lblValidSampGainLimits, content);



                    dryCalibration.SetValidatingTableConfiguration(GridValidSampFlowAvgDaniel,
                                                                   GridValidSampFlowAvgDaniel1R,
                                                                   GridValidSampFlowAvgDaniel2R,
                                                                   GridValidSampFlowAvgFmu,
                                                                   GridValidSampFlowAvgSick,
                                                                   GridValidSampFlowAvgInstS5,
                                                                   GridValidSampFlowAvgInstS6,
                                                                   GridValidSampFlowAvgKrohneAltV12,
                                                                   GridValidSampSoundErrDaniel,
                                                                   GridValidSampSoundErrDaniel1R,
                                                                   GridValidSampSoundErrDaniel2R,
                                                                   GridValidSampSoundErrFmu,
                                                                   GridValidSampSoundErrSick,
                                                                   GridValidSampSoundErrInstS5,
                                                                   GridValidSampSoundErrInstS6,
                                                                   GridValidSampSoundErrKrohneAltV12,
                                                                   GridValidSampEffUltDaniel,
                                                                   GridValidSampEffUltDaniel1R,
                                                                   GridValidSampEffUltDaniel2R,
                                                                   GridValidSampEffUltFmu,
                                                                   GridValidSampEffUltSick,
                                                                   GridValidSampEffUltInstS5,
                                                                   GridValidSampEffUltInstS6,
                                                                   GridValidSampEffUltKrohneAltV12,
                                                                   GridValidSampGainUltDaniel,
                                                                   GridValidSampGainUltDaniel1R,
                                                                   GridValidSampGainUltDaniel2R,
                                                                   GridValidSampGainUltFMU,
                                                                   GridValidSampGainUltSick,
                                                                   GridValidSampGainUltIntS5,
                                                                   GridValidSampGainUltIntS6,
                                                                   GridValidSampGainUltKrohneAltV12);

                    break;
                case FSMState.GENERATING_REPORT:

                    SetControlEnabled(mnuCancel, true);
                    SetControlEnabled(mnuConfiguration, false);

                    SetControlEnabled(BtnGenReportNext, true);
                    SetControlEnabled(btnEditGenReportMeasurers, true);
                    SetControlEnabled(btnSaveGenReportMeasurers, true);

                    CleanPreviusState(current, ultrasonicModel);

                    dryCalibration.SetControlVisibility(GeneratingReportState, Visibility.Visible);
                    dryCalibration.SetReportMeasurersTableConfiguration( GenRepMeasTable1, GenRepMeasTable2, GenRepMeasTable3, 
                                                                         GenRepMeasTable4, GenRepMeasTable5, GenRepMeasTable6,
                                                                         GenRepMeasTable7, GenRepMeasTable8, GenRepMeasTable9, 
                                                                         GenRepMeasTable10,GenRepMeasTable11, GenRepMeasTable12,
                                                                         GenRepMeasTable13, GenRepMeasTable14, GenRepMeasTable15);
                    break;
                case FSMState.ENDING:
                    CleanPreviusState(current, ultrasonicModel);
                    SetControlEnabled(mnuConfiguration, true);

                    bool reportOk = !string.IsNullOrEmpty(dryCalibration.CurrentFullReportPath);
                    SetControlEnabled(mnuReport, reportOk);

                    SetInitCalibrationState();

                    break;
                default:
                    break;
            }
        }

        private void SetSampleModeEdition(int sampleNum, UltrasonicModel ultrasonicModel, bool readOnly)
        {

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<int, UltrasonicModel, bool>(SetSampleModeEdition), sampleNum, ultrasonicModel, readOnly);
            }
            else
            {
                sampleNum--;

                TextBox flowSampleA = null;
                TextBox flowSampleB = null;
                TextBox flowSampleC = null;
                TextBox flowSampleD = null;
                TextBox flowSampleE = null;
                TextBox flowSampleF = null;
                TextBox flowSampleG = null;
                TextBox flowSampleH = null;

                TextBox soundSampleA = null;
                TextBox soundSampleB = null;
                TextBox soundSampleC = null;
                TextBox soundSampleD = null;
                TextBox soundSampleE = null;
                TextBox soundSampleF = null;
                TextBox soundSampleG = null;
                TextBox soundSampleH = null;

                TextBox effSampleA = null;
                TextBox effSampleB = null;
                TextBox effSampleC = null;
                TextBox effSampleD = null;
                TextBox effSampleE = null;
                TextBox effSampleF = null;
                TextBox effSampleG = null;
                TextBox effSampleH = null;

                TextBox gainSampleA1 = null;
                TextBox gainSampleB1 = null;
                TextBox gainSampleC1 = null;
                TextBox gainSampleD1 = null;
                TextBox gainSampleE1 = null;
                TextBox gainSampleF1 = null;
                TextBox gainSampleG1 = null;
                TextBox gainSampleH1 = null;

                TextBox gainSampleA2 = null;
                TextBox gainSampleB2 = null;
                TextBox gainSampleC2 = null;
                TextBox gainSampleD2 = null;
                TextBox gainSampleE2 = null;
                TextBox gainSampleF2 = null;
                TextBox gainSampleG2 = null;
                TextBox gainSampleH2 = null;

                switch (ultrasonicModel)
                {
                case UltrasonicModel.Daniel:
                    flowSampleA = ObtSampFlowDanielA.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleB = ObtSampFlowDanielB.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleC = ObtSampFlowDanielC.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleD = ObtSampFlowDanielD.Children.OfType<TextBox>().ElementAt(sampleNum);

                  
                    soundSampleA = ObtSampSoundDanielA.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleB = ObtSampSoundDanielB.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleC = ObtSampSoundDanielC.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleD = ObtSampSoundDanielD.Children.OfType<TextBox>().ElementAt(sampleNum);


                    effSampleA = ObtSampEffDanielA.Children.OfType<TextBox>().ElementAt(0);
                    effSampleB = ObtSampEffDanielB.Children.OfType<TextBox>().ElementAt(0);
                    effSampleC = ObtSampEffDanielC.Children.OfType<TextBox>().ElementAt(0);
                    effSampleD = ObtSampEffDanielD.Children.OfType<TextBox>().ElementAt(0);

                    gainSampleA1 = ObtSampGainsDanielA.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleA2 = ObtSampGainsDanielA.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleB1 = ObtSampGainsDanielB.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleB2 = ObtSampGainsDanielB.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleC1 = ObtSampGainsDanielC.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleC2 = ObtSampGainsDanielC.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleD1 = ObtSampGainsDanielD.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleD2 = ObtSampGainsDanielD.Children.OfType<TextBox>().ElementAt(1);

                        break;
                    case UltrasonicModel.DanielJunior1R:
                        flowSampleA = ObtSampFlowDaniel1RA.Children.OfType<TextBox>().ElementAt(sampleNum);
                        soundSampleA = ObtSampSoundDaniel1RA.Children.OfType<TextBox>().ElementAt(sampleNum);
                        effSampleA = ObtSampEffDaniel1RA.Children.OfType<TextBox>().ElementAt(0);
                        gainSampleA1 = ObtSampGainsDaniel1RA.Children.OfType<TextBox>().ElementAt(0);
                        gainSampleA2 = ObtSampGainsDaniel1RA.Children.OfType<TextBox>().ElementAt(1);
                      
                        break;
                    case UltrasonicModel.DanielJunior2R:
                        flowSampleA = ObtSampFlowDaniel2RA.Children.OfType<TextBox>().ElementAt(sampleNum);
                        flowSampleB = ObtSampFlowDaniel2RB.Children.OfType<TextBox>().ElementAt(sampleNum);
                        soundSampleA = ObtSampSoundDaniel2RA.Children.OfType<TextBox>().ElementAt(sampleNum);
                        soundSampleB = ObtSampSoundDaniel2RB.Children.OfType<TextBox>().ElementAt(sampleNum);
                        effSampleA = ObtSampEffDaniel2RA.Children.OfType<TextBox>().ElementAt(0);
                        effSampleB = ObtSampEffDaniel2RB.Children.OfType<TextBox>().ElementAt(0);
                        gainSampleA1 = ObtSampGainsDaniel2RA.Children.OfType<TextBox>().ElementAt(0);
                        gainSampleA2 = ObtSampGainsDaniel2RA.Children.OfType<TextBox>().ElementAt(1);
                        gainSampleB1 = ObtSampGainsDaniel2RB.Children.OfType<TextBox>().ElementAt(0);
                        gainSampleB2 = ObtSampGainsDaniel2RB.Children.OfType<TextBox>().ElementAt(1);
                   
                        break;
                    case UltrasonicModel.InstrometS5:
                    flowSampleA = ObtSampFlowInstS5A.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleB = ObtSampFlowInstS5B.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleC = ObtSampFlowInstS5C.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleD = ObtSampFlowInstS5D.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleE = ObtSampFlowInstS5E.Children.OfType<TextBox>().ElementAt(sampleNum);

                    soundSampleA = ObtSampSoundInstS5A.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleB = ObtSampSoundInstS5B.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleC = ObtSampSoundInstS5C.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleD = ObtSampSoundInstS5D.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleE = ObtSampSoundInstS5E.Children.OfType<TextBox>().ElementAt(sampleNum);

                    effSampleA = ObtSampEffIntS5A.Children.OfType<TextBox>().ElementAt(0);
                    effSampleB = ObtSampEffIntS5B.Children.OfType<TextBox>().ElementAt(0);
                    effSampleC = ObtSampEffIntS5C.Children.OfType<TextBox>().ElementAt(0);
                    effSampleD = ObtSampEffIntS5D.Children.OfType<TextBox>().ElementAt(0);
                    effSampleE = ObtSampEffIntS5E.Children.OfType<TextBox>().ElementAt(0);

                    gainSampleA1 = ObtSampGainsInstS5A.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleA2 = ObtSampGainsInstS5A.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleB1 = ObtSampGainsInstS5B.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleB2 = ObtSampGainsInstS5B.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleC1 = ObtSampGainsInstS5C.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleC2 = ObtSampGainsInstS5C.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleD1 = ObtSampGainsInstS5D.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleD2 = ObtSampGainsInstS5D.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleE1 = ObtSampGainsInstS5E.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleE2 = ObtSampGainsInstS5E.Children.OfType<TextBox>().ElementAt(1);

                        break;
                case UltrasonicModel.InstrometS6:
                    flowSampleA = ObtSampFlowInstS6A.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleB = ObtSampFlowInstS6B.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleC = ObtSampFlowInstS6C.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleD = ObtSampFlowInstS6D.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleE = ObtSampFlowInstS6E.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleF = ObtSampFlowInstS6F.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleG = ObtSampFlowInstS6G.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleH = ObtSampFlowInstS6H.Children.OfType<TextBox>().ElementAt(sampleNum);

                   
                    soundSampleA = ObtSampSoundInstS6A.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleB = ObtSampSoundInstS6B.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleC = ObtSampSoundInstS6C.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleD = ObtSampSoundInstS6D.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleE = ObtSampSoundInstS6E.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleF = ObtSampSoundInstS6F.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleG = ObtSampSoundInstS6G.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleH = ObtSampSoundInstS6H.Children.OfType<TextBox>().ElementAt(sampleNum);


                    effSampleA = ObtSampEffIntS6A.Children.OfType<TextBox>().ElementAt(0);
                    effSampleB = ObtSampEffIntS6B.Children.OfType<TextBox>().ElementAt(0);
                    effSampleC = ObtSampEffIntS6C.Children.OfType<TextBox>().ElementAt(0);
                    effSampleD = ObtSampEffIntS6D.Children.OfType<TextBox>().ElementAt(0);
                    effSampleE = ObtSampEffIntS6E.Children.OfType<TextBox>().ElementAt(0);
                    effSampleF = ObtSampEffIntS6F.Children.OfType<TextBox>().ElementAt(0);
                    effSampleG = ObtSampEffIntS6G.Children.OfType<TextBox>().ElementAt(0);
                    effSampleH = ObtSampEffIntS6H.Children.OfType<TextBox>().ElementAt(0);

                    gainSampleA1 = ObtSampGainsInstS6A.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleA2 = ObtSampGainsInstS6A.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleB1 = ObtSampGainsInstS6B.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleB2 = ObtSampGainsInstS6B.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleC1 = ObtSampGainsInstS6C.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleC2 = ObtSampGainsInstS6C.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleD1 = ObtSampGainsInstS6D.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleD2 = ObtSampGainsInstS6D.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleE1 = ObtSampGainsInstS6E.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleE2 = ObtSampGainsInstS6E.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleF1 = ObtSampGainsInstS6F.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleF2 = ObtSampGainsInstS6F.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleG1 = ObtSampGainsInstS6G.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleG2 = ObtSampGainsInstS6G.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleH1 = ObtSampGainsInstS6H.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleH2 = ObtSampGainsInstS6H.Children.OfType<TextBox>().ElementAt(1);

                        break;
                case UltrasonicModel.Sick:
                    flowSampleA = ObtSampFlowSickA.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleB = ObtSampFlowSickB.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleC = ObtSampFlowSickC.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleD = ObtSampFlowSickD.Children.OfType<TextBox>().ElementAt(sampleNum);

                    soundSampleA = ObtSampSoundSickA.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleB = ObtSampSoundSickB.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleC = ObtSampSoundSickC.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleD = ObtSampSoundSickD.Children.OfType<TextBox>().ElementAt(sampleNum);


                    effSampleA = ObtSampEffSickA.Children.OfType<TextBox>().ElementAt(0);
                    effSampleB = ObtSampEffSickB.Children.OfType<TextBox>().ElementAt(0);
                    effSampleC = ObtSampEffSickC.Children.OfType<TextBox>().ElementAt(0);
                    effSampleD = ObtSampEffSickD.Children.OfType<TextBox>().ElementAt(0);

                    gainSampleA1 = ObtSampGainsSickA.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleA2 = ObtSampGainsSickA.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleB1 = ObtSampGainsSickB.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleB2 = ObtSampGainsSickB.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleC1 = ObtSampGainsSickC.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleC2 = ObtSampGainsSickC.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleD1 = ObtSampGainsSickD.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleD2 = ObtSampGainsSickD.Children.OfType<TextBox>().ElementAt(1);

                        break;
                case UltrasonicModel.FMU:
                    flowSampleA = ObtSampFlowFmuA.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleB = ObtSampFlowFmuB.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleC = ObtSampFlowFmuC.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleD = ObtSampFlowFmuD.Children.OfType<TextBox>().ElementAt(sampleNum);

                    soundSampleA = ObtSampSoundFmuA.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleB = ObtSampSoundFmuB.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleC = ObtSampSoundFmuC.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleD = ObtSampSoundFmuD.Children.OfType<TextBox>().ElementAt(sampleNum);
                
                    effSampleA = ObtSampEffFmuA.Children.OfType<TextBox>().ElementAt(0);
                    effSampleB = ObtSampEffFmuB.Children.OfType<TextBox>().ElementAt(0);
                    effSampleC = ObtSampEffFmuC.Children.OfType<TextBox>().ElementAt(0);
                    effSampleD = ObtSampEffFmuD.Children.OfType<TextBox>().ElementAt(0);

                    gainSampleA1 = ObtSampGainsFMUA.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleA2 = ObtSampGainsFMUA.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleB1 = ObtSampGainsFMUB.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleB2 = ObtSampGainsFMUB.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleC1 = ObtSampGainsFMUC.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleC2 = ObtSampGainsFMUC.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleD1 = ObtSampGainsFMUD.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleD2 = ObtSampGainsFMUD.Children.OfType<TextBox>().ElementAt(1);

                        break;
                case UltrasonicModel.KrohneAltosonicV12:
                    flowSampleA = ObtSampFlowKrohneAltV12A.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleB = ObtSampFlowKrohneAltV12B.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleC = ObtSampFlowKrohneAltV12C.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleD = ObtSampFlowKrohneAltV12D.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleE = ObtSampFlowKrohneAltV12E.Children.OfType<TextBox>().ElementAt(sampleNum);
                    flowSampleF = ObtSampFlowKrohneAltV12F.Children.OfType<TextBox>().ElementAt(sampleNum);

                    soundSampleA = ObtSampSoundKrohneAltV12A.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleB = ObtSampSoundKrohneAltV12B.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleC = ObtSampSoundKrohneAltV12C.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleD = ObtSampSoundKrohneAltV12D.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleE = ObtSampSoundKrohneAltV12E.Children.OfType<TextBox>().ElementAt(sampleNum);
                    soundSampleF = ObtSampSoundKrohneAltV12F.Children.OfType<TextBox>().ElementAt(sampleNum);
          
                    effSampleA = ObtSampEffKrohneAltV12A.Children.OfType<TextBox>().ElementAt(0);
                    effSampleB = ObtSampEffKrohneAltV12B.Children.OfType<TextBox>().ElementAt(0);
                    effSampleC = ObtSampEffKrohneAltV12C.Children.OfType<TextBox>().ElementAt(0);
                    effSampleD = ObtSampEffKrohneAltV12D.Children.OfType<TextBox>().ElementAt(0);
                    effSampleE = ObtSampEffKrohneAltV12E.Children.OfType<TextBox>().ElementAt(0);
                    effSampleF = ObtSampEffKrohneAltV12F.Children.OfType<TextBox>().ElementAt(0);

                    gainSampleA1 = ObtSampGainsKrohneAltV12A.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleA2 = ObtSampGainsKrohneAltV12A.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleB1 = ObtSampGainsKrohneAltV12B.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleB2 = ObtSampGainsKrohneAltV12B.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleC1 = ObtSampGainsKrohneAltV12C.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleC2 = ObtSampGainsKrohneAltV12C.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleD1 = ObtSampGainsKrohneAltV12D.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleD2 = ObtSampGainsKrohneAltV12D.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleE1 = ObtSampGainsKrohneAltV12E.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleE2 = ObtSampGainsKrohneAltV12E.Children.OfType<TextBox>().ElementAt(1);
                    gainSampleF1 = ObtSampGainsKrohneAltV12F.Children.OfType<TextBox>().ElementAt(0);
                    gainSampleF2 = ObtSampGainsKrohneAltV12F.Children.OfType<TextBox>().ElementAt(1);

                        break;
                default:
                    break;
                }

                SetEditable(flowSampleA, readOnly);
                SetEditable(flowSampleB, readOnly);
                SetEditable(flowSampleC, readOnly);
                SetEditable(flowSampleD, readOnly);
                SetEditable(flowSampleE, readOnly);
                SetEditable(flowSampleF, readOnly);
                SetEditable(flowSampleG, readOnly);
                SetEditable(flowSampleH, readOnly);

                SetEditable(soundSampleA, readOnly);
                SetEditable(soundSampleB, readOnly);
                SetEditable(soundSampleC, readOnly);
                SetEditable(soundSampleD, readOnly);
                SetEditable(soundSampleE, readOnly);
                SetEditable(soundSampleF, readOnly);
                SetEditable(soundSampleG, readOnly);
                SetEditable(soundSampleH, readOnly);

                SetEditable(effSampleA, readOnly);
                SetEditable(effSampleB, readOnly);
                SetEditable(effSampleC, readOnly);
                SetEditable(effSampleD, readOnly);
                SetEditable(effSampleE, readOnly);
                SetEditable(effSampleF, readOnly);
                SetEditable(effSampleG, readOnly);
                SetEditable(effSampleH, readOnly);

                SetEditable(gainSampleA1, readOnly);
                SetEditable(gainSampleB1, readOnly);
                SetEditable(gainSampleC1, readOnly);
                SetEditable(gainSampleD1, readOnly);
                SetEditable(gainSampleE1, readOnly);
                SetEditable(gainSampleF1, readOnly);
                SetEditable(gainSampleG1, readOnly);
                SetEditable(gainSampleH1, readOnly);

                SetEditable(gainSampleA2, readOnly);
                SetEditable(gainSampleB2, readOnly);
                SetEditable(gainSampleC2, readOnly);
                SetEditable(gainSampleD2, readOnly);
                SetEditable(gainSampleE2, readOnly);
                SetEditable(gainSampleF2, readOnly);
                SetEditable(gainSampleG2, readOnly);
                SetEditable(gainSampleH2, readOnly);
            }

        }

        private void SetEditable(TextBox textBox, bool readOnly)
        {
            if (textBox != null) 
            {
                textBox.IsReadOnly = readOnly;

                if (readOnly)
                {
                    dryCalibration.SetColor(textBox, Brushes.WhiteSmoke);
                }
                else
                {
                    dryCalibration.SetColor(textBox, Brushes.LightGreen);
                }
            }        
        }

        private void CleanAllStates()
        {
            CleanPreviusState(FSMState.OBTAINING_SAMPLES, UltrasonicModel.Daniel);
            CleanPreviusState(FSMState.VALIDATING, UltrasonicModel.Daniel);
            CleanPreviusState(FSMState.GENERATING_REPORT, UltrasonicModel.Daniel);

            CleanPreviusState(FSMState.OBTAINING_SAMPLES, UltrasonicModel.DanielJunior1R);
            CleanPreviusState(FSMState.VALIDATING, UltrasonicModel.DanielJunior1R);
            CleanPreviusState(FSMState.GENERATING_REPORT, UltrasonicModel.DanielJunior1R);

            CleanPreviusState(FSMState.OBTAINING_SAMPLES, UltrasonicModel.DanielJunior2R);
            CleanPreviusState(FSMState.VALIDATING, UltrasonicModel.DanielJunior2R);
            CleanPreviusState(FSMState.GENERATING_REPORT, UltrasonicModel.DanielJunior2R);

            CleanPreviusState(FSMState.OBTAINING_SAMPLES, UltrasonicModel.InstrometS5);
            CleanPreviusState(FSMState.VALIDATING, UltrasonicModel.InstrometS5);
            CleanPreviusState(FSMState.GENERATING_REPORT, UltrasonicModel.InstrometS5);

            CleanPreviusState(FSMState.OBTAINING_SAMPLES, UltrasonicModel.InstrometS6);
            CleanPreviusState(FSMState.VALIDATING, UltrasonicModel.InstrometS6);
            CleanPreviusState(FSMState.GENERATING_REPORT, UltrasonicModel.InstrometS6);

            CleanPreviusState(FSMState.OBTAINING_SAMPLES, UltrasonicModel.Sick);
            CleanPreviusState(FSMState.VALIDATING, UltrasonicModel.Sick);
            CleanPreviusState(FSMState.GENERATING_REPORT, UltrasonicModel.Sick);

            CleanPreviusState(FSMState.OBTAINING_SAMPLES, UltrasonicModel.FMU);
            CleanPreviusState(FSMState.VALIDATING, UltrasonicModel.FMU);
            CleanPreviusState(FSMState.GENERATING_REPORT, UltrasonicModel.FMU);

            CleanPreviusState(FSMState.OBTAINING_SAMPLES, UltrasonicModel.KrohneAltosonicV12);
            CleanPreviusState(FSMState.VALIDATING, UltrasonicModel.KrohneAltosonicV12);
            CleanPreviusState(FSMState.GENERATING_REPORT, UltrasonicModel.KrohneAltosonicV12);
        }

        private void CleanPreviusState(FSMState current, UltrasonicModel ultrasonicModel)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<FSMState, UltrasonicModel>(CleanPreviusState), current, ultrasonicModel);
            }
            else
            {
                switch (current)
                {
                    case FSMState.OBTAINING_SAMPLES:
                        SetText(stateStabRtd1, 0d);
                        SetText(stateStabRtd2, 0d);
                        SetText(stateStabRtd3, 0d);
                        SetText(stateStabRtd4, 0d);
                        SetText(stateStabRtd5, 0d);
                        SetText(stateStabRtd6, 0d);
                        SetText(stateStabRtd7, 0d);
                        SetText(stateStabRtd8, 0d);
                        SetText(stateStabRtd9, 0d);
                        SetText(stateStabRtd10, 0d);
                        SetText(stateStabRtd11, 0d);
                        SetText(stateStabRtd12, 0d);
                        SetText(stateStabRtdAVG, 0d);
                        SetText(stateStabRtdDifference, 0d);
                        break;
                    case FSMState.VALIDATING:

                        IEnumerable<TextBox> pressSample = ObtSampPressure.Children.OfType<TextBox>();
                        IEnumerable<TextBox> tempSample = ObtSampTemperature.Children.OfType<TextBox>();
                        IEnumerable<TextBox> avgTempPressSample = ObtSampAvgTempPress.Children.OfType<TextBox>();

                        IEnumerable<TextBox> flowSample = null;
                        IEnumerable<TextBox> flowSampleA = null;
                        IEnumerable<TextBox> flowSampleB = null;
                        IEnumerable<TextBox> flowSampleC = null;
                        IEnumerable<TextBox> flowSampleD = null;
                        IEnumerable<TextBox> flowSampleE = null;
                        IEnumerable<TextBox> flowSampleF = null;
                        IEnumerable<TextBox> flowSampleG = null;
                        IEnumerable<TextBox> flowSampleH = null;

                        IEnumerable<TextBox> soundSample = null;
                        IEnumerable<TextBox> soundSampleA = null;
                        IEnumerable<TextBox> soundSampleB = null;
                        IEnumerable<TextBox> soundSampleC = null;
                        IEnumerable<TextBox> soundSampleD = null;
                        IEnumerable<TextBox> soundSampleE = null;
                        IEnumerable<TextBox> soundSampleF = null;
                        IEnumerable<TextBox> soundSampleG = null;
                        IEnumerable<TextBox> soundSampleH = null;

                        IEnumerable<TextBox> averageSample = null;
                       
                        IEnumerable<TextBox> effSample = null;
                        IEnumerable<TextBox> effSampleA = null;
                        IEnumerable<TextBox> effSampleB = null;
                        IEnumerable<TextBox> effSampleC = null;
                        IEnumerable<TextBox> effSampleD = null;
                        IEnumerable<TextBox> effSampleE = null;
                        IEnumerable<TextBox> effSampleF = null;
                        IEnumerable<TextBox> effSampleG = null;
                        IEnumerable<TextBox> effSampleH = null;


                        switch (ultrasonicModel)
                        {
                            case UltrasonicModel.Daniel:
                                flowSampleA = ObtSampFlowDanielA.Children.OfType<TextBox>();
                                flowSampleB = ObtSampFlowDanielB.Children.OfType<TextBox>();
                                flowSampleC = ObtSampFlowDanielC.Children.OfType<TextBox>();
                                flowSampleD = ObtSampFlowDanielD.Children.OfType<TextBox>();

                                flowSample = flowSampleA.Union(flowSampleB)
                                                        .Union(flowSampleC)
                                                        .Union(flowSampleD);

                                soundSampleA = ObtSampSoundDanielA.Children.OfType<TextBox>();
                                soundSampleB = ObtSampSoundDanielB.Children.OfType<TextBox>();
                                soundSampleC = ObtSampSoundDanielC.Children.OfType<TextBox>();
                                soundSampleD = ObtSampSoundDanielD.Children.OfType<TextBox>();

                                soundSample = soundSampleA.Union(soundSampleB)
                                                          .Union(soundSampleC)
                                                          .Union(soundSampleD);

                                averageSample = ObtSampAvgDaniel.Children.OfType<TextBox>();

                                effSampleA = ObtSampEffDanielA.Children.OfType<TextBox>();
                                effSampleB = ObtSampEffDanielB.Children.OfType<TextBox>();
                                effSampleC = ObtSampEffDanielC.Children.OfType<TextBox>();
                                effSampleD = ObtSampEffDanielD.Children.OfType<TextBox>();

                                effSample = effSampleA.Union(effSampleB)
                                                      .Union(effSampleC)
                                                      .Union(effSampleD);

                                break;
                            case UltrasonicModel.DanielJunior1R:
                                flowSample = ObtSampFlowDaniel1RA.Children.OfType<TextBox>();     
                                soundSample = ObtSampSoundDaniel1RA.Children.OfType<TextBox>();           
                                averageSample = ObtSampAvgDaniel1R.Children.OfType<TextBox>();
                                effSample = ObtSampEffDaniel1RA.Children.OfType<TextBox>();
                   
                                break;
                            case UltrasonicModel.DanielJunior2R:
                                flowSampleA = ObtSampFlowDaniel2RA.Children.OfType<TextBox>();
                                flowSampleB = ObtSampFlowDaniel2RB.Children.OfType<TextBox>();
                                flowSample = flowSampleA.Union(flowSampleB);
                                                     
                                soundSampleA = ObtSampSoundDaniel2RA.Children.OfType<TextBox>();
                                soundSampleB = ObtSampSoundDaniel2RB.Children.OfType<TextBox>();
                                soundSample = soundSampleA.Union(soundSampleB);
                                                          
                                averageSample = ObtSampAvgDaniel2R.Children.OfType<TextBox>();

                                effSampleA = ObtSampEffDanielA.Children.OfType<TextBox>();
                                effSampleB = ObtSampEffDanielB.Children.OfType<TextBox>();
                                effSample = effSampleA.Union(effSampleB);
                                      
                                break;
                            case UltrasonicModel.InstrometS5:
                                flowSampleA = ObtSampFlowInstS5A.Children.OfType<TextBox>();
                                flowSampleB = ObtSampFlowInstS5B.Children.OfType<TextBox>();
                                flowSampleC = ObtSampFlowInstS5C.Children.OfType<TextBox>();
                                flowSampleD = ObtSampFlowInstS5D.Children.OfType<TextBox>();
                                flowSampleE = ObtSampFlowInstS5E.Children.OfType<TextBox>();

                                flowSample = flowSampleA.Union(flowSampleB)
                                                        .Union(flowSampleC)
                                                        .Union(flowSampleD)
                                                        .Union(flowSampleE);

                                soundSampleA = ObtSampSoundInstS5A.Children.OfType<TextBox>();
                                soundSampleB = ObtSampSoundInstS5B.Children.OfType<TextBox>();
                                soundSampleC = ObtSampSoundInstS5C.Children.OfType<TextBox>();
                                soundSampleD = ObtSampSoundInstS5D.Children.OfType<TextBox>();
                                soundSampleE = ObtSampSoundInstS5E.Children.OfType<TextBox>();

                                soundSample = soundSampleA.Union(soundSampleB)
                                                          .Union(soundSampleC)
                                                          .Union(soundSampleD)
                                                          .Union(soundSampleE);

                                averageSample = ObtSampAvgInsS5.Children.OfType<TextBox>();

                                effSampleA = ObtSampEffIntS5A.Children.OfType<TextBox>();
                                effSampleB = ObtSampEffIntS5B.Children.OfType<TextBox>();
                                effSampleC = ObtSampEffIntS5C.Children.OfType<TextBox>();
                                effSampleD = ObtSampEffIntS5D.Children.OfType<TextBox>();
                                effSampleE = ObtSampEffIntS5E.Children.OfType<TextBox>();

                                effSample = effSampleA.Union(effSampleB)
                                                      .Union(effSampleC)
                                                      .Union(effSampleD)
                                                      .Union(effSampleE);
                                break;
                            case UltrasonicModel.InstrometS6:
                                flowSampleA = ObtSampFlowInstS6A.Children.OfType<TextBox>();
                                flowSampleB = ObtSampFlowInstS6B.Children.OfType<TextBox>();
                                flowSampleC = ObtSampFlowInstS6C.Children.OfType<TextBox>();
                                flowSampleD = ObtSampFlowInstS6D.Children.OfType<TextBox>();
                                flowSampleE = ObtSampFlowInstS6E.Children.OfType<TextBox>();
                                flowSampleF = ObtSampFlowInstS6F.Children.OfType<TextBox>();
                                flowSampleG = ObtSampFlowInstS6G.Children.OfType<TextBox>();
                                flowSampleH = ObtSampFlowInstS6H.Children.OfType<TextBox>();

                                flowSample = flowSampleA.Union(flowSampleB)
                                                        .Union(flowSampleC)
                                                        .Union(flowSampleD)
                                                        .Union(flowSampleE)
                                                        .Union(flowSampleF)
                                                        .Union(flowSampleG)
                                                        .Union(flowSampleH);


                                soundSampleA = ObtSampSoundInstS6A.Children.OfType<TextBox>();
                                soundSampleB = ObtSampSoundInstS6B.Children.OfType<TextBox>();
                                soundSampleC = ObtSampSoundInstS6C.Children.OfType<TextBox>();
                                soundSampleD = ObtSampSoundInstS6D.Children.OfType<TextBox>();
                                soundSampleE = ObtSampSoundInstS6E.Children.OfType<TextBox>();
                                soundSampleF = ObtSampSoundInstS6F.Children.OfType<TextBox>();
                                soundSampleG = ObtSampSoundInstS6G.Children.OfType<TextBox>();
                                soundSampleH = ObtSampSoundInstS6H.Children.OfType<TextBox>();

                                soundSample = soundSampleA.Union(soundSampleB)
                                                          .Union(soundSampleC)
                                                          .Union(soundSampleD)
                                                          .Union(soundSampleE)
                                                          .Union(soundSampleF)
                                                          .Union(soundSampleG)
                                                          .Union(soundSampleH);

                                averageSample = ObtSampAvgInstS6.Children.OfType<TextBox>();

                                effSampleA = ObtSampEffIntS6A.Children.OfType<TextBox>();
                                effSampleB = ObtSampEffIntS6B.Children.OfType<TextBox>();
                                effSampleC = ObtSampEffIntS6C.Children.OfType<TextBox>();
                                effSampleD = ObtSampEffIntS6D.Children.OfType<TextBox>();
                                effSampleE = ObtSampEffIntS6E.Children.OfType<TextBox>();
                                effSampleF = ObtSampEffIntS6F.Children.OfType<TextBox>();
                                effSampleG = ObtSampEffIntS6G.Children.OfType<TextBox>();
                                effSampleH = ObtSampEffIntS6H.Children.OfType<TextBox>();

                                effSample = effSampleA.Union(effSampleB)
                                                      .Union(effSampleC)
                                                      .Union(effSampleD)
                                                      .Union(effSampleE)
                                                      .Union(effSampleF)
                                                      .Union(effSampleG)
                                                      .Union(effSampleH);
                                break;
                            case UltrasonicModel.Sick:
                                flowSampleA = ObtSampFlowSickA.Children.OfType<TextBox>();
                                flowSampleB = ObtSampFlowSickB.Children.OfType<TextBox>();
                                flowSampleC = ObtSampFlowSickC.Children.OfType<TextBox>();
                                flowSampleD = ObtSampFlowSickD.Children.OfType<TextBox>();

                                flowSample = flowSampleA.Union(flowSampleB)
                                                        .Union(flowSampleC)
                                                        .Union(flowSampleD);

                                soundSampleA = ObtSampSoundSickA.Children.OfType<TextBox>();
                                soundSampleB = ObtSampSoundSickB.Children.OfType<TextBox>();
                                soundSampleC = ObtSampSoundSickC.Children.OfType<TextBox>();
                                soundSampleD = ObtSampSoundSickD.Children.OfType<TextBox>();

                                soundSample = soundSampleA.Union(soundSampleB)
                                                          .Union(soundSampleC)
                                                          .Union(soundSampleD);

                                averageSample = ObtSampAvgSick.Children.OfType<TextBox>();

                                effSampleA = ObtSampEffSickA.Children.OfType<TextBox>();
                                effSampleB = ObtSampEffSickB.Children.OfType<TextBox>();
                                effSampleC = ObtSampEffSickC.Children.OfType<TextBox>();
                                effSampleD = ObtSampEffSickD.Children.OfType<TextBox>();

                                effSample = effSampleA.Union(effSampleB)
                                                      .Union(effSampleC)
                                                      .Union(effSampleD);
                                break;
                            case UltrasonicModel.FMU:
                                flowSampleA = ObtSampFlowFmuA.Children.OfType<TextBox>();
                                flowSampleB = ObtSampFlowFmuB.Children.OfType<TextBox>();
                                flowSampleC = ObtSampFlowFmuC.Children.OfType<TextBox>();
                                flowSampleD = ObtSampFlowFmuD.Children.OfType<TextBox>();

                                flowSample = flowSampleA.Union(flowSampleB)
                                                        .Union(flowSampleC)
                                                        .Union(flowSampleD);

                                soundSampleA = ObtSampSoundFmuA.Children.OfType<TextBox>();
                                soundSampleB = ObtSampSoundFmuB.Children.OfType<TextBox>();
                                soundSampleC = ObtSampSoundFmuC.Children.OfType<TextBox>();
                                soundSampleD = ObtSampSoundFmuD.Children.OfType<TextBox>();

                                soundSample = soundSampleA.Union(soundSampleB)
                                                          .Union(soundSampleC)
                                                          .Union(soundSampleD);

                                averageSample = ObtSampAvgFmu.Children.OfType<TextBox>();

                                effSampleA = ObtSampEffFmuA.Children.OfType<TextBox>();
                                effSampleB = ObtSampEffFmuB.Children.OfType<TextBox>();
                                effSampleC = ObtSampEffFmuC.Children.OfType<TextBox>();
                                effSampleD = ObtSampEffFmuD.Children.OfType<TextBox>();

                                effSample = effSampleA.Union(effSampleB)
                                                      .Union(effSampleC)
                                                      .Union(effSampleD);
                                break;
                            case UltrasonicModel.KrohneAltosonicV12:
                                flowSampleA = ObtSampFlowKrohneAltV12A.Children.OfType<TextBox>();
                                flowSampleB = ObtSampFlowKrohneAltV12B.Children.OfType<TextBox>();
                                flowSampleC = ObtSampFlowKrohneAltV12C.Children.OfType<TextBox>();
                                flowSampleD = ObtSampFlowKrohneAltV12D.Children.OfType<TextBox>();
                                flowSampleE = ObtSampFlowKrohneAltV12E.Children.OfType<TextBox>();
                                flowSampleF = ObtSampFlowKrohneAltV12F.Children.OfType<TextBox>();
                               

                                flowSample = flowSampleA.Union(flowSampleB)
                                                        .Union(flowSampleC)
                                                        .Union(flowSampleD)
                                                        .Union(flowSampleE)
                                                        .Union(flowSampleF);

                                soundSampleA = ObtSampSoundKrohneAltV12A.Children.OfType<TextBox>();
                                soundSampleB = ObtSampSoundKrohneAltV12B.Children.OfType<TextBox>();
                                soundSampleC = ObtSampSoundKrohneAltV12C.Children.OfType<TextBox>();
                                soundSampleD = ObtSampSoundKrohneAltV12D.Children.OfType<TextBox>();
                                soundSampleE = ObtSampSoundKrohneAltV12E.Children.OfType<TextBox>();
                                soundSampleF = ObtSampSoundKrohneAltV12F.Children.OfType<TextBox>();
                               
                                soundSample = soundSampleA.Union(soundSampleB)
                                                          .Union(soundSampleC)
                                                          .Union(soundSampleD)
                                                          .Union(soundSampleE)
                                                          .Union(soundSampleF);

                                averageSample = ObtSampAvgKrohneAltV12.Children.OfType<TextBox>();

                                effSampleA = ObtSampEffKrohneAltV12A.Children.OfType<TextBox>();
                                effSampleB = ObtSampEffKrohneAltV12B.Children.OfType<TextBox>();
                                effSampleC = ObtSampEffKrohneAltV12C.Children.OfType<TextBox>();
                                effSampleD = ObtSampEffKrohneAltV12D.Children.OfType<TextBox>();
                                effSampleE = ObtSampEffKrohneAltV12E.Children.OfType<TextBox>();
                                effSampleF = ObtSampEffKrohneAltV12F.Children.OfType<TextBox>();
                               
                                effSample = effSampleA.Union(effSampleB)
                                                      .Union(effSampleC)
                                                      .Union(effSampleD)
                                                      .Union(effSampleE)
                                                      .Union(effSampleF);
                                break;
                            default:
                                break;
                        }

                        IEnumerable<TextBox> toClear = pressSample.Union(tempSample)
                                                                 .Union(avgTempPressSample)
                                                                 .Union(flowSample)
                                                                 .Union(soundSample)
                                                                 .Union(averageSample)
                                                                 .Union(effSample);

                        foreach (TextBox textBox in toClear)
                        {
                            SetText(textBox, "");
                            dryCalibration.SetColor(textBox, Brushes.WhiteSmoke);
                        }

                        break;
                    case FSMState.GENERATING_REPORT:

                        IEnumerable<TextBox> pressValid = ValidSampAveragePress.Children.OfType<TextBox>();
                        IEnumerable<TextBox> tempValid = ValidSampAverageTemp.Children.OfType<TextBox>();
                        IEnumerable<TextBox> theoricalSoundSpeed = ValidTheoricalSoundSpeed.Children.OfType<TextBox>();
                        IEnumerable<TextBox> diffValidMaxMin = ValidSampDiffMaxMin.Children.OfType<TextBox>();
                        IEnumerable<TextBox> diffValid = ValidSampDifference.Children.OfType<TextBox>();

                        IEnumerable<TextBox> flowValid = null;
                        IEnumerable<TextBox> flowValidA = null;
                        IEnumerable<TextBox> flowValidB = null;
                        IEnumerable<TextBox> flowValidC = null;
                        IEnumerable<TextBox> flowValidD = null;
                        IEnumerable<TextBox> flowValidE = null;
                        IEnumerable<TextBox> flowValidF = null;
                        IEnumerable<TextBox> flowValidG = null;
                        IEnumerable<TextBox> flowValidH = null;

                        IEnumerable<TextBox> errValid = null;
                        IEnumerable<TextBox> errValidA = null;
                        IEnumerable<TextBox> errValidB = null;
                        IEnumerable<TextBox> errValidC = null;
                        IEnumerable<TextBox> errValidD = null;
                        IEnumerable<TextBox> errValidE = null;
                        IEnumerable<TextBox> errValidF = null;
                        IEnumerable<TextBox> errValidG = null;
                        IEnumerable<TextBox> errValidH = null;

                        switch (ultrasonicModel)
                        {
                            case UltrasonicModel.Daniel:

                                flowValidA = ValidSampAvgDanielA.Children.OfType<TextBox>();
                                flowValidB = ValidSampAvgDanielB.Children.OfType<TextBox>();
                                flowValidC = ValidSampAvgDanielC.Children.OfType<TextBox>();
                                flowValidD = ValidSampAvgDanielD.Children.OfType<TextBox>();

                                flowValid = flowValidA.Union(flowValidB)
                                                      .Union(flowValidC)
                                                      .Union(flowValidD);

                                errValidA = ValidSampErrDanielA.Children.OfType<TextBox>();
                                errValidB = ValidSampErrDanielB.Children.OfType<TextBox>();
                                errValidC = ValidSampErrDanielC.Children.OfType<TextBox>();
                                errValidD = ValidSampErrDanielD.Children.OfType<TextBox>();

                                errValid = errValidA.Union(errValidB)
                                                    .Union(errValidC)
                                                    .Union(errValidD);


                                break;
                            case UltrasonicModel.DanielJunior1R:

                                flowValid = ValidSampAvgDaniel1RA.Children.OfType<TextBox>();
                                errValid = ValidSampErrDaniel1RA.Children.OfType<TextBox>();
  
                                break;
                            case UltrasonicModel.DanielJunior2R:

                                flowValidA = ValidSampAvgDaniel2RA.Children.OfType<TextBox>();
                                flowValidB = ValidSampAvgDaniel2RB.Children.OfType<TextBox>();

                                flowValid = flowValidA.Union(flowValidB);
                                                     
                                errValidA = ValidSampErrDaniel2RA.Children.OfType<TextBox>();
                                errValidB = ValidSampErrDaniel2RB.Children.OfType<TextBox>();
                           
                                errValid = errValidA.Union(errValidB);
                                                    
                                break;
                            case UltrasonicModel.InstrometS5:

                                flowValidA = ValidSampAvgInstS5A.Children.OfType<TextBox>();
                                flowValidB = ValidSampAvgInstS5B.Children.OfType<TextBox>();
                                flowValidC = ValidSampAvgInstS5C.Children.OfType<TextBox>();
                                flowValidD = ValidSampAvgInstS5D.Children.OfType<TextBox>();
                                flowValidE = ValidSampAvgInstS5E.Children.OfType<TextBox>();

                                flowValid = flowValidA.Union(flowValidB)
                                                      .Union(flowValidC)
                                                      .Union(flowValidD)
                                                      .Union(flowValidE);

                                errValidA = ValidSampErrInstS5A.Children.OfType<TextBox>();
                                errValidB = ValidSampErrInstS5B.Children.OfType<TextBox>();
                                errValidC = ValidSampErrInstS5C.Children.OfType<TextBox>();
                                errValidD = ValidSampErrInstS5D.Children.OfType<TextBox>();
                                errValidE = ValidSampErrInstS5E.Children.OfType<TextBox>();

                                errValid = errValidA.Union(errValidB)
                                                    .Union(errValidC)
                                                    .Union(errValidD)
                                                    .Union(errValidE);

                                break;
                            case UltrasonicModel.InstrometS6:

                                flowValidA = ValidSampAvgInstS6A.Children.OfType<TextBox>();
                                flowValidB = ValidSampAvgInstS6B.Children.OfType<TextBox>();
                                flowValidC = ValidSampAvgInstS6C.Children.OfType<TextBox>();
                                flowValidD = ValidSampAvgInstS6D.Children.OfType<TextBox>();
                                flowValidE = ValidSampAvgInstS6E.Children.OfType<TextBox>();
                                flowValidF = ValidSampAvgInstS6F.Children.OfType<TextBox>();
                                flowValidG = ValidSampAvgInstS6G.Children.OfType<TextBox>();
                                flowValidH = ValidSampAvgInstS6H.Children.OfType<TextBox>();

                                flowValid = flowValidA.Union(flowValidB)
                                                      .Union(flowValidC)
                                                      .Union(flowValidD)
                                                      .Union(flowValidE)
                                                      .Union(flowValidF)
                                                      .Union(flowValidG)
                                                      .Union(flowValidH);

                                errValidA = ValidSampErrInstS6A.Children.OfType<TextBox>();
                                errValidB = ValidSampErrInstS6B.Children.OfType<TextBox>();
                                errValidC = ValidSampErrInstS6C.Children.OfType<TextBox>();
                                errValidD = ValidSampErrInstS6D.Children.OfType<TextBox>();
                                errValidE = ValidSampErrInstS6E.Children.OfType<TextBox>();
                                errValidF = ValidSampErrInstS6F.Children.OfType<TextBox>();
                                errValidG = ValidSampErrInstS6G.Children.OfType<TextBox>();
                                errValidH = ValidSampErrInstS6H.Children.OfType<TextBox>();

                                errValid = errValidA.Union(errValidB)
                                                    .Union(errValidC)
                                                    .Union(errValidD)
                                                    .Union(errValidE)
                                                    .Union(errValidF)
                                                    .Union(errValidG)
                                                    .Union(errValidH);

                                break;
                            case UltrasonicModel.Sick:

                                flowValidA = ValidSampAvgSickA.Children.OfType<TextBox>();
                                flowValidB = ValidSampAvgSickB.Children.OfType<TextBox>();
                                flowValidC = ValidSampAvgSickC.Children.OfType<TextBox>();
                                flowValidD = ValidSampAvgSickD.Children.OfType<TextBox>();

                                flowValid = flowValidA.Union(flowValidB)
                                                      .Union(flowValidC)
                                                      .Union(flowValidD);

                                errValidA = ValidSampErrSickA.Children.OfType<TextBox>();
                                errValidB = ValidSampErrSickB.Children.OfType<TextBox>();
                                errValidC = ValidSampErrSickC.Children.OfType<TextBox>();
                                errValidD = ValidSampErrSickD.Children.OfType<TextBox>();

                                errValid = errValidA.Union(errValidB)
                                                    .Union(errValidC)
                                                    .Union(errValidD);

                                break;
                            case UltrasonicModel.FMU:

                                flowValidA = ValidSampAvgFmuA.Children.OfType<TextBox>();
                                flowValidB = ValidSampAvgFmuB.Children.OfType<TextBox>();
                                flowValidC = ValidSampAvgFmuC.Children.OfType<TextBox>();
                                flowValidD = ValidSampAvgFmuD.Children.OfType<TextBox>();

                                flowValid = flowValidA.Union(flowValidB)
                                                      .Union(flowValidC)
                                                      .Union(flowValidD);

                                errValidA = ValidSampErrFmuA.Children.OfType<TextBox>();
                                errValidB = ValidSampErrFmuB.Children.OfType<TextBox>();
                                errValidC = ValidSampErrFmuC.Children.OfType<TextBox>();
                                errValidD = ValidSampErrFmuD.Children.OfType<TextBox>();

                                errValid = errValidA.Union(errValidB)
                                                    .Union(errValidC)
                                                    .Union(errValidD);
                                break;
                            case UltrasonicModel.KrohneAltosonicV12:

                                flowValidA = ValidSampAvgKrohneAltV12A.Children.OfType<TextBox>();
                                flowValidB = ValidSampAvgKrohneAltV12B.Children.OfType<TextBox>();
                                flowValidC = ValidSampAvgKrohneAltV12C.Children.OfType<TextBox>();
                                flowValidD = ValidSampAvgKrohneAltV12D.Children.OfType<TextBox>();
                                flowValidE = ValidSampAvgKrohneAltV12E.Children.OfType<TextBox>();
                                flowValidF = ValidSampAvgKrohneAltV12F.Children.OfType<TextBox>();
                                
                                flowValid = flowValidA.Union(flowValidB)
                                                      .Union(flowValidC)
                                                      .Union(flowValidD)
                                                      .Union(flowValidE)
                                                      .Union(flowValidF);

                                errValidA = ValidSampErrKrohneAltV12A.Children.OfType<TextBox>();
                                errValidB = ValidSampErrKrohneAltV12B.Children.OfType<TextBox>();
                                errValidC = ValidSampErrKrohneAltV12C.Children.OfType<TextBox>();
                                errValidD = ValidSampErrKrohneAltV12D.Children.OfType<TextBox>();
                                errValidE = ValidSampErrKrohneAltV12E.Children.OfType<TextBox>();
                                errValidF = ValidSampErrKrohneAltV12F.Children.OfType<TextBox>();
                               
                                errValid = errValidA.Union(errValidB)
                                                    .Union(errValidC)
                                                    .Union(errValidD)
                                                    .Union(errValidE)
                                                    .Union(errValidF);

                                break;
                            default:
                                break;

                        }

                        IEnumerable<TextBox> toValidClear = pressValid.Union(tempValid)
                                                                    .Union(theoricalSoundSpeed)
                                                                    .Union(diffValidMaxMin)
                                                                    .Union(diffValid)
                                                                    .Union(flowValid)
                                                                    .Union(errValid);

                        foreach (TextBox textBox in toValidClear)
                        {
                            textBox.Text = "";
                        }


                        break;
                    case FSMState.ENDING:
                        SetText(GenRepMeasObservation, "");


                        break;
                    default:
                        break;
                }
            }
        }

        private void DryCalibration_SampleObtained(int sampleNumber, UltrasonicModel ultrasonicModel)
        {
            try
            {
                UltSampMode mode = (UltSampMode)dryCalibration.CurrentModbusConfiguration.UltrasonicSampleMode;

                if (mode == UltSampMode.Manual)
                {
                    dryCalibration.PauseTimerControl();

                    dryCalibration.SetControlEnabled(btnNextManSample, true);

                    if (sampleNumber <= 10)
                    {
                        SetSampleModeEdition(sampleNumber, ultrasonicModel, false);

                        UpdateSampleValues(sampleNumber, ultrasonicModel);
                    }

                }
                else
                {
                    UpdateSampleValues(sampleNumber, ultrasonicModel);
                }
                       
            }
            catch (FSMStateProcessException ex)
            {
                SetTextBlock(Status, ex.Message);

                dryCalibration.AbortDryCalibration();               
            }      
        }

        private void UpdateSampleValues(int sampleNumber, UltrasonicModel ultrasonicModel) 
        {
     
            log.Log.WriteIfExists("UpdateSampleValues -> Model: " + ultrasonicModel.ToString());

            if (sampleNumber == 1) 
            {
                SetDefaultEffValues(ultrasonicModel);

                SetDefaultGainValues(ultrasonicModel); 
            }

   

            switch (ultrasonicModel)
            {
                case UltrasonicModel.Daniel:
                    switch (sampleNumber)
                    {
                        case 1:
                            log.Log.WriteIfExists("UpdateObtainedSampleLayout -> Model: " + ultrasonicModel.ToString());
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp1,
                                ObtSampPress1,
                                ObtSampDanielFlowRopeA1, ObtSampDanielFlowRopeB1, ObtSampDanielFlowRopeC1, ObtSampDanielFlowRopeD1,
                                ObtSampDanielSoundRopeA1, ObtSampDanielSoundRopeB1, ObtSampDanielSoundRopeC1, ObtSampDanielSoundRopeD1,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 2:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp2,
                                ObtSampPress2,
                                ObtSampDanielFlowRopeA2, ObtSampDanielFlowRopeB2, ObtSampDanielFlowRopeC2, ObtSampDanielFlowRopeD2,
                                ObtSampDanielSoundRopeA2, ObtSampDanielSoundRopeB2, ObtSampDanielSoundRopeC2, ObtSampDanielSoundRopeD2,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 3:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp3,
                                ObtSampPress3,
                                ObtSampDanielFlowRopeA3, ObtSampDanielFlowRopeB3, ObtSampDanielFlowRopeC3, ObtSampDanielFlowRopeD3,
                                ObtSampDanielSoundRopeA3, ObtSampDanielSoundRopeB3, ObtSampDanielSoundRopeC3, ObtSampDanielSoundRopeD3,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 4:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp4,
                                ObtSampPress4,
                                ObtSampDanielFlowRopeA4, ObtSampDanielFlowRopeB4, ObtSampDanielFlowRopeC4, ObtSampDanielFlowRopeD4,
                                ObtSampDanielSoundRopeA4, ObtSampDanielSoundRopeB4, ObtSampDanielSoundRopeC4, ObtSampDanielSoundRopeD4,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 5:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp5,
                                ObtSampPress5,
                                ObtSampDanielFlowRopeA5, ObtSampDanielFlowRopeB5, ObtSampDanielFlowRopeC5, ObtSampDanielFlowRopeD5,
                                ObtSampDanielSoundRopeA5, ObtSampDanielSoundRopeB5, ObtSampDanielSoundRopeC5, ObtSampDanielSoundRopeD5,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 6:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp6,
                                ObtSampPress6,
                                ObtSampDanielFlowRopeA6, ObtSampDanielFlowRopeB6, ObtSampDanielFlowRopeC6, ObtSampDanielFlowRopeD6,
                                ObtSampDanielSoundRopeA6, ObtSampDanielSoundRopeB6, ObtSampDanielSoundRopeC6, ObtSampDanielSoundRopeD6,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 7:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp7,
                                ObtSampPress7,
                                ObtSampDanielFlowRopeA7, ObtSampDanielFlowRopeB7, ObtSampDanielFlowRopeC7, ObtSampDanielFlowRopeD7,
                                ObtSampDanielSoundRopeA7, ObtSampDanielSoundRopeB7, ObtSampDanielSoundRopeC7, ObtSampDanielSoundRopeD7,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 8:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp8,
                                ObtSampPress8,
                                ObtSampDanielFlowRopeA8, ObtSampDanielFlowRopeB8, ObtSampDanielFlowRopeC8, ObtSampDanielFlowRopeD8,
                                ObtSampDanielSoundRopeA8, ObtSampDanielSoundRopeB8, ObtSampDanielSoundRopeC8, ObtSampDanielSoundRopeD8,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 9:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp9,
                                ObtSampPress9,
                                ObtSampDanielFlowRopeA9, ObtSampDanielFlowRopeB9, ObtSampDanielFlowRopeC9, ObtSampDanielFlowRopeD9,
                                ObtSampDanielSoundRopeA9, ObtSampDanielSoundRopeB9, ObtSampDanielSoundRopeC9, ObtSampDanielSoundRopeD9,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 10:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp10,
                                ObtSampPress10,
                                ObtSampDanielFlowRopeA10, ObtSampDanielFlowRopeB10, ObtSampDanielFlowRopeC10, ObtSampDanielFlowRopeD10,
                                ObtSampDanielSoundRopeA10, ObtSampDanielSoundRopeB10, ObtSampDanielSoundRopeC10, ObtSampDanielSoundRopeD10,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 11:
                            dryCalibration.UpdateObtainedSampleAverageLayout(ultrasonicModel,
                                ObtSampAverageTemp,
                                ObtSampAveragePress,
                                ObtSampAvgDanielFlowRopeA, ObtSampAvgDanielFlowRopeB, ObtSampAvgDanielFlowRopeC, ObtSampAvgDanielFlowRopeD,
                                ObtSampAvgDanielSoundRopeA, ObtSampAvgDanielSoundRopeB, ObtSampAvgDanielSoundRopeC, ObtSampAvgDanielSoundRopeD,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD);

                            break;
                    }
                    break;
                case UltrasonicModel.DanielJunior1R:
                    switch (sampleNumber)
                    {
                        case 1:
                            log.Log.WriteIfExists("UpdateObtainedSampleLayout -> Model: " + ultrasonicModel.ToString());
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp1,
                                ObtSampPress1,
                                ObtSampDaniel1RFlowRopeA1,
                                ObtSampDaniel1RSoundRopeA1,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 2:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp2,
                                ObtSampPress2,
                                ObtSampDaniel1RFlowRopeA2,
                                ObtSampDaniel1RSoundRopeA2,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 3:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp3,
                                ObtSampPress3,
                                ObtSampDaniel1RFlowRopeA3,
                                ObtSampDaniel1RSoundRopeA3,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 4:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp4,
                                ObtSampPress4,
                                ObtSampDaniel1RFlowRopeA4,
                                ObtSampDaniel1RSoundRopeA4,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 5:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp5,
                                ObtSampPress5,
                                ObtSampDaniel1RFlowRopeA5, 
                                ObtSampDaniel1RSoundRopeA5,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 6:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp6,
                                ObtSampPress6,
                                ObtSampDaniel1RFlowRopeA6,
                                ObtSampDaniel1RSoundRopeA6,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 7:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp7,
                                ObtSampPress7,
                                ObtSampDaniel1RFlowRopeA7,
                                ObtSampDaniel1RSoundRopeA7,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 8:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp8,
                                ObtSampPress8,
                                ObtSampDaniel1RFlowRopeA8,
                                ObtSampDaniel1RSoundRopeA8,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 9:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp9,
                                ObtSampPress9,
                                ObtSampDaniel1RFlowRopeA9,
                                ObtSampDaniel1RSoundRopeA9,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 10:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp10,
                                ObtSampPress10,
                                ObtSampDaniel1RFlowRopeA10,
                                ObtSampDaniel1RSoundRopeA10,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 11:
                            dryCalibration.UpdateObtainedSampleAverageLayout(ultrasonicModel,
                                ObtSampAverageTemp,
                                ObtSampAveragePress,
                                ObtSampAvgDaniel1RFlowRopeA,
                                ObtSampAvgDaniel1RSoundRopeA,
                                ObtSampEffDaniel1RRopeA);

                            break;
                    }
                    break;

                case UltrasonicModel.DanielJunior2R:
                    switch (sampleNumber)
                    {
                        case 1:
                            log.Log.WriteIfExists("UpdateObtainedSampleLayout -> Model: " + ultrasonicModel.ToString());
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp1,
                                ObtSampPress1,
                                ObtSampDaniel2RFlowRopeA1, ObtSampDaniel2RFlowRopeB1,
                                ObtSampDaniel2RSoundRopeA1, ObtSampDaniel2RSoundRopeB1,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 2:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp2,
                                ObtSampPress2,
                                ObtSampDaniel2RFlowRopeA2, ObtSampDaniel2RFlowRopeB2,
                                ObtSampDaniel2RSoundRopeA2, ObtSampDaniel2RSoundRopeB2,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 3:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp3,
                                ObtSampPress3,
                                ObtSampDaniel2RFlowRopeA3, ObtSampDaniel2RFlowRopeB3,
                                ObtSampDaniel2RSoundRopeA3, ObtSampDaniel2RSoundRopeB3,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 4:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp4,
                                ObtSampPress4,
                                ObtSampDaniel2RFlowRopeA4, ObtSampDaniel2RFlowRopeB4,
                                ObtSampDaniel2RSoundRopeA4, ObtSampDaniel2RSoundRopeB4,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 5:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp5,
                                ObtSampPress5,
                                ObtSampDaniel2RFlowRopeA5, ObtSampDaniel2RFlowRopeB5, 
                                ObtSampDaniel2RSoundRopeA5, ObtSampDaniel2RSoundRopeB5,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 6:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp6,
                                ObtSampPress6,
                                ObtSampDaniel2RFlowRopeA6, ObtSampDaniel2RFlowRopeB6,
                                ObtSampDaniel2RSoundRopeA6, ObtSampDaniel2RSoundRopeB6,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 7:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp7,
                                ObtSampPress7,
                                ObtSampDaniel2RFlowRopeA7, ObtSampDaniel2RFlowRopeB7,
                                ObtSampDaniel2RSoundRopeA7, ObtSampDaniel2RSoundRopeB7,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 8:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp8,
                                ObtSampPress8,
                                ObtSampDaniel2RFlowRopeA8, ObtSampDaniel2RFlowRopeB8,
                                ObtSampDaniel2RSoundRopeA8, ObtSampDaniel2RSoundRopeB8,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 9:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp9,
                                ObtSampPress9,
                                ObtSampDaniel2RFlowRopeA9, ObtSampDaniel2RFlowRopeB9,
                                ObtSampDaniel2RSoundRopeA9, ObtSampDaniel2RSoundRopeB9,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 10:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp10,
                                ObtSampPress10,
                                ObtSampDaniel2RFlowRopeA10, ObtSampDaniel2RFlowRopeB10,
                                ObtSampDaniel2RSoundRopeA10, ObtSampDaniel2RSoundRopeB10,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 11:
                            dryCalibration.UpdateObtainedSampleAverageLayout(ultrasonicModel,
                                ObtSampAverageTemp,
                                ObtSampAveragePress,
                                ObtSampAvgDaniel2RFlowRopeA, ObtSampAvgDaniel2RFlowRopeB,
                                ObtSampAvgDaniel2RSoundRopeA, ObtSampAvgDaniel2RSoundRopeB,
                                ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB);

                            break;
                    }
                    break;

                case UltrasonicModel.InstrometS5:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp1,
                                ObtSampPress1,
                                ObtSampIntS5FlowRopeA1, ObtSampIntS5FlowRopeB1, ObtSampIntS5FlowRopeC1, ObtSampIntS5FlowRopeD1, ObtSampIntS5FlowRopeE1,
                                ObtSampIntS5SoundRopeA1, ObtSampIntS5SoundRopeB1, ObtSampIntS5SoundRopeC1, ObtSampIntS5SoundRopeD1, ObtSampIntS5SoundRopeE1,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 2:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp2,
                                ObtSampPress2,
                                ObtSampIntS5FlowRopeA2, ObtSampIntS5FlowRopeB2, ObtSampIntS5FlowRopeC2, ObtSampIntS5FlowRopeD2, ObtSampIntS5FlowRopeE2,
                                ObtSampIntS5SoundRopeA2, ObtSampIntS5SoundRopeB2, ObtSampIntS5SoundRopeC2, ObtSampIntS5SoundRopeD2, ObtSampIntS5SoundRopeE2,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 3:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp3,
                                ObtSampPress3,
                                ObtSampIntS5FlowRopeA3, ObtSampIntS5FlowRopeB3, ObtSampIntS5FlowRopeC3, ObtSampIntS5FlowRopeD3, ObtSampIntS5FlowRopeE3,
                                ObtSampIntS5SoundRopeA3, ObtSampIntS5SoundRopeB3, ObtSampIntS5SoundRopeC3, ObtSampIntS5SoundRopeD3, ObtSampIntS5SoundRopeE3,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 4:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp4,
                                ObtSampPress4,
                                ObtSampIntS5FlowRopeA4, ObtSampIntS5FlowRopeB4, ObtSampIntS5FlowRopeC4, ObtSampIntS5FlowRopeD4, ObtSampIntS5FlowRopeE4,
                                ObtSampIntS5SoundRopeA4, ObtSampIntS5SoundRopeB4, ObtSampIntS5SoundRopeC4, ObtSampIntS5SoundRopeD4, ObtSampIntS5SoundRopeE4,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 5:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp5,
                                ObtSampPress5,
                                ObtSampIntS5FlowRopeA5, ObtSampIntS5FlowRopeB5, ObtSampIntS5FlowRopeC5, ObtSampIntS5FlowRopeD5, ObtSampIntS5FlowRopeE5,
                                ObtSampIntS5SoundRopeA5, ObtSampIntS5SoundRopeB5, ObtSampIntS5SoundRopeC5, ObtSampIntS5SoundRopeD5, ObtSampIntS5SoundRopeE5,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 6:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                 ObtSampTemp6,
                                 ObtSampPress6,
                                 ObtSampIntS5FlowRopeA6, ObtSampIntS5FlowRopeB6, ObtSampIntS5FlowRopeC6, ObtSampIntS5FlowRopeD6, ObtSampIntS5FlowRopeE6,
                                 ObtSampIntS5SoundRopeA6, ObtSampIntS5SoundRopeB6, ObtSampIntS5SoundRopeC6, ObtSampIntS5SoundRopeD6, ObtSampIntS5SoundRopeE6,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 7:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp7,
                                ObtSampPress7,
                                ObtSampIntS5FlowRopeA7, ObtSampIntS5FlowRopeB7, ObtSampIntS5FlowRopeC7, ObtSampIntS5FlowRopeD7, ObtSampIntS5FlowRopeE7,
                                ObtSampIntS5SoundRopeA7, ObtSampIntS5SoundRopeB7, ObtSampIntS5SoundRopeC7, ObtSampIntS5SoundRopeD7, ObtSampIntS5SoundRopeE7,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 8:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp8,
                                 ObtSampPress8,
                                 ObtSampIntS5FlowRopeA8, ObtSampIntS5FlowRopeB8, ObtSampIntS5FlowRopeC8, ObtSampIntS5FlowRopeD8, ObtSampIntS5FlowRopeE8,
                                 ObtSampIntS5SoundRopeA8, ObtSampIntS5SoundRopeB8, ObtSampIntS5SoundRopeC8, ObtSampIntS5SoundRopeD8, ObtSampIntS5SoundRopeE8,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 9:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp9,
                                ObtSampPress9,
                                ObtSampIntS5FlowRopeA9, ObtSampIntS5FlowRopeB9, ObtSampIntS5FlowRopeC9, ObtSampIntS5FlowRopeD9, ObtSampIntS5FlowRopeE9,
                                ObtSampIntS5SoundRopeA9, ObtSampIntS5SoundRopeB9, ObtSampIntS5SoundRopeC9, ObtSampIntS5SoundRopeD9, ObtSampIntS5SoundRopeE9,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 10:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp10,
                                ObtSampPress10,
                                ObtSampIntS5FlowRopeA10, ObtSampIntS5FlowRopeB10, ObtSampIntS5FlowRopeC10, ObtSampIntS5FlowRopeD10, ObtSampIntS5FlowRopeE10,
                                ObtSampIntS5SoundRopeA10, ObtSampIntS5SoundRopeB10, ObtSampIntS5SoundRopeC10, ObtSampIntS5SoundRopeD10, ObtSampIntS5SoundRopeE10,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 11:
                            dryCalibration.UpdateObtainedSampleAverageLayout(ultrasonicModel,
                                ObtSampAverageTemp,
                                ObtSampAveragePress,
                                ObtSampAvgInstS5FlowRopeA, ObtSampAvgInstS5FlowRopeB, ObtSampAvgInstS5FlowRopeC, ObtSampAvgInstS5FlowRopeD, ObtSampAvgInstS5FlowRopeE,
                                ObtSampAvgInstS5SoundRopeA, ObtSampAvgInstS5SoundRopeB, ObtSampAvgInstS5SoundRopeC, ObtSampAvgInstS5SoundRopeD, ObtSampAvgInstS5SoundRopeE,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE
                                );
                            break;
                    }
                    break;
                case UltrasonicModel.InstrometS6:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp1,
                                ObtSampPress1,
                                ObtSampIntS6FlowRopeA1, ObtSampIntS6FlowRopeB1, ObtSampIntS6FlowRopeC1, ObtSampIntS6FlowRopeD1,
                                ObtSampIntS6FlowRopeE1, ObtSampIntS6FlowRopeF1, ObtSampIntS6FlowRopeG1, ObtSampIntS6FlowRopeH1,
                                ObtSampIntS6SoundRopeA1, ObtSampIntS6SoundRopeB1, ObtSampIntS6SoundRopeC1, ObtSampIntS6SoundRopeD1,
                                ObtSampIntS6SoundRopeE1, ObtSampIntS6SoundRopeF1, ObtSampIntS6SoundRopeG1, ObtSampIntS6SoundRopeH1,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 2:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp2,
                                ObtSampPress2,
                                ObtSampIntS6FlowRopeA2, ObtSampIntS6FlowRopeB2, ObtSampIntS6FlowRopeC2, ObtSampIntS6FlowRopeD2,
                                ObtSampIntS6FlowRopeE2, ObtSampIntS6FlowRopeF2, ObtSampIntS6FlowRopeG2, ObtSampIntS6FlowRopeH2,
                                ObtSampIntS6SoundRopeA2, ObtSampIntS6SoundRopeB2, ObtSampIntS6SoundRopeC2, ObtSampIntS6SoundRopeD2,
                                ObtSampIntS6SoundRopeE2, ObtSampIntS6SoundRopeF2, ObtSampIntS6SoundRopeG2, ObtSampIntS6SoundRopeH2,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 3:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp3,
                                ObtSampPress3,
                                ObtSampIntS6FlowRopeA3, ObtSampIntS6FlowRopeB3, ObtSampIntS6FlowRopeC3, ObtSampIntS6FlowRopeD3,
                                ObtSampIntS6FlowRopeE3, ObtSampIntS6FlowRopeF3, ObtSampIntS6FlowRopeG3, ObtSampIntS6FlowRopeH3,
                                ObtSampIntS6SoundRopeA3, ObtSampIntS6SoundRopeB3, ObtSampIntS6SoundRopeC3, ObtSampIntS6SoundRopeD3,
                                ObtSampIntS6SoundRopeE3, ObtSampIntS6SoundRopeF3, ObtSampIntS6SoundRopeG3, ObtSampIntS6SoundRopeH3,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 4:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp4,
                                 ObtSampPress4,
                                 ObtSampIntS6FlowRopeA4, ObtSampIntS6FlowRopeB4, ObtSampIntS6FlowRopeC4, ObtSampIntS6FlowRopeD4,
                                 ObtSampIntS6FlowRopeE4, ObtSampIntS6FlowRopeF4, ObtSampIntS6FlowRopeG4, ObtSampIntS6FlowRopeH4,
                                 ObtSampIntS6SoundRopeA4, ObtSampIntS6SoundRopeB4, ObtSampIntS6SoundRopeC4, ObtSampIntS6SoundRopeD4,
                                 ObtSampIntS6SoundRopeE4, ObtSampIntS6SoundRopeF4, ObtSampIntS6SoundRopeG4, ObtSampIntS6SoundRopeH4,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 5:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp5,
                                 ObtSampPress5,
                                 ObtSampIntS6FlowRopeA5, ObtSampIntS6FlowRopeB5, ObtSampIntS6FlowRopeC5, ObtSampIntS6FlowRopeD5,
                                 ObtSampIntS6FlowRopeE5, ObtSampIntS6FlowRopeF5, ObtSampIntS6FlowRopeG5, ObtSampIntS6FlowRopeH5,
                                 ObtSampIntS6SoundRopeA5, ObtSampIntS6SoundRopeB5, ObtSampIntS6SoundRopeC5, ObtSampIntS6SoundRopeD5,
                                 ObtSampIntS6SoundRopeE5, ObtSampIntS6SoundRopeF5, ObtSampIntS6SoundRopeG5, ObtSampIntS6SoundRopeH5,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 6:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp6,
                                ObtSampPress6,
                                ObtSampIntS6FlowRopeA6, ObtSampIntS6FlowRopeB6, ObtSampIntS6FlowRopeC6, ObtSampIntS6FlowRopeD6,
                                ObtSampIntS6FlowRopeE6, ObtSampIntS6FlowRopeF6, ObtSampIntS6FlowRopeG6, ObtSampIntS6FlowRopeH6,
                                ObtSampIntS6SoundRopeA6, ObtSampIntS6SoundRopeB6, ObtSampIntS6SoundRopeC6, ObtSampIntS6SoundRopeD6,
                                ObtSampIntS6SoundRopeE6, ObtSampIntS6SoundRopeF6, ObtSampIntS6SoundRopeG6, ObtSampIntS6SoundRopeH6,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 7:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp7,
                                ObtSampPress7,
                                ObtSampIntS6FlowRopeA7, ObtSampIntS6FlowRopeB7, ObtSampIntS6FlowRopeC7, ObtSampIntS6FlowRopeD7,
                                ObtSampIntS6FlowRopeE7, ObtSampIntS6FlowRopeF7, ObtSampIntS6FlowRopeG7, ObtSampIntS6FlowRopeH7,
                                ObtSampIntS6SoundRopeA7, ObtSampIntS6SoundRopeB7, ObtSampIntS6SoundRopeC7, ObtSampIntS6SoundRopeD7,
                                ObtSampIntS6SoundRopeE7, ObtSampIntS6SoundRopeF7, ObtSampIntS6SoundRopeG7, ObtSampIntS6SoundRopeH7,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 8:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp8,
                                ObtSampPress8,
                                ObtSampIntS6FlowRopeA8, ObtSampIntS6FlowRopeB8, ObtSampIntS6FlowRopeC8, ObtSampIntS6FlowRopeD8,
                                ObtSampIntS6FlowRopeE8, ObtSampIntS6FlowRopeF8, ObtSampIntS6FlowRopeG8, ObtSampIntS6FlowRopeH8,
                                ObtSampIntS6SoundRopeA8, ObtSampIntS6SoundRopeB8, ObtSampIntS6SoundRopeC8, ObtSampIntS6SoundRopeD8,
                                ObtSampIntS6SoundRopeE8, ObtSampIntS6SoundRopeF8, ObtSampIntS6SoundRopeG8, ObtSampIntS6SoundRopeH8,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 9:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp9,
                                 ObtSampPress9,
                                 ObtSampIntS6FlowRopeA9, ObtSampIntS6FlowRopeB9, ObtSampIntS6FlowRopeC9, ObtSampIntS6FlowRopeD9,
                                 ObtSampIntS6FlowRopeE9, ObtSampIntS6FlowRopeF9, ObtSampIntS6FlowRopeG9, ObtSampIntS6FlowRopeH9,
                                 ObtSampIntS6SoundRopeA9, ObtSampIntS6SoundRopeB9, ObtSampIntS6SoundRopeC9, ObtSampIntS6SoundRopeD9,
                                 ObtSampIntS6SoundRopeE9, ObtSampIntS6SoundRopeF9, ObtSampIntS6SoundRopeG9, ObtSampIntS6SoundRopeH9,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 10:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp10,
                                 ObtSampPress10,
                                 ObtSampIntS6FlowRopeA10, ObtSampIntS6FlowRopeB10, ObtSampIntS6FlowRopeC10, ObtSampIntS6FlowRopeD10,
                                 ObtSampIntS6FlowRopeE10, ObtSampIntS6FlowRopeF10, ObtSampIntS6FlowRopeG10, ObtSampIntS6FlowRopeH10,
                                 ObtSampIntS6SoundRopeA10, ObtSampIntS6SoundRopeB10, ObtSampIntS6SoundRopeC10, ObtSampIntS6SoundRopeD10,
                                 ObtSampIntS6SoundRopeE10, ObtSampIntS6SoundRopeF10, ObtSampIntS6SoundRopeG10, ObtSampIntS6SoundRopeH10,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 11:
                            dryCalibration.UpdateObtainedSampleAverageLayout(ultrasonicModel,
                                ObtSampAverageTemp,
                                ObtSampAveragePress,
                                ObtSampAvgInstS6FlowRopeA, ObtSampAvgInstS6FlowRopeB, ObtSampAvgInstS6FlowRopeC, ObtSampAvgInstS6FlowRopeD,
                                ObtSampAvgInstS6FlowRopeE, ObtSampAvgInstS6FlowRopeF, ObtSampAvgInstS6FlowRopeG, ObtSampAvgInstS6FlowRopeH,
                                ObtSampAvgInstS6SoundRopeA, ObtSampAvgInstS6SoundRopeB, ObtSampAvgInstS6SoundRopeC, ObtSampAvgInstS6SoundRopeD,
                                ObtSampAvgInstS6SoundRopeE, ObtSampAvgInstS6SoundRopeF, ObtSampAvgInstS6SoundRopeG, ObtSampAvgInstS6SoundRopeH,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH
                                );
                            break;
                    }
                    break;
                case UltrasonicModel.Sick:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp1,
                                ObtSampPress1,
                                ObtSampSickFlowRopeA1, ObtSampSickFlowRopeB1, ObtSampSickFlowRopeC1, ObtSampSickFlowRopeD1,
                                ObtSampSickSoundRopeA1, ObtSampSickSoundRopeB1, ObtSampSickSoundRopeC1, ObtSampSickSoundRopeD1,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 2:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp2,
                                 ObtSampPress2,
                                 ObtSampSickFlowRopeA2, ObtSampSickFlowRopeB2, ObtSampSickFlowRopeC2, ObtSampSickFlowRopeD2,
                                 ObtSampSickSoundRopeA2, ObtSampSickSoundRopeB2, ObtSampSickSoundRopeC2, ObtSampSickSoundRopeD2,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 3:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp3,
                                ObtSampPress3,
                                ObtSampSickFlowRopeA3, ObtSampSickFlowRopeB3, ObtSampSickFlowRopeC3, ObtSampSickFlowRopeD3,
                                ObtSampSickSoundRopeA3, ObtSampSickSoundRopeB3, ObtSampSickSoundRopeC3, ObtSampSickSoundRopeD3,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 4:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp4,
                                ObtSampPress4,
                                ObtSampSickFlowRopeA4, ObtSampSickFlowRopeB4, ObtSampSickFlowRopeC4, ObtSampSickFlowRopeD4,
                                ObtSampSickSoundRopeA4, ObtSampSickSoundRopeB4, ObtSampSickSoundRopeC4, ObtSampSickSoundRopeD4,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 5:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp5,
                                 ObtSampPress5,
                                 ObtSampSickFlowRopeA5, ObtSampSickFlowRopeB5, ObtSampSickFlowRopeC5, ObtSampSickFlowRopeD5,
                                 ObtSampSickSoundRopeA5, ObtSampSickSoundRopeB5, ObtSampSickSoundRopeC5, ObtSampSickSoundRopeD5,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 6:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp6,
                                 ObtSampPress6,
                                 ObtSampSickFlowRopeA6, ObtSampSickFlowRopeB6, ObtSampSickFlowRopeC6, ObtSampSickFlowRopeD6,
                                 ObtSampSickSoundRopeA6, ObtSampSickSoundRopeB6, ObtSampSickSoundRopeC6, ObtSampSickSoundRopeD6,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 7:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp7,
                                 ObtSampPress7,
                                 ObtSampSickFlowRopeA7, ObtSampSickFlowRopeB7, ObtSampSickFlowRopeC7, ObtSampSickFlowRopeD7,
                                 ObtSampSickSoundRopeA7, ObtSampSickSoundRopeB7, ObtSampSickSoundRopeC7, ObtSampSickSoundRopeD7,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 8:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp8,
                                ObtSampPress8,
                                ObtSampSickFlowRopeA8, ObtSampSickFlowRopeB8, ObtSampSickFlowRopeC8, ObtSampSickFlowRopeD8,
                                ObtSampSickSoundRopeA8, ObtSampSickSoundRopeB8, ObtSampSickSoundRopeC8, ObtSampSickSoundRopeD8,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 9:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp9,
                                ObtSampPress9,
                                ObtSampSickFlowRopeA9, ObtSampSickFlowRopeB9, ObtSampSickFlowRopeC9, ObtSampSickFlowRopeD9,
                                ObtSampSickSoundRopeA9, ObtSampSickSoundRopeB9, ObtSampSickSoundRopeC9, ObtSampSickSoundRopeD9,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 10:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp10,
                                ObtSampPress10,
                                ObtSampSickFlowRopeA10, ObtSampSickFlowRopeB10, ObtSampSickFlowRopeC10, ObtSampSickFlowRopeD10,
                                ObtSampSickSoundRopeA10, ObtSampSickSoundRopeB10, ObtSampSickSoundRopeC10, ObtSampSickSoundRopeD10,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 11:
                            dryCalibration.UpdateObtainedSampleAverageLayout(ultrasonicModel,
                                ObtSampAverageTemp,
                                ObtSampAveragePress,
                                ObtSampAvgSickFlowRopeA, ObtSampAvgSickFlowRopeB, ObtSampAvgSickFlowRopeC, ObtSampAvgSickFlowRopeD,
                                ObtSampAvgSickSoundRopeA, ObtSampAvgSickSoundRopeB, ObtSampAvgSickSoundRopeC, ObtSampAvgSickSoundRopeD,
                                ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD
                                );
                            break;
                    }
                    break;
                case UltrasonicModel.FMU:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp1,
                                ObtSampPress1,
                                ObtSampFmuFlowRopeA1, ObtSampFmuFlowRopeB1, ObtSampFmuFlowRopeC1, ObtSampFmuFlowRopeD1,
                                ObtSampFmuSoundRopeA1, ObtSampFmuSoundRopeB1, ObtSampFmuSoundRopeC1, ObtSampFmuSoundRopeD1,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 2:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp2,
                                ObtSampPress2,
                                ObtSampFmuFlowRopeA2, ObtSampFmuFlowRopeB2, ObtSampFmuFlowRopeC2, ObtSampFmuFlowRopeD2,
                                ObtSampFmuSoundRopeA2, ObtSampFmuSoundRopeB2, ObtSampFmuSoundRopeC2, ObtSampFmuSoundRopeD2,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 3:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp3,
                                 ObtSampPress3,
                                 ObtSampFmuFlowRopeA3, ObtSampFmuFlowRopeB3, ObtSampFmuFlowRopeC3, ObtSampFmuFlowRopeD3,
                                 ObtSampFmuSoundRopeA3, ObtSampFmuSoundRopeB3, ObtSampFmuSoundRopeC3, ObtSampFmuSoundRopeD3,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 4:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp4,
                                ObtSampPress4,
                                ObtSampFmuFlowRopeA4, ObtSampFmuFlowRopeB4, ObtSampFmuFlowRopeC4, ObtSampFmuFlowRopeD4,
                                ObtSampFmuSoundRopeA4, ObtSampFmuSoundRopeB4, ObtSampFmuSoundRopeC4, ObtSampFmuSoundRopeD4,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 5:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp5,
                                ObtSampPress5,
                                ObtSampFmuFlowRopeA5, ObtSampFmuFlowRopeB5, ObtSampFmuFlowRopeC5, ObtSampFmuFlowRopeD5,
                                ObtSampFmuSoundRopeA5, ObtSampFmuSoundRopeB5, ObtSampFmuSoundRopeC5, ObtSampFmuSoundRopeD5,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 6:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp6,
                                 ObtSampPress6,
                                 ObtSampFmuFlowRopeA6, ObtSampFmuFlowRopeB6, ObtSampFmuFlowRopeC6, ObtSampFmuFlowRopeD6,
                                 ObtSampFmuSoundRopeA6, ObtSampFmuSoundRopeB6, ObtSampFmuSoundRopeC6, ObtSampFmuSoundRopeD6,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 7:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp7,
                                 ObtSampPress7,
                                 ObtSampFmuFlowRopeA7, ObtSampFmuFlowRopeB7, ObtSampFmuFlowRopeC7, ObtSampFmuFlowRopeD7,
                                 ObtSampFmuSoundRopeA7, ObtSampFmuSoundRopeB7, ObtSampFmuSoundRopeC7, ObtSampFmuSoundRopeD7,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 8:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp8,
                                ObtSampPress8,
                                ObtSampFmuFlowRopeA8, ObtSampFmuFlowRopeB8, ObtSampFmuFlowRopeC8, ObtSampFmuFlowRopeD8,
                                ObtSampFmuSoundRopeA8, ObtSampFmuSoundRopeB8, ObtSampFmuSoundRopeC8, ObtSampFmuSoundRopeD8,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 9:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp9,
                                ObtSampPress9,
                                ObtSampFmuFlowRopeA9, ObtSampFmuFlowRopeB9, ObtSampFmuFlowRopeC9, ObtSampFmuFlowRopeD9,
                                ObtSampFmuSoundRopeA9, ObtSampFmuSoundRopeB9, ObtSampFmuSoundRopeC9, ObtSampFmuSoundRopeD9,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 10:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp10,
                                ObtSampPress10,
                                ObtSampFmuFlowRopeA10, ObtSampFmuFlowRopeB10, ObtSampFmuFlowRopeC10, ObtSampFmuFlowRopeD10,
                                ObtSampFmuSoundRopeA10, ObtSampFmuSoundRopeB10, ObtSampFmuSoundRopeC10, ObtSampFmuSoundRopeD10,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 11:
                            dryCalibration.UpdateObtainedSampleAverageLayout(ultrasonicModel,
                                ObtSampAverageTemp,
                                ObtSampAveragePress,
                                ObtSampAvgFmuFlowRopeA, ObtSampAvgFmuFlowRopeB, ObtSampAvgFmuFlowRopeC, ObtSampAvgFmuFlowRopeD,
                                ObtSampAvgFmuSoundRopeA, ObtSampAvgFmuSoundRopeB, ObtSampAvgFmuSoundRopeC, ObtSampAvgFmuSoundRopeD,
                                ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD
                                );
                            break;
                    }
                    break;
                case UltrasonicModel.KrohneAltosonicV12:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp1,
                                ObtSampPress1,
                                ObtSampKrohneAltV12FlowRopeA1, ObtSampKrohneAltV12FlowRopeB1, ObtSampKrohneAltV12FlowRopeC1,
                                ObtSampKrohneAltV12FlowRopeD1, ObtSampKrohneAltV12FlowRopeE1, ObtSampKrohneAltV12FlowRopeF1,
                                ObtSampKrohneAltV12SoundRopeA1, ObtSampKrohneAltV12SoundRopeB1, ObtSampKrohneAltV12SoundRopeC1,
                                ObtSampKrohneAltV12SoundRopeD1, ObtSampKrohneAltV12SoundRopeE1, ObtSampKrohneAltV12SoundRopeF1,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 2:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp2,
                                ObtSampPress2,
                                ObtSampKrohneAltV12FlowRopeA2, ObtSampKrohneAltV12FlowRopeB2, ObtSampKrohneAltV12FlowRopeC2,
                                ObtSampKrohneAltV12FlowRopeD2, ObtSampKrohneAltV12FlowRopeE2, ObtSampKrohneAltV12FlowRopeF2,
                                ObtSampKrohneAltV12SoundRopeA2, ObtSampKrohneAltV12SoundRopeB2, ObtSampKrohneAltV12SoundRopeC2,
                                ObtSampKrohneAltV12SoundRopeD2, ObtSampKrohneAltV12SoundRopeE2, ObtSampKrohneAltV12SoundRopeF2,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 3:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp3,
                                ObtSampPress3,
                                ObtSampKrohneAltV12FlowRopeA3, ObtSampKrohneAltV12FlowRopeB3, ObtSampKrohneAltV12FlowRopeC3,
                                ObtSampKrohneAltV12FlowRopeD3, ObtSampKrohneAltV12FlowRopeE3, ObtSampKrohneAltV12FlowRopeF3,
                                ObtSampKrohneAltV12SoundRopeA3, ObtSampKrohneAltV12SoundRopeB3, ObtSampKrohneAltV12SoundRopeC3,
                                ObtSampKrohneAltV12SoundRopeD3, ObtSampKrohneAltV12SoundRopeE3, ObtSampKrohneAltV12SoundRopeF3,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 4:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp4,
                                 ObtSampPress4,
                                 ObtSampKrohneAltV12FlowRopeA4, ObtSampKrohneAltV12FlowRopeB4, ObtSampKrohneAltV12FlowRopeC4,
                                 ObtSampKrohneAltV12FlowRopeD4, ObtSampKrohneAltV12FlowRopeE4, ObtSampKrohneAltV12FlowRopeF4,
                                 ObtSampKrohneAltV12SoundRopeA4, ObtSampKrohneAltV12SoundRopeB4, ObtSampKrohneAltV12SoundRopeC4,
                                 ObtSampKrohneAltV12SoundRopeD4, ObtSampKrohneAltV12SoundRopeE4, ObtSampKrohneAltV12SoundRopeF4,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 5:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp5,
                                 ObtSampPress5,
                                 ObtSampKrohneAltV12FlowRopeA5, ObtSampKrohneAltV12FlowRopeB5, ObtSampKrohneAltV12FlowRopeC5,
                                 ObtSampKrohneAltV12FlowRopeD5, ObtSampKrohneAltV12FlowRopeE5, ObtSampKrohneAltV12FlowRopeF5,
                                 ObtSampKrohneAltV12SoundRopeA5, ObtSampKrohneAltV12SoundRopeB5, ObtSampKrohneAltV12SoundRopeC5,
                                 ObtSampKrohneAltV12SoundRopeD5, ObtSampKrohneAltV12SoundRopeE5, ObtSampKrohneAltV12SoundRopeF5,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 6:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp6,
                                ObtSampPress6,
                                ObtSampKrohneAltV12FlowRopeA6, ObtSampKrohneAltV12FlowRopeB6, ObtSampKrohneAltV12FlowRopeC6,
                                ObtSampKrohneAltV12FlowRopeD6, ObtSampKrohneAltV12FlowRopeE6, ObtSampKrohneAltV12FlowRopeF6,
                                ObtSampKrohneAltV12SoundRopeA6, ObtSampKrohneAltV12SoundRopeB6, ObtSampKrohneAltV12SoundRopeC6,
                                ObtSampKrohneAltV12SoundRopeD6, ObtSampKrohneAltV12SoundRopeE6, ObtSampKrohneAltV12SoundRopeF6,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 7:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp7,
                                ObtSampPress7,
                                ObtSampKrohneAltV12FlowRopeA7, ObtSampKrohneAltV12FlowRopeB7, ObtSampKrohneAltV12FlowRopeC7,
                                ObtSampKrohneAltV12FlowRopeD7, ObtSampKrohneAltV12FlowRopeE7, ObtSampKrohneAltV12FlowRopeF7,
                                ObtSampKrohneAltV12SoundRopeA7, ObtSampKrohneAltV12SoundRopeB7, ObtSampKrohneAltV12SoundRopeC7,
                                ObtSampKrohneAltV12SoundRopeD7, ObtSampKrohneAltV12SoundRopeE7, ObtSampKrohneAltV12SoundRopeF7,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 8:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampTemp8,
                                ObtSampPress8,
                                ObtSampKrohneAltV12FlowRopeA8, ObtSampKrohneAltV12FlowRopeB8, ObtSampKrohneAltV12FlowRopeC8,
                                ObtSampKrohneAltV12FlowRopeD8, ObtSampKrohneAltV12FlowRopeE8, ObtSampKrohneAltV12FlowRopeF8,
                                ObtSampKrohneAltV12SoundRopeA8, ObtSampKrohneAltV12SoundRopeB8, ObtSampKrohneAltV12SoundRopeC8,
                                ObtSampKrohneAltV12SoundRopeD8, ObtSampKrohneAltV12SoundRopeE8, ObtSampKrohneAltV12SoundRopeF8,
                                ObtSampRtdDifferenceBySample);
                            break;
                        case 9:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp9,
                                 ObtSampPress9,
                                 ObtSampKrohneAltV12FlowRopeA9, ObtSampKrohneAltV12FlowRopeB9, ObtSampKrohneAltV12FlowRopeC9,
                                 ObtSampKrohneAltV12FlowRopeD9, ObtSampKrohneAltV12FlowRopeE9, ObtSampKrohneAltV12FlowRopeF9,
                                 ObtSampKrohneAltV12SoundRopeA9, ObtSampKrohneAltV12SoundRopeB9, ObtSampKrohneAltV12SoundRopeC9,
                                 ObtSampKrohneAltV12SoundRopeD9, ObtSampKrohneAltV12SoundRopeE9, ObtSampKrohneAltV12SoundRopeF9,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 10:
                            dryCalibration.UpdateObtainedSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampTemp10,
                                 ObtSampPress10,
                                 ObtSampKrohneAltV12FlowRopeA10, ObtSampKrohneAltV12FlowRopeB10, ObtSampKrohneAltV12FlowRopeC10,
                                 ObtSampKrohneAltV12FlowRopeD10, ObtSampKrohneAltV12FlowRopeE10, ObtSampKrohneAltV12FlowRopeF10,
                                 ObtSampKrohneAltV12SoundRopeA10, ObtSampKrohneAltV12SoundRopeB10, ObtSampKrohneAltV12SoundRopeC10,
                                 ObtSampKrohneAltV12SoundRopeD10, ObtSampKrohneAltV12SoundRopeE10, ObtSampKrohneAltV12SoundRopeF10,
                                 ObtSampRtdDifferenceBySample);
                            break;
                        case 11:
                            dryCalibration.UpdateObtainedSampleAverageLayout(ultrasonicModel,
                                ObtSampAverageTemp,
                                ObtSampAveragePress,
                                ObtSampAvgKrohneAltV12FlowRopeA, ObtSampAvgKrohneAltV12FlowRopeB, ObtSampAvgKrohneAltV12FlowRopeC,
                                ObtSampAvgKrohneAltV12FlowRopeD, ObtSampAvgKrohneAltV12FlowRopeE, ObtSampAvgKrohneAltV12FlowRopeF,
                                ObtSampAvgKrohneAltV12SoundRopeA, ObtSampAvgKrohneAltV12SoundRopeB, ObtSampAvgKrohneAltV12SoundRopeC,
                                ObtSampAvgKrohneAltV12SoundRopeD, ObtSampAvgKrohneAltV12SoundRopeE, ObtSampAvgKrohneAltV12SoundRopeF,
                                ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF
                                );
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        private void SetDefaultEffValues(UltrasonicModel ultrasonicModel)
        {
            // valores por defecto para la eficiencia por cuerda, solo modo manual
            if (dryCalibration.CurrentModbusConfiguration.UltrasonicSampleMode == (int)UltSampMode.Manual)
            {
                switch (ultrasonicModel)
                {
                    case UltrasonicModel.Daniel:
                        dryCalibration.SetText(ObtSampEffDanielRopeA, 100);
                        dryCalibration.SetText(ObtSampEffDanielRopeB, 100);
                        dryCalibration.SetText(ObtSampEffDanielRopeC, 100);
                        dryCalibration.SetText(ObtSampEffDanielRopeD, 100);
                        break;
                    case UltrasonicModel.DanielJunior1R:
                        dryCalibration.SetText(ObtSampEffDaniel1RRopeA, 100);
                        break;
                    case UltrasonicModel.DanielJunior2R:
                        dryCalibration.SetText(ObtSampEffDaniel2RRopeA, 100);
                        dryCalibration.SetText(ObtSampEffDaniel2RRopeB, 100);
                        break;
                    case UltrasonicModel.InstrometS5:
                        dryCalibration.SetText(ObtSampEffInsS5RopeA, 100);
                        dryCalibration.SetText(ObtSampEffInsS5RopeB, 100);
                        dryCalibration.SetText(ObtSampEffInsS5RopeC, 100);
                        dryCalibration.SetText(ObtSampEffInsS5RopeD, 100);
                        dryCalibration.SetText(ObtSampEffInsS5RopeE, 100);
                        break;
                    case UltrasonicModel.InstrometS6:
                        dryCalibration.SetText(ObtSampEffInsS6RopeA, 100);
                        dryCalibration.SetText(ObtSampEffInsS6RopeB, 100);
                        dryCalibration.SetText(ObtSampEffInsS6RopeC, 100);
                        dryCalibration.SetText(ObtSampEffInsS6RopeD, 100);
                        dryCalibration.SetText(ObtSampEffInsS6RopeE, 100);
                        dryCalibration.SetText(ObtSampEffInsS6RopeF, 100);
                        dryCalibration.SetText(ObtSampEffInsS6RopeG, 100);
                        dryCalibration.SetText(ObtSampEffInsS6RopeH, 100);
                        break;
                    case UltrasonicModel.Sick:
                        dryCalibration.SetText(ObtSampEffSickRopeA, 100);
                        dryCalibration.SetText(ObtSampEffSickRopeB, 100);
                        dryCalibration.SetText(ObtSampEffSickRopeC, 100);
                        dryCalibration.SetText(ObtSampEffSickRopeD, 100);
                        break;
                    case UltrasonicModel.FMU:
                        dryCalibration.SetText(ObtSampEffFmuRopeA, 100);
                        dryCalibration.SetText(ObtSampEffFmuRopeB, 100);
                        dryCalibration.SetText(ObtSampEffFmuRopeC, 100);
                        dryCalibration.SetText(ObtSampEffFmuRopeD, 100);
                        break;
                    case UltrasonicModel.KrohneAltosonicV12:
                        dryCalibration.SetText(ObtSampEffKrohneAltV12RopeA, 100);
                        dryCalibration.SetText(ObtSampEffKrohneAltV12RopeB, 100);
                        dryCalibration.SetText(ObtSampEffKrohneAltV12RopeC, 100);
                        dryCalibration.SetText(ObtSampEffKrohneAltV12RopeD, 100);
                        dryCalibration.SetText(ObtSampEffKrohneAltV12RopeE, 100);
                        dryCalibration.SetText(ObtSampEffKrohneAltV12RopeF, 100);
                        break;  
                }
            }
        }

        private void SetDefaultGainValues(UltrasonicModel ultrasonicModel)
        {
            // valores por defecto para la ganacia por cuerda, solo modo manual
            if (dryCalibration.CurrentModbusConfiguration.UltrasonicSampleMode == (int)UltSampMode.Manual)
            {
                switch (ultrasonicModel)
                {
                    case UltrasonicModel.Daniel:
                        dryCalibration.SetText(ObtSampGainDanielRopeAT1, 0);
                        dryCalibration.SetText(ObtSampGainDanielRopeAT2, 0);
                        dryCalibration.SetText(ObtSampGainDanielRopeBT1, 0);
                        dryCalibration.SetText(ObtSampGainDanielRopeBT2, 0);
                        dryCalibration.SetText(ObtSampGainDanielRopeCT1, 0);
                        dryCalibration.SetText(ObtSampGainDanielRopeCT2, 0);
                        dryCalibration.SetText(ObtSampGainDanielRopeDT1, 0);
                        dryCalibration.SetText(ObtSampGainDanielRopeDT2, 0);

                        break;
                    case UltrasonicModel.DanielJunior1R:
                        dryCalibration.SetText(ObtSampGainDaniel1RRopeAT1, 0);
                        dryCalibration.SetText(ObtSampGainDaniel1RRopeAT2, 0);
                     
                        break;
                    case UltrasonicModel.DanielJunior2R:
                        dryCalibration.SetText(ObtSampGainDaniel2RRopeAT1, 0);
                        dryCalibration.SetText(ObtSampGainDaniel2RRopeAT2, 0);
                        dryCalibration.SetText(ObtSampGainDaniel2RRopeBT1, 0);
                        dryCalibration.SetText(ObtSampGainDaniel2RRopeBT2, 0);
                       
                        break;
                    case UltrasonicModel.InstrometS5:
                        dryCalibration.SetText(ObtSampGainInstS5RopeAT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS5RopeAT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS5RopeBT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS5RopeBT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS5RopeCT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS5RopeCT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS5RopeDT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS5RopeDT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS5RopeET1, 0);
                        dryCalibration.SetText(ObtSampGainInstS5RopeET2, 0);

                        break;
                    case UltrasonicModel.InstrometS6:
                        dryCalibration.SetText(ObtSampGainInstS6RopeAT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeAT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeBT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeBT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeCT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeCT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeDT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeDT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeET1, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeET2, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeFT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeFT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeGT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeGT2, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeHT1, 0);
                        dryCalibration.SetText(ObtSampGainInstS6RopeHT2, 0);

                        break;
                    case UltrasonicModel.Sick:
                        dryCalibration.SetText(ObtSampGainSickRopeAT1, 0);
                        dryCalibration.SetText(ObtSampGainSickRopeAT2, 0);
                        dryCalibration.SetText(ObtSampGainSickRopeBT1, 0);
                        dryCalibration.SetText(ObtSampGainSickRopeBT2, 0);
                        dryCalibration.SetText(ObtSampGainSickRopeCT1, 0);
                        dryCalibration.SetText(ObtSampGainSickRopeCT2, 0);
                        dryCalibration.SetText(ObtSampGainSickRopeDT1, 0);
                        dryCalibration.SetText(ObtSampGainSickRopeDT2, 0);

                        break;
                    case UltrasonicModel.FMU:
                        dryCalibration.SetText(ObtSampGainFMURopeAT1, 0);
                        dryCalibration.SetText(ObtSampGainFMURopeAT2, 0);
                        dryCalibration.SetText(ObtSampGainFMURopeBT1, 0);
                        dryCalibration.SetText(ObtSampGainFMURopeBT2, 0);
                        dryCalibration.SetText(ObtSampGainFMURopeCT1, 0);
                        dryCalibration.SetText(ObtSampGainFMURopeCT2, 0);
                        dryCalibration.SetText(ObtSampGainFMURopeDT1, 0);
                        dryCalibration.SetText(ObtSampGainFMURopeDT2, 0);

                        break;
                    case UltrasonicModel.KrohneAltosonicV12:
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeAT1, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeAT2, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeBT1, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeBT2, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeCT1, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeCT2, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeDT1, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeDT2, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeET1, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeET2, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeFT1, 0);
                        dryCalibration.SetText(ObtSampGainKrohneAltV12RopeFT2, 0);

                        break;
                }
            }
        }

        private void UpdateSampleValuesFromSampleLayout(int sampleNumber, UltrasonicModel ultrasonicModel) 
        {
            switch (ultrasonicModel)
            {
                case UltrasonicModel.Daniel:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber, 
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA1, ObtSampDanielFlowRopeB1, ObtSampDanielFlowRopeC1, ObtSampDanielFlowRopeD1,
                                ObtSampDanielSoundRopeA1, ObtSampDanielSoundRopeB1, ObtSampDanielSoundRopeC1, ObtSampDanielSoundRopeD1,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2, 
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;
                        case 2:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA2, ObtSampDanielFlowRopeB2, ObtSampDanielFlowRopeC2, ObtSampDanielFlowRopeD2,
                                ObtSampDanielSoundRopeA2, ObtSampDanielSoundRopeB2, ObtSampDanielSoundRopeC2, ObtSampDanielSoundRopeD2,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2,
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;
                        case 3:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA3, ObtSampDanielFlowRopeB3, ObtSampDanielFlowRopeC3, ObtSampDanielFlowRopeD3,
                                ObtSampDanielSoundRopeA3, ObtSampDanielSoundRopeB3, ObtSampDanielSoundRopeC3, ObtSampDanielSoundRopeD3,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2,
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;
                        case 4:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA4, ObtSampDanielFlowRopeB4, ObtSampDanielFlowRopeC4, ObtSampDanielFlowRopeD4,
                                ObtSampDanielSoundRopeA4, ObtSampDanielSoundRopeB4, ObtSampDanielSoundRopeC4, ObtSampDanielSoundRopeD4,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2,
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;
                        case 5:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA5, ObtSampDanielFlowRopeB5, ObtSampDanielFlowRopeC5, ObtSampDanielFlowRopeD5,
                                ObtSampDanielSoundRopeA5, ObtSampDanielSoundRopeB5, ObtSampDanielSoundRopeC5, ObtSampDanielSoundRopeD5,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2,
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;
                        case 6:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA6, ObtSampDanielFlowRopeB6, ObtSampDanielFlowRopeC6, ObtSampDanielFlowRopeD6,
                                ObtSampDanielSoundRopeA6, ObtSampDanielSoundRopeB6, ObtSampDanielSoundRopeC6, ObtSampDanielSoundRopeD6,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2,
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;
                        case 7:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA7, ObtSampDanielFlowRopeB7, ObtSampDanielFlowRopeC7, ObtSampDanielFlowRopeD7,
                                ObtSampDanielSoundRopeA7, ObtSampDanielSoundRopeB7, ObtSampDanielSoundRopeC7, ObtSampDanielSoundRopeD7,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2,
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;
                        case 8:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA8, ObtSampDanielFlowRopeB8, ObtSampDanielFlowRopeC8, ObtSampDanielFlowRopeD8,
                                ObtSampDanielSoundRopeA8, ObtSampDanielSoundRopeB8, ObtSampDanielSoundRopeC8, ObtSampDanielSoundRopeD8,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2,
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;
                        case 9:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA9, ObtSampDanielFlowRopeB9, ObtSampDanielFlowRopeC9, ObtSampDanielFlowRopeD9,
                                ObtSampDanielSoundRopeA9, ObtSampDanielSoundRopeB9, ObtSampDanielSoundRopeC9, ObtSampDanielSoundRopeD9,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2,
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;
                        case 10:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDanielFlowRopeA10, ObtSampDanielFlowRopeB10, ObtSampDanielFlowRopeC10, ObtSampDanielFlowRopeD10,
                                ObtSampDanielSoundRopeA10, ObtSampDanielSoundRopeB10, ObtSampDanielSoundRopeC10, ObtSampDanielSoundRopeD10,
                                ObtSampEffDanielRopeA, ObtSampEffDanielRopeB, ObtSampEffDanielRopeC, ObtSampEffDanielRopeD,
                                ObtSampGainDanielRopeAT1, ObtSampGainDanielRopeAT2, ObtSampGainDanielRopeBT1, ObtSampGainDanielRopeBT2,
                                ObtSampGainDanielRopeCT1, ObtSampGainDanielRopeCT2, ObtSampGainDanielRopeDT1, ObtSampGainDanielRopeDT2);
                            break;

                    }
                    break;
                case UltrasonicModel.DanielJunior1R:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDaniel1RFlowRopeA1,
                                ObtSampDaniel1RSoundRopeA1,
                                ObtSampEffDaniel1RRopeA,
                                ObtSampGainDaniel1RRopeAT1,ObtSampGainDaniel1RRopeAT2);
                            break;
                        case 2:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDaniel1RFlowRopeA2,
                                ObtSampDaniel1RSoundRopeA2,
                                ObtSampEffDaniel1RRopeA,
                                ObtSampGainDaniel1RRopeAT1, ObtSampGainDaniel1RRopeAT2);
                            break;
                        case 3:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel1RFlowRopeA3,
                               ObtSampDaniel1RSoundRopeA3,
                               ObtSampEffDaniel1RRopeA,
                               ObtSampGainDaniel1RRopeAT1, ObtSampGainDaniel1RRopeAT2);
                            break;
                        case 4:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDaniel1RFlowRopeA4,
                                ObtSampDaniel1RSoundRopeA4,
                                ObtSampEffDaniel1RRopeA,
                                ObtSampGainDaniel1RRopeAT1, ObtSampGainDaniel1RRopeAT2);
                            break;
                        case 5:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel1RFlowRopeA5,
                               ObtSampDaniel1RSoundRopeA5,
                               ObtSampEffDaniel1RRopeA,
                               ObtSampGainDaniel1RRopeAT1, ObtSampGainDaniel1RRopeAT2);
                            break;
                        case 6:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel1RFlowRopeA6,
                               ObtSampDaniel1RSoundRopeA6,
                               ObtSampEffDaniel1RRopeA,
                               ObtSampGainDaniel1RRopeAT1, ObtSampGainDaniel1RRopeAT2);
                            break;
                        case 7:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel1RFlowRopeA7,
                               ObtSampDaniel1RSoundRopeA7,
                               ObtSampEffDaniel1RRopeA,
                               ObtSampGainDaniel1RRopeAT1, ObtSampGainDaniel1RRopeAT2);
                            break;
                        case 8:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel1RFlowRopeA8,
                               ObtSampDaniel1RSoundRopeA8,
                               ObtSampEffDaniel1RRopeA,
                               ObtSampGainDaniel1RRopeAT1, ObtSampGainDaniel1RRopeAT2);
                            break;
                        case 9:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel1RFlowRopeA9,
                               ObtSampDaniel1RSoundRopeA9,
                               ObtSampEffDaniel1RRopeA,
                               ObtSampGainDaniel1RRopeAT1, ObtSampGainDaniel1RRopeAT2);
                            break;
                        case 10:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel1RFlowRopeA10,
                               ObtSampDaniel1RSoundRopeA10,
                               ObtSampEffDaniel1RRopeA,
                               ObtSampGainDaniel1RRopeAT1, ObtSampGainDaniel1RRopeAT2);
                            break;

                    }
                    break;
                case UltrasonicModel.DanielJunior2R:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDaniel2RFlowRopeA1, ObtSampDaniel2RFlowRopeB1,
                                ObtSampDaniel2RSoundRopeA1, ObtSampDaniel2RSoundRopeB1,
                                ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                                ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                                ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;
                        case 2:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel2RFlowRopeA2, ObtSampDaniel2RFlowRopeB2,
                               ObtSampDaniel2RSoundRopeA2, ObtSampDaniel2RSoundRopeB2,
                               ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                               ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                               ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;
                        case 3:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel2RFlowRopeA3, ObtSampDaniel2RFlowRopeB3,
                               ObtSampDaniel2RSoundRopeA3, ObtSampDaniel2RSoundRopeB3,
                               ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                               ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                               ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;
                        case 4:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel2RFlowRopeA4, ObtSampDaniel2RFlowRopeB4,
                               ObtSampDaniel2RSoundRopeA4, ObtSampDaniel2RSoundRopeB4,
                               ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                               ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                               ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;
                        case 5:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel2RFlowRopeA5, ObtSampDaniel2RFlowRopeB5,
                               ObtSampDaniel2RSoundRopeA5, ObtSampDaniel2RSoundRopeB5,
                               ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                               ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                               ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;
                        case 6:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel2RFlowRopeA6, ObtSampDaniel2RFlowRopeB6,
                               ObtSampDaniel2RSoundRopeA6, ObtSampDaniel2RSoundRopeB6,
                               ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                               ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                               ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;
                        case 7:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel2RFlowRopeA7, ObtSampDaniel2RFlowRopeB7,
                               ObtSampDaniel2RSoundRopeA7, ObtSampDaniel2RSoundRopeB7,
                               ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                               ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                               ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;
                        case 8:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel2RFlowRopeA8, ObtSampDaniel2RFlowRopeB8,
                               ObtSampDaniel2RSoundRopeA8, ObtSampDaniel2RSoundRopeB8,
                               ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                               ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                               ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;
                        case 9:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                               ultrasonicModel,
                               ObtSampDaniel2RFlowRopeA9, ObtSampDaniel2RFlowRopeB9,
                               ObtSampDaniel2RSoundRopeA9, ObtSampDaniel2RSoundRopeB9,
                               ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                               ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                               ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;
                        case 10:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampDaniel2RFlowRopeA10, ObtSampDaniel2RFlowRopeB10,
                                ObtSampDaniel2RSoundRopeA10, ObtSampDaniel2RSoundRopeB10,
                                ObtSampEffDaniel2RRopeA, ObtSampEffDaniel2RRopeB,
                                ObtSampGainDaniel2RRopeAT1, ObtSampGainDaniel2RRopeAT2,
                                ObtSampGainDaniel2RRopeBT1, ObtSampGainDaniel2RRopeBT2);
                            break;

                    }
                    break;

                case UltrasonicModel.InstrometS5:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS5FlowRopeA1, ObtSampIntS5FlowRopeB1, ObtSampIntS5FlowRopeC1, ObtSampIntS5FlowRopeD1, ObtSampIntS5FlowRopeE1,
                                ObtSampIntS5SoundRopeA1, ObtSampIntS5SoundRopeB1, ObtSampIntS5SoundRopeC1, ObtSampIntS5SoundRopeD1, ObtSampIntS5SoundRopeE1,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                        case 2:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS5FlowRopeA2, ObtSampIntS5FlowRopeB2, ObtSampIntS5FlowRopeC2, ObtSampIntS5FlowRopeD2, ObtSampIntS5FlowRopeE2,
                                ObtSampIntS5SoundRopeA2, ObtSampIntS5SoundRopeB2, ObtSampIntS5SoundRopeC2, ObtSampIntS5SoundRopeD2, ObtSampIntS5SoundRopeE2,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                        case 3:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS5FlowRopeA3, ObtSampIntS5FlowRopeB3, ObtSampIntS5FlowRopeC3, ObtSampIntS5FlowRopeD3, ObtSampIntS5FlowRopeE3,
                                ObtSampIntS5SoundRopeA3, ObtSampIntS5SoundRopeB3, ObtSampIntS5SoundRopeC3, ObtSampIntS5SoundRopeD3, ObtSampIntS5SoundRopeE3,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                        case 4:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS5FlowRopeA4, ObtSampIntS5FlowRopeB4, ObtSampIntS5FlowRopeC4, ObtSampIntS5FlowRopeD4, ObtSampIntS5FlowRopeE4,
                                ObtSampIntS5SoundRopeA4, ObtSampIntS5SoundRopeB4, ObtSampIntS5SoundRopeC4, ObtSampIntS5SoundRopeD4, ObtSampIntS5SoundRopeE4,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                        case 5:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS5FlowRopeA5, ObtSampIntS5FlowRopeB5, ObtSampIntS5FlowRopeC5, ObtSampIntS5FlowRopeD5, ObtSampIntS5FlowRopeE5,
                                ObtSampIntS5SoundRopeA5, ObtSampIntS5SoundRopeB5, ObtSampIntS5SoundRopeC5, ObtSampIntS5SoundRopeD5, ObtSampIntS5SoundRopeE5,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                        case 6:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS5FlowRopeA6, ObtSampIntS5FlowRopeB6, ObtSampIntS5FlowRopeC6, ObtSampIntS5FlowRopeD6, ObtSampIntS5FlowRopeE6,
                                ObtSampIntS5SoundRopeA6, ObtSampIntS5SoundRopeB6, ObtSampIntS5SoundRopeC6, ObtSampIntS5SoundRopeD6, ObtSampIntS5SoundRopeE6,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                        case 7:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS5FlowRopeA7, ObtSampIntS5FlowRopeB7, ObtSampIntS5FlowRopeC7, ObtSampIntS5FlowRopeD7, ObtSampIntS5FlowRopeE7,
                                ObtSampIntS5SoundRopeA7, ObtSampIntS5SoundRopeB7, ObtSampIntS5SoundRopeC7, ObtSampIntS5SoundRopeD7, ObtSampIntS5SoundRopeE7,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                        case 8:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                 ultrasonicModel,
                                 ObtSampIntS5FlowRopeA8, ObtSampIntS5FlowRopeB8, ObtSampIntS5FlowRopeC8, ObtSampIntS5FlowRopeD8, ObtSampIntS5FlowRopeE8,
                                 ObtSampIntS5SoundRopeA8, ObtSampIntS5SoundRopeB8, ObtSampIntS5SoundRopeC8, ObtSampIntS5SoundRopeD8, ObtSampIntS5SoundRopeE8,
                                 ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                 ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                 ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                 ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                        case 9:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS5FlowRopeA9, ObtSampIntS5FlowRopeB9, ObtSampIntS5FlowRopeC9, ObtSampIntS5FlowRopeD9, ObtSampIntS5FlowRopeE9,
                                ObtSampIntS5SoundRopeA9, ObtSampIntS5SoundRopeB9, ObtSampIntS5SoundRopeC9, ObtSampIntS5SoundRopeD9, ObtSampIntS5SoundRopeE9,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                        case 10:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS5FlowRopeA10, ObtSampIntS5FlowRopeB10, ObtSampIntS5FlowRopeC10, ObtSampIntS5FlowRopeD10, ObtSampIntS5FlowRopeE10,
                                ObtSampIntS5SoundRopeA10, ObtSampIntS5SoundRopeB10, ObtSampIntS5SoundRopeC10, ObtSampIntS5SoundRopeD10, ObtSampIntS5SoundRopeE10,
                                ObtSampEffInsS5RopeA, ObtSampEffInsS5RopeB, ObtSampEffInsS5RopeC, ObtSampEffInsS5RopeD, ObtSampEffInsS5RopeE,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS5RopeAT2, ObtSampGainInstS5RopeBT1, ObtSampGainInstS5RopeBT2,
                                ObtSampGainInstS5RopeCT1, ObtSampGainInstS5RopeCT2, ObtSampGainInstS5RopeDT1, ObtSampGainInstS5RopeDT2,
                                ObtSampGainInstS5RopeET1, ObtSampGainInstS5RopeET2);
                            break;
                    }
                    break;
                case UltrasonicModel.InstrometS6:
                    switch (sampleNumber)
                    {
                        case 1:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA1, ObtSampIntS6FlowRopeB1, ObtSampIntS6FlowRopeC1, ObtSampIntS6FlowRopeD1,
                                ObtSampIntS6FlowRopeE1, ObtSampIntS6FlowRopeF1, ObtSampIntS6FlowRopeG1, ObtSampIntS6FlowRopeH1,
                                ObtSampIntS6SoundRopeA1, ObtSampIntS6SoundRopeB1, ObtSampIntS6SoundRopeC1, ObtSampIntS6SoundRopeD1,
                                ObtSampIntS6SoundRopeE1, ObtSampIntS6SoundRopeF1, ObtSampIntS6SoundRopeG1, ObtSampIntS6SoundRopeH1,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD, 
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;
                        case 2:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA2, ObtSampIntS6FlowRopeB2, ObtSampIntS6FlowRopeC2, ObtSampIntS6FlowRopeD2,
                                ObtSampIntS6FlowRopeE2, ObtSampIntS6FlowRopeF2, ObtSampIntS6FlowRopeG2, ObtSampIntS6FlowRopeH2,
                                ObtSampIntS6SoundRopeA2, ObtSampIntS6SoundRopeB2, ObtSampIntS6SoundRopeC2, ObtSampIntS6SoundRopeD2,
                                ObtSampIntS6SoundRopeE2, ObtSampIntS6SoundRopeF2, ObtSampIntS6SoundRopeG2, ObtSampIntS6SoundRopeH2,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;
                        case 3:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA3, ObtSampIntS6FlowRopeB3, ObtSampIntS6FlowRopeC3, ObtSampIntS6FlowRopeD3,
                                ObtSampIntS6FlowRopeE3, ObtSampIntS6FlowRopeF3, ObtSampIntS6FlowRopeG3, ObtSampIntS6FlowRopeH3,
                                ObtSampIntS6SoundRopeA3, ObtSampIntS6SoundRopeB3, ObtSampIntS6SoundRopeC3, ObtSampIntS6SoundRopeD3,
                                ObtSampIntS6SoundRopeE3, ObtSampIntS6SoundRopeF3, ObtSampIntS6SoundRopeG3, ObtSampIntS6SoundRopeH3,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;
                        case 4:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA4, ObtSampIntS6FlowRopeB4, ObtSampIntS6FlowRopeC4, ObtSampIntS6FlowRopeD4,
                                ObtSampIntS6FlowRopeE4, ObtSampIntS6FlowRopeF4, ObtSampIntS6FlowRopeG4, ObtSampIntS6FlowRopeH4,
                                ObtSampIntS6SoundRopeA4, ObtSampIntS6SoundRopeB4, ObtSampIntS6SoundRopeC4, ObtSampIntS6SoundRopeD4,
                                ObtSampIntS6SoundRopeE4, ObtSampIntS6SoundRopeF4, ObtSampIntS6SoundRopeG4, ObtSampIntS6SoundRopeH4,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;
                        case 5:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA5, ObtSampIntS6FlowRopeB5, ObtSampIntS6FlowRopeC5, ObtSampIntS6FlowRopeD5,
                                ObtSampIntS6FlowRopeE5, ObtSampIntS6FlowRopeF5, ObtSampIntS6FlowRopeG5, ObtSampIntS6FlowRopeH5,
                                ObtSampIntS6SoundRopeA5, ObtSampIntS6SoundRopeB5, ObtSampIntS6SoundRopeC5, ObtSampIntS6SoundRopeD5,
                                ObtSampIntS6SoundRopeE5, ObtSampIntS6SoundRopeF5, ObtSampIntS6SoundRopeG5, ObtSampIntS6SoundRopeH5,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;
                        case 6:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA6, ObtSampIntS6FlowRopeB6, ObtSampIntS6FlowRopeC6, ObtSampIntS6FlowRopeD6,
                                ObtSampIntS6FlowRopeE6, ObtSampIntS6FlowRopeF6, ObtSampIntS6FlowRopeG6, ObtSampIntS6FlowRopeH6,
                                ObtSampIntS6SoundRopeA6, ObtSampIntS6SoundRopeB6, ObtSampIntS6SoundRopeC6, ObtSampIntS6SoundRopeD6,
                                ObtSampIntS6SoundRopeE6, ObtSampIntS6SoundRopeF6, ObtSampIntS6SoundRopeG6, ObtSampIntS6SoundRopeH6,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;
                        case 7:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA7, ObtSampIntS6FlowRopeB7, ObtSampIntS6FlowRopeC7, ObtSampIntS6FlowRopeD7,
                                ObtSampIntS6FlowRopeE7, ObtSampIntS6FlowRopeF7, ObtSampIntS6FlowRopeG7, ObtSampIntS6FlowRopeH7,
                                ObtSampIntS6SoundRopeA7, ObtSampIntS6SoundRopeB7, ObtSampIntS6SoundRopeC7, ObtSampIntS6SoundRopeD7,
                                ObtSampIntS6SoundRopeE7, ObtSampIntS6SoundRopeF7, ObtSampIntS6SoundRopeG7, ObtSampIntS6SoundRopeH7,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;
                        case 8:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA8, ObtSampIntS6FlowRopeB8, ObtSampIntS6FlowRopeC8, ObtSampIntS6FlowRopeD8,
                                ObtSampIntS6FlowRopeE8, ObtSampIntS6FlowRopeF8, ObtSampIntS6FlowRopeG8, ObtSampIntS6FlowRopeH8,
                                ObtSampIntS6SoundRopeA8, ObtSampIntS6SoundRopeB8, ObtSampIntS6SoundRopeC8, ObtSampIntS6SoundRopeD8,
                                ObtSampIntS6SoundRopeE8, ObtSampIntS6SoundRopeF8, ObtSampIntS6SoundRopeG8, ObtSampIntS6SoundRopeH8,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;
                        case 9:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA9, ObtSampIntS6FlowRopeB9, ObtSampIntS6FlowRopeC9, ObtSampIntS6FlowRopeD9,
                                ObtSampIntS6FlowRopeE9, ObtSampIntS6FlowRopeF9, ObtSampIntS6FlowRopeG9, ObtSampIntS6FlowRopeH9,
                                ObtSampIntS6SoundRopeA9, ObtSampIntS6SoundRopeB9, ObtSampIntS6SoundRopeC9, ObtSampIntS6SoundRopeD9,
                                ObtSampIntS6SoundRopeE9, ObtSampIntS6SoundRopeF9, ObtSampIntS6SoundRopeG9, ObtSampIntS6SoundRopeH9,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;
                        case 10:
                            dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                ultrasonicModel,
                                ObtSampIntS6FlowRopeA10, ObtSampIntS6FlowRopeB10, ObtSampIntS6FlowRopeC10, ObtSampIntS6FlowRopeD10,
                                ObtSampIntS6FlowRopeE10, ObtSampIntS6FlowRopeF10, ObtSampIntS6FlowRopeG10, ObtSampIntS6FlowRopeH10,
                                ObtSampIntS6SoundRopeA10, ObtSampIntS6SoundRopeB10, ObtSampIntS6SoundRopeC10, ObtSampIntS6SoundRopeD10,
                                ObtSampIntS6SoundRopeE10, ObtSampIntS6SoundRopeF10, ObtSampIntS6SoundRopeG10, ObtSampIntS6SoundRopeH10,
                                ObtSampEffInsS6RopeA, ObtSampEffInsS6RopeB, ObtSampEffInsS6RopeC, ObtSampEffInsS6RopeD,
                                ObtSampEffInsS6RopeE, ObtSampEffInsS6RopeF, ObtSampEffInsS6RopeG, ObtSampEffInsS6RopeH,
                                ObtSampGainInstS5RopeAT1, ObtSampGainInstS6RopeAT2, ObtSampGainInstS6RopeBT1, ObtSampGainInstS6RopeBT2,
                                ObtSampGainInstS6RopeCT1, ObtSampGainInstS6RopeCT2, ObtSampGainInstS6RopeDT1, ObtSampGainInstS6RopeDT2,
                                ObtSampGainInstS6RopeET1, ObtSampGainInstS6RopeET2, ObtSampGainInstS6RopeFT1, ObtSampGainInstS6RopeFT2,
                                ObtSampGainInstS6RopeGT1, ObtSampGainInstS6RopeGT2, ObtSampGainInstS6RopeHT1, ObtSampGainInstS6RopeHT2);
                            break;

                    }
                    break;
                case UltrasonicModel.Sick:
                    switch (sampleNumber)
                        {
                            case 1:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA1, ObtSampSickFlowRopeB1, ObtSampSickFlowRopeC1, ObtSampSickFlowRopeD1,
                                    ObtSampSickSoundRopeA1, ObtSampSickSoundRopeB1, ObtSampSickSoundRopeC1, ObtSampSickSoundRopeD1,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;
                            case 2:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA2, ObtSampSickFlowRopeB2, ObtSampSickFlowRopeC2, ObtSampSickFlowRopeD2,
                                    ObtSampSickSoundRopeA2, ObtSampSickSoundRopeB2, ObtSampSickSoundRopeC2, ObtSampSickSoundRopeD2,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;
                            case 3:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA3, ObtSampSickFlowRopeB3, ObtSampSickFlowRopeC3, ObtSampSickFlowRopeD3,
                                    ObtSampSickSoundRopeA3, ObtSampSickSoundRopeB3, ObtSampSickSoundRopeC3, ObtSampSickSoundRopeD3,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;
                            case 4:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA4, ObtSampSickFlowRopeB4, ObtSampSickFlowRopeC4, ObtSampSickFlowRopeD4,
                                    ObtSampSickSoundRopeA4, ObtSampSickSoundRopeB4, ObtSampSickSoundRopeC4, ObtSampSickSoundRopeD4,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;
                            case 5:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA5, ObtSampSickFlowRopeB5, ObtSampSickFlowRopeC5, ObtSampSickFlowRopeD5,
                                    ObtSampSickSoundRopeA5, ObtSampSickSoundRopeB5, ObtSampSickSoundRopeC5, ObtSampSickSoundRopeD5,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;
                            case 6:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA6, ObtSampSickFlowRopeB6, ObtSampSickFlowRopeC6, ObtSampSickFlowRopeD6,
                                    ObtSampSickSoundRopeA6, ObtSampSickSoundRopeB6, ObtSampSickSoundRopeC6, ObtSampSickSoundRopeD6,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;
                            case 7:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA7, ObtSampSickFlowRopeB7, ObtSampSickFlowRopeC7, ObtSampSickFlowRopeD7,
                                    ObtSampSickSoundRopeA7, ObtSampSickSoundRopeB7, ObtSampSickSoundRopeC7, ObtSampSickSoundRopeD7,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;
                            case 8:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA8, ObtSampSickFlowRopeB8, ObtSampSickFlowRopeC8, ObtSampSickFlowRopeD8,
                                    ObtSampSickSoundRopeA8, ObtSampSickSoundRopeB8, ObtSampSickSoundRopeC8, ObtSampSickSoundRopeD8,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;
                            case 9:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA9, ObtSampSickFlowRopeB9, ObtSampSickFlowRopeC9, ObtSampSickFlowRopeD9,
                                    ObtSampSickSoundRopeA9, ObtSampSickSoundRopeB9, ObtSampSickSoundRopeC9, ObtSampSickSoundRopeD9,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;
                            case 10:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampSickFlowRopeA10, ObtSampSickFlowRopeB10, ObtSampSickFlowRopeC10, ObtSampSickFlowRopeD10,
                                    ObtSampSickSoundRopeA10, ObtSampSickSoundRopeB10, ObtSampSickSoundRopeC10, ObtSampSickSoundRopeD10,
                                    ObtSampEffSickRopeA, ObtSampEffSickRopeB, ObtSampEffSickRopeC, ObtSampEffSickRopeD,
                                    ObtSampGainSickRopeAT1, ObtSampGainSickRopeAT2, ObtSampGainSickRopeBT1, ObtSampGainSickRopeBT2,
                                    ObtSampGainSickRopeCT1, ObtSampGainSickRopeCT2, ObtSampGainSickRopeDT1, ObtSampGainSickRopeDT2);
                            break;

                        }
                    break;
                case UltrasonicModel.FMU:
                    switch (sampleNumber)
                        {
                            case 1:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA1, ObtSampFmuFlowRopeB1, ObtSampFmuFlowRopeC1, ObtSampFmuFlowRopeD1,
                                    ObtSampFmuSoundRopeA1, ObtSampFmuSoundRopeB1, ObtSampFmuSoundRopeC1, ObtSampFmuSoundRopeD1,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;
                            case 2:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA2, ObtSampFmuFlowRopeB2, ObtSampFmuFlowRopeC2, ObtSampFmuFlowRopeD2,
                                    ObtSampFmuSoundRopeA2, ObtSampFmuSoundRopeB2, ObtSampFmuSoundRopeC2, ObtSampFmuSoundRopeD2,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;
                            case 3:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA3, ObtSampFmuFlowRopeB3, ObtSampFmuFlowRopeC3, ObtSampFmuFlowRopeD3,
                                    ObtSampFmuSoundRopeA3, ObtSampFmuSoundRopeB3, ObtSampFmuSoundRopeC3, ObtSampFmuSoundRopeD3,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;
                            case 4:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA4, ObtSampFmuFlowRopeB4, ObtSampFmuFlowRopeC4, ObtSampFmuFlowRopeD4,
                                    ObtSampFmuSoundRopeA4, ObtSampFmuSoundRopeB4, ObtSampFmuSoundRopeC4, ObtSampFmuSoundRopeD4,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;
                            case 5:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA5, ObtSampFmuFlowRopeB5, ObtSampFmuFlowRopeC5, ObtSampFmuFlowRopeD5,
                                    ObtSampFmuSoundRopeA5, ObtSampFmuSoundRopeB5, ObtSampFmuSoundRopeC5, ObtSampFmuSoundRopeD5,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;
                            case 6:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA6, ObtSampFmuFlowRopeB6, ObtSampFmuFlowRopeC6, ObtSampFmuFlowRopeD6,
                                    ObtSampFmuSoundRopeA6, ObtSampFmuSoundRopeB6, ObtSampFmuSoundRopeC6, ObtSampFmuSoundRopeD6,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;
                            case 7:
                                    dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA7, ObtSampFmuFlowRopeB7, ObtSampFmuFlowRopeC7, ObtSampFmuFlowRopeD7,
                                    ObtSampFmuSoundRopeA7, ObtSampFmuSoundRopeB7, ObtSampFmuSoundRopeC7, ObtSampFmuSoundRopeD7,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;
                            case 8:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA8, ObtSampFmuFlowRopeB8, ObtSampFmuFlowRopeC8, ObtSampFmuFlowRopeD8,
                                    ObtSampFmuSoundRopeA8, ObtSampFmuSoundRopeB8, ObtSampFmuSoundRopeC8, ObtSampFmuSoundRopeD8,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;
                            case 9:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA9, ObtSampFmuFlowRopeB9, ObtSampFmuFlowRopeC9, ObtSampFmuFlowRopeD9,
                                    ObtSampFmuSoundRopeA9, ObtSampFmuSoundRopeB9, ObtSampFmuSoundRopeC9, ObtSampFmuSoundRopeD9,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;
                            case 10:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampFmuFlowRopeA10, ObtSampFmuFlowRopeB10, ObtSampFmuFlowRopeC10, ObtSampFmuFlowRopeD10,
                                    ObtSampFmuSoundRopeA10, ObtSampFmuSoundRopeB10, ObtSampFmuSoundRopeC10, ObtSampFmuSoundRopeD10,
                                    ObtSampEffFmuRopeA, ObtSampEffFmuRopeB, ObtSampEffFmuRopeC, ObtSampEffFmuRopeD,
                                    ObtSampGainFMURopeAT1, ObtSampGainFMURopeAT2, ObtSampGainFMURopeBT1, ObtSampGainFMURopeBT2,
                                    ObtSampGainFMURopeCT1, ObtSampGainFMURopeCT2, ObtSampGainFMURopeDT1, ObtSampGainFMURopeDT2);
                            break;

                        }
                    break;
                case UltrasonicModel.KrohneAltosonicV12:
                    switch (sampleNumber)
                        {
                            case 1:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA1, ObtSampKrohneAltV12FlowRopeB1, ObtSampKrohneAltV12FlowRopeC1,
                                    ObtSampKrohneAltV12FlowRopeD1, ObtSampKrohneAltV12FlowRopeE1, ObtSampKrohneAltV12FlowRopeF1,
                                    ObtSampKrohneAltV12SoundRopeA1, ObtSampKrohneAltV12SoundRopeB1, ObtSampKrohneAltV12SoundRopeC1,
                                    ObtSampKrohneAltV12SoundRopeD1, ObtSampKrohneAltV12SoundRopeE1, ObtSampKrohneAltV12SoundRopeF1,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;
                            case 2:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA2, ObtSampKrohneAltV12FlowRopeB2, ObtSampKrohneAltV12FlowRopeC2,
                                    ObtSampKrohneAltV12FlowRopeD2, ObtSampKrohneAltV12FlowRopeE2, ObtSampKrohneAltV12FlowRopeF2,
                                    ObtSampKrohneAltV12SoundRopeA2, ObtSampKrohneAltV12SoundRopeB2, ObtSampKrohneAltV12SoundRopeC2,
                                    ObtSampKrohneAltV12SoundRopeD2, ObtSampKrohneAltV12SoundRopeE2, ObtSampKrohneAltV12SoundRopeF2,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;
                            case 3:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA3, ObtSampKrohneAltV12FlowRopeB3, ObtSampKrohneAltV12FlowRopeC3,
                                    ObtSampKrohneAltV12FlowRopeD3, ObtSampKrohneAltV12FlowRopeE3, ObtSampKrohneAltV12FlowRopeF3,
                                    ObtSampKrohneAltV12SoundRopeA3, ObtSampKrohneAltV12SoundRopeB3, ObtSampKrohneAltV12SoundRopeC3,
                                    ObtSampKrohneAltV12SoundRopeD3, ObtSampKrohneAltV12SoundRopeE3, ObtSampKrohneAltV12SoundRopeF3,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;
                            case 4:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA4, ObtSampKrohneAltV12FlowRopeB4, ObtSampKrohneAltV12FlowRopeC4,
                                    ObtSampKrohneAltV12FlowRopeD4, ObtSampKrohneAltV12FlowRopeE4, ObtSampKrohneAltV12FlowRopeF4,
                                    ObtSampKrohneAltV12SoundRopeA4, ObtSampKrohneAltV12SoundRopeB4, ObtSampKrohneAltV12SoundRopeC4,
                                    ObtSampKrohneAltV12SoundRopeD4, ObtSampKrohneAltV12SoundRopeE4, ObtSampKrohneAltV12SoundRopeF4,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;
                            case 5:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA5, ObtSampKrohneAltV12FlowRopeB5, ObtSampKrohneAltV12FlowRopeC5,
                                    ObtSampKrohneAltV12FlowRopeD5, ObtSampKrohneAltV12FlowRopeE5, ObtSampKrohneAltV12FlowRopeF5,
                                    ObtSampKrohneAltV12SoundRopeA5, ObtSampKrohneAltV12SoundRopeB5, ObtSampKrohneAltV12SoundRopeC5,
                                    ObtSampKrohneAltV12SoundRopeD5, ObtSampKrohneAltV12SoundRopeE5, ObtSampKrohneAltV12SoundRopeF5,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;
                            case 6:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA6, ObtSampKrohneAltV12FlowRopeB6, ObtSampKrohneAltV12FlowRopeC6,
                                    ObtSampKrohneAltV12FlowRopeD6, ObtSampKrohneAltV12FlowRopeE6, ObtSampKrohneAltV12FlowRopeF6,
                                    ObtSampKrohneAltV12SoundRopeA6, ObtSampKrohneAltV12SoundRopeB6, ObtSampKrohneAltV12SoundRopeC6,
                                    ObtSampKrohneAltV12SoundRopeD6, ObtSampKrohneAltV12SoundRopeE6, ObtSampKrohneAltV12SoundRopeF6,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;
                            case 7:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA7, ObtSampKrohneAltV12FlowRopeB7, ObtSampKrohneAltV12FlowRopeC7,
                                    ObtSampKrohneAltV12FlowRopeD7, ObtSampKrohneAltV12FlowRopeE7, ObtSampKrohneAltV12FlowRopeF7,
                                    ObtSampKrohneAltV12SoundRopeA7, ObtSampKrohneAltV12SoundRopeB7, ObtSampKrohneAltV12SoundRopeC7,
                                    ObtSampKrohneAltV12SoundRopeD7, ObtSampKrohneAltV12SoundRopeE7, ObtSampKrohneAltV12SoundRopeF7,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;
                            case 8:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA8, ObtSampKrohneAltV12FlowRopeB8, ObtSampKrohneAltV12FlowRopeC8,
                                    ObtSampKrohneAltV12FlowRopeD8, ObtSampKrohneAltV12FlowRopeE8, ObtSampKrohneAltV12FlowRopeF8,
                                    ObtSampKrohneAltV12SoundRopeA8, ObtSampKrohneAltV12SoundRopeB8, ObtSampKrohneAltV12SoundRopeC8,
                                    ObtSampKrohneAltV12SoundRopeD8, ObtSampKrohneAltV12SoundRopeE8, ObtSampKrohneAltV12SoundRopeF8,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;
                            case 9:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA9, ObtSampKrohneAltV12FlowRopeB9, ObtSampKrohneAltV12FlowRopeC9,
                                    ObtSampKrohneAltV12FlowRopeD9, ObtSampKrohneAltV12FlowRopeE9, ObtSampKrohneAltV12FlowRopeF9,
                                    ObtSampKrohneAltV12SoundRopeA9, ObtSampKrohneAltV12SoundRopeB9, ObtSampKrohneAltV12SoundRopeC9,
                                    ObtSampKrohneAltV12SoundRopeD9, ObtSampKrohneAltV12SoundRopeE9, ObtSampKrohneAltV12SoundRopeF9,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;
                            case 10:
                                dryCalibration.UpdateSampleValuesFromSampleLayout(sampleNumber,
                                    ultrasonicModel,
                                    ObtSampKrohneAltV12FlowRopeA10, ObtSampKrohneAltV12FlowRopeB10, ObtSampKrohneAltV12FlowRopeC10,
                                    ObtSampKrohneAltV12FlowRopeD10, ObtSampKrohneAltV12FlowRopeE10, ObtSampKrohneAltV12FlowRopeF10,
                                    ObtSampKrohneAltV12SoundRopeA10, ObtSampKrohneAltV12SoundRopeB10, ObtSampKrohneAltV12SoundRopeC10,
                                    ObtSampKrohneAltV12SoundRopeD10, ObtSampKrohneAltV12SoundRopeE10, ObtSampKrohneAltV12SoundRopeF10,
                                    ObtSampEffKrohneAltV12RopeA, ObtSampEffKrohneAltV12RopeB, ObtSampEffKrohneAltV12RopeC,
                                    ObtSampEffKrohneAltV12RopeD, ObtSampEffKrohneAltV12RopeE, ObtSampEffKrohneAltV12RopeF,
                                    ObtSampGainKrohneAltV12RopeAT1, ObtSampGainKrohneAltV12RopeAT2, ObtSampGainKrohneAltV12RopeBT1, ObtSampGainKrohneAltV12RopeBT2,
                                    ObtSampGainKrohneAltV12RopeCT1, ObtSampGainKrohneAltV12RopeCT2, ObtSampGainKrohneAltV12RopeDT1, ObtSampGainKrohneAltV12RopeDT2,
                                    ObtSampGainKrohneAltV12RopeET1, ObtSampGainKrohneAltV12RopeET2, ObtSampGainKrohneAltV12RopeFT1, ObtSampGainKrohneAltV12RopeFT2);
                            break;

                        }
                    break;
                 default:
                    break;
               
            }

        }

        private void DryCalibration_ObtainingSampleFinished()
        { 
            dryCalibration.ProcessingDryCalibration = false;
            dryCalibration.SetControlVisibility(btnObtSampNext, Visibility.Visible);
            dryCalibration.TimerControl.Enabled = false;
        }

        private void DryCalibration_ValidationFinished(ValidatedResult validatedResult, UltrasonicModel ultrasonicModel)
        {
            switch (ultrasonicModel)
            {
                case UltrasonicModel.Daniel:
                    dryCalibration.UpdateValidatedSampleLayout(validatedResult, ultrasonicModel, ValidSampAvgTemp, ValidSampRtdDifference, ValidSampAvgPress, ValidSampTheorSoundSpeed,
                                                               ValidSampAvgDanielFlowRopeA, ValidSampAvgDanielFlowRopeB, ValidSampAvgDanielFlowRopeC, ValidSampAvgDanielFlowRopeD,
                                                               ValidSampDanielSoundRopeA, ValidSampDanielSoundRopeB, ValidSampDanielSoundRopeC, ValidSampDanielSoundRopeD,
                                                               ValidSampDanielErrRopeA, ValidSampDanielErrRopeB, ValidSampDanielErrRopeC, ValidSampDanielErrRopeD,
                                                               ValidSampDiffMax, ValidSampDiffMin, ValidSampDiff,
                                                               ValidSampEffDanielRopeA, ValidSampEffDanielRopeB, ValidSampEffDanielRopeC, ValidSampEffDanielRopeD,
                                                               ValidSampGainDanielRopeA, ValidSampGainDanielRopeB, ValidSampGainDanielRopeC, ValidSampGainDanielRopeD
                                                               );

                    break;
                case UltrasonicModel.DanielJunior1R:
                    dryCalibration.UpdateValidatedSampleLayout(validatedResult, ultrasonicModel, ValidSampAvgTemp, ValidSampRtdDifference, ValidSampAvgPress, ValidSampTheorSoundSpeed,
                                                               ValidSampAvgDaniel1RFlowRopeA, 
                                                               ValidSampDaniel1RSoundRopeA,
                                                               ValidSampDaniel1RErrRopeA,
                                                               ValidSampDiffMax, ValidSampDiffMin, ValidSampDiff,
                                                               ValidSampEffDaniel1RRopeA,
                                                               ValidSampGainDaniel1RRopeA
                                                               );

                    break;
                case UltrasonicModel.DanielJunior2R:
                    dryCalibration.UpdateValidatedSampleLayout(validatedResult, ultrasonicModel, ValidSampAvgTemp, ValidSampRtdDifference, ValidSampAvgPress, ValidSampTheorSoundSpeed,
                                                               ValidSampAvgDaniel2RFlowRopeA, ValidSampAvgDaniel2RFlowRopeB,
                                                               ValidSampDaniel2RSoundRopeA, ValidSampDaniel2RSoundRopeB,
                                                               ValidSampDaniel2RErrRopeA, ValidSampDaniel2RErrRopeB,
                                                               ValidSampDiffMax, ValidSampDiffMin, ValidSampDiff,
                                                               ValidSampEffDaniel2RRopeA, ValidSampEffDaniel2RRopeB,
                                                               ValidSampGainDaniel2RRopeA, ValidSampGainDaniel2RRopeB
                                                               );

                    break;
                case UltrasonicModel.InstrometS5:
                    dryCalibration.UpdateValidatedSampleLayout(validatedResult, ultrasonicModel, ValidSampAvgTemp, ValidSampRtdDifference, ValidSampAvgPress, ValidSampTheorSoundSpeed,
                                                              ValidSampAvgInstS5FlowRopeA, ValidSampAvgInstS5FlowRopeB, ValidSampAvgInstS5FlowRopeC, ValidSampAvgInstS5FlowRopeD, ValidSampAvgInstS5FlowRopeE, //ValidSampAvgInstS5FlowRopeF
                                                              ValidSampInstS5SoundRopeA, ValidSampInstS5SoundRopeB, ValidSampInstS5SoundRopeC, ValidSampInstS5SoundRopeD, ValidSampInstS5SoundRopeE, //ValidSampInstS5SoundRopeF,
                                                              ValidSampInstS5ErrRopeA, ValidSampInstS5ErrRopeB, ValidSampInstS5ErrRopeC, ValidSampInstS5ErrRopeD, ValidSampInstS5ErrRopeE, //ValidSampInstS5ErrRopeF,
                                                              ValidSampDiffMax, ValidSampDiffMin, ValidSampDiff,
                                                              ValidSampEffInsS5RopeA, ValidSampEffInsS5RopeB, ValidSampEffInsS5RopeC, ValidSampEffInsS5RopeD, ValidSampEffInsS5RopeE, //, ValidSampEffInsS5RopeF
                                                              ValidSampGainIntS5RopeA, ValidSampGainIntS5RopeB,ValidSampGainIntS5RopeC, ValidSampGainIntS5RopeD, ValidSampGainIntS5RopeE); //, ValidSampGainIntS5RopeF);
                    break;
                case UltrasonicModel.InstrometS6:
                    dryCalibration.UpdateValidatedSampleLayout(validatedResult, ultrasonicModel, ValidSampAvgTemp, ValidSampRtdDifference, ValidSampAvgPress, ValidSampTheorSoundSpeed,
                                                              ValidSampAvgInstS6FlowRopeA, ValidSampAvgInstS6FlowRopeB, ValidSampAvgInstS6FlowRopeC, ValidSampAvgInstS6FlowRopeD, 
                                                              ValidSampAvgInstS6FlowRopeE, ValidSampAvgInstS6FlowRopeF, ValidSampAvgInstS6FlowRopeG, ValidSampAvgInstS6FlowRopeH,
                                                              ValidSampInstS6SoundRopeA, ValidSampInstS6SoundRopeB, ValidSampInstS6SoundRopeC, ValidSampInstS6SoundRopeD,
                                                              ValidSampInstS6SoundRopeE, ValidSampInstS6SoundRopeF, ValidSampInstS6SoundRopeG, ValidSampInstS6SoundRopeH,
                                                              ValidSampInstS6ErrRopeA, ValidSampInstS6ErrRopeB, ValidSampInstS6ErrRopeC, ValidSampInstS6ErrRopeD,
                                                              ValidSampInstS6ErrRopeE, ValidSampInstS6ErrRopeF, ValidSampInstS6ErrRopeG, ValidSampInstS6ErrRopeH,
                                                              ValidSampDiffMax, ValidSampDiffMin, ValidSampDiff,
                                                              ValidSampEffInsS6RopeA, ValidSampEffInsS6RopeB, ValidSampEffInsS6RopeC, ValidSampEffInsS6RopeD,
                                                              ValidSampEffInsS6RopeE, ValidSampEffInsS6RopeF, ValidSampEffInsS6RopeG, ValidSampEffInsS6RopeH,
                                                              ValidSampGainIntS6RopeA, ValidSampGainIntS6RopeB, ValidSampGainIntS6RopeC, ValidSampGainIntS6RopeD, 
                                                              ValidSampGainIntS6RopeE, ValidSampGainIntS6RopeF, ValidSampGainIntS6RopeG, ValidSampGainIntS6RopeH);
                    break;
                case UltrasonicModel.Sick:
                    dryCalibration.UpdateValidatedSampleLayout(validatedResult, ultrasonicModel, ValidSampAvgTemp, ValidSampRtdDifference, ValidSampAvgPress, ValidSampTheorSoundSpeed,
                                                               ValidSampAvgSickFlowRopeA, ValidSampAvgSickFlowRopeB, ValidSampAvgSickFlowRopeC, ValidSampAvgSickFlowRopeD,
                                                               ValidSampSickSoundRopeA, ValidSampSickSoundRopeB, ValidSampSickSoundRopeC, ValidSampSickSoundRopeD,
                                                               ValidSampSickErrRopeA, ValidSampSickErrRopeB, ValidSampSickErrRopeC, ValidSampSickErrRopeD,
                                                               ValidSampDiffMax, ValidSampDiffMin, ValidSampDiff,
                                                               ValidSampEffSickRopeA, ValidSampEffSickRopeB, ValidSampEffSickRopeC, ValidSampEffSickRopeD,
                                                               ValidSampGainSickRopeA, ValidSampGainSickRopeB, ValidSampGainSickRopeC, ValidSampGainSickRopeD);
                    break;
                case UltrasonicModel.FMU:
                    dryCalibration.UpdateValidatedSampleLayout(validatedResult, ultrasonicModel, ValidSampAvgTemp, ValidSampRtdDifference, ValidSampAvgPress, ValidSampTheorSoundSpeed,
                                                               ValidSampAvgFmuFlowRopeA, ValidSampAvgFmuFlowRopeB, ValidSampAvgFmuFlowRopeC, ValidSampAvgFmuFlowRopeD,
                                                               ValidSampFmuSoundRopeA, ValidSampFmuSoundRopeB, ValidSampFmuSoundRopeC, ValidSampFmuSoundRopeD,
                                                               ValidSampFmuErrRopeA, ValidSampFmuErrRopeB, ValidSampFmuErrRopeC, ValidSampFmuErrRopeD,
                                                               ValidSampDiffMax, ValidSampDiffMin, ValidSampDiff,
                                                               ValidSampEffFmuRopeA, ValidSampEffFmuRopeB, ValidSampEffFmuRopeC, ValidSampEffFmuRopeD,
                                                               ValidSampGainFMURopeA, ValidSampGainFMURopeB, ValidSampGainFMURopeC, ValidSampGainFMURopeD);
                    break;
                case UltrasonicModel.KrohneAltosonicV12:
                    dryCalibration.UpdateValidatedSampleLayout(validatedResult, ultrasonicModel, ValidSampAvgTemp, ValidSampRtdDifference, ValidSampAvgPress, ValidSampTheorSoundSpeed,
                                                               ValidSampAvgKrohneAltV12FlowRopeA, ValidSampAvgKrohneAltV12FlowRopeB, ValidSampAvgKrohneAltV12FlowRopeC, 
                                                               ValidSampAvgKrohneAltV12FlowRopeD, ValidSampAvgKrohneAltV12FlowRopeE, ValidSampAvgKrohneAltV12FlowRopeF,
                                                               ValidSampKrohneAltV12SoundRopeA, ValidSampKrohneAltV12SoundRopeB, ValidSampKrohneAltV12SoundRopeC,
                                                               ValidSampKrohneAltV12SoundRopeD, ValidSampKrohneAltV12SoundRopeE, ValidSampKrohneAltV12SoundRopeF,                                                       
                                                               ValidSampKrohneAltV12ErrRopeA, ValidSampKrohneAltV12ErrRopeB, ValidSampKrohneAltV12ErrRopeC,
                                                               ValidSampKrohneAltV12ErrRopeD, ValidSampKrohneAltV12ErrRopeE, ValidSampKrohneAltV12ErrRopeF, 
                                                               ValidSampDiffMax, ValidSampDiffMin, ValidSampDiff, 
                                                               ValidSampEffKrohneAltV12RopeA, ValidSampEffKrohneAltV12RopeB, ValidSampEffKrohneAltV12RopeC,
                                                               ValidSampEffKrohneAltV12RopeD, ValidSampEffKrohneAltV12RopeE, ValidSampEffKrohneAltV12RopeF,                                                              
                                                               ValidSampGainKrohneAltV12RopeA, ValidSampGainKrohneAltV12RopeB, ValidSampGainKrohneAltV12RopeC,
                                                               ValidSampGainKrohneAltV12RopeD, ValidSampGainKrohneAltV12RopeE, ValidSampGainKrohneAltV12RopeF
                                                               );
                    break;
                default:
                    break;  
            }

            SetControlEnabled(mnuCancel, true);
            SetControlEnabled(mnuConfiguration, false);

            dryCalibration.SetControlVisibility(btnValidStateNext, Visibility.Visible);
        }

        private void InitDryCalibration()
        {
            // inicializar ensayo
            dryCalibration.Initialize();
        }

        private void CancelDryCalibration()
        {
            GridConfigurationUser.Visibility = Visibility.Hidden;
            GrigConfigurationDevice.Visibility = Visibility.Hidden;

            ObtainingSampleState.Visibility = Visibility.Hidden;
            ValidatingState.Visibility = Visibility.Hidden;
            GridDryCalibration.Visibility = Visibility.Visible;
            StabilizingState.Visibility = Visibility.Visible;
            
            mnuConfiguration.IsEnabled = true;
            mnuInit.IsEnabled = true;
            mnuCancel.IsEnabled = false;
            btnUserConfig.IsEnabled = true;


            dryCalibration.StopTimerControl();
            dryCalibration.CancelDryCalibration();

            CleanAllStates();

            SetTextBlock(Status, "Listo.");
        }

        private void DryCalibration_UpdateSensorReceived(MonitorType type, object value)
        {
            //log.Log.WriteIfExists("DryCalibration_UpdateSensorReceived... Value: " + value.ToString() + ", Type: " + type.ToString());

            switch (type)
            {
                case MonitorType.RTD:

                    if (dryCalibration.ProcessingStartUp)
                    {
                        dryCalibration.UpdateRTDLayout(stUpRTD1, stUpRTD2, stUpRTD3, stUpRTD4,
                            stUpRTD5, stUpRTD6, stUpRTD7, stUpRTD8, stUpRTD9, stUpRTD10, stUpRTD11,
                            stUpRTD12, stUpRTDAVG, stUpRTDDifference);
                        break;
                    }

                    if (currenState == FSMState.INITIALIZED)
                    {
                        dryCalibration.UpdateRTDLayout(stateStabRtd1, stateStabRtd2, stateStabRtd3, stateStabRtd4,
                          stateStabRtd5, stateStabRtd6, stateStabRtd7, stateStabRtd8, stateStabRtd9, stateStabRtd10, stateStabRtd11,
                          stateStabRtd12, stateStabRtdAVG, stateStabRtdDifference);

                        break;
                    }

                    if (dryCalibration.ProcessingDryCalibration)
                    {
                        if (currenState == FSMState.STABILIZING)
                        {
                            dryCalibration.UpdateRTDLayout(stateStabRtd1, stateStabRtd2, stateStabRtd3, stateStabRtd4,
                           stateStabRtd5, stateStabRtd6, stateStabRtd7, stateStabRtd8, stateStabRtd9, stateStabRtd10, stateStabRtd11,
                           stateStabRtd12, stateStabRtdAVG, stateStabRtdDifference);
                        }

                        if (currenState == FSMState.OBTAINING_SAMPLES)
                        {
                            dryCalibration.UpdateObtainedSampleRTDDiffLayout(ObtSampRtdAVG, ObtSampRtdDifference);
                        }
                    }

                    break;
                case MonitorType.Pressure:
                    if (dryCalibration.ProcessingStartUp)
                    {
                        dryCalibration.UpdatePressureLayout(stUpPress1);
                    }

                    if (dryCalibration.ProcessingDaqModuleAdjustament)
                    {

                        dryCalibration.UpdatePressureLayout(adjModPress1);
                    }

                    break;
                case MonitorType.Ultrasonic:
                    UpdateUltrasonicLayout();
                    break;
                default:
                    break;
            }          
        }
      
        private void DryCalibration_StatisticReceived(MonitorType type, StatisticValue value)
        {
            // Estadísticas

            switch (type)
            {
                case MonitorType.RTD:
                    break;
                case MonitorType.Pressure:
                    break;
                case MonitorType.Ultrasonic:
                    SetText(stUpStatTotOk, value.TotalRequests);
                    SetText(stUpStatTotErr, value.TotalWrongRequests);
                    SetText(stUpStatTotTimeOut, value.TotalTimeoutWrongRequests);
                    SetText(stUpStatTotCheckSum, value.TotalChecksumWrongRequests);
                    break;
                default:
                    break;
            }

        }

        private void DryCalibration_ElapsedTimeControl(string timeElapsed)
        {
            SetTextBlock(TimeElapsed, timeElapsed);
            TimeElapsed.Refresh();
        }

        private void StateStabRTDActiveCheck_Change(object sender, RoutedEventArgs e)
        {
            if (dryCalibration != null)
            {
                if (!saveRTDAdjustament)
                {
                    CheckBox item = sender as CheckBox;
                    int number = Convert.ToInt32(item.Uid);
                    bool active = (bool)item.IsChecked;
                    dryCalibration.StartUpRTDActiveStateChange(number, active);
                }           
            }
        }

        private void ChangeTabConfiguration(int index)
        {

            if (!Dispatcher.CheckAccess())// al generar el reporte se llama desde otro thread
            {
                Dispatcher.Invoke(new Action<int>(ChangeTabConfiguration), index);
            }
            else
            {
                GridCursor.Margin = new Thickness(10 + (200 * index), 0, 0, 0);
                SetTextBlock(Status, "");

                // detener procesos abiertos
                dryCalibration.StopMonitorContollers();

                switch (index)
                {
                    case 0: // Configuración General
                        SetTextBlock(Status, "Ingrese los datos requeridos para continuar con el ensayo.");
                        ConfigGeneral.Visibility = Visibility.Visible;
                        DAQAdjustment.Visibility = Visibility.Hidden;
                        CommUltrasonic.Visibility = Visibility.Hidden;
                        StartUp.Visibility = Visibility.Hidden;
                        break;
                    case 1: // Ajuste NI cDAQ-9184
                        CleanAdjustment();
                        ConfigGeneral.Visibility = Visibility.Hidden;
                        DAQAdjustment.Visibility = Visibility.Visible;
                        CommUltrasonic.Visibility = Visibility.Hidden;
                        StartUp.Visibility = Visibility.Hidden;

                        // iniciar modulo de ajuste
                        InitNIcDaqModuleAdjustment();
                        break;
                    case 2: // Comunicación Ultrasónico
                        ConfigGeneral.Visibility = Visibility.Hidden;
                        DAQAdjustment.Visibility = Visibility.Hidden;
                        CommUltrasonic.Visibility = Visibility.Visible;
                        StartUp.Visibility = Visibility.Hidden;
                        InitUltrasonicCommunication();
                        break;

                    case 3:// Puesta en Marcha
                        ConfigGeneral.Visibility = Visibility.Hidden;
                        DAQAdjustment.Visibility = Visibility.Hidden;
                        CommUltrasonic.Visibility = Visibility.Hidden;
                        StartUp.Visibility = Visibility.Visible;

                        // iniciar módulo de puesta en marcha
                        UpdateStarUpUltrasonicModel();
                        break;

                }
            }       
        }

        private void CleanAdjustment()
        {
            // temperatura
            SetText(adjModRefTemp, 0d);

            // presión
            SetText(adjModPress1, 0d);

            //// presión atmosférica
            //SetText(adjModPressAtmospheric, 0d);

            // referecia
            SetText(adjModReferencePress, 0d);
        }

        private void CleanStartUp()
        {
            // temperatura

            // muestras 
            SetText(stUpRTD1, 0d);
            SetText(stUpRTD2, 0d);
            SetText(stUpRTD3, 0d);
            SetText(stUpRTD4, 0d);
            SetText(stUpRTD5, 0d);
            SetText(stUpRTD6, 0d);
            SetText(stUpRTD7, 0d);
            SetText(stUpRTD8, 0d);
            SetText(stUpRTD9, 0d);
            SetText(stUpRTD10, 0d);
            SetText(stUpRTD11, 0d);
            SetText(stUpRTD12, 0d);

            // promedio/diferencia
            SetText(stUpRTDAVG, 0d);
            SetText(stUpRTDDifference, 0d);

            // presión
            SetText(stUpPress1, 0d);

            // ultrasónico

            // estadísticas
            SetText(stUpStatTotOk, 0);
            SetText(stUpStatTotErr, 0);
            SetText(stUpStatTotCheckSum, 0);
            SetText(stUpStatTotTimeOut, 0);
            
            // cuerdas


        }

        private void UpdateUltrasonicLayout()
        {
            // Ultrasonic monitor
            ModbusConfiguration config = dryCalibration.CurrentModbusConfiguration;
            UltrasonicModel ultrasonicModel = (UltrasonicModel)config.SlaveConfig.Model;

            RopeValue ropeA = dryCalibration.UltrasonicVal.Ropes.Find(r => r.Name == "A");
            RopeValue ropeB = dryCalibration.UltrasonicVal.Ropes.Find(r => r.Name == "B");
            RopeValue ropeC = dryCalibration.UltrasonicVal.Ropes.Find(r => r.Name == "C");
            RopeValue ropeD = dryCalibration.UltrasonicVal.Ropes.Find(r => r.Name == "D");
            RopeValue ropeE = dryCalibration.UltrasonicVal.Ropes.Find(r => r.Name == "E");
            RopeValue ropeF = dryCalibration.UltrasonicVal.Ropes.Find(r => r.Name == "F");
            RopeValue ropeG = dryCalibration.UltrasonicVal.Ropes.Find(r => r.Name == "G");
            RopeValue ropeH = dryCalibration.UltrasonicVal.Ropes.Find(r => r.Name == "H");

            if (dryCalibration.ProcessingStartUp) // puesta en marcha
            {
                switch (ultrasonicModel)
                {
                    case UltrasonicModel.Daniel: // Daniel
                        SetText(stUpUltDanFlowA, ropeA.FlowSpeedValue);
                        SetText(stUpUltDanFlowB, ropeB.FlowSpeedValue);
                        SetText(stUpUltDanFlowC, ropeC.FlowSpeedValue);
                        SetText(stUpUltDanFlowD, ropeD.FlowSpeedValue);

                        SetText(stUpUltDanSoundA, ropeA.SoundSpeedValue);
                        SetText(stUpUltDanSoundB, ropeB.SoundSpeedValue);
                        SetText(stUpUltDanSoundC, ropeC.SoundSpeedValue);
                        SetText(stUpUltDanSoundD, ropeD.SoundSpeedValue);

                        SetText(stUpUltDanEfiA, ropeA.EfficiencyValue);
                        SetText(stUpUltDanEfiB, ropeB.EfficiencyValue);
                        SetText(stUpUltDanEfiC, ropeC.EfficiencyValue);
                        SetText(stUpUltDanEfiD, ropeD.EfficiencyValue);

                        SetText(stUpUltDanGain1A, ropeA.GainValues.T1, 1);
                        SetText(stUpUltDanGain2A, ropeA.GainValues.T2, 1);
                        SetText(stUpUltDanGain1B, ropeB.GainValues.T1, 1);
                        SetText(stUpUltDanGain2B, ropeB.GainValues.T2, 1);
                        SetText(stUpUltDanGain1C, ropeC.GainValues.T1, 1);
                        SetText(stUpUltDanGain2C, ropeC.GainValues.T2, 1);
                        SetText(stUpUltDanGain1D, ropeD.GainValues.T1, 1);
                        SetText(stUpUltDanGain2D, ropeD.GainValues.T2, 1);

                        break;
                    case UltrasonicModel.DanielJunior1R: // Daniel Junior Una Cuerda
                        SetText(stUpUltDan1RFlowA, ropeA.FlowSpeedValue);  
                        SetText(stUpUltDan1RSoundA, ropeA.SoundSpeedValue);
                        SetText(stUpUltDan1REfiA, ropeA.EfficiencyValue);
                        SetText(stUpUltDan1RGain1A, ropeA.GainValues.T1, 1);
                        SetText(stUpUltDan1RGain2A, ropeA.GainValues.T2, 1);
                       
                        break;
                    case UltrasonicModel.DanielJunior2R: // Daniel Junior Dos Cuerdas
                        SetText(stUpUltDan2RFlowA, ropeA.FlowSpeedValue);
                        SetText(stUpUltDan2RFlowB, ropeB.FlowSpeedValue);
                     
                        SetText(stUpUltDan2RSoundA, ropeA.SoundSpeedValue);
                        SetText(stUpUltDan2RSoundB, ropeB.SoundSpeedValue);

                        SetText(stUpUltDan2REfiA, ropeA.EfficiencyValue);
                        SetText(stUpUltDan2REfiB, ropeB.EfficiencyValue);

                        SetText(stUpUltDan2RGain1A, ropeA.GainValues.T1, 1);
                        SetText(stUpUltDan2RGain2A, ropeA.GainValues.T2, 1);
                        SetText(stUpUltDan2RGain1B, ropeB.GainValues.T1, 1);
                        SetText(stUpUltDan2RGain2B, ropeB.GainValues.T2, 1);

                        break;
                    case UltrasonicModel.InstrometS5: // Instromet Series 3 al 5
                        SetText(stUpUltInst5FlowA, ropeA.FlowSpeedValue);
                        SetText(stUpUltInst5FlowB, ropeB.FlowSpeedValue);
                        SetText(stUpUltInst5FlowC, ropeC.FlowSpeedValue);
                        SetText(stUpUltInst5FlowD, ropeD.FlowSpeedValue);
                        SetText(stUpUltInst5FlowE, ropeE.FlowSpeedValue);
                        //SetText(stUpUltInst5FlowF, ropeF.FlowSpeedValue);

                        SetText(stUpUltInst5SoundA, ropeA.SoundSpeedValue);
                        SetText(stUpUltInst5SoundB, ropeB.SoundSpeedValue);
                        SetText(stUpUltInst5SoundC, ropeC.SoundSpeedValue);
                        SetText(stUpUltInst5SoundD, ropeD.SoundSpeedValue);
                        SetText(stUpUltInst5SoundE, ropeE.SoundSpeedValue);
                        //SetText(stUpUltInst5SoundF, ropeF.SoundSpeedValue);

                        SetText(stUpUltInst5EfiA, ropeA.EfficiencyValue);
                        SetText(stUpUltInst5EfiB, ropeB.EfficiencyValue);
                        SetText(stUpUltInst5EfiC, ropeC.EfficiencyValue);
                        SetText(stUpUltInst5EfiD, ropeD.EfficiencyValue);
                        SetText(stUpUltInst5EfiE, ropeE.EfficiencyValue);
                        //SetText(stUpUltInst5EfiF, ropeF.EfficiencyValue);

                        SetText(stUpUltInst5Gain1A, ropeA.GainValues.T1,1);
                        SetText(stUpUltInst5Gain2A, ropeA.GainValues.T2,1);
                        SetText(stUpUltInst5Gain1B, ropeB.GainValues.T1,1);
                        SetText(stUpUltInst5Gain2B, ropeB.GainValues.T2,1);
                        SetText(stUpUltInst5Gain1C, ropeC.GainValues.T1,1);
                        SetText(stUpUltInst5Gain2C, ropeC.GainValues.T2,1);
                        SetText(stUpUltInst5Gain1D, ropeD.GainValues.T1,1);
                        SetText(stUpUltInst5Gain2D, ropeD.GainValues.T2,1);
                        SetText(stUpUltInst5Gain1E, ropeE.GainValues.T1,1);
                        SetText(stUpUltInst5Gain2E, ropeE.GainValues.T2,1);
                        //SetText(stUpUltInst5Gain1F, ropeF.GainValue.T1,1);
                        //SetText(stUpUltInst5Gain2F, ropeF.GainValue.T2,1);


                        break;
                    case UltrasonicModel.InstrometS6: // Instromet Series 6
                        SetText(stUpUltInst6FlowA, ropeA.FlowSpeedValue);
                        SetText(stUpUltInst6FlowB, ropeB.FlowSpeedValue);
                        SetText(stUpUltInst6FlowC, ropeC.FlowSpeedValue);
                        SetText(stUpUltInst6FlowD, ropeD.FlowSpeedValue);
                        SetText(stUpUltInst6FlowE, ropeE.FlowSpeedValue);
                        SetText(stUpUltInst6FlowF, ropeF.FlowSpeedValue);
                        SetText(stUpUltInst6FlowG, ropeG.FlowSpeedValue);
                        SetText(stUpUltInst6FlowH, ropeH.FlowSpeedValue);

                        SetText(stUpUltInst6SoundA, ropeA.SoundSpeedValue);
                        SetText(stUpUltInst6SoundB, ropeB.SoundSpeedValue);
                        SetText(stUpUltInst6SoundC, ropeC.SoundSpeedValue);
                        SetText(stUpUltInst6SoundD, ropeD.SoundSpeedValue);
                        SetText(stUpUltInst6SoundE, ropeE.SoundSpeedValue);
                        SetText(stUpUltInst6SoundF, ropeF.SoundSpeedValue);
                        SetText(stUpUltInst6SoundG, ropeG.SoundSpeedValue);
                        SetText(stUpUltInst6SoundH, ropeH.SoundSpeedValue);

                        SetText(stUpUltInst6EfiA, ropeA.EfficiencyValue);
                        SetText(stUpUltInst6EfiB, ropeB.EfficiencyValue);
                        SetText(stUpUltInst6EfiC, ropeC.EfficiencyValue);
                        SetText(stUpUltInst6EfiD, ropeD.EfficiencyValue);
                        SetText(stUpUltInst6EfiE, ropeE.EfficiencyValue);
                        SetText(stUpUltInst6EfiF, ropeF.EfficiencyValue);
                        SetText(stUpUltInst6EfiG, ropeG.EfficiencyValue);
                        SetText(stUpUltInst6EfiH, ropeH.EfficiencyValue);

                        SetText(stUpUltInst6Gain1A, ropeA.GainValues.T1,1);
                        SetText(stUpUltInst6Gain2A, ropeA.GainValues.T2,1);
                        SetText(stUpUltInst6Gain1B, ropeB.GainValues.T1,1);
                        SetText(stUpUltInst6Gain2B, ropeB.GainValues.T2,1);
                        SetText(stUpUltInst6Gain1C, ropeC.GainValues.T1,1);
                        SetText(stUpUltInst6Gain2C, ropeC.GainValues.T2,1);
                        SetText(stUpUltInst6Gain1D, ropeD.GainValues.T1,1);
                        SetText(stUpUltInst6Gain2D, ropeD.GainValues.T2,1);
                        SetText(stUpUltInst6Gain1E, ropeE.GainValues.T1,1);
                        SetText(stUpUltInst6Gain2E, ropeE.GainValues.T2,1);
                        SetText(stUpUltInst6Gain1F, ropeF.GainValues.T1,1);
                        SetText(stUpUltInst6Gain2F, ropeF.GainValues.T2,1);
                        SetText(stUpUltInst6Gain1G, ropeG.GainValues.T1,1);
                        SetText(stUpUltInst6Gain2G, ropeG.GainValues.T2,1);
                        SetText(stUpUltInst6Gain1H, ropeH.GainValues.T1,1);
                        SetText(stUpUltInst6Gain2H, ropeH.GainValues.T2,1);

                        break;
                    case UltrasonicModel.Sick: // Sick
                        SetText(stUpUltSickFlowA, ropeA.FlowSpeedValue);
                        SetText(stUpUltSickFlowB, ropeB.FlowSpeedValue);
                        SetText(stUpUltSickFlowC, ropeC.FlowSpeedValue);
                        SetText(stUpUltSickFlowD, ropeD.FlowSpeedValue);

                        SetText(stUpUltSickSoundA, ropeA.SoundSpeedValue);
                        SetText(stUpUltSickSoundB, ropeB.SoundSpeedValue);
                        SetText(stUpUltSickSoundC, ropeC.SoundSpeedValue);
                        SetText(stUpUltSickSoundD, ropeD.SoundSpeedValue);

                        SetText(stUpUltSickEfiA, ropeA.EfficiencyValue);
                        SetText(stUpUltSickEfiB, ropeB.EfficiencyValue);
                        SetText(stUpUltSickEfiC, ropeC.EfficiencyValue);
                        SetText(stUpUltSickEfiD, ropeD.EfficiencyValue);

                        SetText(stUpUltSickGain1A, ropeA.GainValues.T1,1);
                        SetText(stUpUltSickGain2A, ropeA.GainValues.T2,1);
                        SetText(stUpUltSickGain1B, ropeB.GainValues.T1,1);
                        SetText(stUpUltSickGain2B, ropeB.GainValues.T2,1);
                        SetText(stUpUltSickGain1C, ropeC.GainValues.T1,1);
                        SetText(stUpUltSickGain2C, ropeC.GainValues.T2,1);
                        SetText(stUpUltSickGain1D, ropeD.GainValues.T1,1);
                        SetText(stUpUltSickGain2D, ropeD.GainValues.T2,1);

                        break;
                    case UltrasonicModel.FMU: // Fmu
                        SetText(stUpUltFmuFlowA, ropeA.FlowSpeedValue);
                        SetText(stUpUltFmuFlowB, ropeB.FlowSpeedValue);
                        SetText(stUpUltFmuFlowC, ropeC.FlowSpeedValue);
                        SetText(stUpUltFmuFlowD, ropeD.FlowSpeedValue);

                        SetText(stUpUltFmuSoundA, ropeA.SoundSpeedValue);
                        SetText(stUpUltFmuSoundB, ropeB.SoundSpeedValue);
                        SetText(stUpUltFmuSoundC, ropeC.SoundSpeedValue);
                        SetText(stUpUltFmuSoundD, ropeD.SoundSpeedValue);

                        SetText(stUpUltFmuEfiA, ropeA.EfficiencyValue);
                        SetText(stUpUltFmuEfiB, ropeB.EfficiencyValue);
                        SetText(stUpUltFmuEfiC, ropeC.EfficiencyValue);
                        SetText(stUpUltFmuEfiD, ropeD.EfficiencyValue);

                        SetText(stUpUltFmuGain1A, ropeA.GainValues.T1,1);
                        SetText(stUpUltFmuGain2A, ropeA.GainValues.T2,1);
                        SetText(stUpUltFmuGain1B, ropeB.GainValues.T1,1);
                        SetText(stUpUltFmuGain2B, ropeB.GainValues.T2,1);
                        SetText(stUpUltFmuGain1C, ropeC.GainValues.T1,1);
                        SetText(stUpUltFmuGain2C, ropeC.GainValues.T2,1);
                        SetText(stUpUltFmuGain1D, ropeD.GainValues.T1,1);
                        SetText(stUpUltFmuGain2D, ropeD.GainValues.T2,1);

                        break;
                    case UltrasonicModel.KrohneAltosonicV12: // Krohne Altosonic V12
                        SetText(stUpUltKrohneAltV12FlowA, ropeA.FlowSpeedValue);
                        SetText(stUpUltKrohneAltV12FlowB, ropeB.FlowSpeedValue);
                        SetText(stUpUltKrohneAltV12FlowC, ropeC.FlowSpeedValue);
                        SetText(stUpUltKrohneAltV12FlowD, ropeD.FlowSpeedValue);
                        SetText(stUpUltKrohneAltV12FlowE, ropeE.FlowSpeedValue);
                        SetText(stUpUltKrohneAltV12FlowF, ropeF.FlowSpeedValue);
                       
                        SetText(stUpUltKrohneAltV12SoundA, ropeA.SoundSpeedValue);
                        SetText(stUpUltKrohneAltV12SoundB, ropeB.SoundSpeedValue);
                        SetText(stUpUltKrohneAltV12SoundC, ropeC.SoundSpeedValue);
                        SetText(stUpUltKrohneAltV12SoundD, ropeD.SoundSpeedValue);
                        SetText(stUpUltKrohneAltV12SoundE, ropeE.SoundSpeedValue);
                        SetText(stUpUltKrohneAltV12SoundF, ropeF.SoundSpeedValue);
                       
                        SetText(stUpUltKrohneAltV12EfiA, ropeA.EfficiencyValue);
                        SetText(stUpUltKrohneAltV12EfiB, ropeB.EfficiencyValue);
                        SetText(stUpUltKrohneAltV12EfiC, ropeC.EfficiencyValue);
                        SetText(stUpUltKrohneAltV12EfiD, ropeD.EfficiencyValue);
                        SetText(stUpUltKrohneAltV12EfiE, ropeE.EfficiencyValue);
                        SetText(stUpUltKrohneAltV12EfiF, ropeF.EfficiencyValue);

                        SetText(stUpUltKrohneAltV12Gain1A, ropeA.GainValues.T1,1);
                        SetText(stUpUltKrohneAltV12Gain2A, ropeA.GainValues.T2,1);
                        SetText(stUpUltKrohneAltV12Gain1B, ropeB.GainValues.T1,1);
                        SetText(stUpUltKrohneAltV12Gain2B, ropeB.GainValues.T2,1);
                        SetText(stUpUltKrohneAltV12Gain1C, ropeC.GainValues.T1,1);
                        SetText(stUpUltKrohneAltV12Gain2C, ropeC.GainValues.T2,1);
                        SetText(stUpUltKrohneAltV12Gain1D, ropeD.GainValues.T1,1);
                        SetText(stUpUltKrohneAltV12Gain2D, ropeD.GainValues.T2,1);
                        SetText(stUpUltKrohneAltV12Gain1E, ropeE.GainValues.T1,1);
                        SetText(stUpUltKrohneAltV12Gain2E, ropeE.GainValues.T2,1);
                        SetText(stUpUltKrohneAltV12Gain1F, ropeF.GainValues.T1,1);
                        SetText(stUpUltKrohneAltV12Gain2F, ropeF.GainValues.T2,1);

                        break;
                }
            }

            if (dryCalibration.ProcessingDryCalibration)
            {
                if (currenState == FSMState.OBTAINING_SAMPLES)
                {
                    switch (ultrasonicModel)
                    {
                        case UltrasonicModel.Daniel:
                            SetText(ObtSampEffDanielRopeA, ropeA.EfficiencyValue);
                            SetText(ObtSampEffDanielRopeB, ropeB.EfficiencyValue);
                            SetText(ObtSampEffDanielRopeC, ropeC.EfficiencyValue);
                            SetText(ObtSampEffDanielRopeD, ropeD.EfficiencyValue);

                            SetText(ObtSampGainDanielRopeAT1, ropeA.GainValues.T1,1);
                            SetText(ObtSampGainDanielRopeAT2, ropeA.GainValues.T2,1);
                            SetText(ObtSampGainDanielRopeBT1, ropeB.GainValues.T1,1);
                            SetText(ObtSampGainDanielRopeBT2, ropeB.GainValues.T2,1);
                            SetText(ObtSampGainDanielRopeCT1, ropeC.GainValues.T1,1);
                            SetText(ObtSampGainDanielRopeCT2, ropeC.GainValues.T2,1);
                            SetText(ObtSampGainDanielRopeDT1, ropeD.GainValues.T1,1);
                            SetText(ObtSampGainDanielRopeDT2, ropeD.GainValues.T2,1);

                            break;
                        case UltrasonicModel.DanielJunior1R:
                            SetText(ObtSampEffDaniel1RRopeA, ropeA.EfficiencyValue);
                           
                            SetText(ObtSampGainDaniel1RRopeAT1, ropeA.GainValues.T1, 1);
                            SetText(ObtSampGainDaniel1RRopeAT2, ropeA.GainValues.T2, 1);
                        
                            break;
                        case UltrasonicModel.DanielJunior2R:
                            SetText(ObtSampEffDaniel2RRopeA, ropeA.EfficiencyValue);
                            SetText(ObtSampEffDaniel2RRopeB, ropeB.EfficiencyValue);
                         
                            SetText(ObtSampGainDaniel2RRopeAT1, ropeA.GainValues.T1, 1);
                            SetText(ObtSampGainDaniel2RRopeAT2, ropeA.GainValues.T2, 1);
                            SetText(ObtSampGainDaniel2RRopeBT1, ropeB.GainValues.T1, 1);
                            SetText(ObtSampGainDaniel2RRopeBT2, ropeB.GainValues.T2, 1);
                         
                            break;
                        case UltrasonicModel.InstrometS5:
                            SetText(ObtSampEffInsS5RopeA, ropeA.EfficiencyValue);
                            SetText(ObtSampEffInsS5RopeB, ropeB.EfficiencyValue);
                            SetText(ObtSampEffInsS5RopeC, ropeC.EfficiencyValue);
                            SetText(ObtSampEffInsS5RopeD, ropeD.EfficiencyValue);
                            SetText(ObtSampEffInsS5RopeE, ropeE.EfficiencyValue);
                            //SetText(ObtSampEffInsS5RopeF, ropeF.EfficiencyValue);

                            SetText(ObtSampGainInstS5RopeAT1, ropeA.GainValues.T1,1);
                            SetText(ObtSampGainInstS5RopeAT2, ropeA.GainValues.T2,1);
                            SetText(ObtSampGainInstS5RopeBT1, ropeB.GainValues.T1,1);
                            SetText(ObtSampGainInstS5RopeBT2, ropeB.GainValues.T2,1);
                            SetText(ObtSampGainInstS5RopeCT1, ropeC.GainValues.T1,1);
                            SetText(ObtSampGainInstS5RopeCT2, ropeC.GainValues.T2,1);
                            SetText(ObtSampGainInstS5RopeDT1, ropeD.GainValues.T1,1);
                            SetText(ObtSampGainInstS5RopeDT2, ropeD.GainValues.T2,1);
                            SetText(ObtSampGainInstS5RopeET1, ropeE.GainValues.T1,1);
                            SetText(ObtSampGainInstS5RopeET2, ropeE.GainValues.T2,1);
                            //SetText(ObtSampGainInstS5RopeFT1, ropeF.GainValue.T1,1);
                            //SetText(ObtSampGainInstS5RopeFT2, ropeF.GainValue.T2,1);

                            break;
                        case UltrasonicModel.InstrometS6:
                            SetText(ObtSampEffInsS6RopeA, ropeA.EfficiencyValue);
                            SetText(ObtSampEffInsS6RopeB, ropeB.EfficiencyValue);
                            SetText(ObtSampEffInsS6RopeC, ropeC.EfficiencyValue);
                            SetText(ObtSampEffInsS6RopeD, ropeD.EfficiencyValue);
                            SetText(ObtSampEffInsS6RopeE, ropeE.EfficiencyValue);
                            SetText(ObtSampEffInsS6RopeF, ropeF.EfficiencyValue);
                            SetText(ObtSampEffInsS6RopeG, ropeG.EfficiencyValue);
                            SetText(ObtSampEffInsS6RopeH, ropeH.EfficiencyValue);

                            SetText(ObtSampGainInstS6RopeAT1, ropeA.GainValues.T1,1);
                            SetText(ObtSampGainInstS6RopeAT2, ropeA.GainValues.T2,1);
                            SetText(ObtSampGainInstS6RopeBT1, ropeB.GainValues.T1,1);
                            SetText(ObtSampGainInstS6RopeBT2, ropeB.GainValues.T2,1);
                            SetText(ObtSampGainInstS6RopeCT1, ropeC.GainValues.T1,1);
                            SetText(ObtSampGainInstS6RopeCT2, ropeC.GainValues.T2,1);
                            SetText(ObtSampGainInstS6RopeDT1, ropeD.GainValues.T1,1);
                            SetText(ObtSampGainInstS6RopeDT2, ropeD.GainValues.T2,1);
                            SetText(ObtSampGainInstS6RopeET1, ropeE.GainValues.T1,1);
                            SetText(ObtSampGainInstS6RopeET2, ropeE.GainValues.T2,1);
                            SetText(ObtSampGainInstS6RopeFT1, ropeF.GainValues.T1,1);
                            SetText(ObtSampGainInstS6RopeFT2, ropeF.GainValues.T2,1);
                            SetText(ObtSampGainInstS6RopeGT1, ropeG.GainValues.T1,1);
                            SetText(ObtSampGainInstS6RopeGT2, ropeG.GainValues.T2,1);
                            SetText(ObtSampGainInstS6RopeHT1, ropeH.GainValues.T1,1);
                            SetText(ObtSampGainInstS6RopeHT2, ropeH.GainValues.T2,1);

                            break;
                        case UltrasonicModel.Sick:
                            SetText(ObtSampEffSickRopeA, ropeA.EfficiencyValue);
                            SetText(ObtSampEffSickRopeB, ropeB.EfficiencyValue);
                            SetText(ObtSampEffSickRopeC, ropeC.EfficiencyValue);
                            SetText(ObtSampEffSickRopeD, ropeD.EfficiencyValue);

                            SetText(ObtSampGainSickRopeAT1, ropeA.GainValues.T1,1);
                            SetText(ObtSampGainSickRopeAT2, ropeA.GainValues.T2,1);
                            SetText(ObtSampGainSickRopeBT1, ropeB.GainValues.T1,1);
                            SetText(ObtSampGainSickRopeBT2, ropeB.GainValues.T2,1);
                            SetText(ObtSampGainSickRopeCT1, ropeC.GainValues.T1,1);
                            SetText(ObtSampGainSickRopeCT2, ropeC.GainValues.T2,1);
                            SetText(ObtSampGainSickRopeDT1, ropeD.GainValues.T1,1);
                            SetText(ObtSampGainSickRopeDT2, ropeD.GainValues.T2,1);
                            break;
                        case UltrasonicModel.FMU:
                            SetText(ObtSampEffFmuRopeA, ropeA.EfficiencyValue);
                            SetText(ObtSampEffFmuRopeB, ropeB.EfficiencyValue);
                            SetText(ObtSampEffFmuRopeC, ropeC.EfficiencyValue);
                            SetText(ObtSampEffFmuRopeD, ropeD.EfficiencyValue);

                            SetText(ObtSampGainFMURopeAT1, ropeA.GainValues.T1,1);
                            SetText(ObtSampGainFMURopeAT2, ropeA.GainValues.T2,1);
                            SetText(ObtSampGainFMURopeBT1, ropeB.GainValues.T1,1);
                            SetText(ObtSampGainFMURopeBT2, ropeB.GainValues.T2,1);
                            SetText(ObtSampGainFMURopeCT1, ropeC.GainValues.T1,1);
                            SetText(ObtSampGainFMURopeCT2, ropeC.GainValues.T2,1);
                            SetText(ObtSampGainFMURopeDT1, ropeD.GainValues.T1,1);
                            SetText(ObtSampGainFMURopeDT2, ropeD.GainValues.T2,1);
                            break;
                        case UltrasonicModel.KrohneAltosonicV12:
                            SetText(ObtSampEffKrohneAltV12RopeA, ropeA.EfficiencyValue);
                            SetText(ObtSampEffKrohneAltV12RopeB, ropeB.EfficiencyValue);
                            SetText(ObtSampEffKrohneAltV12RopeC, ropeC.EfficiencyValue);
                            SetText(ObtSampEffKrohneAltV12RopeD, ropeD.EfficiencyValue);
                            SetText(ObtSampEffKrohneAltV12RopeE, ropeE.EfficiencyValue);
                            SetText(ObtSampEffKrohneAltV12RopeF, ropeF.EfficiencyValue);

                            SetText(ObtSampGainKrohneAltV12RopeAT1, ropeA.GainValues.T1,1);
                            SetText(ObtSampGainKrohneAltV12RopeAT2, ropeA.GainValues.T2,1);
                            SetText(ObtSampGainKrohneAltV12RopeBT1, ropeB.GainValues.T1,1);
                            SetText(ObtSampGainKrohneAltV12RopeBT2, ropeB.GainValues.T2,1);
                            SetText(ObtSampGainKrohneAltV12RopeCT1, ropeC.GainValues.T1,1);
                            SetText(ObtSampGainKrohneAltV12RopeCT2, ropeC.GainValues.T2,1);
                            SetText(ObtSampGainKrohneAltV12RopeDT1, ropeD.GainValues.T1,1);
                            SetText(ObtSampGainKrohneAltV12RopeDT2, ropeD.GainValues.T2,1);
                            SetText(ObtSampGainKrohneAltV12RopeET1, ropeE.GainValues.T1,1);
                            SetText(ObtSampGainKrohneAltV12RopeET2, ropeE.GainValues.T2,1);
                            SetText(ObtSampGainKrohneAltV12RopeFT1, ropeF.GainValues.T1,1);
                            SetText(ObtSampGainKrohneAltV12RopeFT2, ropeF.GainValues.T2,1);
                            break;
                    }

                }
            }

        }

        private void UpdateStarUpUltrasonicModel()
        {
            // iniciar controles
            btnStopStartUp.Visibility = Visibility.Hidden;
            btnInitStartUp.Visibility = Visibility.Visible;

            int selected = dryCalibration.CurrentModbusConfiguration.SlaveConfig.Model;

            if (cbCommUltrasonicModel != null && txtStartUpUltrasonicModel != null)
            {      
                //cuerdas por modelo en la puesta en marcha
                StartUpRopesUltrasonic_0.Visibility = Visibility.Hidden;
                StartUpRopesUltrasonic_1.Visibility = Visibility.Hidden;
                StartUpRopesUltrasonic_2.Visibility = Visibility.Hidden;
                StartUpRopesUltrasonic_3.Visibility = Visibility.Hidden;
                StartUpRopesUltrasonic_4.Visibility = Visibility.Hidden;
                StartUpRopesUltrasonic_5.Visibility = Visibility.Hidden;
                StartUpRopesUltrasonic_6.Visibility = Visibility.Hidden;
                StartUpRopesUltrasonic_7.Visibility = Visibility.Hidden;

                switch (selected)
                {
                    case 0: // Daniel
                        txtStartUpUltrasonicModel.Text = "Daniel";
                        StartUpRopesUltrasonic_0.Visibility = Visibility.Visible;                     
                        break;
                    case 1: // Daniel Junior Una Cuerda
                        txtStartUpUltrasonicModel.Text = "Daniel Junior 1 Cuerda";
                        StartUpRopesUltrasonic_1.Visibility = Visibility.Visible;
                        break;
                    case 2: // Daniel Junior Dos Cuerdas
                        txtStartUpUltrasonicModel.Text = "Daniel Junior 2 Cuerdas";
                        StartUpRopesUltrasonic_2.Visibility = Visibility.Visible;
                        break;
                    case 3: // Instromet Series 3 al 5
                        txtStartUpUltrasonicModel.Text = "Instromet Series 3 al 5";
                        StartUpRopesUltrasonic_3.Visibility = Visibility.Visible;                       
                        break;
                    case 4: // Instromet Series 6
                        txtStartUpUltrasonicModel.Text = "Instromet Series 6";
                        StartUpRopesUltrasonic_4.Visibility = Visibility.Visible;
                        break;
                    case 5: // Sick
                        txtStartUpUltrasonicModel.Text = "Sick";
                        StartUpRopesUltrasonic_5.Visibility = Visibility.Visible;
                        break;
                    case 6: // FMU
                        txtStartUpUltrasonicModel.Text = "FMU";
                        StartUpRopesUltrasonic_6.Visibility = Visibility.Visible;
                        break;
                    case 7: // Krohne Altosonic V12
                        txtStartUpUltrasonicModel.Text = "Krohne Altosonic V12";
                        StartUpRopesUltrasonic_7.Visibility = Visibility.Visible;
                        break;
                
                }
            }
        }

        private void InitUltrasonicCommunication()
        {
            string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");

            if (!System.IO.File.Exists(path))
            {
                ModbusConfiguration.Generate(path);
            }

            ModbusConfiguration configuration = ModbusConfiguration.Read(path);

            // model
            cbCommUltrasonicModel.SelectedIndex = configuration.SlaveConfig.Model;

            // communication
            cbConfigComunication.SelectedIndex = configuration.SlaveConfig.ModbusCommunication;

            if (cbConfigComunication.SelectedIndex == 0) //serial
            {
                // nombre del puerto
                cbConfigSerialPortNames.Text = configuration.MasterSerial.PortName;
                // baudios
                cbConfigSerialBaudRate.SelectedIndex = configuration.MasterSerial.BaudRate;
                // bits de datos
                cbConfigSerialDataBits.SelectedIndex = configuration.MasterSerial.DataBits;
                // paridad
                cbConfigSerialParity.SelectedIndex = configuration.MasterSerial.Parity;
                // bits de parada
                cbConfigSerialStopBits.SelectedIndex = configuration.MasterSerial.StopBits;
            }
            else // tcp
            {
                // ip/puerto
                SetText(txtConfigUltrasonicTcpIP, configuration.Tcp.IPAddress);
                SetText(txtConfigUltrasonicTcpPort, configuration.Tcp.PortNumber);
            }

            // intervalo de muestra
            SetText(txtConfigUltrasonicSampleInterval, configuration.SlaveConfig.SampleInterval);
            // timeout de la muestra
            SetText(txtConfigUltrasonicSampleTimeOut, configuration.SlaveConfig.SampleTimeOut);
            // slave id
            SetText(txtConfigUltrasonicSlaveId, configuration.SlaveConfig.SlaveId);
            // modbus formato de la trama
            cbConfigModbusFrameFormat.SelectedIndex = configuration.SlaveConfig.ModbusFrameFormat;

            // límites de ganancia del ultrasónico
            UltrasonicModel model = (UltrasonicModel) configuration.SlaveConfig.Model;

            GainConfig gainConfig = configuration.UltGainConfig.FirstOrDefault(g => g.UltModel.Equals(model));

            if (gainConfig != null) 
            {
                txtConfigUltrasonicGainMin.Text = Convert.ToString(gainConfig.Min);
                txtConfigUltrasonicGainMax.Text = Convert.ToString(gainConfig.Max);
            }

            // modo de obtención de muestras
            bool isAutomatic = configuration.UltrasonicSampleMode == (int)UltSampMode.Automatic;
            cbUltSampleMode.SelectedIndex = (isAutomatic) ? 0 : 1;
        }

        private void InitNIcDaqModuleAdjustment()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-AR"); 

            // iniciar controles
            btnStopDaqModuleAdjustament.Visibility = Visibility.Hidden;
            btnInitDaqModuleAdjustament.Visibility = Visibility.Visible;

            // deshabilitar botones de ajuste
            EnabledAdjustament(false);

            // temperatura
            SetText(adjModRefTemp, 0d);

            string path = Path.Combine(Utils.ConfigurationPath, "RtdCalibration.xml");

            if (!System.IO.File.Exists(path))
            {
                RtdTable.Generate(path, "0.0.0.0");
            }

            RtdTable configRTD = RtdTable.Read(path);

            RtdCalibration calRtd1 = configRTD.RtdSensors.Find(c => c.Number == 0);
            RtdCalibration calRtd2 = configRTD.RtdSensors.Find(c => c.Number == 1);
            RtdCalibration calRtd3 = configRTD.RtdSensors.Find(c => c.Number == 2);
            RtdCalibration calRtd4 = configRTD.RtdSensors.Find(c => c.Number == 3);
            RtdCalibration calRtd5 = configRTD.RtdSensors.Find(c => c.Number == 4);
            RtdCalibration calRtd6 = configRTD.RtdSensors.Find(c => c.Number == 5);
            RtdCalibration calRtd7 = configRTD.RtdSensors.Find(c => c.Number == 6);
            RtdCalibration calRtd8 = configRTD.RtdSensors.Find(c => c.Number == 7);
            RtdCalibration calRtd9 = configRTD.RtdSensors.Find(c => c.Number == 8);
            RtdCalibration calRtd10 = configRTD.RtdSensors.Find(c => c.Number == 9);
            RtdCalibration calRtd11 = configRTD.RtdSensors.Find(c => c.Number == 10);
            RtdCalibration calRtd12= configRTD.RtdSensors.Find(c => c.Number == 11);

            // puntos de temperatura

            object point1 = configRTD.TempPoints.Find(t => t.Number == 1).Value;
            object point2 = configRTD.TempPoints.Find(t => t.Number == 2).Value;
            object point3 = configRTD.TempPoints.Find(t => t.Number == 3).Value;
            object point4 = configRTD.TempPoints.Find(t => t.Number == 4).Value;
            object point5 = configRTD.TempPoints.Find(t => t.Number == 5).Value;

            SetTextBlock(adjLblBtnPoint1, (double)point1 != 0 ? point1 : "Punto 1");
            SetTextBlock(adjLblBtnPoint2, (double)point2 != 0 ? point2 : "Punto 2");
            SetTextBlock(adjLblBtnPoint3, (double)point3 != 0 ? point3 : "Punto 3");
            SetTextBlock(adjLblBtnPoint4, (double)point4 != 0 ? point4 : "Punto 4");
            SetTextBlock(adjLblBtnPoint5, (double)point5 != 0 ? point5 : "Punto 5");

            // valores de resistencia

            SetText(adjResPoint1RTD1, calRtd1.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD1, calRtd1.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD1, calRtd1.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD1, calRtd1.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD1, calRtd1.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD2, calRtd2.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD2, calRtd2.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD2, calRtd2.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD2, calRtd2.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD2, calRtd2.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD3, calRtd3.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD3, calRtd3.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD3, calRtd3.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD3, calRtd3.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD3, calRtd3.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD4, calRtd4.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD4, calRtd4.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD4, calRtd4.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD4, calRtd4.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD4, calRtd4.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD5, calRtd5.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD5, calRtd5.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD5, calRtd5.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD5, calRtd5.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD5, calRtd5.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD6, calRtd6.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD6, calRtd6.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD6, calRtd6.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD6, calRtd6.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD6, calRtd6.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD7, calRtd7.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD7, calRtd7.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD7, calRtd7.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD7, calRtd7.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD7, calRtd7.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD8, calRtd8.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD8, calRtd8.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD8, calRtd8.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD8, calRtd8.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD8, calRtd8.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD9, calRtd9.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD9, calRtd9.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD9, calRtd9.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD9, calRtd9.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD9, calRtd9.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD10, calRtd10.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD10, calRtd10.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD10, calRtd10.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD10, calRtd10.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD10, calRtd10.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD11, calRtd11.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD11, calRtd11.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD11, calRtd11.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD11, calRtd11.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD11, calRtd11.ResPoints.Find(r => r.Number == 5).Value);

            SetText(adjResPoint1RTD12, calRtd12.ResPoints.Find(r => r.Number == 1).Value);
            SetText(adjResPoint2RTD12, calRtd12.ResPoints.Find(r => r.Number == 2).Value);
            SetText(adjResPoint3RTD12, calRtd12.ResPoints.Find(r => r.Number == 3).Value);
            SetText(adjResPoint4RTD12, calRtd12.ResPoints.Find(r => r.Number == 4).Value);
            SetText(adjResPoint5RTD12, calRtd12.ResPoints.Find(r => r.Number == 5).Value);

            // presión
            SetText(adjModReferencePress, 0d);

            path = Path.Combine(Utils.ConfigurationPath, "PressureCalibration.xml");

            if (!System.IO.File.Exists(path))
            {
                PressureCalibration.Generate(path, "0.0.0.0");
            }

            PressureCalibration configPress = PressureCalibration.Read(path);

            SetText(adjModPressDif1, (double)configPress.Error);
            SetText(adjModPressZero, configPress.Zero);
            SetText(adjModPressSpan, configPress.Span);
            cbPressSensorTypeAdj.SelectedIndex = configPress.SensorType;
            //SetText(adjModPressAtmospheric, configPress.AtmosphericPresssure);

            // comunicación
            SetText(adjModIPAddress, configPress.DaqModuleIPAddress);

            if (configPress.DaqModuleIPAddress == "0.0.0.0")
            {
                SetControlEnabled(btnSaveAdjDaqModule, true);
            }
        }

        private void EnabledAdjustament(bool enabled)
        {
            btnAdjPoint1.IsEnabled = enabled;
            btnAdjPoint2.IsEnabled = enabled;
            btnAdjPoint3.IsEnabled = enabled;
            btnAdjPoint4.IsEnabled = enabled;
            btnAdjPoint5.IsEnabled = enabled;
            btnAdjPressCalculate.IsEnabled = enabled;
            btnSaveAdjDaqModule.IsEnabled = enabled;
        }

        private void SetContent(Label label, object arg, int decQ = 3)
        {
            if (!label.Dispatcher.CheckAccess())
            {
                label.Dispatcher.Invoke(new Action<Label, object, int>(SetContent), label, arg, decQ);
            }
            else
            {
                if (arg is double)
                {
                    string strValue = String.Format("{0:0.000}", arg);

                    if (decQ == 1)
                    {
                        strValue = String.Format("{0:0.0}", arg);
                    }
                    else if (decQ == 2)
                    {
                        strValue = String.Format("{0:0.00}", arg);
                    }

                    label.Content = strValue;
                    return;
                }

                label.Content = arg.ToString();
            }
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
                    string strValue = String.Format("{0:0.000}", arg);

                    if (decQ == 1)
                    {
                        strValue = String.Format("{0:0.0}", arg);
                    }
                    else if (decQ == 2)
                    {
                        strValue = String.Format("{0:0.00}", arg);
                    }

                    textBox.Text = strValue;
                    return;
                }

                textBox.Text = arg.ToString();
            }
        }

        private void SetTextBlock(TextBlock textBox, object arg)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<TextBlock, object>(SetTextBlock), textBox, arg);
            }
            else
            {
                if (arg is double)
                {
                    string strValue = String.Format("{0:n}", arg);
                    textBox.Text = strValue;
                    return;
                }

                textBox.Text = arg.ToString();
            }
        }

        private void SetControlEnabled(UIElement control, bool arg)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(new Action<UIElement, bool>(SetControlEnabled), control, arg);
            }
            else
            {
                control.IsEnabled = arg;
            }
        }

    }
}

    public static class EtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };
        public static void Refresh(this UIElement uiElement)
        {
            try
            {
                 uiElement.Dispatcher.Invoke(EmptyDelegate, DispatcherPriority.Render);
            }
            catch (System.Threading.Tasks.TaskCanceledException)
            {
                Thread.CurrentThread.Abort();
            }
        }

    }



   
