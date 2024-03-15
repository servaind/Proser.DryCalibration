using Proser.DryCalibration.controller.interfaces;
using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.fsm.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proser.DryCalibration.fsm.states
{
    public class ReadyState : IState
    {
        public event ExitStateHandler ExitState;
        public event Action<string> RefreshState;

        private IController rtdController;
        private IController pressureController;

        public CancellationTokenSource token { get; private set; }
        public FSMState Name { get; private set; }
        public string Description { get; private set; }

        public ReadyState()
        {
            this.Name = FSMState.INITIALIZED;
            this.Description = "Listo.";
        }

        public ReadyState(IController rtdController, IController pressureController)
            : this()
        {

            this.rtdController = rtdController;
            this.pressureController = pressureController;
        }

        public void Execute()
        {
            //estado de reposo.
            Thread.Sleep(200);
        }

        public void Dispose()
        {

        }
    }
}
