using Proser.DryCalibration.aga10;
using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.fsm.interfaces;
using Proser.DryCalibration.sensor.pressure.calibration;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using Proser.DryCalibration.sensor.ultrasonic.modbus.configuration;
using Proser.DryCalibration.sensor.ultrasonic.modbus.maps;
using Proser.DryCalibration.util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proser.DryCalibration.fsm.states
{
    public class ValidatingState : IState
    {
        private const double TEMP_ERROR = 0.3;
        private const double TEMP_ENV_ERROR = 0.5;
        private const double FLOW_ERROR = 0.006;
        private const double EFFICIENCY_OK = 100;
        private const double PERCENT_ERROR = 0.2;
        private const double SOUND_ERROR = 0.5;

        private bool ContinueToNextState;
        private PressureCalibration pressureCalibration;
        private UltrasonicModel ultrasonicModel;

        public event ExitStateHandler ExitState;
        public event Action<string> RefreshState;
        public event Action ValidatingStateReady;
        public event Action<ValidatedResult> ValidationFinished;

        public CancellationTokenSource token { get; private set; }
        public FSMState Name { get; private set; }
        public string Description { get; private set; }

        
        public Sample Averages { get; private set; }

        public List<Sample> Samples { get; set; }

        public bool ValidationComplete { get; set; }

        public ValidatingState(Sample averages, PressureCalibration pressureCalibration, UltrasonicModel ultrasonicModel, List<Sample> Samples)
        {
            Averages = averages; // promedios de todas las muestras obtenidas
            token = new CancellationTokenSource();
            this.Name = FSMState.VALIDATING;
            this.Description = "Validando el ensayo...";
            this.pressureCalibration = pressureCalibration;
            this.ultrasonicModel = ultrasonicModel;
            this.Samples = Samples;
        }

        public void Execute()
        {
            ValidationComplete = false;
            ContinueToNextState = false;

            Thread th = new Thread(new ThreadStart(excecuteTh));
            th.Start();
        }

        public void ContinueToGeneratingReportState()
        {
            ContinueToNextState = true;
        }

        private void excecuteTh()
        {
            try
            {
                Thread.Sleep(1000); // transición
                ValidatingStateReady?.Invoke(); // detener monitores

                // iniciar validación
                ValidatedResult validationResult = new ValidatedResult();

                RefreshState?.Invoke("Calculando la velocidad del sonido teórica...");
                //log.Log.WriteIfExists("Calculando la velocidad del sonido teórica...");
                Thread.Sleep(1000);

                // cálculo da la velocidad del sonido teórica
                double theoreticaloundSpeed = CalculateTheoreticalSoundSpeed();
                validationResult.TheoreticalSoundSpeed = theoreticaloundSpeed;

                RefreshState?.Invoke("Validando estabilidad térmica...");
                //log.Log.WriteIfExists("Validando estabilidad térmica...");

                Thread.Sleep(1000);

                // valores de presión
                ValResValue resVal = new ValResValue()
                {
                    ValidationType = ValidationType.PressAVG,
                    Value = Averages.PressureValue,
                    Success = true // no se valida la presión
                };
                validationResult.Averages.Add(resVal);

                // estabilidad de temperatura
                resVal = ValidateTemperature();
                validationResult.Averages.Add(resVal);

                // temperatura ambiente
                resVal = ValidateEnvironmentTemperature();
                validationResult.Averages.Add(resVal);

                RefreshState?.Invoke("Validando velocidad de flujo por cuerda...");
                //log.Log.WriteIfExists("Validando velocidad de flujo por cuerda...");
                Thread.Sleep(1000);

                // velocidad de flujo por cuerda
                List<ValResValue> resVals = ValidateFlowSpeed();
                validationResult.Averages.AddRange(resVals);

                RefreshState?.Invoke("Validando error porcentual en la velocidad de sonido por cuerda...");
                ///log.Log.WriteIfExists("Validando error porcentual en la velocidad de sonido por cuerda...");
                Thread.Sleep(1000);

                // errores porcentuales en la velocidad del sonido
                List<ValPercentErrorValue> resPercVals = ValidateSoundSpeedPercentError(theoreticaloundSpeed);
                validationResult.PercentErrors.AddRange(resPercVals);

                RefreshState?.Invoke("Validando diferencia entre cuerdas en la velocidad del sonido...");
                //log.Log.WriteIfExists("Validando diferencia entre cuerdas en la velocidad del sonido...");
                Thread.Sleep(1000);

                // diferencia entre cuerdas en la velocidad del sonido
                validationResult.SoundDifference = ValidateSoundSpeedDifference();

                RefreshState?.Invoke("Validando porcentaje de pulsos por cuerda...");
                //log.Log.WriteIfExists("Validando porcentaje de pulsos por cuerda...");
                Thread.Sleep(1000);

                // porcentaje de pulsos por cuerda
                List<ValResValue> resEffVals = ValidateRopeEfficiency();
                validationResult.Averages.AddRange(resEffVals);

                // nivel de ganancia por cuerda
                List<ValResValue> resGainVals = ValidateRopeGain();
                validationResult.Averages.AddRange(resGainVals);

                // enviar resultados
                ValidationFinished?.Invoke(validationResult);
                RefreshState?.Invoke("Completando validación...");
                //log.Log.WriteIfExists("Completando validación...");

            }
            catch (Exception e)
            {
                ValidationComplete = false;
                //log.Log.WriteIfExists("Error: ValidatingSatate. ", e);
                RefreshState?.Invoke("Ocurrió un error el proceso de validación.");
            }
            
            do
            {    
                if (ValidationComplete)
                {
                    ValidationComplete = false;
                    RefreshState?.Invoke("Validación completa.");
                }

                if (ContinueToNextState)
                {
                    Thread.Sleep(1000);
                    ExitState?.Invoke(FSMState.GENERATING_REPORT);
                    break;
                }

                Thread.Sleep(200);
               
            } while (!token.IsCancellationRequested);

        }

        private ValSoundDifference ValidateSoundSpeedDifference()
        {
            // solo se tiene en cuenta para el calculo, las cuerdas con 100% de eficiencia.
            List<RopeValue> averages = Averages.Ropes.Where(w => w.EfficiencyValue.Equals(100)).ToList();

            ValSoundDifference diffResult = new ValSoundDifference() { ValidationType = ValidationType.SoundDiff, Success = false};

            if (averages.Count > 0)
            {
                double maxValue = averages.Max(r => r.SoundSpeedValue);
                double minValue = averages.Min(r => r.SoundSpeedValue);
                double difference = Math.Abs(maxValue - minValue);

                diffResult.Max = maxValue;
                diffResult.Min = minValue;
                diffResult.Value = difference;
                diffResult.Success = difference <= SOUND_ERROR;
            }
           
            return diffResult;
        }

        private List<ValPercentErrorValue> ValidateSoundSpeedPercentError(double theoreticaloundSpeed)
        {
            List<ValPercentErrorValue> resValues = new List<ValPercentErrorValue>();

            foreach (var rope in Averages.Ropes)
            {
                double soundSpeed = rope.SoundSpeedValue;
                double percentError = Math.Abs((soundSpeed - theoreticaloundSpeed) / theoreticaloundSpeed) * 100d;

                ValPercentErrorValue resValue = new ValPercentErrorValue()
                {
                    Name = rope.Name,
                    ValidationType = ValidationType.PercentErr,
                    Value = soundSpeed,
                    PercentError = percentError,
                    Success = percentError <= PERCENT_ERROR
                };

                resValues.Add(resValue);
            }

            return resValues;
        }

        private List<ValResValue> ValidateFlowSpeed()
        {
            List<ValResValue> resValues = new List<ValResValue>();

            foreach (var rope in Averages.Ropes)
            {
                double flowSpeed = rope.FlowSpeedValue;

                ValResValue resValue = new ValResValue()
                {
                    Name = rope.Name,
                    ValidationType = ValidationType.FlowAvg,
                    //Value = Math.Abs(flowSpeed),
                    Value = flowSpeed,
                    Success = Math.Abs(flowSpeed) <= FLOW_ERROR
                };

                resValues.Add(resValue);
            }

            return resValues;
        }

        private List<ValResValue> ValidateRopeEfficiency()
        {
            List<ValResValue> resValues = new List<ValResValue>();

            foreach (var rope in Averages.Ropes)
            {
                double effValue = rope.EfficiencyValue;

                ValResValue resValue = new ValResValue()
                {
                    Name = rope.Name,
                    ValidationType = ValidationType.EffAvg,
                    Value = effValue,
                    Success = effValue >= EFFICIENCY_OK
                };

                resValues.Add(resValue);
            }

            return resValues;
        }

        private List<ValResValue> ValidateRopeGain()
        {
            
            string path = Path.Combine(Utils.ConfigurationPath, "ModbusConfiguration.xml");
            ModbusConfiguration modBusConfg = ModbusConfiguration.Read(path);

            GainConfig gainConfig = modBusConfg.UltGainConfig.FirstOrDefault(u => u.UltModel.Equals(this.ultrasonicModel));

            List<ValResValue> resValues = new List<ValResValue>();

            foreach (var rope in Averages.Ropes)
            {
                double gainValue = rope.GainValue;

                ValResValue resValue = new ValResValue()
                {
                    Name = rope.Name,
                    ValidationType = ValidationType.GainAvg,
                    Value = gainValue,
                    Success = (gainConfig != null) ? (gainValue >= gainConfig.Min && gainValue <= gainConfig.Max) : false
                };

                resValues.Add(resValue);
            }

            return resValues;
        }

        private ValResValue ValidateTemperature()
        {
            var promedio = Averages.TemperatureDetail.Count();
            Console.WriteLine(promedio);

            ValTempDifference resValue = new ValTempDifference()
            {
                ValidationType = ValidationType.TempAVG,
                Value = Averages.CalibrationTemperature.Value,
                //TempDifference = Averages.CalibrationTemperature.Uncertainty,
                //Success = Averages.CalibrationTemperature.Uncertainty <= TEMP_ERROR
                TempDifference = Averages.CalibrationTemperature.Difference,
                Success = Averages.CalibrationTemperature.Difference <= TEMP_ERROR

            };
           
            return resValue;
        }

        private ValResValue ValidateEnvironmentTemperature()
        {
            ValTempDifference resValue = new ValTempDifference()
            {
                ValidationType = ValidationType.TempEnvAVG,
                Value = Averages.EnvirontmentTemperature.Value,
                //TempDifference = Averages.EnvirontmentTemperature.Uncertainty,
                //Success = Averages.EnvirontmentTemperature.Uncertainty <= TEMP_ENV_ERROR
                TempDifference = Averages.EnvirontmentTemperature.Difference,
                Success = Averages.EnvirontmentTemperature.Difference <= TEMP_ENV_ERROR
            };

            return resValue;
        }

        private double CalculateTheoreticalSoundSpeed()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");

            double pressure;
            double temperature;
            double soundSpeed;
            double atmosphericPressure;
            int pressureSensorType;

            try
            {
                pressureSensorType = this.pressureCalibration.SensorType;
                atmosphericPressure = Convert.ToDouble(this.pressureCalibration.AtmosphericPresssure);

                Aga10Calc calculo = new Aga10Calc();
                Console.WriteLine("Cargando cromatografía: 100% N2 para el ensayo");

                calculo.gas_comp.CH4 = 0;             // Metano
                calculo.gas_comp.N2 = 100;            // Nitrogeno
                calculo.gas_comp.CO2 = 0;             // Dioxido de carbono
                calculo.gas_comp.C2H6 = 0;            // Etano
                calculo.gas_comp.C3H8 = 0;            // Propano
                calculo.gas_comp.H2O = 0;             // Agua
                calculo.gas_comp.H2S = 0;             // Sulfhidrico
                calculo.gas_comp.H2 = 0;              // Hidrogeno
                calculo.gas_comp.CO = 0;              // Monoxido de carbono
                calculo.gas_comp.O2 = 0;              // Oxigeno
                calculo.gas_comp.iC4H10 = 0;          // iso Butano
                calculo.gas_comp.nC4H10 = 0;          // normal Butano
                calculo.gas_comp.iC5H12 = 0;          // iso Pentano
                calculo.gas_comp.nC5H12 = 0;          // normal Pentano
                calculo.gas_comp.C6H14 = 0;           // Hexano
                calculo.gas_comp.C7H16 = 0;           // Heptano
                calculo.gas_comp.C8H18 = 0;           // Octano
                calculo.gas_comp.C9H20 = 0;           // Nonano
                calculo.gas_comp.C10H22 = 0;          // Decano
                calculo.gas_comp.HE = 0;              // Helio
                calculo.gas_comp.AR = 0;              // Argon

                Console.WriteLine("Calculando AGA10: parámetros de Temperatura en ºC y Presión en bar");

                pressure = Averages.PressureValue; // promedio de la presión

                if (pressureSensorType == 1) // relativo
                {
                    pressure += atmosphericPressure;
                }

                temperature = Averages.CalibrationTemperature.Value;  // promedio de las temperaturas
                soundSpeed = calculo.calcVoS(temperature, pressure);  // velocidad del sonido en m/s
                
                Console.WriteLine("Vos=" + soundSpeed + " m/s");
            }
            catch (Exception e)
            {
                // log
                soundSpeed = 0;
            }

            return soundSpeed;
        }

        public void Dispose()
        {
            token.Cancel();
        }
    }

    public class ValidatedResult
    {
        public double TheoreticalSoundSpeed { get; set; }
        public List<ValResValue> Averages { get; set; }
        public List<ValPercentErrorValue> PercentErrors { get; set; }
        public ValSoundDifference SoundDifference { get; set; }

        public ValidatedResult()
        {
            Averages = new  List<ValResValue>();
            PercentErrors = new List<ValPercentErrorValue>();
        }
    }

    public class ValResValue
    {
        public string Name { get; set; }
        public ValidationType ValidationType { get; set; }
        public double Value { get; set; }
        public bool Success { get; set; }
    }

    public class ValPercentErrorValue : ValResValue
    {
        public double PercentError { get; set; }
    }

    public class ValSoundDifference : ValResValue
    {
        public double Max { get; set; }
        public double Min { get; set; }
    }

    public class ValTempDifference : ValResValue
    {
        public double TempDifference { get; set; }
    }

    public enum ValidationType
    {
        TempAVG = 1,
        TempEnvAVG,
        PressAVG,
        FlowAvg,
        EffAvg,
        PercentErr,
        SoundDiff, 
        GainAvg
    }

}
