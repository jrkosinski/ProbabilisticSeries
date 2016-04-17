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
    class TestTradingSystem : ITradingSystem
    {
        private DataFeatureDefinition _definition;
        private int _holdPeriod = 0;

        public bool[] BuySignal { get; private set; }

        public bool[] SellSignal { get; private set; }

        public bool[] CloseoutSignal { get; private set; }


        public TestTradingSystem(DataFeatureDefinition definition, int holdPeriod)
        {
            _definition = definition;
            _holdPeriod = holdPeriod;
        }

        public void Calculate(DataSet dataSet)
        {
            this.BuySignal = new bool[dataSet.Rows.Length];
            this.SellSignal = new bool[dataSet.Rows.Length];
            this.CloseoutSignal = new bool[dataSet.Rows.Length];

            DataSetWithIndicators ds = new DataSetWithIndicators(dataSet, _definition.GetUniqueIndicators().ToArray());

            var outputs = _definition.GetAllOutputs();

            int buyIndex = -1;
            for (int n = 0; n < dataSet.Rows.Length; n++)
            {
                var row = dataSet.Rows[n];

                if (buyIndex >= 0)
                {
                    //TODO: this must not be hard-coded
                    //if (n >= buyIndex + _holdPeriod)
                    if (((dataSet.Open[buyIndex+1] - dataSet.Close[n]) / dataSet.Open[buyIndex]) >= 0.03)
                        CloseoutSignal[n] = true;

                    if (CloseoutSignal[n])
                        buyIndex = -1;
                }
                else
                {
                    bool isMatch = true;
                    foreach (var predictor in _definition.Predictors)
                    {
                        if (predictor.Value.Length > (n + 1))
                        {
                            isMatch = false;
                            break;
                        }

                        for (int i = 0; i < predictor.Value.Length; i++)
                        {
                            var thisIndex = ((n - predictor.Value.Length) + i) + 1;
                            if (predictor.Value[i] != outputs[predictor.Key][thisIndex])
                            {
                                isMatch = false;
                                break;
                            }
                        }
                    }

                    if (isMatch)
                    {
                        buyIndex = n;
                        BuySignal[n] = true;
                    }
                }
            }
        }
    }
}
