using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading;
using Trading.Utilities;
using Trading.Utilities.Data;
using Trading.Utilities.Indicators;

namespace ProbabilisticSeries
{
    //find the surest predictor of the series (data is completely random) 
    class Problem3
    {
        private static Dictionary<string, ProbabilityVector> _probabilities = new Dictionary<string, ProbabilityVector>();

        public static void Run()
        {
            int seriesLen = 100000;
            Random rand = new Random();
            int[] A = new int[seriesLen];
            int[] B = new int[seriesLen];
            int[] C = new int[seriesLen];
            int[] X = new int[seriesLen];
            for (int i = 0; i < seriesLen; i++)
            {
                A[i] = rand.Next(2);
                B[i] = rand.Next(2);
                C[i] = rand.Next(2);
                X[i] = rand.Next(2);
            }

            List<int[]> features = new List<int[]>();
            features.Add(A);
            features.Add(B);
            features.Add(C);

            List<string> keys = new List<string>();
            keys.Add("A");
            keys.Add("B");
            keys.Add("C");

            int maxSize = 3;

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

            var probs = SortProbabilities();
            foreach (var p in probs)
                Console.WriteLine(p.ToString());
        }

        static void IterateRecursive(int featureIndex, int unitIndex, List<string> keys, List<int[]> features, int[] hits, int[] lengths, int maxSize)
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

        static ProbabilityVector CalculateProbability(string key, int currentIndex, List<int[]> features, int[] lengths, int[] X)
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
                return new ProbabilityVector() { Key = key, Frequency = hits, Chances=chances}; //Probability = (double)hits / (double)chances };

            return new ProbabilityVector();
        }

        static string CreateKey(int currentIndex, List<string> keys, List<int[]> features, int[] lengths)
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

        static int FindMax(int[] values)
        {
            int max = Int32.MinValue;
            foreach (var n in values)
            {
                if (n > max)
                    max = n;
            }

            return max;
        }

        static List<ProbabilityVector> SortProbabilities()
        {
            var output = _probabilities.Values.ToList();
            output.Sort();
            return output;
        }
    }
}
