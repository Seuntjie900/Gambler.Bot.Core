namespace Gambler.Bot.Common.Games.RangeDice;

public class RangeDiceResult:IGameResult
{
    public decimal Roll { get; set; }

    public Games Game{ get => Games.Dice; }
}