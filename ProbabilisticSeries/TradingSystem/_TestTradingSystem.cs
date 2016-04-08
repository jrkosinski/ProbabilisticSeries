using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;
using Trading.Utilities.Indicators;

namespace Trading.Utilities.TradingSystems
{
    public class TestTradingSystem : ITradingSystem
    {
        private int _shortMaLen;
        private int _longMaLen;
        private int _trendLen;
        private int _breakoutLen; 

        public bool[] BuySignal { get; private set; }

        public bool[] SellSignal { get; private set; }

        public bool[] CloseoutSignal { get; private set; }


        public TestTradingSystem(int shortMaLen, int longMaLen, int trendLen, int breakoutLen)
        {
            _shortMaLen = shortMaLen;
            _longMaLen = longMaLen;
            _trendLen = trendLen;
            _breakoutLen = breakoutLen;
        }

        public void Calculate(DataSet dataSet)
        {
            this.BuySignal = new bool[dataSet.Rows.Length];
            this.SellSignal = new bool[dataSet.Rows.Length];
            this.CloseoutSignal = new bool[dataSet.Rows.Length];

            int[] isUp = new int[dataSet.Rows.Length];
            int[] X = new int[dataSet.Rows.Length];

            for (int n = 0; n < dataSet.Rows.Length; n++)
            {
                var row = dataSet.Rows[n];
                isUp[n] = (row.Close > row.Open) ? 1 : 0;
            }

            List<IIndicator> indicators = new List<IIndicator>();

            indicators.Add(new Ema(_shortMaLen));
            indicators.Add(new Ema(_longMaLen));

            DataSetWithIndicators ds = new DataSetWithIndicators(dataSet, indicators.ToArray());

            int[] trendIsUp = new int[dataSet.Rows.Length];
            int[] trendIsDn = new int[dataSet.Rows.Length];
            int trendUpCount = 0;
            int trendDnCount = 0;

            for (int n = 0; n < ds.Data.Rows.Length; n++)
            {
                trendIsUp[n] = 0;
                trendIsDn[n] = 0;
                if (n >= 1)
                {
                    var ma1 = (ds.Indicators[0] as MovingAverage);
                    var ma2 = (ds.Indicators[1] as MovingAverage);

                    if (ma1.Output[n] > ma1.Output[n - 1] && ma2.Output[n] > ma2.Output[n - 1])
                    {
                        trendUpCount++;
                        trendDnCount = 0;
                    }
                    else
                    {
                        trendUpCount = 0;

                        if (ma1.Output[n] < ma1.Output[n - 1] && ma2.Output[n] < ma2.Output[n - 1])
                            trendDnCount++;
                        else
                            trendDnCount = 0;
                    }
                }

                trendIsUp[n] = (trendUpCount >= _trendLen) ? 1 : 0;
                trendIsDn[n] = (trendDnCount >= _trendLen) ? 1 : 0;
            }

            int buyIndex = -1; 
            for (int n = 0; n < dataSet.Rows.Length; n++)
            {
                var row = dataSet.Rows[n];

                if (buyIndex >= 0)
                {
                    if (row.Low < dataSet.Rows[buyIndex].Low || row.Close < dataSet.Rows[buyIndex].Close)
                        CloseoutSignal[n] = true;
                    else if (n >= buyIndex + _breakoutLen)
                        CloseoutSignal[n] = true;

                    if (CloseoutSignal[n])
                        buyIndex = -1;
                }
                else
                {
                    //if (n > 0 && isUp[n] == 0 && isUp[n - 1] == 1)
                    if (n > _trendLen && trendIsDn[n] == 1)
                    {
                        this.BuySignal[n] = true;
                        buyIndex = n;
                    }
                }
            }
        }
    }
}
