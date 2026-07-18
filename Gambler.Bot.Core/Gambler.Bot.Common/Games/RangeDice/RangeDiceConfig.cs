namespace Gambler.Bot.Common.Games.RangeDice;

public class RangeDiceConfig : IGameConfig
{
    public decimal Edge { get; set; }
    public decimal MaxRoll { get; set; }
    public bool SupportsDouble { get; set; }
}