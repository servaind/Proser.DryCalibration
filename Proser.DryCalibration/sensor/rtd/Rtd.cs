using Proser.DryCalibration.modules;
using Proser.DryCalibration.sensor.rtd.calibration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.sensor.rtd
{
    public class Rtd : IComparable<Rtd>
    {

        public int Number { get; private set; }
        public string AI { get; private set; }
        public int Module { get; private set; }
        public double TempValue { get; private set; }
        public double RealResValue { get; private set; }
        public List<ResPoint> ResPoints { get; set; }
        public bool Active { get; internal set; }
        public bool ValueObtained { get; set; }
        public double R0 { get; internal set; }

#if DEBUG

        public Rtd(int number, double value) // Unit Test
        {
            Number = number;
            TempValue = value;
            
        }

#endif

        public Rtd(int number, List<ResPoint> resPoints, double r0)
        {
            Number = number;
            ResPoints = resPoints;
            R0 = r0;

            // Cada módulo tiene 4 AI.
            Module = Number / NIDaqModule.AI_PER_MODULE;
            AI = "ai" + Number % NIDaqModule.AI_PER_MODULE;
        }

        public int CompareTo(Rtd other)
        {
            return Number.CompareTo(other.Number);
        }

        public override string ToString()
        {
            return String.Format("{0} - Value: {1}", Number, TempValue == null ? "-" : String.Format("{0:0.00}", TempValue));
        }

        public void Update(double? realResValue, List<TempPoint> tempPoints)
        {
            if (realResValue != null)
            {
                RealResValue = ((double)realResValue);
                TempValue = calculateCalibratedValue((double) realResValue, tempPoints); // valor calibrado
            }
            else
            {
                RealResValue = -99;
                TempValue = -99;
            }
        }

        private double calculateCalibratedValue(double realResValue, List<TempPoint> tempPoints)
        {
            double calibratedValue = -99;// condición de error

            try
            {
                double tempExtPoint1 = tempPoints.Find(m => m.Number == 1).Value;
                double tempExtPoint2 = tempPoints.Find(m => m.Number == 2).Value;
                double tempExtPoint3 = tempPoints.Find(m => m.Number == 3).Value;
                double tempExtPoint4 = tempPoints.Find(m => m.Number == 4).Value;
                double tempExtPoint5 = tempPoints.Find(m => m.Number == 5).Value;

                double resExtPoint1 = ResPoints.Find(m => m.Number == 1).Value;
                double resExtPoint2 = ResPoints.Find(m => m.Number == 2).Value;
                double resExtPoint3 = ResPoints.Find(m => m.Number == 3).Value;
                double resExtPoint4 = ResPoints.Find(m => m.Number == 4).Value;
                double resExtPoint5 = ResPoints.Find(m => m.Number == 5).Value;

                //valores inferiores al punto 2
                if (realResValue < resExtPoint2)
                {
                    calibratedValue = ((tempExtPoint2 - tempExtPoint1) / (resExtPoint2 - resExtPoint1)) * ( realResValue - resExtPoint1) + tempExtPoint1; 
                }

                //esta entre punto 2 y 3
                if (realResValue >= resExtPoint2 && realResValue < resExtPoint3)
                {
                    calibratedValue = ((tempExtPoint3 - tempExtPoint2) / (resExtPoint3 - resExtPoint2)) * (realResValue - resExtPoint2) + tempExtPoint2;
                }

                //esta entre punto 3 y 4
                if (realResValue >= resExtPoint3 && realResValue < resExtPoint4)
                {
                    calibratedValue = ((tempExtPoint4 - tempExtPoint3) / (resExtPoint4 - resExtPoint3)) * (realResValue - resExtPoint3) + tempExtPoint3;
                }

                //valores entre el punto 4 o superior
                if (realResValue >= resExtPoint4)
                {
                    calibratedValue = ((tempExtPoint5 - tempExtPoint4) / (resExtPoint5 - resExtPoint4)) * (realResValue - resExtPoint4) + tempExtPoint4;
                }
            }
            catch (Exception e)
            {
                //log
                
            }

            /*RTD(ajustado) = ((TempPunto2 - TempPunto1) / (ResPunto2 - ResPunto1)) *
            (ValorResistencia[Ohm] - ResPunto1) + TempPunto1;*/

            return calibratedValue;
        }

    }
}
