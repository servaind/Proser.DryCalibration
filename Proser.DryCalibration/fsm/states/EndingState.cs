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
    public class EndingState : IState
    {
        public event ExitStateHandler ExitState;
        public event Action<string> RefreshState;

        public CancellationTokenSource token { get; private set; }
        public FSMState Name { get; private set; }
        public string Description { get; private set; }

        public EndingState()
        {
            token = new CancellationTokenSource();
            this.Name = FSMState.ENDING;
            this.Description = "El ensayo finalizó correctamente.";
        }

        public void Execute()
        {
            Thread th = new Thread(new ThreadStart(excecuteTh));
            th.Start();

            th.Join();

            if (ExitState != null)
            {
                ExitState(FSMState.REPOSE);
            }
        }

        private void excecuteTh()
        {
            Thread.Sleep(1000);
        }

        public void Dispose()
        {
            token.Cancel();
        }
    }
}
