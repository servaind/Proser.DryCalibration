using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Proser.DryCalibration.controller.data.interfaces
{
    public interface IDataBase
    {
        void GetConnection();
        bool ExecuteQuery(string query);
    }
}
