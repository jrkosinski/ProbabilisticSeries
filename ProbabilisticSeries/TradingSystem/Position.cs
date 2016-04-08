using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;
using Trading.Utilities.Indicators;

namespace Trading.Utilities.TradingSystems
{
    public class Position
    {
        public string Symbol { get; private set; }

        public int Shares { get; private set; }

        public int AbsoluteShares { get { return Math.Abs(this.Shares); } }

        public double Price { get; private set; }

        public bool IsShort { get { return this.Shares < 0; } }

        public bool IsLong { get { return this.Shares > 0; } }


        public Position(string symbol, int shares, double price)
        {
            this.Symbol = symbol;
            this.Shares = shares;
            this.Price = price;
        }
    }
}
