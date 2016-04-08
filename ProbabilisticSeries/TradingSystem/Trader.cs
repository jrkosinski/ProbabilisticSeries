using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Trading.Utilities.Data;
using Trading.Utilities.Indicators;

namespace Trading.Utilities.TradingSystems
{
    public class Trader
    {
        private List<Position> _positions = new List<Position>();

        public double Equity { get; private set; }

        public double Cash { get; private set; }

        public double CashPlusEquity { get { return this.Cash + this.Equity; } }

        public Position[] Positions { get { return this._positions.ToArray(); } }


        public Trader(double cash)
        {
            this.Cash = cash;
            this.Equity = 0;
        }

        public void OpenPosition(Position position)
        {
            var cost = (position.AbsoluteShares * position.Price);
            if (cost > this.Cash)
                throw new Exception("Trader cannot afford this trade.");

            this.Cash -= cost;
            this.Equity += cost;

            this._positions.Add(position);
        }

        public void ClosePosition(string symbol, double price)
        {
            var position = this.FindPosition(symbol);
            if (position != null)
            {
                double profit = 0;
                double originalCost = (position.AbsoluteShares * position.Price);

                if (position.IsShort)
                {
                    var diff = position.Price - price;
                    profit = (diff * position.AbsoluteShares);
                    this.Cash += originalCost + profit;
                    this.Equity -= originalCost;
                }
                else
                {
                    profit = (price - position.Price) * position.AbsoluteShares;
                    this.Equity -= (position.AbsoluteShares * position.Price);
                    this.Cash += (position.AbsoluteShares * price);
                }

                Console.WriteLine(
                        String.Format("Closing {0} position in {1} for profit of {2} or {3}%. Total worth: {4}.",
                        position.IsLong ? "long" : "short",
                        symbol,
                        profit.ToString("0.##"),
                        ((profit / originalCost) * 100).ToString("0.00"),
                        this.CashPlusEquity.ToString("0")
                    ));

                //in case there are other positions in the same 
                this._positions.Remove(position);
                this.ClosePosition(symbol, price);
            }
        }

        public int MaxNumberOfShares(double sharePrice)
        {
            return (int)Math.Floor(this.Cash / sharePrice);
        }

        public Position FindPosition(string symbol)
        {
            return this._positions.FirstOrDefault((p) => p.Symbol == symbol);
        }

        public bool HasPosition(string symbol)
        {
            return this.FindPosition(symbol) != null;
        }

        public bool IsShort(string symbol)
        {
            return (this._positions.FirstOrDefault((p) => p.Symbol == symbol && p.Shares < 0) != null);
        }

        public bool IsLong(string symbol)
        {
            return (this._positions.FirstOrDefault((p) => p.Symbol == symbol && p.Shares > 0) != null);
        }
    }
}
