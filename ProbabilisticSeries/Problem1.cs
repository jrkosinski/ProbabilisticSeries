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
    //find the surest predictor of the series (single feature predicts with 100% accuracy) 
    //       *       *     *       *     *       *     *       * 
    //A: 1 1 0 0 1 1 0 1 1 1 1 0 0 0 1 0 1 0 1 1 0 0 1 1 0 1 1 0 1 1
    //B: 0 0 1 0 0 0 1 0 0 1 0 0 0 1 0 0 1 0 0 0 1 0 0 1 0 0 0 1 0 0 
    class Problem1
    {
        public static void Run()
        {
            int[] A = { 1, 1, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 1, 0, 1, 1 };
            int[] B = { 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0 };
            int[] X = { 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0 };

            int maxSize = 3;
            Dictionary<string, double> probabilities = new Dictionary<string, double>();

            for (int n = (maxSize - 1); n < X.Length; n++)
            {
                if (X[n] == 1)
                {
                    for (int ai = 0; ai < maxSize; ai++)
                    {
                        for (int bi = 0; bi < maxSize; bi++)
                        {
                            string key = CreateKey(n, A, ai, B, bi);
                            if (!probabilities.ContainsKey(key))
                                probabilities.Add(key, CalculateProbability(n, A, ai, B, bi, X));
                        }
                    }
                }
            }


            //now test 
            int[] A_test = { 1, 1, 0, 0, 1, 1, 0, 1, 1, 1, 1, 0, 0, 0, 1, 0, 1, 0, 1, 1, 0, 0, 1, 1, 0, 1, 1, 0, 1, 1 };
            int[] B_test = { 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0 };
            int[] X_test = { 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0, 1, 0, 0, 0, 1, 0, 0 };

            for (int n = (maxSize - 1); n < X.Length; n++)
            {
                List<double> probabilitiesForUnit = new List<double>();

                for (int ai = 0; ai < maxSize; ai++)
                {
                    for (int bi = 0; bi < maxSize; bi++)
                    {
                        if (ai > 0 || bi > 0)
                        {
                            string key = CreateKey(n, A, ai, B, bi);
                            if (probabilities.ContainsKey(key))
                            {
                                probabilitiesForUnit.Add(probabilities[key]);
                            }
                        }
                    }
                }
            }
        }

        static double CalculateProbability(int index, int[] A, int lenA, int[] B, int lenB, int[] X)
        {
            int chances = 0;
            int hits = 0;
            int start = Math.Max(lenA, lenB);

            for (int n = start; n < X.Length; n++)
            {
                bool isMatch = true;
                bool isHit = X[n] == 1;
                if (lenA > 0)
                {
                    for (int offset = 0; offset < lenA; offset++)
                    {
                        if (A[n - offset] != A[index - offset])
                            isMatch = false;
                    }
                }

                if (lenB > 0 && (isMatch != false))
                {
                    for (int offset = 0; offset < lenB; offset++)
                    {
                        if (B[n - offset] != B[index - offset])
                            isMatch = false;
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
                return (double)hits / (double)chances;

            return 0.0;
        }

        static string CreateKey(int index, int[] A, int lenA, int[] B, int lenB)
        {
            int startA = (index - (lenA - 1));
            int startB = (index - (lenB - 1));

            StringBuilder keyBuilder = new StringBuilder();

            if (lenA > 0)
            {
                keyBuilder.Append("A");
                for (int n = startA; n < (startA + lenA); n++)
                    keyBuilder.Append(A[n].ToString());
            }

            keyBuilder.Append(":");

            if (lenB > 0)
            {
                keyBuilder.Append("B");
                for (int n = startB; n < (startB + lenB); n++)
                    keyBuilder.Append(B[n].ToString());
            }

            return keyBuilder.ToString();
        }
    }
}
