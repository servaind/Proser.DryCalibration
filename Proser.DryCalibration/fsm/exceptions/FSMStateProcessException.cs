using System;

namespace Proser.DryCalibration.fsm.exceptions
{
    public class FSMStateProcessException : Exception
    {
        public FSMStateProcessException() { }
     
        public FSMStateProcessException(string message)
             : base(message)
        {

        }
    }
}
