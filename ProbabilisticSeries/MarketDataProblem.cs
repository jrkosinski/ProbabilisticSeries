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


//#: write results out to a file as they come in 
//#: redesign this with more cohesive data structures (classes) instead of a bunch of separate lists/arrays
//#: represent the functions that get the features as indicators, so that they can be reused 
//#: represent the breakout selector as something dynamic and reusable 
//#: test the results by running a trading system from them dynamically
//#: encode as trinary (-1, 0, 1) to save processing 

//TODO: generify writeOutput;
//TODO: test carefully on very simple datasets that you can verify their correctness manually
//TODO: evaluate trading systems based on different factors including drawdown and win percentage
//TODO: try multithreading 
//TODO: use a stop order definition 
//TODO: optimize inside probabilityrunner
//TODO: organize the relationship between features & keys better 
//TODO: organize the code better 
//TODO: try ever more complex features
//TODO: optimize by pre-calculating EMAs etc. for each dataset (this will yield marginal improvement - better to optimize inside the function) 
//TODO: tighten up buy/sell/closeout logic & prices 
//TODO: optimize with tighter relationship between trader and trading system signals

namespace ProbabilisticSeries
{
    class MarketDataProblem
    {
        public const string OUTPUT_FILE = "output.txt"; 

        public static void Run()
        {
            //delete old log file 
            System.IO.File.Delete(OUTPUT_FILE);

            Dictionary<string, List<ProbabilityVector>> results = new Dictionary<string, List<ProbabilityVector>>();
            Random rand = new Random() ;
            ProbabilityVector bestSoFar = new ProbabilityVector();
            double bestRatingSoFar = 0;

            while (true)
            {
                try
                {
                    ProbabilityVector bestThisRound = new ProbabilityVector();
                    double bestRatingThisRound = 0;

                    var runner = new MarketDataRunner();

                    //set up the parameters 
                    int breakoutLength = 3; // rand.Next(2, 9);
                    int shortMaLen = rand.Next(7, 21);
                    int longMaLen = rand.Next(shortMaLen, 80);
                    int trendLen = rand.Next(2, 10);
                    runner.SeriesMaxLen = 3;

                    string key = String.Format("{0}|{1}|{2}|{3}", breakoutLength, shortMaLen, longMaLen, trendLen);

                    //read in the training data 
                    List<DataSet> dataSets = new List<DataSet>();
                    CsvDataReader reader = new CsvDataReader() { OpenIndex = 1, HighIndex = 2, LowIndex = 3, CloseIndex = 4, DateIndex = 0, VolumeIndex = 5, RowStartIndex = 1 };
                    dataSets.Add(reader.Read(@"e:\MarketData\t.csv"));
                    dataSets.Add(reader.Read(@"e:\MarketData\c.csv"));
                    dataSets.Add(reader.Read(@"e:\MarketData\bac.csv"));
                    dataSets.Add(reader.Read(@"e:\MarketData\f.csv"));
                    dataSets.Add(reader.Read(@"e:\MarketData\goog.csv"));
                    dataSets.Add(reader.Read(@"e:\MarketData\ge.csv"));
                    dataSets.Add(reader.Read(@"e:\MarketData\gs.csv"));
                    dataSets.Add(reader.Read(@"e:\MarketData\ibm.csv"));
                    dataSets.Add(reader.Read(@"e:\MarketData\jpm.csv"));
                    dataSets.Add(reader.Read(@"e:\MarketData\cat.csv"));


                    //TestTrader(dataSets, 3); 

                    //set up the indicators 
                    DataFeatureDefinition definition = new DataFeatureDefinition();
                    definition.GoalIndicator =
                        new IndicatorOutput()
                        {
                            Indicator = new BreakoutIndicator2(breakoutLength),
                            OutputProperty = (i) => { return ((BreakoutIndicator2)i).Output; }
                        };

                    definition.Indicators.Add("isUp",
                        new IndicatorOutput()
                        {
                            Indicator = new UpDownIndicator(),
                            OutputProperty = (i) => { return ((UpDownIndicator)i).Output; }
                        });

                    TrendIndicator trendIndicator = new TrendIndicator(shortMaLen, longMaLen, trendLen);
                    definition.Indicators.Add("trendUp",
                        new IndicatorOutput()
                        {
                            Indicator = trendIndicator,
                            OutputProperty = (i) => { return ((TrendIndicator)i).TrendUp; }
                        });
                    definition.Indicators.Add("shortMaOverLong",
                        new IndicatorOutput()
                        {
                            Indicator = trendIndicator,
                            OutputProperty = (i) => { return ((TrendIndicator)i).ShortMaOverLong; }
                        });
                    definition.Indicators.Add("newHigh",
                        new IndicatorOutput()
                        {
                            Indicator = trendIndicator,
                            OutputProperty = (i) => { return ((TrendIndicator)i).NewHigh; }
                        });


                    if (!results.ContainsKey(key))
                    {
                        runner.Run(dataSets, definition);
                        var best = runner.SelectVectors(200, 0.12).OrderBy(p => p.Probability).Reverse().ToList();

                        results.Add(key, best);

                        if (best.Count > 0)
                        {
                            Console.WriteLine("BreakoutLength: {0}", breakoutLength);
                            Console.WriteLine("ShortMaLen: {0}", shortMaLen);
                            Console.WriteLine("LongMaLen: {0}", longMaLen);
                            Console.WriteLine("TrendLen: {0}", trendLen);
                            Console.WriteLine("SeriesMaxLen: {0}", runner.SeriesMaxLen);
                            
                            foreach (var b in best)
                            {
                                string outputLine = key + " " + b;
                                Console.WriteLine(outputLine);
                                System.IO.File.AppendAllText(OUTPUT_FILE, outputLine + "\n");

                                //run a trading system 
                                definition.Predictors = PredictorsFromKey(b.Key);
                                var r = TestTrader(dataSets, definition, breakoutLength);

                                //if (b.Probability > bestSoFar.Probability)    {
                                var rating = RateTradingResults(r);
                                if ((rating) > bestRatingSoFar)
                                {
                                    bestRatingSoFar = rating;
                                    bestSoFar = b;
                                }

                                if ((rating) > bestRatingThisRound)
                                {
                                    bestRatingThisRound = rating;
                                    bestThisRound = b;
                                }

                                Console.WriteLine(" - {0} - {1} : {2}", r.PercentGain, r.AverageDrawdown, rating);
                            }

                            Console.WriteLine();
                            Console.WriteLine("Best so far: " + bestSoFar.ToString() + " " + bestRatingSoFar.ToString());
                            Console.WriteLine("Best this round: " + bestThisRound.ToString() + " " + bestRatingThisRound.ToString());
                            System.IO.File.AppendAllText(OUTPUT_FILE, bestSoFar.ToString() + "\n");
                        }

                        Console.WriteLine();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
            }
        }

        static TradingResults TestTrader(List<DataSet> dataSets, DataFeatureDefinition definition, int holdPeriod)
        {
            /*
            //set up the indicators 
            DataFeatureDefinition definition = new DataFeatureDefinition();

            definition.Indicators.Add("isUp",
                new IndicatorOutput()
                {
                    Indicator = new UpDownIndicator(),
                    OutputProperty = (i) => { return ((UpDownIndicator)i).Output; }
                });

            TrendIndicator trendIndicator = new TrendIndicator(11, 30, 8);
            definition.Indicators.Add("trendUp",
                new IndicatorOutput()
                {
                    Indicator = trendIndicator,
                    OutputProperty = (i) => { return ((TrendIndicator)i).TrendUp; }
                });
            definition.Indicators.Add("trendDn",
                new IndicatorOutput()
                {
                    Indicator = trendIndicator,
                    OutputProperty = (i) => { return ((TrendIndicator)i).TrendDown; }
                });
            definition.Indicators.Add("shortMaOverLong",
                new IndicatorOutput()
                {
                    Indicator = trendIndicator,
                    OutputProperty = (i) => { return ((TrendIndicator)i).ShortMaOverLong; }
                });

            definition.Predictors.Add("isUp", new int[] { 0 });
            definition.Predictors.Add("trendUp", new int[] { 0, 0, 0 });
            definition.Predictors.Add("shortMaOverLong", new int[] { 0, 0, 0 });
            */

            ITradingSystem ts = new TestTradingSystem(definition, holdPeriod); 

            int count = 0; 
            double startingEquity = 10000;
            double totalGain = 0;
            List<double> drawdowns = new List<double>(); 

            foreach (var ds in dataSets)
            {
                Trader t = new Trader(startingEquity);
                bool buyNext = false;
                ts.Calculate(ds);

                double highThisRound = 0;
                double maxDrawdown = 0; 
                double buyPrice = 0; 
                double stopOrder = 0; 

                for (int n = 0; n < ds.Count; n++)
                {
                    var row = ds.Rows[n];

                    //calculate drawdown
                    if (t.CashPlusEquity > highThisRound)
                        highThisRound = t.CashPlusEquity;

                    var drawdown = 100 * (highThisRound - t.CashPlusEquity) / highThisRound;
                    if (drawdown > maxDrawdown)
                        maxDrawdown = drawdown;

                    if (t.HasPosition("X"))
                    {
                        //time to close out? 
                        if (ts.CloseoutSignal[n])
                            t.ClosePosition("X", row.Close);
                        else if (row.Low < stopOrder)
                            t.ClosePosition("X", stopOrder); 
                    }
                    else
                    {
                        //time to buy? 
                        if (buyNext)
                        {
                            //open a position
                            if (!t.HasPosition("X"))
                            {
                                if (row.Open > 0)
                                {
                                    var price = row.Open;
                                    t.OpenPosition(new Position("X", (int)(t.Cash / price), price));
                                    buyPrice = price;
                                    stopOrder = row.Low;
                                    count++;
                                }
                            }
                            buyNext = false;
                        }
                        else
                        {
                            //buy next round at open
                            if (ts.BuySignal[n] && !t.HasPosition("X"))
                                buyNext = true;
                        }
                    }
                }

                //close remaining position
                if (t.HasPosition("X"))
                    t.ClosePosition("X", ds.Rows[ds.Rows.Length - 1].Close);

                //calculate drawdown
                drawdowns.Add(maxDrawdown);

                double gain = t.CashPlusEquity - startingEquity;
                double gainPercent = (100 * gain) / startingEquity;

                totalGain += gain;
            }

            TradingResults output = new TradingResults();
            var averageGain = (totalGain / (double)dataSets.Count);
            output.PercentGain = (100 * averageGain) / startingEquity;

            //calculate drawdown
            output.MaxDrawdown = drawdowns.Max();
            output.AverageDrawdown = drawdowns.Average(); 

            return output; 
        }

        static Dictionary<string, int[]> PredictorsFromKey(string key)
        {
            Dictionary<string, int[]> output = new Dictionary<string, int[]>();

            if (key.Length > 0)
            {
                string[] items = key.Split(',');

                foreach (string item in items)
                {
                    string[] pair = item.Split(':');

                    string k = pair[0];
                    string[] p = pair[1].Split('|');

                    int[] values = new int[p.Length];
                    for (int n = 0; n < p.Length; n++)
                        values[n] = Int32.Parse(p[n]);

                    output.Add(k, values);
                }
            }

            return output;
        }

        static double RateTradingResults(TradingResults results)
        {
            return (results.PercentGain - results.AverageDrawdown);
        }
    }

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
                foreach(var ind in testData.Indicators)
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

                    for (int i = n + 1; i < n + _breakoutLen+1; i++)
                    {
                        if (dataSet.Rows[i].Low < dataSet.Rows[n+1].Low)
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

                    double start = dataSet.Rows[n+1].Open;

                    for (int i = n + 1; i<dataSet.Count; i++)
                    {
                        if (dataSet.Rows[i].Low < dataSet.Rows[n + 1].Low)
                        {
                            this.Output[n] = 0;
                            break;
                        }

                        if( ((start - dataSet.Close[i]) / start) >= 0.01)
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
            get {
                if (this.Indicator != null && this.OutputProperty != null)
                    return this.OutputProperty(this.Indicator); 

                return new int[0]; 
            }
        }
    }

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
                    if (n >= buyIndex + _holdPeriod)
                        CloseoutSignal[n] = true;

                    if (CloseoutSignal[n])
                        buyIndex = -1;
                }
                else
                {
                    bool isMatch = true; 
                    foreach (var predictor in _definition.Predictors)
                    {
                        if (predictor.Value.Length > (n+1))
                        {
                            isMatch = false;
                            break;
                        }

                        for (int i = 0; i < predictor.Value.Length; i++)
                        {
                            var thisIndex = ((n-predictor.Value.Length)+i)+1;
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
