using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
//#: break code into separate files 
//#: generify writeOutput;
//#: try multithreading 

//TODO: test carefully on very simple datasets that you can verify their correctness manually
//TODO: evaluate trading systems based on different factors including drawdown and win percentage
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
        private const string OUTPUT_FILE = "output.txt";
        private const string DATA_DIRECTORY = @"D:\MarketData\";
        private const int PARALLEL_MAX = 4;

        private static object _syncLock = new object(); 

        public static void Run()
        {
            //delete old log file 
            //System.IO.File.Delete(OUTPUT_FILE);

            int epoch = 0; 
            var results = new Dictionary<string, List<ProbabilityVector>>(); 
            var rand = new Random() ;
            var bestSoFar = new ProbabilityVector();
            double bestRatingSoFar = 0;

            while (true)
            {
                Parallel.For(0, PARALLEL_MAX, (nn) => 
                {
                    var outputQueue = new OutputQueue(OUTPUT_FILE); 

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
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "t.csv"));
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "c.csv"));
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "bac.csv"));
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "f.csv"));
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "goog.csv"));
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "ge.csv"));
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "gs.csv"));
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "ibm.csv"));
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "jpm.csv"));
                        dataSets.Add(reader.Read(DATA_DIRECTORY + "cat.csv"));


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
                                outputQueue.AppendLine("BreakoutLength: {0}", breakoutLength);
                                outputQueue.AppendLine("ShortMaLen: {0}", shortMaLen);
                                outputQueue.AppendLine("LongMaLen: {0}", longMaLen);
                                outputQueue.AppendLine("TrendLen: {0}", trendLen);
                                outputQueue.AppendLine("SeriesMaxLen: {0}", runner.SeriesMaxLen);

                                foreach (var b in best)
                                {
                                    string outputLine = key + " " + b;
                                    outputQueue.AppendLine(outputLine);

                                    //run a trading system 
                                    definition.Predictors = PredictorsFromKey(b.Key);
                                    var r = TestTrader(dataSets, definition, breakoutLength);
                                    var rating = RateTradingResults(r);

                                    lock (_syncLock)
                                    {
                                        //if (b.Probability > bestSoFar.Probability)    {
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
                                    }

                                    outputQueue.AppendLine(" * {0} - {1} : {2}", r.PercentGain, r.AverageDrawdown, rating);
                                }

                                outputQueue.AppendLine();
                                outputQueue.AppendLine("Best so far: " + bestSoFar.ToString() + " " + bestRatingSoFar.ToString());
                                outputQueue.AppendLine("Best this round: " + bestThisRound.ToString() + " " + bestRatingThisRound.ToString());
                            }

                            outputQueue.AppendLine();
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.ToString());
                    }

                    epoch++;
                    outputQueue.AppendLine("END EPOCH " + epoch.ToString());
                    outputQueue.Flush(); 
                }); 
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
                        else
                        {
                            if (row.Low < stopOrder)
                                t.ClosePosition("X", stopOrder);
                        }
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


        class OutputQueue
        {
            private static object _syncLock = new object();
            private static string _crlf = ASCIIEncoding.ASCII.GetString(new byte[] { 13, 10 }); 
            private List<string> _lines = new List<string>();
            private string _outputFile;

            public OutputQueue(string outputFile)
            {
                _outputFile = outputFile;
            }

            public void AppendLine()
            {
                this.AppendLine(String.Empty); 
            }

            public void AppendLine(string format, params object[] args)
            {
                _lines.Add(String.Format(format, args)); 
            }

            public void Flush()
            {
                lock (_syncLock)
                {
                    foreach (string line in _lines)
                    {
                        Console.WriteLine(line);
                        System.Diagnostics.Debug.WriteLine(line);
                    }

                    //if (_outputFile != null)
                    //    System.IO.File.AppendAllLines(_outputFile, _lines);
                }
            }
        }
    }
}
