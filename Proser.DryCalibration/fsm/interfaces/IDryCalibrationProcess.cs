using Proser.DryCalibration.fsm.enums;
using Proser.DryCalibration.monitor.enums;
using Proser.DryCalibration.monitor.statistic;
using Proser.DryCalibration.sensor.ultrasonic.enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.fsm.interfaces
{

    public delegate void RefreshStateHandler(string description);

    public interface IDryCalibrationProcess
    {
        event Action<MonitorType, StatisticValue> StatisticReceived;
        event Action<MonitorType, object> UpdateSensorReceived;
        event Action<string> RefreshState;
        event Action<FSMState, UltrasonicModel> DryCalibrationStateChange;
        event Action DryCalibrationAborted;

        IState CurrentState { get; }

        void Initialize();

        void InitDryCalibration();

        void CancelDryCalibration();

        void AbortDryCalibration();
    }
}
