namespace Gambler.Bot.Common.Games.RangeDice;

public interface iRangeDice
{
    Task<RangeDiceBet> PlaceRangeDiceBet(PlaceRangeDiceBet BetDetails);
    RangeDiceConfig RangeDiceSettings { get; set; }

}