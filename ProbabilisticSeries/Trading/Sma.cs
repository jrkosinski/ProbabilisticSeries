using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;

namespace Trading.Utilities.Indicators
{
    public class Sma : MovingAverage
    {
        public Sma(int length)
            : base(length, false)
        {
        }
    }
}
