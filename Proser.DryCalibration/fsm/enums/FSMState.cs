using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.fsm.enums
{
    public enum FSMState
    {
        ERROR,
        REPOSE,
        INITIALIZING,
        INITIALIZED,
        STABILIZING,
        OBTAINING_SAMPLES,
        VALIDATING,
        GENERATING_REPORT,
        ENDING
    }
}


/* -	Duración:(configurable). 
          -	Verificar siempre que las condiciones de estabilidad térmica se cumplan. 
          -	Leer temperaturas, presión, velocidad de sonido y de flujo por cuerda del medidor n veces durante el ensayo.
          -	Finalizado el periodo, calcular promedios de los valores obtenidos.
          -	Realizar cálculo de velocidad del sonido (Promedio de Temp y Presion).
          -	Verificar que:
          o	Lectura de flujo del medidor por cuerda sea próxima a cero. 
          o	La velocidad del sonido leída por las diferentes cuerdas sean similares. 
          o	La velocidad del sonido leída por cada cuerda no difiera de la velocidad del sonido teórica calculada. 
          El sistema genera un reporte en PDF con los resultados obtenidos y datos del medidor. 
          Se incluyen cada una de las mediciones y un resumen con el promedio de las 10 mediciones. 


 */
