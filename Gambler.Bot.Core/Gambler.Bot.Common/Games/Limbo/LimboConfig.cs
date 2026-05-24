using Gambler.Bot.Common.Games.Dice;
using System;
using System.Linq;

namespace Gambler.Bot.Common.Games.Limbo
{
    public class LimboConfig : IGameConfig
    {
        public decimal Edge { get; set; }
        public decimal MaxPayout { get; set; }
    }
}
