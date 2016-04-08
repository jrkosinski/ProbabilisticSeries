using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;

namespace Trading.Utilities.Indicators
{
    public interface IIndicator
    {
        void Calculate(DataSet data);

        void Calculate(double[] data);

        string GetOutputString(int index); 
    }
}
