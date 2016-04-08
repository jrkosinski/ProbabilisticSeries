using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;

namespace Trading.Utilities.Indicators
{
    public class MacdVxi : IIndicator
    {
        public int FastLength { get; private set; }

        public int SlowLength { get; private set; }

        public int SignalLength { get; private set; }

        public bool[] Bullish { get; private set; }

        public bool[] Cross { get; private set; }


        public MacdVxi(int fastLength = 13, int slowLength = 21, int signalLength = 8)
        {
            this.FastLength = fastLength;
            this.SlowLength = slowLength;
            this.SignalLength = signalLength;
            this.Bullish = new bool[0];
            this.Cross = new bool[0];
        }

        public void Calculate(double[] data)
        {
            this.Bullish = new bool[data.Length];
            this.Cross = new bool[data.Length];

            Ema fastEma = new Ema(this.FastLength);
            Ema slowEma = new Ema(this.SlowLength);

            fastEma.Calculate(data);
            slowEma.Calculate(data);

            double[] macd = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
                macd[i] = (fastEma.Output[i] - slowEma.Output[i]);

            Sma signal = new Sma(this.SignalLength);
            signal.Calculate(macd);

            for (int i = 0; i < data.Length; i++)
            {
                this.Bullish[i] = (signal.Output[i] < macd[i]);
                if (i > 1)
                {
                    this.Cross[i] = (this.Bullish[i - 1] != this.Bullish[i]);
                }
            }
        }

        public void Calculate(DataSet data)
        {
            this.Calculate(data.Close);
        }

        public string GetOutputString(int index)
        {
            return String.Format("{0},{1}", this.Bullish[index].ToString(), this.Cross[index].ToString());
        }
    }
}
