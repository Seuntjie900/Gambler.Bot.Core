namespace Gambler.Bot.Common.Games.RangeDice;

public class RangeDiceBet : Bet
{
    public RangeDiceBet()
    {
        Game = Games.Dice;
    }
    public RangeDiceType Type { get; set; }
    public decimal Roll { get; set; }
    public long Nonce { get; set; }
    public string? ServerHash { get; set; }
    public string? ServerSeed { get; set; }
    public string? ClientSeed { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Min2 { get; set; }
    public decimal Max2 { get; set; }
    public int WinnableType { get; set; }
    public override PlaceBet CreateRetry()
    {
        return new PlaceRangeDiceBet(Type, TotalAmount, Min,Max, Min2, Max2);
    }

    public override bool GetWin(Dice.IGameConfig config)
    {
        switch (Type)
        {
            default:
            case RangeDiceType.In: return Roll > Min && Roll < Min; 
            case RangeDiceType.Out: return Roll < Min || Roll > Max;
            case RangeDiceType.Double: return Roll > Min && Roll < Max || Roll > Min2 && Roll < Max2;
        }
        
    }

   

    string ToRange()
    {
        switch (Type)
        {
            default:
            case RangeDiceType.In: return $"In {Min} - {Max}"; 
            case RangeDiceType.Out: return $"Out {Min} - {Max}";
            case RangeDiceType.Double: return $"In {Min} - {Max} or {Min2} - {Max2}";
        }
    }
    
    public override string ToCSV(Dice.IGameConfig gamecofig, long TotalBetsPlaced, decimal Balance)
    {
        return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}"
            , TotalBetsPlaced, Roll, Type.ToString(), ToRange(), GetWin(gamecofig) ? "win" : "lose", TotalAmount, Profit, Balance, Profit);
    }
}