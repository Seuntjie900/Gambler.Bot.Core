namespace Gambler.Bot.Common.Games.RangeDice;

public interface iRangeDice
{
    Task<RangeDiceBet> PlaceDiceBet(PlaceRangeDiceBet BetDetails);
    RangeDiceConfig DiceSettings { get; set; }

}