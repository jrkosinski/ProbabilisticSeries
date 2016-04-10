using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Trading.Utilities.Data
{
    public class DataSet
    {
        public int Count { get { return this.Rows.Length; } }

        public DataRow[] Rows { get; private set; }

        public double[] Open { get; private set; }

        public double[] High { get; private set; }

        public double[] Low { get; private set; }

        public double[] Close { get; private set; }

        public double[] Volume { get; private set; }


        public DataSet(DataRow[] rows)
        {
            this.Rows = rows;
            this.Open = rows.GetOpen();
            this.High = rows.GetHigh();
            this.Low = rows.GetLow();
            this.Close = rows.GetClose();
            this.Volume = rows.GetVolume();
        }
    }
}
