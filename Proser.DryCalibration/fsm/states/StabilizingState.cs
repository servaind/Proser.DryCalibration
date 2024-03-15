using Proser.DryCalibration.controller.interfaces;
using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.fsm.interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace Proser.DryCalibration.fsm.states
{
    public class StabilizingState : IState, ITimerControl
    {
        private const int ESTABILIZED_WAIT_INTERVAL = 5000; // tiempo de espera cuando la temperatura esta estable

        public event ExitStateHandler ExitState;
        public event Action<string> RefreshState;
        public event Action<string> ElapsedTimeControl;

        private IController rtdController;
        private IController pressureController;
        private bool WaitElapsed;

        public CancellationTokenSource token { get; private set; }
        public FSMState Name { get; private set; }
        public string Description { get; private set; }
        public System.Timers.Timer TimerControl { get; set; }

        public StabilizingState()
        {
            this.Name = FSMState.STABILIZING;
            this.Description = "Esperando estabilidad térmica...";
        }

        public StabilizingState(IController rtdController, IController pressureController)
            : this()
        {
            this.rtdController = rtdController;
            this.pressureController = pressureController;
            TimerControl = new System.Timers.Timer(ESTABILIZED_WAIT_INTERVAL);
            TimerControl.Elapsed += TimerControl_Elapsed;
        }


        public void Execute()
        {
            token = new CancellationTokenSource();

            Thread th = new Thread(new ThreadStart(excecuteTh));
            th.Start();
        }

        private void excecuteTh()
        {
            bool isStable = false;
          
            do
            {
                isStable = rtdController.Monitor.IsStable;

                if (isStable)
                {
                    Thread.Sleep(1000);
                    RefreshState?.Invoke("Temperatura estable.");
                    InitTimerControl(); // inicio tiempo de espera
                    break;
                }

                Thread.Sleep(200);

            } while (!token.IsCancellationRequested);  
        }

        public void Dispose()
        {
            token.Cancel();
            TimerControl.Close();
            TimerControl.Dispose();
        }

        public void InitTimerControl()
        {
            try
            {
                TimerControl.Start();
            }
            catch
            {
                // log
              
            }      
        }

        public void StopTimerControl()
        {
            TimerControl.Enabled = false;
        }

        private void TimerControl_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimerControl.Elapsed -= TimerControl_Elapsed;
            StopTimerControl();

            ExitState?.Invoke(FSMState.OBTAINING_SAMPLES);
        }


    }
}
