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
    class Program
    {
        static void Main(string[] args)
        {
            MarketDataProblem.Run();
        }
    }


    public static class Extensions 
    {
        public static int[] ToIntArray(this string value)
        {
            int[] output = new int[value.Length];

            for (int n = 0; n < output.Length; n++)
            {
                output[n] = Int32.Parse(value.Substring(n, 1)); 
            }

            return output; 
        }

        public static Dictionary<string, ProbabilityVector> Clone(this Dictionary<string, ProbabilityVector> dict)
        {
            var clone = new Dictionary<string, ProbabilityVector>();
            foreach (string key in dict.Keys)
            {
                clone.Add(key, dict[key]); 
            }

            return clone;
        }
    }

    public struct ProbabilityVector : IComparable
    {
        public double Probability
        {
            get
            {
                if (Chances > 0 && Frequency > 0)
                    return (double)Frequency / (double)Chances;

                return 0; 
            }
        }

        public int Frequency { get; set; }

        public int Chances { get; set; }

        public string Key { get; set; }

        public override string ToString()
        {
            return String.Format("{0}: {1} ({2})", this.Key, this.Probability, this.Frequency); 
        }

        public int CompareTo(object value)
        {
            int output = 1;

            if (value != null && value is ProbabilityVector)
            {
                var v = (ProbabilityVector)value ; 

                output = this.Probability.CompareTo(v.Probability);
                if (output == 0)
                    output = this.Frequency.CompareTo(v.Frequency);
            }

            return output * -1; 
        }
    }
}
