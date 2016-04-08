using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;

namespace Trading.Utilities.Indicators
{
    public class MACross : IIndicator
    {
        public MovingAverage ShortMA { get; private set; }

        public MovingAverage LongMA { get; private set; }

        public bool[] Bullish { get; private set; }

        public bool[] Cross { get; private set; }


        public MACross(MovingAverage shortMA, MovingAverage longMA)
        {
            this.ShortMA = shortMA;
            this.LongMA = longMA;
            this.Bullish = new bool[0];
            this.Cross = new bool[0];
        }

        public void Calculate(DataSet data)
        {
            this.Calculate(data.Close);
        }

        public void Calculate(double[] data)
        {
            this.ShortMA.Calculate(data);
            this.LongMA.Calculate(data);

            this.Bullish = new bool[data.Length];
            this.Cross = new bool[data.Length];

            for (int i = 0; i < data.Length; i++)
            {
                this.Cross[i] = false;
                this.Bullish[i] = (ShortMA.Output[i] > LongMA.Output[i]);
                if (i > 0)
                {
                    if (this.Bullish[i] != this.Bullish[i - 1])
                        this.Cross[i] = true;
                }
            }
        }

        public string GetOutputString(int index)
        {
            return String.Format("{0},{1}", this.Bullish[index].ToString(), this.Cross[index].ToString());
        }
    }
}
