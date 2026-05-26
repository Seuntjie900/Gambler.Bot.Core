using System;
using System.Linq;

namespace Gambler.Bot.Common.Games.Limbo
{
    public class PlaceLimboBet : PlaceBet
    {
        
        /// <summary>
        /// Bet high when true, low when false
        /// </summary>

        public decimal Payout { get; set; }

        /// <summary>
        /// The unique internal identifier to detect duplicate or skipped bets.
        /// </summary>


        public PlaceLimboBet(decimal Amount, decimal Payout, string Guid)
        {
            this.Amount = Amount;
            this.Payout = Payout;
            GUID = GUID;
            Game = Games.Limbo;
        }
        public PlaceLimboBet(decimal Amount, decimal Payout)
        {
            this.Amount = Amount;
            this.Payout = Payout;
            Game = Games.Limbo;

        }
    }
}
