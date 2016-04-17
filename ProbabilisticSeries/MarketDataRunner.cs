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
    class MarketDataRunner
    {
        private ProbabilityRunner _probabilityRunner = new ProbabilityRunner();

        public int SeriesMaxLen = 3;


        public void Run(List<DataSet> dataSets, DataFeatureDefinition testData)
        {
            List<DiscreteFeaturesForDataSet> discreteFeatures = new List<DiscreteFeaturesForDataSet>();
            List<string> keys = new List<string>();

            //[1] calculate the features for each dataset 
            foreach (var dataSet in dataSets)
            {
                //calculate all indicators 
                var indicators = testData.GetUniqueIndicators();
                foreach (var ind in indicators)
                    ind.Calculate(dataSet);

                testData.GoalIndicator.Indicator.Calculate(dataSet);


                //extract the outputs 
                List<int[]> features = new List<int[]>();
                foreach (var ind in testData.Indicators)
                {
                    if (!keys.Contains(ind.Key))
                        keys.Add(ind.Key);
                    features.Add(ind.Value.Output);
                }

                discreteFeatures.Add(new DiscreteFeaturesForDataSet() { DataSet = dataSet, Features = features, Goals = testData.GoalIndicator.Output });
            }

            _probabilityRunner.Run(keys, discreteFeatures, SeriesMaxLen);
        }

        public List<ProbabilityVector> SelectVectors(int minFrequency, double minProbability)
        {
            return _probabilityRunner.SelectVectors(minFrequency, minProbability);
        }
    }

    class DiscreteFeaturesForDataSet
    {
        public DataSet DataSet { get; set; }

        public List<int[]> Features { get; set; }

        public int[] Goals { get; set; }
    }

    class DataFeatureDefinition
    {
        public Dictionary<string, IndicatorOutput> Indicators { get; private set; }

        public IndicatorOutput GoalIndicator { get; set; }

        public Dictionary<string, int[]> Predictors { get; set; }

        public List<IIndicator> GetUniqueIndicators()
        {
            List<IIndicator> output = new List<IIndicator>();

            foreach (var value in Indicators)
            {
                if (!output.Contains(value.Value.Indicator))
                {
                    output.Add(value.Value.Indicator);
                }
            }

            return output;
        }

        public Dictionary<string, int[]> GetAllOutputs()
        {
            Dictionary<string, int[]> outputs = new Dictionary<string, int[]>();

            foreach (var i in Indicators)
            {
                outputs.Add(i.Key, i.Value.Output);
            }

            return outputs;
        }


        public DataFeatureDefinition()
        {
            this.Indicators = new Dictionary<string, IndicatorOutput>();
            this.Predictors = new Dictionary<string, int[]>();
        }
    }

    class IndicatorOutput
    {
        public IIndicator Indicator { get; set; }

        public Func<IIndicator, int[]> OutputProperty { get; set; }

        public int[] Output
        {
            get
            {
                if (this.Indicator != null && this.OutputProperty != null)
                    return this.OutputProperty(this.Indicator);

                return new int[0];
            }
        }
    }
}
