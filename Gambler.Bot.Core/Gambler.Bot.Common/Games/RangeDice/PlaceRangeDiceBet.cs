namespace Gambler.Bot.Common.Games.RangeDice;

public class PlaceRangeDiceBet : PlaceBet
{
    public RangeDiceType Type { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal Min2 { get; set; }
    public decimal Max2 { get; set; }

    public PlaceRangeDiceBet()
    {
        
    }

    public PlaceRangeDiceBet(RangeDiceType type, decimal amount, decimal min, decimal max, string guid):this(type,amount, min, max)
    {
        GUID = guid;
    }
    public PlaceRangeDiceBet(RangeDiceType type, decimal amount, decimal min, decimal max)
    {
        Type = type;
        Min = min;
        Max = max;
        this.Amount = amount;
        this.Game = Games.RangeDice;
    }
    public PlaceRangeDiceBet(RangeDiceType type, decimal amount, decimal min, decimal max, decimal min2, decimal max2):this(type,amount, min, max)
    {
        Min2 = min2;
        Max2 = max2;
    }
    public PlaceRangeDiceBet(RangeDiceType type, decimal amount, decimal min, decimal max, decimal min2, decimal max2, string guid):this(type,amount, min, max, min2, max2)
    {
        GUID = guid;
    }
}