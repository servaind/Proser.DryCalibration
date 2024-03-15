using Modbus.Utility;
using Proser.DryCalibration.sensor.ultrasonic;
using Proser.DryCalibration.sensor.ultrasonic.interfaces;
using Proser.DryCalibration.sensor.ultrasonic.modbus.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps.interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using NationalInstruments.DAQmx;
using Proser.DryCalibration.monitor.enums;

namespace Proser.DryCalibration.monitor
{
    public class UltrasonicMonitor : MonitorBase
    {
        private int slaveId;
        IUltrasonicSensor sensor;

        private IModBusMap measurer;
        private ModbusCommunication modbusCommunication;
        private ModbusFrameFormat frameFormat;
        private UltrasonicValue ultrasonicValue;
        private bool FlowSpeedRead;
        private bool SoundSpeedRead;
        private bool RopeEfficiencyRead;
        private bool RopeGainRead;

        private CancellationTokenSource token;
        
        public UltrasonicMonitor(IModBusMap measurer, ModbusCommunication modbusComunication, ModbusFrameFormat frameFormat)
        {
            this.measurer = measurer;
            this.modbusCommunication = modbusComunication;
            this.frameFormat = frameFormat;
            this.Type = enums.MonitorType.Ultrasonic;
            this.ultrasonicValue = new UltrasonicValue();
        }

        public override void InitMonitor()
        {
            objLock = new object();

            LoadSensor();

            State = MonitorState.Running;

            MonitorWorkThread = new Thread(new ThreadStart(DoMonitorWork));
            MonitorWorkThread.Start();
        }

        public override void StopMonitor()
        {
            base.StopMonitor();
        }

        public override void LoadSensor()
        {
           
            sensor = new UltrasonicSensor(this.measurer,
                                          this.modbusCommunication,
                                          this.frameFormat);

            sensor.Init();
        }


        public override void DoMonitorWork()
        {
            while (State.Equals(MonitorState.Running))
            {
                UpdateSensor();
            }

            sensor.Dispose();
        }

        public override void UpdateSensor()
        {
            try
            {
                FlowSpeedRead = false;
                SoundSpeedRead = false;

                System.Console.WriteLine("Leyendo dispositivo: " + measurer.Measurer.ToString());
                System.Console.WriteLine("---------------------------------------------------------");

                // update values
                GetFlowSpeed(sensor.Ropes);


                if (FlowSpeedRead) 
                {
                    GetSoundSpeed(sensor.Ropes);


                    if (SoundSpeedRead) 
                    {

                        foreach (RopeValue ropeValue in sensor.Ropes)
                        {
                            RopeGainRead = false;

                            GetRopeGain(ropeValue);

                            if (!RopeGainRead)
                            {
                                break;
                            }
                        }


                        foreach (RopeValue ropeValue in sensor.Ropes)
                        {
                            RopeEfficiencyRead = false;

                            GetRopeEfficiency(ropeValue);

                            if (!RopeEfficiencyRead)
                            {
                                break;
                            }
                        }
                    }
                }

               
                System.Console.WriteLine("-------------------");

                ultrasonicValue.Ropes.Clear();
                ultrasonicValue.Ropes.AddRange(sensor.Ropes);

                //log.Log.WriteIfExists("Send monitor values (Ultrasonic)");

                // send values
                SendMonitorValues(ultrasonicValue);

                // send statistic
                SendMonitorStatistics();

                System.Console.WriteLine("");

                System.Console.WriteLine("---------------------------------------------------------");

            }
            catch (Exception e)
            {
                log.Log.WriteIfExists("Error: (UpdateSensor) Send monitor values (Ultrasonic) " + e.Message + " Detalle:  " + e.StackTrace);
                
                if(e.InnerException != null) 
                {
                    log.Log.WriteIfExists("Detalle: " + e.InnerException.Message + "----" + e.InnerException.StackTrace);
                }
                //throw;
            }
        }

        private void GetFlowSpeed(List<RopeValue>value)
        {
            Thread th = new Thread(new ParameterizedThreadStart(doGetFlowSpeedTh));
            th.Start(value);
            th.Join();
        }

        private void GetSoundSpeed(List<RopeValue>value)
        {
            Thread th = new Thread(new ParameterizedThreadStart(doGetSoundSpeedTh));
            th.Start(value);
            th.Join();
        }

        private void GetRopeGain(RopeValue value) 
        {
            Thread th = new Thread(new ParameterizedThreadStart(doGetRopeGainTh));
            th.Start(value);
            th.Join();
        }

        private void GetRopeEfficiency(RopeValue value)
        {
            Thread th = new Thread(new ParameterizedThreadStart(doGetRopeEfficiencyTh));
            th.Start(value);
            th.Join();
        }


        private void doGetFlowSpeedTh(object obj)
        {
            List<RopeValue> ropeValues = (List<RopeValue>)obj;

            try
            {
                ropeValues = ropeValues.OrderBy(r => r.Name).ToList();

                foreach (var ropeValue in ropeValues)
                {
                    ushort address = measurer.Ropes.Find(a => a.Name.Equals(ropeValue.Name)).FlowSpeedAddress;

                    ushort numberOfPoints = 2;
                    ushort[] registers = sensor.Master.ReadHoldingRegisters(sensor.SlaveId, address, numberOfPoints);

                    int highOrderValue = 0;
                    int lowOrderValue = 1;

                    float value = ModbusUtility.GetSingle(registers[highOrderValue], registers[lowOrderValue]);
                    ropeValue.FlowSpeedValue = value;
                }

                Statistic.TotalRequests++;

                FlowSpeedRead = true;

            }
            catch (System.IO.IOException)
            {
                foreach (var ropeValue in ropeValues)
                {
                    ropeValue.FlowSpeedValue = 0;
                }

                Statistic.TotalWrongRequests++;
                Statistic.TotalChecksumWrongRequests++;

            }
            catch (Exception)
            {
                foreach (var ropeValue in ropeValues)
                {
                    ropeValue.FlowSpeedValue = 0;
                }

                Statistic.TotalWrongRequests++;
                Statistic.TotalTimeoutWrongRequests++;
            }

            foreach (var ropeValue in ropeValues)
            {
                ushort address = measurer.Ropes.Find(a => a.Name.Equals(ropeValue.Name)).FlowSpeedAddress;
                System.Console.WriteLine(string.Format("Cuerda {0} - Velocidad de Flujo       ({1}): {2}", ropeValue.Name, address, ropeValue.FlowSpeedValue));
            }

            Thread.Sleep(100);
        }

        private void doGetSoundSpeedTh(object obj)
        {
            List<RopeValue> ropeValues = (List<RopeValue>)obj;

            try
            {
                ropeValues = ropeValues.OrderBy(r => r.Name).ToList();

                foreach (var ropeValue in ropeValues)
                {
                    ushort address = measurer.Ropes.Find(a => a.Name.Equals(ropeValue.Name)).SoundSpeedAddress;

                    ushort numberOfPoints = 2;
                    ushort[] registers = sensor.Master.ReadHoldingRegisters(sensor.SlaveId, address, numberOfPoints);

                    int highOrderValue = 0;
                    int lowOrderValue = 1;

                    float value = ModbusUtility.GetSingle(registers[highOrderValue], registers[lowOrderValue]);
                    ropeValue.SoundSpeedValue = value;
                }

                Statistic.TotalRequests++;

                SoundSpeedRead = true;

            }
            catch (System.IO.IOException)
            {
                foreach (var ropeValue in ropeValues)
                {
                    ropeValue.SoundSpeedValue = 0;
                }

                Statistic.TotalWrongRequests++;
                Statistic.TotalChecksumWrongRequests++;

            }
            catch (Exception e)
            {
                foreach (var ropeValue in ropeValues)
                {
                    ropeValue.SoundSpeedValue = 0;
                }

                Statistic.TotalWrongRequests++;
                Statistic.TotalTimeoutWrongRequests++;
            }

            foreach (var ropeValue in ropeValues)
            {
                ushort address = measurer.Ropes.Find(a => a.Name.Equals(ropeValue.Name)).SoundSpeedAddress;
                System.Console.WriteLine(string.Format("Cuerda {0} - Velocidad de Sonido       ({1}): {2}", ropeValue.Name, address, ropeValue.SoundSpeedValue));
            }

            Thread.Sleep(100);
        }

        private void doGetRopeGainTh(object obj)
        {
            RopeValue ropeValue = (RopeValue)obj;

            try
            {
                ropeValue.GainValues = measurer.CalculateRopeGain(ropeValue.Name, sensor.Master, sensor.SlaveId);

                System.Console.WriteLine(string.Format("Cuerda {0} - Ganancia de la cuerda T1: {1}", ropeValue.Name, ropeValue.GainValues.T1));
                System.Console.WriteLine(string.Format("Cuerda {0} - Ganancia de la cuerda T2: {1}", ropeValue.Name, ropeValue.GainValues.T2));

                Statistic.TotalRequests++;

                RopeGainRead = true;
            }
            catch (System.IO.IOException e)
            {
                ropeValue.GainValues = new GainValue();
                Statistic.TotalWrongRequests++;
                Statistic.TotalTimeoutWrongRequests++;
                
                Console.WriteLine(e.Message);
            }
            catch (Exception e)
            {
                ropeValue.GainValues = new GainValue();
                Statistic.TotalWrongRequests++;
                Statistic.TotalTimeoutWrongRequests++;

                Console.WriteLine(e.Message);
            }

            Thread.Sleep(100);
        }

        private void doGetRopeEfficiencyTh(object obj)
        {
            RopeValue ropeValue = (RopeValue)obj;

            try
            {
                ropeValue.EfficiencyValue = (int)Math.Abs(measurer.CalculateRopeEfficiency(ropeValue.Name, sensor.Master, sensor.SlaveId));

                Statistic.TotalRequests++;

                RopeEfficiencyRead = true;
            }
            catch (System.IO.IOException e)
            {
                ropeValue.EfficiencyValue = 0;
                Statistic.TotalWrongRequests++;
                Statistic.TotalChecksumWrongRequests++;

                Console.WriteLine(e.Message);
            }
            catch (Exception e) 
            {
                ropeValue.EfficiencyValue = 0;
                Statistic.TotalWrongRequests++;
                Statistic.TotalTimeoutWrongRequests++;

                Console.WriteLine(e.Message);
            }

            System.Console.WriteLine(string.Format("Cuerda {0} - Eficiencia de la cuerda: {1}", ropeValue.Name, ropeValue.EfficiencyValue));

            Thread.Sleep(100);
        }
    }

    public class UltrasonicValue
    {
        public List<RopeValue> Ropes { get; set; }

        public UltrasonicValue()
        {
            Ropes = new List<RopeValue>();
        }
    }

}
