using Modbus;
using Modbus.Data;
using Modbus.Device;
using Modbus.Utility;
using Proser.DryCalibration.controller.ultrasonic;
using Proser.DryCalibration.fsm;
using Proser.DryCalibration.Report;
using Proser.DryCalibration.sensor.ultrasonic.modbus.IO;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.interfaces;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.measurers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;

namespace Proser.DryCalibration.Console
{
    class Program
    {
        static void Main(string[] args)
        {

            //try
            //{
            //    // Set <Domain> to your domain name.
            //    // Set <UserName> to the user account.
            //    SetPermissions("C:\\Test", "<Domain>\\<UserName>");
            //    MessageBox.Show("Full Access control granted.");
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}



            // string result = decimalComplete("9,75", 3);

            //MeasuringConfiguration configuration = new MeasuringConfiguration();

            //configuration.MeasuringInstruments.Add(new MeasuringInstrument()
            //{
            //    BrandName = "Equipo 1",
            //    InternalIdentification = "Descripcion interna 1",
            //    CalibrationCode = "Código de calibración 1"
            //});

            //configuration.MeasuringInstruments.Add(new MeasuringInstrument()
            //{
            //    BrandName = "Equipo 2",
            //    InternalIdentification = "Descripcion interna 2",
            //    CalibrationCode = "Código de calibración 2"
            //});

            //configuration.MeasuringInstruments.Add(new MeasuringInstrument()
            //{
            //    BrandName = "Equipo 3",
            //    InternalIdentification = "Descripcion interna 3",
            //    CalibrationCode = "Código de calibración 3"
            //});

            //MeasuringConfiguration.Generate("MeasuringConfiguration.xml", configuration);

            //RtdTable calibration = RtdTable.Read("RtdCalibration.xml");


            //RtdController controller = new RtdController();

            //controller.Initialize();


            //if (!System.IO.File.Exists("PressureCalibration.xml"))
            //{
            //    sensor.pressure.calibration.PressureCalibration.Generate("PressureCalibration.xml", "10.0.0.235");
            //}

            //sensor.pressure.calibration.PressureCalibration calibration2 = sensor.pressure.calibration.PressureCalibration.Read("PressureCalibration.xml");

            //PressureController controller2 = new PressureController();

            //controller.Initialize();


            //ReadWrite32BitValue();


            //StartModbusSerialAsciiSlave();

            //ModbusSerialAsciiMasterReadRegisters();

            //ModbusSerialRtuMasterReadRegisters();

            //IModBusMap device = new DanielMeasurer();

            //device = new InstrometS5Measurer();

            //device = new InstrometS6Measurer();

            //device = new SickMeasurer();



            //if (!System.IO.File.Exists("ModbusConfiguration.xml"))
            //{
            //    sensor.ultrasonic.modbus.configuration.ModbusConfiguration.Generate("ModbusConfiguration.xml");
            //}


            //sensor.ultrasonic.modbus.configuration.ModbusConfiguration config = sensor.ultrasonic.modbus.configuration.ModbusConfiguration.Read("ModbusConfiguration.xml");

            //IModBusMap measurer = new DanielMeasurer();

            //IModBusMap measurer = new FMUMeasurer();

            //IModBusMap measurer = new SickMeasurer();

            //IModBusMap measurer = new KrohneAltV12Measurer();

            //IModBusMap measurer = new InstrometS6Measurer();

            //IModBusMap measurer = new InstrometS5Measurer();

            //IMonitor monitor = new UltrasonicMonitor(measurer,
            //                                         ModbusCommunication.Serial,
            //                                         ModbusFrameFormat.ASCII);


            //monitor.InitMonitor();




            UltrasonicController controller = new UltrasonicController();

            controller.Initialize();




            //process = new DryCalibrationProcess();
            //process.RefreshState += Process_RefreshState;

            //process.Initialize();


            //Task.Run(async () =>
            //{
            //    await generateReportAsync();

            //}).GetAwaiter().GetResult();


            //Report.ReportApp report = new Report.ReportApp();

            //ReportModel reportModel = new ReportModel()
            //{
            //    Header = new ReportHeader()
            //    {
            //        Measurer = new Measurer()
            //        {
            //            Brand = "Daniel",
            //            Model = "123456",
            //            SerieNumber = "123456789",
            //            FirmwareVersion = "2.333",

            //        },
            //        Petitioner = new Petitioner()
            //        {
            //            BusinessName = "Una Empresa",
            //            BusinessAddress = "Una dirección de Empresa",
            //            RealizationAddress = "Una dirección de realización",
            //            RealizationPlace = "Un lugar de realización",

            //        },
            //        CalibrationInformation = new CalibrationInformation()
            //        {
            //            ReportNumber = "DC-19-0001-03",
            //            Responsible = "Un responsable del ensayo",
            //            CalibrationObject = "1 (un) medidor ultrasónico",
            //            RequiredDetermination = "Ensayo de verificación de flujo cero y velocidad de sonido",
            //            CalibrationDate = DateTime.Now.ToString("dd/MM/yyyy"),
            //            EmitionDate = DateTime.Now.ToString("dd/MM/yyyy")

            //        },
            //        EnvironmentalCondition = new EnvironmentalCondition()
            //        {
            //            AtmosphericPressure = "1",
            //            EnvironmentTemperature = "22,002"
            //        }
            //    },

            //    Body = new ReportBody()
            //    {
            //        CalibrationTerms = new CalibrationTerms()
            //        {
            //            Duration = "500",
            //            EfficiencyAverage = "100",
            //            PressureAverage = "1",
            //            ReferenceFlow = "Nitrogeno 5.0",
            //            TemperatureAverage = "22,56",
            //            TemperatureDifference = "0,03"
            //        },
            //        FlowSpeedResults = new FlowSpeedResult()
            //        {
            //            AverageResults = new List<RopeResult>() {
            //                 new RopeResult(){
            //                      Name = "A",
            //                       Value="0,005",
            //                       Uncertainty="0,001"
            //                 }//,
            //                 //new RopeResult(){
            //                 //     Name = "B",
            //                 //      Value="0,005",
            //                 //       Uncertainty="0,001"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "C",
            //                 //      Value="0,005",
            //                 //      Uncertainty="0,001"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "D",
            //                 //      Value="0,005",
            //                 //      Uncertainty="0,001"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "E",
            //                 //      Value="0,005",
            //                 //      Uncertainty="0,001"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "F",
            //                 //      Value="0,005",
            //                 //      Uncertainty="0,001"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "G",
            //                 //      Value="0,005",
            //                 //      Uncertainty="0,001"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "H",
            //                 //      Value="0,005",
            //                 //      Uncertainty="0,001"
            //                 //}

            //             }
            //        },
            //        SoundSpeedResults = new SoundSpeedResult()
            //        {
            //            TheoreticalSoundSpeed = "3,006",

            //            AverageResults = new List<RopeResult>() {
            //                 new RopeResult(){
            //                      Name = "A",
            //                       Value="3,006",
            //                       Uncertainty="0,001",
            //                        Error = "0,2"
            //                 }//,
            //                 //new RopeResult(){
            //                 //     Name = "B",|
            //                 //      Value="3,006",
            //                 //       Uncertainty="0,001",
            //                 //      Error = "0,2"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "C",
            //                 //      Value="3,006",
            //                 //      Uncertainty="0,001",
            //                 //      Error = "0,2"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "D",
            //                 //      Value="3,006",
            //                 //       Uncertainty="0,001",
            //                 //      Error = "0,2"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "E",
            //                 //      Value="3,006",
            //                 //       Uncertainty="0,001",
            //                 //      Error = "0,2"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "F",
            //                 //      Value="3,006",
            //                 //      Uncertainty="0,001",
            //                 //      Error = "0,2"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "G",
            //                 //      Value="3,006",
            //                 //       Uncertainty="0,001",
            //                 //      Error = "0,2"
            //                 //},
            //                 //new RopeResult(){
            //                 //     Name = "H",
            //                 //      Value="3,006",
            //                 //       Uncertainty="0,001",
            //                 //      Error = "0,2"
            //                 //}

            //             },

            //            SoundSpeedValMax = "3,009",
            //            SoundSpeedValMin = "3,006",
            //            SoundSpeedDifferece = "0,003"

            //        },
            //        CalibrationMeasuring = new CalibrationMeasuring()
            //        {
            //            MeasuringInstruments = new List<ReportMeasuringInstrument>() {
            //                    new ReportMeasuringInstrument(){
            //                         BrandName = "Equipo 1",
            //                          InternalIdentification = "Descripción 1",
            //                          CalibrationCode ="111111111111"
            //                    },
            //                    new ReportMeasuringInstrument(){
            //                         BrandName = "Equipo 2",
            //                          InternalIdentification = "Descripción 2",
            //                          CalibrationCode ="222222222222"
            //                    },
            //              },
            //            Observations = "Nro. Serie 1, Nro Serie 2...."

            //        }
            //    }
            //};

            //string reportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Dry Calibration\\Certificados");
            //report.Generate(reportModel, reportPath);


            System.Console.ReadKey();
        }

        private static void SetPermissions(String vPath, String UserName)
        {
            //ADsSecurity objADsSec;
            //SecurityDescriptor objSecDes;
            //AccessControlList objDAcl;
            //AccessControlEntry objAce1;
            //AccessControlEntry objAce2;
            //Object objSIdHex;
            //ADsSID objSId;

            //objADsSec = new ADsSecurityClass();
            //objSecDes = (SecurityDescriptor)(objADsSec.GetSecurityDescriptor("FILE://" + vPath));
            //objDAcl = (AccessControlList)objSecDes.DiscretionaryAcl;

            //objSId = new ADsSIDClass();
            //objSId.SetAs((int)ADSSECURITYLib.ADS_SID_FORMAT.ADS_SID_SAM, UserName.ToString());
            //objSIdHex = objSId.GetAs((int)ADSSECURITYLib.ADS_SID_FORMAT.ADS_SID_SDDL);

            //// Add a new access control entry (ACE) object (objAce) so that the user has Full Control permissions on NTFS file system files.
            //objAce1 = new AccessControlEntryClass();
            //objAce1.Trustee = (objSIdHex).ToString();
            //objAce1.AccessMask = (int)ActiveDs.ADS_RIGHTS_ENUM.ADS_RIGHT_GENERIC_ALL;
            //objAce1.AceType = (int)ActiveDs.ADS_ACETYPE_ENUM.ADS_ACETYPE_ACCESS_ALLOWED;
            //objAce1.AceFlags = (int)ActiveDs.ADS_ACEFLAG_ENUM.ADS_ACEFLAG_INHERIT_ACE | (int)ActiveDs.ADS_ACEFLAG_ENUM.ADS_ACEFLAG_INHERIT_ONLY_ACE | 1;
            //objDAcl.AddAce(objAce1);

            //// Add a new access control entry object (objAce) so that the user has Full Control permissions on NTFS file system folders.
            //objAce2 = new AccessControlEntryClass();
            //objAce2.Trustee = (objSIdHex).ToString();
            //objAce2.AccessMask = (int)ActiveDs.ADS_RIGHTS_ENUM.ADS_RIGHT_GENERIC_ALL;
            //objAce2.AceType = (int)ActiveDs.ADS_ACETYPE_ENUM.ADS_ACETYPE_ACCESS_ALLOWED;
            //objAce2.AceFlags = (int)ActiveDs.ADS_ACEFLAG_ENUM.ADS_ACEFLAG_INHERIT_ACE | 1;
            //objDAcl.AddAce(objAce2);

            //objSecDes.DiscretionaryAcl = objDAcl;

            //// Set permissions on the NTFS file system folder.
            //objADsSec.SetSecurityDescriptor(objSecDes, "FILE://" + vPath);

        }

        private static string decimalComplete(string toConvert, int decQ)
        {
            toConvert = toConvert.Replace(".", ",");

            string[] parts = toConvert.Split(',');
            bool fracExist = parts.Length > 1;

            string integer = fracExist ? parts[0] : toConvert;
            string frac = fracExist ? parts[1] : "";
           
            frac = frac.PadRight(decQ, '0');

            string result = string.Format("{0},{1}", integer, frac);

            return result;
        }

        public static DryCalibrationProcess process;
        public static bool iniciado = false;

        private static void Process_RefreshState(string description)
        {
            System.Console.WriteLine(description);

            if (process.CurrentState.Name == fsm.enums.FSMState.INITIALIZED && !iniciado)
            {
                iniciado = true;
                process.InitDryCalibration();
            }
        }


        #region Debug



        public static void ReadWrite32BitValue()
        {
            using (SerialPort port = new SerialPort("COM4"))
            {
                // configure serial port
                port.BaudRate = 9600;
                port.DataBits = 8;
                port.Parity = Parity.None;
                port.StopBits = StopBits.One;
                port.Open();

                // create modbus master
                ModbusSerialMaster master = ModbusSerialMaster.CreateRtu(port);

                byte slaveId = 1;
                ushort startAddress = 352;
                uint largeValue = UInt16.MaxValue + 5;

                ushort lowOrderValue = BitConverter.ToUInt16(BitConverter.GetBytes(largeValue), 0);
                ushort highOrderValue = BitConverter.ToUInt16(BitConverter.GetBytes(largeValue), 2);

                // write large value in two 16 bit chunks
                master.WriteMultipleRegisters(slaveId, startAddress, new ushort[] { lowOrderValue, highOrderValue });

                // read large value in two 16 bit chunks and perform conversion
                ushort[] registers = master.ReadHoldingRegisters(slaveId, startAddress, 2);
                uint value = ModbusUtility.GetUInt32(registers[1], registers[0]);
            }
        }


        private static void ModbusSerialRtuMasterReadRegisters()
        {
            using (SerialPort port = new SerialPort("COM4"))
            {
                // configure serial port
                port.BaudRate = 9600;
                port.DataBits = 8;//7 => Ascii
                port.Parity = Parity.Even;
                port.StopBits = StopBits.One;
                port.Open();

                var adapter = new SerialPortAdapter(port);

                // create modbus master
                IModbusSerialMaster master = ModbusSerialMaster.CreateRtu(adapter);

                //slave id
                byte slaveId = 32;

                //map
                IModBusMap map = new DanielMeasurer();
                System.Console.WriteLine("Leyendo dispositivo: " + map.Measurer.ToString());
                System.Console.WriteLine("---------------------------------------------------------");

            

                ushort[] registers = new ushort[100]; 

                try
                {
                    // Cuerda A -Velocidad de Flujo
                    ushort address = map.Ropes.Find(a => a.Name == "A").FlowSpeedAddress;

                    registers = master.ReadHoldingRegisters(slaveId, address, 2);


                    float value = ModbusUtility.GetSingle(registers[0], registers[1]);
                    System.Console.WriteLine(string.Format("Cuerda A - Velocidad de Flujo ({0}): {1}", address, value));


                    //Cuerda A - Velocidad de Sonido
                    address = map.Ropes.Find(a => a.Name == "A").SoundSpeedAddress;
                    registers = master.ReadHoldingRegisters(slaveId, address, 2);

                    value = ModbusUtility.GetSingle(registers[0], registers[1]);
                    System.Console.WriteLine(string.Format("Cuerda A - Velocidad de Sonido ({0}): {1}", address, value));

                    System.Console.WriteLine("---------------------------------------------------------");


                }
                catch (SlaveException e)
                {
                    System.Console.WriteLine(e.Message);
                    
                }

                

               

            }
        }



        public static void ModbusSerialAsciiMasterReadRegisters()
        {
            using (System.IO.Ports.SerialPort port = new System.IO.Ports.SerialPort("COM6"))
            {
                // configure serial port
                port.BaudRate = 9600;
                port.DataBits = 7;
                port.Parity = Parity.Even;
                port.StopBits = StopBits.One;
                port.Open();

                var adapter = new SerialPortAdapter(port);
               
                // create modbus master
                IModbusSerialMaster master = ModbusSerialMaster.CreateAscii(adapter);

                //slave id
                byte slaveId = 32;         

                //map
                IModBusMap map = new DanielMeasurer();
                System.Console.WriteLine("Leyendo dispositivo: " + map.Measurer.ToString());
                System.Console.WriteLine("---------------------------------------------------------");

                // Cuerda A -Velocidad de Flujo
                ushort address = map.Ropes.Find(a => a.Name == "A").FlowSpeedAddress;
                ushort[] registers = master.ReadHoldingRegisters(slaveId, address, 1);
               
                float value = ModbusUtility.GetSingle(registers[0], registers[1]);
                System.Console.WriteLine(string.Format("Cuerda A - Velocidad de Flujo ( {0} ): {1}", address, value));


                //Cuerda A - Velocidad de Sonido
                address = map.Ropes.Find(a => a.Name == "A").SoundSpeedAddress;
                registers = master.ReadHoldingRegisters(slaveId, address, 1);

                value = ModbusUtility.GetSingle(registers[0], registers[1]);
                System.Console.WriteLine(string.Format("Cuerda A - Velocidad de Sonido ( {0} ): {1}", address, value));

                System.Console.WriteLine("---------------------------------------------------------");

            }
        }

        public static void StartModbusSerialAsciiSlave()
        {
            using (SerialPort slavePort = new SerialPort("COM5"))
            {
                // configure serial port
                slavePort.BaudRate = 9600;
                slavePort.DataBits = 8;
                slavePort.Parity = Parity.None;
                slavePort.StopBits = StopBits.One;
                slavePort.Open();

                byte unitId = 1;

                var adapter = new SerialPortAdapter(slavePort);
                // create modbus slave
                ModbusSlave slave = ModbusSerialSlave.CreateAscii(unitId, slavePort);//ModbusSerialSlave.CreateAscii(unitId, adapter);

                slave.ModbusSlaveRequestReceived += Slave_ModbusSlaveRequestReceived;

                slave.DataStore = DataStoreFactory.CreateDefaultDataStore();

                slave.Listen();
            }
        }

        private static void Slave_ModbusSlaveRequestReceived(object sender, ModbusSlaveRequestEventArgs e)
        {
           

        }
        #endregion

    }
}
