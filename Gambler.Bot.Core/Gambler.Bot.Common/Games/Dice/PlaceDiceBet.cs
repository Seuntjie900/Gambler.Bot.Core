﻿using System;

namespace Gambler.Bot.Common.Games.Dice
{
    public class PlaceDiceBet : PlaceBet
    {
        /// <summary>
        /// Bet high when true, low when false
        /// </summary>
        public bool High { get; set; }
        /// <summary>
        /// The chance to place the bet at
        /// </summary>
        public decimal Chance { get; set; }

        /// <summary>
        /// The unique internal identifier to detect duplicate or skipped bets.
        /// </summary>


        public PlaceDiceBet(decimal Amount, bool High, decimal Chance, string Guid)
        {
            this.Amount = Amount;
            this.High = High;
            this.Chance = Chance;
            GUID = GUID;
        }
        public PlaceDiceBet(decimal Amount, bool High, decimal Chance)
        {
            this.Amount = Amount;
            this.High = High;
            this.Chance = Chance;

        }
    }
}
