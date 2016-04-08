using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;

namespace Trading.Utilities.Indicators
{
    public class MovingAverage : IIndicator
    {
        public int Length { get; private set; }

        public double[] Output { get; private set; }

        public bool Exponential { get; private set; }


        public MovingAverage(int length, bool exponential)
        {
            if (length < 1)
                length = 1;

            this.Length = length;
            this.Output = new double[0];
            this.Exponential = exponential;
        }

        public void Calculate(DataSet data)
        {
            this.Calculate(data.Close);
        }

        public void Calculate(double[] data)
        {
            this.Output = new double[data.Length];

            for (int i = 0; i < this.Output.Length; i++)
            {
                int start = (i - this.Length);
                if (start < 0)
                    start = 0;

                if (this.Exponential)
                {
                    double emaPrev = 0;
                    if (i > 0)
                        emaPrev = this.Output[i - 1];

                    //Multiplier: (2 / (Time periods + 1) ) = (2 / (10 + 1) ) = 0.1818 (18.18%)
                    double multiplier = (2.0 / (this.Length + 1));

                    this.Output[i] = data[i] * multiplier + emaPrev * (1.0 - multiplier);
                }
                else
                {
                    double sum = 0;
                    double len = (i - start) + 1;
                    for (int n = start; n <= i; n++)
                    {
                        sum += data[n];
                    }

                    this.Output[i] = (sum / (double)len);
                }
            }
        }

        public string GetOutputString(int index)
        {
            return this.Output[index].ToString();
        }
    }
}
