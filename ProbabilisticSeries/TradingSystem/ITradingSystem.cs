using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;
using Trading.Utilities.Indicators;

namespace Trading.Utilities.TradingSystems
{
    public interface ITradingSystem
    {
        bool[] BuySignal { get; }

        bool[] SellSignal { get; }

        bool[] CloseoutSignal { get; }


        void Calculate(DataSet data);
    }

    public class TradingResults
    {
        public double PercentGain { get; set; }

        public double MaxDrawdown { get; set; }

        public double AverageDrawdown { get; set; }
    }
}
