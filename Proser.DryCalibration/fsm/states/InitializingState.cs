using Proser.DryCalibration.controller.interfaces;
using Proser.DryCalibration.controller.pressure;
using Proser.DryCalibration.controller.rtd;
using Proser.DryCalibration.controller.ultrasonic;
using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.fsm.interfaces;
using Proser.DryCalibration.monitor.exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proser.DryCalibration.fsm.states
{
    public class InitializingState : IState
    {
        public event ExitStateHandler ExitState;
        public event Action<string> RefreshState;

        private IController rtdController;
        private IController pressureController;

        public CancellationTokenSource token { get; private set; }
        public FSMState Name { get; private set; }
        public string Description { get; private set; }
        bool initialized;

        public InitializingState()
        {
            this.Name = FSMState.INITIALIZING;
            this.Description = "Preparando el ensayo...";
        }

        public InitializingState(IController rtdController, IController pressureController)
            : this()
        {
            this.rtdController = rtdController;
            this.pressureController = pressureController;
        }

        public void Execute()
        {
            initialized = false;

            try
            {
                rtdController.Initialize();
                rtdController.UpdateSensorReceived += RtdController_UpdateSensorReceived;
            }
            catch (MonitorInitializationException e)
            {
                RefreshState?.Invoke("Ocurrió un error al iniciar el monitor de temperatura.");
                ExitState?.Invoke(FSMState.ERROR);
                return;
            }

            try
            {
                pressureController.Initialize();
            }
            catch (MonitorInitializationException e)
            {
                RefreshState?.Invoke("Ocurrió un error al iniciar el monitor de presión.");
                ExitState?.Invoke(FSMState.ERROR);
                return;
            }

            do
            {
                RefreshState?.Invoke("Preparando el ensayo...");

            } while (!initialized);

            ExitState?.Invoke(FSMState.INITIALIZED); // transicion

        }

        private void RtdController_UpdateSensorReceived(monitor.enums.MonitorType monitorType, object value)
        {
            if (monitorType == monitor.enums.MonitorType.RTD)
            {
                rtdController.UpdateSensorReceived -= RtdController_UpdateSensorReceived;
                initialized = true;
            }
        }

        public void Dispose()
        {
           
        }
    }
}
