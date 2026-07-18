namespace Gambler.Bot.Common.Games.RangeDice;

public class RangeDiceConfig : Dice.IGameConfig
{
    public decimal Edge { get; set; }
    public decimal MaxRoll { get; set; }
    public bool SupportsDouble { get; set; }
}