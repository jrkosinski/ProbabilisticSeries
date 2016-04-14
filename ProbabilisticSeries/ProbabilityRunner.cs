using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbabilisticSeries
{
    class ProbabilityRunner
    {
        private Dictionary<string, ProbabilityVector> _probabilities = new Dictionary<string, ProbabilityVector>();
        private Dictionary<string, ProbabilityVector> _tempProbabilities = new Dictionary<string, ProbabilityVector>();

        public int SeriesMaxLen = 3;


        //public void Run(List<string> keys, List<List<int[]>> featuresPerDataSet, List<int[]> XPerDataSet, int seriesMaxLen)
        public void Run(List<string> keys, List<DiscreteFeaturesForDataSet> discreteFeatures, int seriesMaxLen)
        {
            //[2] calculate probabilities for each dataset 
            List<Dictionary<string, ProbabilityVector>> probabilitiesPerDataSet = new List<Dictionary<string, ProbabilityVector>>();

            for (int i = 0; i < discreteFeatures.Count; i++)
            {
                var X = discreteFeatures[i].Goals;
                var features = discreteFeatures[i].Features;

                for (int n = (seriesMaxLen - 1); n < X.Length; n++)
                {
                    if (X[n] == 1)
                    {
                        int[] lengths = new int[keys.Count];

                        IterateRecursive(0, n, keys, features, X, lengths, seriesMaxLen, _probabilities);
                    }
                }

                probabilitiesPerDataSet.Add(_probabilities.Clone());
                _probabilities.Clear();
            }

            //[3] merge the probabilities all together 
            //TODO: optimize this process, it's very very slow 

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
                        keyBuilder.Append(",");

                    int start = (currentIndex - (lengths[n] - 1));
                    keyBuilder.Append(keys[n]);
                    keyBuilder.Append(":");

                    int end = start + lengths[n];
                    for (int i = start; i < (end); i++)
                    {
                        keyBuilder.Append(features[n][i].ToString());

                        if (i < end-1)
                            keyBuilder.Append("|");
                    }
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
