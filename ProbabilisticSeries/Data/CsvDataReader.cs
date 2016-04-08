using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Trading.Utilities.Data
{
    public class CsvDataReader : IDataReader
    {
        public int DateIndex = 0;
        public int OpenIndex = 1;
        public int HighIndex = 2;
        public int LowIndex = 3;
        public int CloseIndex = 4;
        public int VolumeIndex = 5;
        public int RowStartIndex = 0;

        public DataSet Read(string source)
        {
            string[] lines = System.IO.File.ReadAllLines(source);
            List<DataRow> rows = new List<DataRow>();

            for (int n = RowStartIndex; n < lines.Length; n++)
            {
                string[] items = lines[n].Split(',');


                if (items.Length > 0)
                {
                    double open, high, low, close, vol;

                    DataRow row = new DataRow();
                    if (OpenIndex >= 0 && items.Length > OpenIndex && !String.IsNullOrWhiteSpace(items[OpenIndex]))
                    {
                        if (Double.TryParse(items[OpenIndex], out open))
                            row.Open = open;
                    }
                    if (HighIndex >= 0 && items.Length > HighIndex && !String.IsNullOrWhiteSpace(items[HighIndex]))
                    {
                        if (Double.TryParse(items[HighIndex], out high))
                            row.High = high;
                    }
                    if (LowIndex >= 0 && items.Length > LowIndex && !String.IsNullOrWhiteSpace(items[LowIndex]))
                    {
                        if (Double.TryParse(items[LowIndex], out low))
                            row.Low = low;
                    }
                    if (CloseIndex >= 0 && items.Length > CloseIndex && !String.IsNullOrWhiteSpace(items[CloseIndex]))
                    {
                        if (Double.TryParse(items[CloseIndex], out close))
                            row.Close = close;
                    }
                    if (DateIndex >= 0 && items.Length > VolumeIndex && !String.IsNullOrWhiteSpace(items[VolumeIndex]))
                    {
                        if (Double.TryParse(items[DateIndex], out vol))
                            row.Volume = vol;
                    }
                    if (DateIndex >= 0 && items.Length > DateIndex && !String.IsNullOrWhiteSpace(items[DateIndex]))
                    {
                        DateTime d;
                        if (DateTime.TryParse(items[DateIndex], out d))
                            row.Period = d;
                    }

                    rows.Add(row);
                }
            }

            return new DataSet(rows.ToArray());
        }
    }
}
