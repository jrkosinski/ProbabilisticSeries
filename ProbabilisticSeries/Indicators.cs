using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading;
using Trading.Utilities;
using Trading.Utilities.Data;
using Trading.Utilities.Indicators;
using Trading.Utilities.TradingSystems;


namespace ProbabilisticSeries
{
    class UpDownIndicator : IIndicator
    {
        public int[] Output { get; private set; }

        public void Calculate(DataSet dataSet)
        {
            this.Output = new int[dataSet.Count];

            for (int n = 0; n < dataSet.Count; n++)
            {
                var row = dataSet.Rows[n];
                this.Output[n] = (row.Close > row.Open) ? 1 : 0;
            }
        }

        public void Calculate(double[] data)
        {
            throw new NotImplementedException();
        }

        public string GetOutputString(int index)
        {
            return this.Output[index].ToString();
        }
    }

    class TrendIndicator : IIndicator
    {
        private Ema _shortMa, _longMa;
        private int _trendLen;

        public int[] TrendUp { get; private set; }

        public int[] ShortMaOverLong { get; private set; }

        public int[] NewHigh { get; private set; }


        public TrendIndicator(int shortMaLen, int longMaLen, int trendLen)
        {
            _shortMa = new Ema(shortMaLen);
            _longMa = new Ema(longMaLen);
            _trendLen = trendLen;
        }

        public void Calculate(DataSet dataSet)
        {
            this.Calculate(dataSet.Close);
        }

        public void Calculate(double[] data)
        {
            this.TrendUp = new int[data.Length];
            this.NewHigh = new int[data.Length];
            this.ShortMaOverLong = new int[data.Length];
            int trendUpCount = 0;
            int trendDnCount = 0;
            double highSoFar = 0;
            double lowSoFar = 0;

            _shortMa.Calculate(data);
            _longMa.Calculate(data);

            for (int n = 0; n < data.Length; n++)
            {
                this.TrendUp[n] = 0;
                this.NewHigh[n] = 0;


                if (n >= 1)
                {
                    if (_shortMa.Output[n] > _shortMa.Output[n - 1] && _longMa.Output[n] > _longMa.Output[n - 1])
                    {
                        trendUpCount++;
                        trendDnCount = 0;
                    }
                    else
                    {
                        trendUpCount = 0;

                        if (_shortMa.Output[n] < _shortMa.Output[n - 1] && _longMa.Output[n] < _longMa.Output[n - 1])
                            trendDnCount++;
                        else
                            trendDnCount = 0;
                    }

                    if (data[n] > highSoFar)
                    {
                        highSoFar = data[n];
                        NewHigh[n] = 1;
                    }

                    if (data[n] < lowSoFar)
                    {
                        lowSoFar = data[n];
                        NewHigh[n] = -1;
                    }
                }
                else
                {
                    highSoFar = data[n];
                    lowSoFar = data[n];
                }

                this.ShortMaOverLong[n] = (_shortMa.Output[n] > _longMa.Output[n]) ? 1 : 0;
                this.ShortMaOverLong[n] = (_shortMa.Output[n] < _longMa.Output[n]) ? -1 : 0;
                this.TrendUp[n] = (trendUpCount >= _trendLen) ? 1 : 0;
                this.TrendUp[n] = (trendDnCount >= _trendLen) ? -1 : 0;
            }
        }

        public string GetOutputString(int index)
        {
            return String.Format("{0}{1}", this.TrendUp[index].ToString());
        }
    }

    //goes up by at least N% without going below today's low
    class BreakoutIndicator1 : IIndicator
    {
        private int _breakoutLen;

        public int[] Output { get; private set; }

        public BreakoutIndicator1(int breakoutLen)
        {
            _breakoutLen = breakoutLen;
        }

        //TODO: optimize 
        public void Calculate(DataSet dataSet)
        {
            this.Output = new int[dataSet.Count];

            for (int n = 0; n < dataSet.Count; n++)
            {
                if (n < (dataSet.Count - _breakoutLen))
                {
                    this.Output[n] = 1;

                    for (int i = n + 1; i < n + _breakoutLen + 1; i++)
                    {
                        if (dataSet.Rows[i].Low < dataSet.Rows[n + 1].Low)
                        {
                            this.Output[n] = 0;
                            break;
                        }

                        if (dataSet.Rows[i].Close < dataSet.Rows[i - 1].Close)
                        {
                            this.Output[n] = 0;
                            break;
                        }
                    }
                }
            }
        }

        public void Calculate(double[] data)
        {
            throw new NotImplementedException();
        }

        public string GetOutputString(int index)
        {
            return this.Output[index].ToString();
        }
    }

    class BreakoutIndicator2 : IIndicator
    {
        private int _breakoutLen;

        public int[] Output { get; private set; }

        public BreakoutIndicator2(int breakoutLen)
        {
            _breakoutLen = breakoutLen;
        }

        //TODO: optimize 
        public void Calculate(DataSet dataSet)
        {
            this.Output = new int[dataSet.Count];

            for (int n = 0; n < dataSet.Count; n++)
            {
                if (n < (dataSet.Count - 1))
                {
                    this.Output[n] = 1;

                    double start = dataSet.Rows[n + 1].Open;

                    for (int i = n + 1; i < dataSet.Count; i++)
                    {
                        if (dataSet.Rows[i].Low < dataSet.Rows[n + 1].Low)
                        {
                            this.Output[n] = 0;
                            break;
                        }

                        if (((start - dataSet.Close[i]) / start) >= 0.01)
                        {
                            this.Output[n] = 1;
                            break;
                        }
                    }
                }
            }
        }

        public void Calculate(double[] data)
        {
            throw new NotImplementedException();
        }

        public string GetOutputString(int index)
        {
            return this.Output[index].ToString();
        }
    }

    class BreakoutIndicator3 : IIndicator
    {
        private int _breakoutLen;

        public int[] Output { get; private set; }

        public BreakoutIndicator3(int breakoutLen)
        {
            _breakoutLen = breakoutLen;
        }

        //TODO: optimize 
        public void Calculate(DataSet dataSet)
        {
            this.Output = new int[dataSet.Count];

            for (int n = 0; n < dataSet.Count; n++)
            {
                if (n < (dataSet.Count - 1))
                {
                    this.Output[n] = 1;

                    double start = dataSet.Rows[n + 1].Open;
                    double maxDrawdown = start - (start * 0.01);

                    for (int i = n + 1; i < dataSet.Count; i++)
                    {
                        if (dataSet.Rows[i].Close < maxDrawdown)
                        {
                            this.Output[n] = 0;
                            break;
                        }

                        if (((start - dataSet.Close[i]) / start) >= 0.03)
                        {
                            this.Output[n] = 1;
                            break;
                        }
                    }
                }
            }
        }

        public void Calculate(double[] data)
        {
            throw new NotImplementedException();
        }

        public string GetOutputString(int index)
        {
            return this.Output[index].ToString();
        }
    }
}
