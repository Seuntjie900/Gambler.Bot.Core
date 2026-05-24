using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Helpers;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Gambler.Bot.Common.Games.Twist
{
    public class TwistBet : Bet
    {
        public TwistBet()
        {
            Game = Games.Twist;
        }
        public decimal Roll { get; set; }
        public bool High { get; set; }
        public decimal Chance { get; set; }
        public long Nonce { get; set; }
        public string? ServerHash { get; set; }
        public string? ServerSeed { get; set; }
        public string? ClientSeed { get; set; }
        public int WinnableType { get; set; }
        public override PlaceBet CreateRetry()
        {
            return new PlaceTwistBet(TotalAmount, High, Chance);
        }

        public override bool GetWin(IGameConfig config)
        {
            return High ? Roll > (config as TwistConfig).MaxRoll - Chance : Roll < Chance;
        }

        public int CalculateWinnableType(decimal maxroll)
        {
            if (Chance >= 50 && Roll > maxroll - Chance && Roll < Chance)
            {
                WinnableType = 1;
            }
            else if (Chance < 50 && Roll < maxroll - Chance && Roll > Chance)
            {
                WinnableType = 2;
            }
            else if (IsWin)
            {
                WinnableType = 3;
            }
            else
            {
                WinnableType = 4;
            }
            //check if roll is between overlap
            //else if chance <50% check if roll is between non overlap
            //else if win
            //else if loss
            return WinnableType;
        }

        public override string ToCSV(IGameConfig gamecofig, long TotalBetsPlaced, decimal Balance)
        {
            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}"
                        , TotalBetsPlaced, Roll, Chance, "N/A", GetWin(gamecofig) ? "win" : "lose", TotalAmount, Profit, Balance, Profit);
        }
    }
}
