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

//TODO: represent the functions that get the features as indicators, so that they can be reused 
//TODO: test the results by running a trading system from them dynamically
//TODO: evaluate trading systems based on different factors including drawdown and win percentage
//TODO: represent the breakout selector as something dynamic and reusable 
//TODO: test carefully on very simple datasets that you can verify their correctness manually
//TODO: organize the code better 
//TODO: try ever more complex features

namespace ProbabilisticSeries
{
    class MarketDataProblem
    {
        private const string OUTPUT_FILE = "output.txt"; 

        public static void Run()
        {
            Dictionary<string, List<ProbabilityVector>> results = new Dictionary<string, List<ProbabilityVector>>();
            Random rand = new Random() ;

            while (true)
            {
                var runner = new MarketDataRunner();

                runner.BreakoutLength = 3; // rand.Next(2, 9);
                runner.ShortMaLen = rand.Next(7, 21);
                runner.LongMaLen = rand.Next(runner.ShortMaLen, 80);
                runner.TrendLen = rand.Next(2, 10);
                runner.SeriesMaxLen = 3;

                string key = String.Format("{0}|{1}|{2}|{3}", runner.BreakoutLength, runner.ShortMaLen, runner.LongMaLen, runner.TrendLen);

                List<DataSet> dataSets = new List<DataSet>();

                CsvDataReader reader = new CsvDataReader() { OpenIndex = 1, HighIndex = 2, LowIndex = 3, CloseIndex = 4, DateIndex = 0, VolumeIndex = 5, RowStartIndex = 1 };
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\t.csv"));
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\c.csv"));
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\bac.csv"));
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\f.csv"));
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\goog.csv"));
                //dataSets.Add(reader.Read(@"D:\Downloads\MarketData\ge.csv"));
                //dataSets.Add(reader.Read(@"D:\Downloads\MarketData\gs.csv"));
                //dataSets.Add(reader.Read(@"D:\Downloads\MarketData\ibm.csv"));
                //dataSets.Add(reader.Read(@"D:\Downloads\MarketData\jpm.csv"));
                //dataSets.Add(reader.Read(@"D:\Downloads\MarketData\cat.csv"));


                if (!results.ContainsKey(key))
                {
                    runner.Run(dataSets);
                    var best = runner.SelectVectors(200, 0.18).OrderBy(p => p.Probability).Reverse().ToList(); 

                    results.Add(key, best);

                    if (best.Count > 0)
                    {
                        Console.WriteLine("BreakoutLength: {0}", runner.BreakoutLength);
                        Console.WriteLine("ShortMaLen: {0}", runner.ShortMaLen);
                        Console.WriteLine("LongMaLen: {0}", runner.LongMaLen);
                        Console.WriteLine("TrendLen: {0}", runner.TrendLen);
                        Console.WriteLine("SeriesMaxLen: {0}", runner.SeriesMaxLen);

                        foreach (var b in best)
                        {
                            string outputLine = key + " " + b;
                            Console.WriteLine(outputLine);
                            System.IO.File.AppendAllText(OUTPUT_FILE, outputLine + "\n"); 

                            //[1] a list of inputs (e.g. breakout length, MA lengths, etc) 
                            //[2] a predictor 
                        }
                    }

                    Console.WriteLine(); 
                }
            }
        }

        static void TestTrader(List<DataSet> dataSets)
        {
            ITradingSystem ts = new TestTradingSystem(
                17,
                76,
                3,
                4
            );

            int count = 0; 
            double startingEquity = 10000; 
            foreach (var ds in dataSets)
            {
                Trader t = new Trader(startingEquity);
                bool buyNext = false;
                ts.Calculate(ds);

                for (int n = 0; n < ts.BuySignal.Length; n++)
                {
                    var row = ds.Rows[n]; 

                    if (t.HasPosition("X"))
                    {
                        if (ts.CloseoutSignal[n])
                            t.ClosePosition("X", row.Close); 
                    }
                    else
                    {
                        if (buyNext)
                        {
                            if (row.Open >= ds.Rows[n - 1].Low && !t.HasPosition("X"))
                            {
                                var price = row.Open;
                                t.OpenPosition(new Position("X", (int)(t.Cash / price), price));
                                count++;
                            }
                            buyNext = false;
                        }
                        else
                        {
                            if (ts.BuySignal[n] && !t.HasPosition("X"))
                                buyNext = true;
                        }
                    }
                }

                if (t.HasPosition("X"))
                    t.ClosePosition("X", ds.Rows[ds.Rows.Length - 1].Close);

                double gain = t.CashPlusEquity - startingEquity;
                double gainPercent = (100 * gain) / startingEquity;
            }
        }
    }

    class MarketDataRunner
    {
        private ProbabilityRunner _probabilityRunner = new ProbabilityRunner(); 

        public int BreakoutLength = 3;
        public int ShortMaLen = 7;
        public int LongMaLen = 40;
        public int TrendLen = 10;
        public int SeriesMaxLen = 3;


        public List<DiscreteFeaturesForDataSet> Run(List<DataSet> dataSets)
        {
            List<DiscreteFeaturesForDataSet> discreteFeatures = new List<DiscreteFeaturesForDataSet>(); 

            List<List<int[]>> featuresPerDataSet = new List<List<int[]>>();
            List<int[]> XPerDataSet = new List<int[]>();

            //[1] calculate the features for each dataset 
            foreach (var dataSet in dataSets)
            {
                int[] isUp = new int[dataSet.Rows.Length];
                int[] X = new int[dataSet.Rows.Length];

                for (int n = 0; n < dataSet.Rows.Length; n++)
                {
                    var row = dataSet.Rows[n];
                    isUp[n] = (row.Close > row.Open) ? 1 : 0;

                    X[n] = IsBreakout1(dataSet.Rows, n) ? 1 : 0;
                }

                List<IIndicator> indicators = new List<IIndicator>();

                indicators.Add(new Ema(ShortMaLen));
                indicators.Add(new Ema(LongMaLen));

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

                    trendIsUp[n] = (trendUpCount >= TrendLen) ? 1 : 0;
                    trendIsDn[n] = (trendDnCount >= TrendLen) ? 1 : 0;
                }

                List<int[]> features = new List<int[]>();
                features.Add(isUp);
                features.Add(trendIsUp);
                features.Add(trendIsDn);

                discreteFeatures.Add(new DiscreteFeaturesForDataSet(){ DataSet=dataSet, Features=features, Goals=X});
                featuresPerDataSet.Add(features);
                XPerDataSet.Add(X);
            }

            List<string> keys = new List<string>();
            keys.Add("upDown");
            keys.Add("trendUp");
            keys.Add("trendDn");

            _probabilityRunner.Run(keys, discreteFeatures, SeriesMaxLen);

            return discreteFeatures;
        }

        public List<ProbabilityVector> SelectVectors(int minFrequency, double minProbability)
        {
            return _probabilityRunner.SelectVectors(minFrequency, minProbability);
        }

        //the close is higher than prev day's, every day. The low is not lower than prev day's low, every day 
        bool IsBreakout1(Trading.Utilities.Data.DataRow[] rows, int index)
        {
            bool output = false;

            if (index < (rows.Length - BreakoutLength))
            {
                output = true;
                for (int i = index + 1; i < index + BreakoutLength; i++)
                {
                    if (rows[i].Low < rows[i - 1].Low)
                    {
                        output = false;
                        break;
                    }

                    if (rows[i].Close < rows[i - 1].Close)
                    {
                        output = false;
                        break;
                    }
                }
            }

            return output;
        }

        //the close is higher than prev day's, every day. The low is not lower than prev day's low, every day 
        bool IsBreakout2(Trading.Utilities.Data.DataRow[] rows, int index)
        {
            bool output = false;

            if (index < (rows.Length - BreakoutLength))
            {
                double start =  rows[index + 1].Open;
                double gain = (rows[index + BreakoutLength].Close - start);
                double gainPct = (gain * 100 / start);
                if (gain > 0 && gainPct >= 1.5)
                {
                    output = true;
                    for (int i = index + 1; i < index + BreakoutLength; i++)
                    {
                        if (rows[i].Low < rows[index + 1].Low)
                        {
                            output = false;
                            break;
                        }
                    }
                }
            }

            return output;
        }
    }

    class DiscreteFeaturesForDataSet
    {
        public DataSet DataSet { get; set; }

        public List<int[]> Features { get; set; }

        public int[] Goals { get; set; }
    }
}
