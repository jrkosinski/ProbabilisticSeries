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
    //find the surest predictor of the series (combination of 3 features is 100% accurate) 
    //       *       *     *       *     *       *     *       *     
    //A: 010010001000101100100001001000001010000010000011000000100000
    //B: 000100010001001001100000010000110000000100000110011001100000
    //C: 001001000010000010001100100000100000001001011000000111000000
    class Problem2
    {
        private static Dictionary<string, ProbabilityVector> _probabilities = new Dictionary<string, ProbabilityVector>();

        public static void Run()
        {
            int[] A = "0100100010001011001000010010000010100000100000110000001000000101".ToIntArray();
            int[] B = "0001000100010010011000000100001100000001000001100110011000110000".ToIntArray();
            int[] C = "0010010000100000100011001000001000000010010110000001110001000100".ToIntArray();

            int[] X = "0000100000001000001000000010000010000000100000100000001000000000".ToIntArray();

            //int[] A = "011".ToIntArray();
            //int[] B = "000".ToIntArray();
            //int[] C = "000".ToIntArray();

            //int[] X = "001".ToIntArray();

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
                    else
                    {
                        for (int ai = 0; ai <= maxSize; ai++)
                        {
                            for (int bi = 0; bi <= maxSize; bi++)
                            {
                                for (int ci = 0; ci <= maxSize; ci++)
                                {
                                    int[] lengths = new int[] { ai, bi, ci };

                                    string key = CreateKey(n, keys, features, lengths);
                                    Console.WriteLine(key);
                                    if (!_probabilities.ContainsKey(key))
                                        _probabilities.Add(key, CalculateProbability(key, n, features, lengths, X));
                                }
                            }
                        }
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
                return new ProbabilityVector() { Key = key, Frequency = hits, Chances =chances}; // Probability = (double)hits / (double)chances };

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
