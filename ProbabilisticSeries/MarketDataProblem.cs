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

//TODO: upload this repo 
//TODO: test carefully on very simple datasets that you can verify their correctness manually
//TODO: test the results by running a trading system from them 
//TODO: redesign this with more cohesive data structures (classes) instead of a bunch of separate lists/arrays
//TODO: evaluate trading systems based on different factors including drawdown and win percentage
//TODO: organize the code better 
//TODO: try ever more complex features

namespace ProbabilisticSeries
{
    class MarketDataProblem
    {
        public static void Run()
        {
            /*
            int[] v1a = { 0, 1, 0, 0, 1, 1, 0 };
            int[] v1b = { 1, 0, 1, 1, 0, 1, 0 };
            int[] x1  = { 0, 0, 0, 0, 0, 1, 0 };


            int[] v2a = { 0, 1, 1, 0, 0, 1, 0 };
            int[] v2b = { 0, 1, 0, 1, 0, 1, 0 };
            int[] x2  = { 0, 0, 0, 0, 0, 1, 0 };

            List<string> keys = new List<string>(new string[]{"v1", "v2"});
            List<List<int[]>> featuresPerDataSet = new List<List<int[]>>(); 
            List<int[]> XPerDataSet = new List<int[]>(); 

            XPerDataSet.Add(x1);
            XPerDataSet.Add(x2);

            List<int[]> features1 = new List<int[]>();
            features1.Add(v1a);
            features1.Add(v1b);

            List<int[]> features2 = new List<int[]>();
            features2.Add(v2a);
            features2.Add(v2b);

            featuresPerDataSet.Add(features1);
            featuresPerDataSet.Add(features2);

            var p = new ProbabilityRunner();
            p.Run(keys, featuresPerDataSet, XPerDataSet, 1); 
            */



            Dictionary<string, List<ProbabilityVector>> results = new Dictionary<string, List<ProbabilityVector>>();
            Random rand = new Random() ;

            while (true)
            {
                var runner = new MarketDataRunner2();

                runner.BreakoutLength = 4; // rand.Next(2, 9);
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
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\ge.csv"));
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\gs.csv"));
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\ibm.csv"));
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\jpm.csv"));
                dataSets.Add(reader.Read(@"D:\Downloads\MarketData\cat.csv"));


                //TestTrader(new List<DataSet>(new DataSet[] { reader.Read(@"D:\Downloads\MarketData\cat.csv") }));
                TestTrader(new List<DataSet>(dataSets));


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
                            Console.WriteLine(key + " " + b);
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

    class _MarketDataRunner
    {
        private Dictionary<string, ProbabilityVector> _probabilities = new Dictionary<string, ProbabilityVector>();

        public int BreakoutLength = 4;
        public int ShortMaLen = 7;
        public int LongMaLen = 40;
        public int TrendLen = 10;
        public int SeriesMaxLen = 3;


        public void Run(List<DataSet> dataSets)
        {
            foreach (var dataSet in dataSets)
            {
                int[] isUp = new int[dataSet.Rows.Length];
                int[] X = new int[dataSet.Rows.Length];

                for (int n = 0; n < dataSet.Rows.Length; n++)
                {
                    var row = dataSet.Rows[n];
                    isUp[n] = (row.Close > row.Open) ? 1 : 0;

                    X[n] = IsBreakout(dataSet.Rows, n) ? 1 : 0;
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

                List<string> keys = new List<string>();
                keys.Add("upDown");
                keys.Add("trendUp");
                keys.Add("trendDn");

                int maxSize = SeriesMaxLen;

                for (int n = (maxSize - 1); n < X.Length; n++)
                {
                    if (X[n] == 1)
                    {
                        bool recursive = true;

                        if (recursive)
                        {
                            int[] lengths = new int[keys.Count];
                            IterateRecursive(0, n, keys, features, X, lengths, maxSize);
                        }
                    }
                }

                //var probs = SortByProbability();
                //foreach (var p in probs)
                //    Console.WriteLine(p.ToString());

                //probs = SortByFrequency();
                //foreach (var p in probs)
                //    Console.WriteLine(p.ToString());
            }
        }

        public void Run2(List<DataSet> dataSets)
        {
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

                    X[n] = IsBreakout(dataSet.Rows, n) ? 1 : 0;
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

                featuresPerDataSet.Add(features);
                XPerDataSet.Add(X);
            }

            List<string> keys = new List<string>();
            keys.Add("upDown");
            keys.Add("trendUp");
            keys.Add("trendDn");

            int maxSize = SeriesMaxLen;



            //[2] calculate probabilities for each dataset 
            List<Dictionary<string, ProbabilityVector>> probabilitiesPerDataSet = new List<Dictionary<string, ProbabilityVector>>(); 

            for(int i=0; i<dataSets.Count; i++)
            {
                var X = XPerDataSet[i];
                var features = featuresPerDataSet[i]; 
                
                for (int n = (maxSize - 1); n < X.Length; n++)
                {
                    if (X[n] == 1)
                    {
                        int[] lengths = new int[keys.Count];

                        var probabilities = new Dictionary<string, ProbabilityVector>();
                        IterateRecursive2(0, n, keys, features, X, lengths, maxSize, probabilities);
                        probabilitiesPerDataSet.Add(probabilities); 
                    }
                }
            }

            //[3] merge the probabilities all together 

            //get all unique keys 
            List<string> uniqueKeys = new List<string>();
            foreach (var list in probabilitiesPerDataSet)
            {
                foreach (var key in list.Keys)
                {
                    if (!uniqueKeys.Contains(key))
                        uniqueKeys.Add(key); 
                }
            }

            //merge from all probability lists into one list 
            foreach (string key in uniqueKeys)
            {
                foreach (var list in probabilitiesPerDataSet)
                {
                    if (list.ContainsKey(key))
                    {
                        if (!_probabilities.ContainsKey(key))
                        {
                            _probabilities.Add(key, list[key]); 
                        }
                        else
                        {
                            var pv = _probabilities[key];
                            pv.Frequency += list[key].Frequency;
                            pv.Chances += list[key].Chances; 
                        }
                    }
                }
            }

            //var probs = SortByProbability();
            //foreach (var p in probs)
            //    Console.WriteLine(p.ToString());

            //probs = SortByFrequency();
            //foreach (var p in probs)
            //    Console.WriteLine(p.ToString());
        }

        public List<ProbabilityVector> SelectVectors(int minFrequency, double minProbability)
        {
            return _probabilities.Where(p => (p.Value.Probability >= minProbability && p.Value.Frequency >= minFrequency)).Select(p => p.Value).ToList();
        }

        void IterateRecursive(int featureIndex, int unitIndex, List<string> keys, List<int[]> features, int[] hits, int[] lengths, int maxSize)
        {
            for (int i = 0; i <= maxSize; i++)
            {
                if (featureIndex < lengths.Length)
                {
                    lengths[featureIndex] = i;

                    if (featureIndex < lengths.Length - 1)
                        IterateRecursive(featureIndex + 1, unitIndex, keys, features, hits, lengths, maxSize);

                    string key = CreateKey(unitIndex, keys, features, lengths);

                    //Console.WriteLine(key);
                    if (!_probabilities.ContainsKey(key))
                        _probabilities.Add(key, CalculateProbability(key, unitIndex, features, lengths, hits));
                }
            }
        }

        void IterateRecursive2(int featureIndex, int unitIndex, List<string> keys, List<int[]> features, int[] hits, int[] lengths, int maxSize, Dictionary<string, ProbabilityVector> probabilities)
        {
            for (int i = 0; i <= maxSize; i++)
            {
                if (featureIndex < lengths.Length)
                {
                    lengths[featureIndex] = i;

                    if (featureIndex < lengths.Length - 1)
                        IterateRecursive2(featureIndex + 1, unitIndex, keys, features, hits, lengths, maxSize, probabilities);

                    string key = CreateKey(unitIndex, keys, features, lengths);

                    //Console.WriteLine(key);
                    if (!probabilities.ContainsKey(key))
                        probabilities.Add(key, CalculateProbability(key, unitIndex, features, lengths, hits));
                }
            }
        }

        ProbabilityVector CalculateProbability(string key, int currentIndex, List<int[]> features, int[] lengths, int[] X)
        {
            int chances = 0;
            int hits = 0;
            int start = FindMax(lengths);

            for (int n = start; n < X.Length; n++)
            {
                bool isMatch = true;
                bool isHit = X[n] == 1;

                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] > 0)
                    {
                        for (int offset = 0; offset < lengths[i]; offset++)
                        {
                            if (features[i][n - offset] != features[i][currentIndex - offset])
                                isMatch = false;
                        }
                    }
                }

                if (isMatch)
                {
                    chances++;
                    if (isHit)
                        hits++;
                }
            }

            if (chances > 0)
                return new ProbabilityVector() { 
                    Key = key, Frequency = hits, Chances = chances //Probability = (double)hits / (double)chances 
                };

            return new ProbabilityVector();
        }

        string CreateKey(int currentIndex, List<string> keys, List<int[]> features, int[] lengths)
        {
            StringBuilder keyBuilder = new StringBuilder();

            for (int n = 0; n < keys.Count; n++)
            {
                if (lengths[n] > 0)
                {
                    if (keyBuilder.Length > 0)
                        keyBuilder.Append(":");

                    int start = (currentIndex - (lengths[n] - 1));
                    keyBuilder.Append(keys[n]);

                    for (int i = start; i < (start + lengths[n]); i++)
                        keyBuilder.Append(features[n][i].ToString());
                }
            }

            return keyBuilder.ToString();
        }

        int FindMax(int[] values)
        {
            int max = Int32.MinValue;
            foreach (var n in values)
            {
                if (n > max)
                    max = n;
            }

            return max;
        }

        List<ProbabilityVector> SortByProbability()
        {
            var output = _probabilities.Values.ToList();
            output.Sort();
            return output;
        }

        List<ProbabilityVector> SortByFrequency()
        {
            var output = _probabilities.Values.ToList();
            return output.OrderBy(p => p.Frequency).Reverse().ToList();
        }

        bool IsBreakout(Trading.Utilities.Data.DataRow[] rows, int index)
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



    class MarketDataRunner2
    {
        private ProbabilityRunner _probabilityRunner = new ProbabilityRunner(); 

        public int BreakoutLength = 4;
        public int ShortMaLen = 7;
        public int LongMaLen = 40;
        public int TrendLen = 10;
        public int SeriesMaxLen = 3;


        public void Run(List<DataSet> dataSets)
        {
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

                    X[n] = IsBreakout2(dataSet.Rows, n) ? 1 : 0;
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

                featuresPerDataSet.Add(features);
                XPerDataSet.Add(X);
            }

            List<string> keys = new List<string>();
            keys.Add("upDown");
            keys.Add("trendUp");
            keys.Add("trendDn");

            
            _probabilityRunner.Run(keys, featuresPerDataSet, XPerDataSet, SeriesMaxLen);
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

    class ProbabilityRunner
    {
        private Dictionary<string, ProbabilityVector> _probabilities = new Dictionary<string, ProbabilityVector>();
        private Dictionary<string, ProbabilityVector> _tempProbabilities = new Dictionary<string, ProbabilityVector>();

        public int SeriesMaxLen = 3;


        public void Run(List<string> keys, List<List<int[]>> featuresPerDataSet, List<int[]> XPerDataSet, int seriesMaxLen)
        {
            //[2] calculate probabilities for each dataset 
            List<Dictionary<string, ProbabilityVector>> probabilitiesPerDataSet = new List<Dictionary<string, ProbabilityVector>>();

            for (int i = 0; i < XPerDataSet.Count; i++)
            {
                var X = XPerDataSet[i];
                var features = featuresPerDataSet[i];

                for (int n = (seriesMaxLen - 1); n < X.Length; n++)
                {
                    if (X[n] == 1)
                    {
                        int[] lengths = new int[keys.Count];

                        IterateRecursive(0, n, keys, features, X, lengths, seriesMaxLen, _probabilities);
                        probabilitiesPerDataSet.Add(_probabilities.Clone()); 
                    }
                }

                _probabilities.Clear();
            }

            //[3] merge the probabilities all together 

            //get all unique keys 
            List<string> uniqueKeys = new List<string>();
            foreach (var list in probabilitiesPerDataSet)
            {
                foreach (var key in list.Keys)
                {
                    if (!uniqueKeys.Contains(key))
                        uniqueKeys.Add(key);
                }
            }

            //merge from all probability lists into one list 
            foreach (string key in uniqueKeys)
            {
                foreach (var list in probabilitiesPerDataSet)
                {
                    if (list.ContainsKey(key))
                    {
                        if (!_probabilities.ContainsKey(key))
                        {
                            _probabilities.Add(key, list[key]);
                        }
                        else
                        {
                            var pv = _probabilities[key];
                            pv.Frequency += list[key].Frequency;
                            pv.Chances += list[key].Chances;
                        }
                    }
                }
            }

            //var probs = SortByProbability();
            //foreach (var p in probs)
            //    Console.WriteLine(p.ToString());

            //probs = SortByFrequency();
            //foreach (var p in probs)
            //    Console.WriteLine(p.ToString());
        }

        public List<ProbabilityVector> SelectVectors(int minFrequency, double minProbability)
        {
            return _probabilities.Where(p => (p.Value.Probability >= minProbability && p.Value.Frequency >= minFrequency)).Select(p => p.Value).ToList();
        }

        public List<ProbabilityVector> SortByProbability()
        {
            var output = _probabilities.Values.ToList();
            output.Sort();
            return output;
        }

        public List<ProbabilityVector> SortByFrequency()
        {
            var output = _probabilities.Values.ToList();
            return output.OrderBy(p => p.Frequency).Reverse().ToList();
        }

        void IterateRecursive(int featureIndex, int unitIndex, List<string> keys, List<int[]> features, int[] hits, int[] lengths, int maxSize, Dictionary<string, ProbabilityVector> probabilities)
        {
            for (int i = 0; i <= maxSize; i++)
            {
                if (featureIndex < lengths.Length)
                {
                    lengths[featureIndex] = i;

                    if (featureIndex < lengths.Length - 1)
                        IterateRecursive(featureIndex + 1, unitIndex, keys, features, hits, lengths, maxSize, probabilities);

                    string key = CreateKey(unitIndex, keys, features, lengths);

                    //Console.WriteLine(key);
                    if (!probabilities.ContainsKey(key))
                        probabilities.Add(key, CalculateProbability(key, unitIndex, features, lengths, hits));
                }
            }
        }

        ProbabilityVector CalculateProbability(string key, int currentIndex, List<int[]> features, int[] lengths, int[] X)
        {
            int chances = 0;
            int hits = 0;
            int start = FindMax(lengths);

            for (int n = start; n < X.Length; n++)
            {
                bool isMatch = true;
                bool isHit = X[n] == 1;

                for (int i = 0; i < lengths.Length; i++)
                {
                    if (lengths[i] > 0)
                    {
                        for (int offset = 0; offset < lengths[i]; offset++)
                        {
                            if (features[i][n - offset] != features[i][currentIndex - offset])
                                isMatch = false;
                        }
                    }
                }

                if (isMatch)
                {
                    chances++;
                    if (isHit)
                        hits++;
                }
            }

            if (chances > 0)
                return new ProbabilityVector()
                {
                    Key = key,
                    Frequency = hits,
                    Chances = chances //Probability = (double)hits / (double)chances 
                };

            return new ProbabilityVector();
        }

        string CreateKey(int currentIndex, List<string> keys, List<int[]> features, int[] lengths)
        {
            StringBuilder keyBuilder = new StringBuilder();

            for (int n = 0; n < keys.Count; n++)
            {
                if (lengths[n] > 0)
                {
                    if (keyBuilder.Length > 0)
                        keyBuilder.Append(":");

                    int start = (currentIndex - (lengths[n] - 1));
                    keyBuilder.Append(keys[n]);

                    for (int i = start; i < (start + lengths[n]); i++)
                        keyBuilder.Append(features[n][i].ToString());
                }
            }

            return keyBuilder.ToString();
        }

        int FindMax(int[] values)
        {
            int max = Int32.MinValue;
            foreach (var n in values)
            {
                if (n > max)
                    max = n;
            }

            return max;
        }
    }
}
