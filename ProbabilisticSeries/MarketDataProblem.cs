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

//TODO: test carefully on very simple datasets that you can verify their correctness manually
//TODO: evaluate trading systems based on different factors including drawdown and win percentage
//TODO: test the results by running a trading system from them dynamically
//TODO: try multithreading 
//TODO: optimize inside probabilityrunner
//TODO: organize the relationship between features & keys better 
//TODO: organize the code better 
//TODO: try ever more complex features
//TODO: optimize by pre-calculating EMAs etc. for each dataset (this will yield marginal improvement - better to optimize inside the function) 

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


                if (!results.ContainsKey(key))
                {
                    runner.Run(dataSets);
                    var best = runner.SelectVectors(200, 0.12).OrderBy(p => p.Probability).Reverse().ToList(); 

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

                            if (b.Probability > bestSoFar.Probability)    {
                                bestSoFar = b;
                            }

                            //[1] a list of inputs (e.g. breakout length, MA lengths, etc) 
                            //[2] a predictor 
                        }

                        Console.WriteLine();
                        Console.WriteLine("Best so far: " + bestSoFar.ToString());
                        System.IO.File.AppendAllText(OUTPUT_FILE, bestSoFar.ToString() + "\n");
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


        public void Run(List<DataSet> dataSets)
        {
            List<DiscreteFeaturesForDataSet> discreteFeatures = new List<DiscreteFeaturesForDataSet>(); 

            //List<List<int[]>> featuresPerDataSet = new List<List<int[]>>();
            //List<int[]> XPerDataSet = new List<int[]>();

            //[1] calculate the features for each dataset 
            foreach (var dataSet in dataSets)
            {
                BreakoutIndicator1 breakoutIndicator = new BreakoutIndicator1(BreakoutLength);
                breakoutIndicator.Calculate(dataSet); 

                UpDownIndicator isUp = new UpDownIndicator();
                isUp.Calculate(dataSet);

                TrendIndicator trend = new TrendIndicator(ShortMaLen, LongMaLen, TrendLen);
                trend.Calculate(dataSet); 

                List<int[]> features = new List<int[]>();
                features.Add(isUp.Output);
                features.Add(trend.TrendUp);
                features.Add(trend.TrendDown);
                features.Add(trend.ShortMaOverLong);

                discreteFeatures.Add(new DiscreteFeaturesForDataSet() { DataSet = dataSet, Features = features, Goals = breakoutIndicator.Output });
            }

            List<string> keys = new List<string>();
            keys.Add("upDown");
            keys.Add("trendUp");
            keys.Add("trendDn");
            keys.Add("shortMaOverLong");

            _probabilityRunner.Run(keys, discreteFeatures, SeriesMaxLen);
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
    }

    class DiscreteFeaturesForDataSet
    {
        public DataSet DataSet { get; set; }

        public List<int[]> Features { get; set; }

        public int[] Goals { get; set; }
    }

    public class UpDownIndicator : IIndicator
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

    public class TrendIndicator : IIndicator
    {
        private Ema _shortMa, _longMa;
        private int _trendLen;

        public int[] TrendUp { get; private set; }

        public int[] TrendDown { get; private set; }

        public int[] ShortMaOverLong { get; private set; }


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
            this.TrendDown = new int[data.Length];
            this.ShortMaOverLong = new int[data.Length];
            int trendUpCount = 0;
            int trendDnCount = 0;

            _shortMa.Calculate(data);
            _longMa.Calculate(data); 

            for (int n = 0; n < data.Length; n++)
            {
                this.TrendUp[n] = 0;
                this.TrendDown[n] = 0;

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
                }

                this.ShortMaOverLong[n] = (_shortMa.Output[n] > _longMa.Output[n]) ? 1 : 0;
                this.TrendUp[n] = (trendUpCount >= _trendLen) ? 1 : 0;
                this.TrendDown[n] = (trendDnCount >= _trendLen) ? 1 : 0;
            }
        }

        public string GetOutputString(int index)
        {
            return String.Format("{0}{1}", this.TrendUp[index].ToString()); 
        }
    }

    //the close is higher than prev day's, every day. The low is not lower than prev day's low, every day 
    public class BreakoutIndicator1 : IIndicator
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
                        //if (dataSet.Rows[i].Low < dataSet.Rows[i - 1].Low)
                        //{
                        //    this.Output[n] = 0; 
                        //    break;
                        //}

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
}
