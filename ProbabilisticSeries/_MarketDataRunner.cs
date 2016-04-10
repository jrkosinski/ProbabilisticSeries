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
    //class _MarketDataRunner
    //{
    //    private Dictionary<string, ProbabilityVector> _probabilities = new Dictionary<string, ProbabilityVector>();

    //    public int BreakoutLength = 4;
    //    public int ShortMaLen = 7;
    //    public int LongMaLen = 40;
    //    public int TrendLen = 10;
    //    public int SeriesMaxLen = 3;


    //    public void Run(List<DataSet> dataSets)
    //    {
    //        foreach (var dataSet in dataSets)
    //        {
    //            int[] isUp = new int[dataSet.Rows.Length];
    //            int[] X = new int[dataSet.Rows.Length];

    //            for (int n = 0; n < dataSet.Rows.Length; n++)
    //            {
    //                var row = dataSet.Rows[n];
    //                isUp[n] = (row.Close > row.Open) ? 1 : 0;

    //                X[n] = IsBreakout(dataSet.Rows, n) ? 1 : 0;
    //            }

    //            List<IIndicator> indicators = new List<IIndicator>();

    //            indicators.Add(new Ema(ShortMaLen));
    //            indicators.Add(new Ema(LongMaLen));

    //            DataSetWithIndicators ds = new DataSetWithIndicators(dataSet, indicators.ToArray());

    //            int[] trendIsUp = new int[dataSet.Rows.Length];
    //            int[] trendIsDn = new int[dataSet.Rows.Length];
    //            int trendUpCount = 0;
    //            int trendDnCount = 0;

    //            for (int n = 0; n < ds.Data.Rows.Length; n++)
    //            {
    //                trendIsUp[n] = 0;
    //                trendIsDn[n] = 0;
    //                if (n >= 1)
    //                {
    //                    var ma1 = (ds.Indicators[0] as MovingAverage);
    //                    var ma2 = (ds.Indicators[1] as MovingAverage);

    //                    if (ma1.Output[n] > ma1.Output[n - 1] && ma2.Output[n] > ma2.Output[n - 1])
    //                    {
    //                        trendUpCount++;
    //                        trendDnCount = 0;
    //                    }
    //                    else
    //                    {
    //                        trendUpCount = 0;

    //                        if (ma1.Output[n] < ma1.Output[n - 1] && ma2.Output[n] < ma2.Output[n - 1])
    //                            trendDnCount++;
    //                        else
    //                            trendDnCount = 0;
    //                    }
    //                }

    //                trendIsUp[n] = (trendUpCount >= TrendLen) ? 1 : 0;
    //                trendIsDn[n] = (trendDnCount >= TrendLen) ? 1 : 0;
    //            }

    //            List<int[]> features = new List<int[]>();
    //            features.Add(isUp);
    //            features.Add(trendIsUp);
    //            features.Add(trendIsDn);

    //            List<string> keys = new List<string>();
    //            keys.Add("upDown");
    //            keys.Add("trendUp");
    //            keys.Add("trendDn");

    //            int maxSize = SeriesMaxLen;

    //            for (int n = (maxSize - 1); n < X.Length; n++)
    //            {
    //                if (X[n] == 1)
    //                {
    //                    bool recursive = true;

    //                    if (recursive)
    //                    {
    //                        int[] lengths = new int[keys.Count];
    //                        IterateRecursive(0, n, keys, features, X, lengths, maxSize);
    //                    }
    //                }
    //            }

    //            //var probs = SortByProbability();
    //            //foreach (var p in probs)
    //            //    Console.WriteLine(p.ToString());

    //            //probs = SortByFrequency();
    //            //foreach (var p in probs)
    //            //    Console.WriteLine(p.ToString());
    //        }
    //    }

    //    public void Run2(List<DataSet> dataSets)
    //    {
    //        List<List<int[]>> featuresPerDataSet = new List<List<int[]>>();
    //        List<int[]> XPerDataSet = new List<int[]>();

    //        //[1] calculate the features for each dataset 
    //        foreach (var dataSet in dataSets)
    //        {
    //            int[] isUp = new int[dataSet.Rows.Length];
    //            int[] X = new int[dataSet.Rows.Length];

    //            for (int n = 0; n < dataSet.Rows.Length; n++)
    //            {
    //                var row = dataSet.Rows[n];
    //                isUp[n] = (row.Close > row.Open) ? 1 : 0;

    //                X[n] = IsBreakout(dataSet.Rows, n) ? 1 : 0;
    //            }

    //            List<IIndicator> indicators = new List<IIndicator>();

    //            indicators.Add(new Ema(ShortMaLen));
    //            indicators.Add(new Ema(LongMaLen));

    //            DataSetWithIndicators ds = new DataSetWithIndicators(dataSet, indicators.ToArray());

    //            int[] trendIsUp = new int[dataSet.Rows.Length];
    //            int[] trendIsDn = new int[dataSet.Rows.Length];
    //            int trendUpCount = 0;
    //            int trendDnCount = 0;

    //            for (int n = 0; n < ds.Data.Rows.Length; n++)
    //            {
    //                trendIsUp[n] = 0;
    //                trendIsDn[n] = 0;
    //                if (n >= 1)
    //                {
    //                    var ma1 = (ds.Indicators[0] as MovingAverage);
    //                    var ma2 = (ds.Indicators[1] as MovingAverage);

    //                    if (ma1.Output[n] > ma1.Output[n - 1] && ma2.Output[n] > ma2.Output[n - 1])
    //                    {
    //                        trendUpCount++;
    //                        trendDnCount = 0;
    //                    }
    //                    else
    //                    {
    //                        trendUpCount = 0;

    //                        if (ma1.Output[n] < ma1.Output[n - 1] && ma2.Output[n] < ma2.Output[n - 1])
    //                            trendDnCount++;
    //                        else
    //                            trendDnCount = 0;
    //                    }
    //                }

    //                trendIsUp[n] = (trendUpCount >= TrendLen) ? 1 : 0;
    //                trendIsDn[n] = (trendDnCount >= TrendLen) ? 1 : 0;
    //            }

    //            List<int[]> features = new List<int[]>();
    //            features.Add(isUp);
    //            features.Add(trendIsUp);
    //            features.Add(trendIsDn);

    //            featuresPerDataSet.Add(features);
    //            XPerDataSet.Add(X);
    //        }

    //        List<string> keys = new List<string>();
    //        keys.Add("upDown");
    //        keys.Add("trendUp");
    //        keys.Add("trendDn");

    //        int maxSize = SeriesMaxLen;



    //        //[2] calculate probabilities for each dataset 
    //        List<Dictionary<string, ProbabilityVector>> probabilitiesPerDataSet = new List<Dictionary<string, ProbabilityVector>>();

    //        for (int i = 0; i < dataSets.Count; i++)
    //        {
    //            var X = XPerDataSet[i];
    //            var features = featuresPerDataSet[i];

    //            for (int n = (maxSize - 1); n < X.Length; n++)
    //            {
    //                if (X[n] == 1)
    //                {
    //                    int[] lengths = new int[keys.Count];

    //                    var probabilities = new Dictionary<string, ProbabilityVector>();
    //                    IterateRecursive2(0, n, keys, features, X, lengths, maxSize, probabilities);
    //                    probabilitiesPerDataSet.Add(probabilities);
    //                }
    //            }
    //        }

    //        //[3] merge the probabilities all together 

    //        //get all unique keys 
    //        List<string> uniqueKeys = new List<string>();
    //        foreach (var list in probabilitiesPerDataSet)
    //        {
    //            foreach (var key in list.Keys)
    //            {
    //                if (!uniqueKeys.Contains(key))
    //                    uniqueKeys.Add(key);
    //            }
    //        }

    //        //merge from all probability lists into one list 
    //        foreach (string key in uniqueKeys)
    //        {
    //            foreach (var list in probabilitiesPerDataSet)
    //            {
    //                if (list.ContainsKey(key))
    //                {
    //                    if (!_probabilities.ContainsKey(key))
    //                    {
    //                        _probabilities.Add(key, list[key]);
    //                    }
    //                    else
    //                    {
    //                        var pv = _probabilities[key];
    //                        pv.Frequency += list[key].Frequency;
    //                        pv.Chances += list[key].Chances;
    //                    }
    //                }
    //            }
    //        }

    //        //var probs = SortByProbability();
    //        //foreach (var p in probs)
    //        //    Console.WriteLine(p.ToString());

    //        //probs = SortByFrequency();
    //        //foreach (var p in probs)
    //        //    Console.WriteLine(p.ToString());
    //    }

    //    public List<ProbabilityVector> SelectVectors(int minFrequency, double minProbability)
    //    {
    //        return _probabilities.Where(p => (p.Value.Probability >= minProbability && p.Value.Frequency >= minFrequency)).Select(p => p.Value).ToList();
    //    }

    //    void IterateRecursive(int featureIndex, int unitIndex, List<string> keys, List<int[]> features, int[] hits, int[] lengths, int maxSize)
    //    {
    //        for (int i = 0; i <= maxSize; i++)
    //        {
    //            if (featureIndex < lengths.Length)
    //            {
    //                lengths[featureIndex] = i;

    //                if (featureIndex < lengths.Length - 1)
    //                    IterateRecursive(featureIndex + 1, unitIndex, keys, features, hits, lengths, maxSize);

    //                string key = CreateKey(unitIndex, keys, features, lengths);

    //                //Console.WriteLine(key);
    //                if (!_probabilities.ContainsKey(key))
    //                    _probabilities.Add(key, CalculateProbability(key, unitIndex, features, lengths, hits));
    //            }
    //        }
    //    }

    //    void IterateRecursive2(int featureIndex, int unitIndex, List<string> keys, List<int[]> features, int[] hits, int[] lengths, int maxSize, Dictionary<string, ProbabilityVector> probabilities)
    //    {
    //        for (int i = 0; i <= maxSize; i++)
    //        {
    //            if (featureIndex < lengths.Length)
    //            {
    //                lengths[featureIndex] = i;

    //                if (featureIndex < lengths.Length - 1)
    //                    IterateRecursive2(featureIndex + 1, unitIndex, keys, features, hits, lengths, maxSize, probabilities);

    //                string key = CreateKey(unitIndex, keys, features, lengths);

    //                //Console.WriteLine(key);
    //                if (!probabilities.ContainsKey(key))
    //                    probabilities.Add(key, CalculateProbability(key, unitIndex, features, lengths, hits));
    //            }
    //        }
    //    }

    //    ProbabilityVector CalculateProbability(string key, int currentIndex, List<int[]> features, int[] lengths, int[] X)
    //    {
    //        int chances = 0;
    //        int hits = 0;
    //        int start = FindMax(lengths);

    //        for (int n = start; n < X.Length; n++)
    //        {
    //            bool isMatch = true;
    //            bool isHit = X[n] == 1;

    //            for (int i = 0; i < lengths.Length; i++)
    //            {
    //                if (lengths[i] > 0)
    //                {
    //                    for (int offset = 0; offset < lengths[i]; offset++)
    //                    {
    //                        if (features[i][n - offset] != features[i][currentIndex - offset])
    //                            isMatch = false;
    //                    }
    //                }
    //            }

    //            if (isMatch)
    //            {
    //                chances++;
    //                if (isHit)
    //                    hits++;
    //            }
    //        }

    //        if (chances > 0)
    //            return new ProbabilityVector()
    //            {
    //                Key = key,
    //                Frequency = hits,
    //                Chances = chances //Probability = (double)hits / (double)chances 
    //            };

    //        return new ProbabilityVector();
    //    }

    //    string CreateKey(int currentIndex, List<string> keys, List<int[]> features, int[] lengths)
    //    {
    //        StringBuilder keyBuilder = new StringBuilder();

    //        for (int n = 0; n < keys.Count; n++)
    //        {
    //            if (lengths[n] > 0)
    //            {
    //                if (keyBuilder.Length > 0)
    //                    keyBuilder.Append(":");

    //                int start = (currentIndex - (lengths[n] - 1));
    //                keyBuilder.Append(keys[n]);

    //                for (int i = start; i < (start + lengths[n]); i++)
    //                    keyBuilder.Append(features[n][i].ToString());
    //            }
    //        }

    //        return keyBuilder.ToString();
    //    }

    //    int FindMax(int[] values)
    //    {
    //        int max = Int32.MinValue;
    //        foreach (var n in values)
    //        {
    //            if (n > max)
    //                max = n;
    //        }

    //        return max;
    //    }

    //    List<ProbabilityVector> SortByProbability()
    //    {
    //        var output = _probabilities.Values.ToList();
    //        output.Sort();
    //        return output;
    //    }

    //    List<ProbabilityVector> SortByFrequency()
    //    {
    //        var output = _probabilities.Values.ToList();
    //        return output.OrderBy(p => p.Frequency).Reverse().ToList();
    //    }

    //    bool IsBreakout(Trading.Utilities.Data.DataRow[] rows, int index)
    //    {
    //        bool output = false;

    //        if (index < (rows.Length - BreakoutLength))
    //        {
    //            output = true;
    //            for (int i = index + 1; i < index + BreakoutLength; i++)
    //            {
    //                if (rows[i].Low < rows[i - 1].Low)
    //                {
    //                    output = false;
    //                    break;
    //                }

    //                if (rows[i].Close < rows[i - 1].Close)
    //                {
    //                    output = false;
    //                    break;
    //                }
    //            }
    //        }

    //        return output;
    //    }
    //}

}
