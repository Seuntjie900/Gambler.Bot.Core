using Gambler.Bot.Common.Games.Limbo;
using System;

namespace Gambler.Bot.Common.Games.Twist
{
    public interface iTwist
    {
        Task<TwistBet> PlaceTwistBet(PlaceTwistBet BetDetails);
        TwistConfig TwistSettings { get; set; }

    }
    public class TwistResult : IGameResult
    {
        public decimal Roll { get; set; }

        public Games Game { get => Games.Twist; }
    }
}
