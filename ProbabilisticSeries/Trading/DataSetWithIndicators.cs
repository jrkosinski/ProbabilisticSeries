using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;

namespace Trading.Utilities.Indicators
{
    public class DataSetWithIndicators
    {
        public DataSet Data { get; private set; }

        public IIndicator[] Indicators { get; private set; }


        public DataSetWithIndicators(DataSet data, IIndicator[] indicators)
        {
            if (indicators == null)
                indicators = new IIndicator[0];

            this.Indicators = indicators;

            this.SetData(data);
        }

        public void SetData(DataSet data)
        {
            this.Data = data;

            foreach (var ind in this.Indicators)
                ind.Calculate(data);
        }

        public void AddIndicator(IIndicator indicator, bool recalculate = true)
        {
            var indicators = new IIndicator[this.Indicators.Length + 1];
            this.Indicators.CopyTo(indicators, 0);
            indicators[indicators.Length - 1] = indicator;

            if (recalculate)
                this.Recalculate();
        }

        public void Recalculate()
        {
            this.SetData(this.Data);
        }

        public void OutputToCsv(string path)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < Data.Rows.Length; i++)
            {
                var row = this.Data.Rows[i];

                sb.Append(row.Period.ToString());
                sb.Append(",");
                sb.Append(row.Open.ToString());
                sb.Append(",");
                sb.Append(row.High.ToString());
                sb.Append(",");
                sb.Append(row.Low.ToString());
                sb.Append(",");
                sb.Append(row.Close.ToString());
                sb.Append(",");
                sb.Append(row.Volume.ToString());


                foreach (var ind in this.Indicators)
                {
                    sb.Append(",");
                    sb.Append(ind.GetOutputString(i));
                }

                sb.Append("\n");
            }

            System.IO.File.WriteAllText(path, sb.ToString());
        }
    }
}
