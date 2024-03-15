using System;

namespace Proser.DryCalibration.fsm.interfaces
{
    public interface ITimerControl : IDisposable
    {
        event Action<string> ElapsedTimeControl;
        System.Timers.Timer TimerControl { get; set; }
        void InitTimerControl();
        void StopTimerControl();
    }
}