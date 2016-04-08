using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Trading.Utilities.Data
{
    public static class Extensions
    {
        public static double[] GetClose(this DataRow[] data)
        {
            double[] output = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
                output[i] = data[i].Close;

            return output;
        }

        public static double[] GetOpen(this DataRow[] data)
        {
            double[] output = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
                output[i] = data[i].Open;

            return output;
        }

        public static double[] GetHigh(this DataRow[] data)
        {
            double[] output = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
                output[i] = data[i].High;

            return output;
        }

        public static double[] GetLow(this DataRow[] data)
        {
            double[] output = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
                output[i] = data[i].Low;

            return output;
        }

        public static double[] GetVolume(this DataRow[] data)
        {
            double[] output = new double[data.Length];
            for (int i = 0; i < data.Length; i++)
                output[i] = data[i].Volume;

            return output;
        }
    }
}
