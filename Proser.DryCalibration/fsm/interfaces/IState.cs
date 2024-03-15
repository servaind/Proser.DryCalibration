using Proser.DryCalibration.fsm.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Proser.DryCalibration.fsm.interfaces
{
    public delegate void ExitStateHandler(FSMState next);

    public interface IState : IDisposable
    {
        event Action<string> RefreshState;
        event ExitStateHandler ExitState;

        CancellationTokenSource token { get; }
        FSMState Name { get; }
        string Description { get; }
        void Execute();
    }
}
