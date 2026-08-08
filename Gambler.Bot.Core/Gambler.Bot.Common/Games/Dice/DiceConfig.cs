using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gambler.Bot.Common.Games.Dice
{
    
    public class DiceConfig : IGameConfig
    {
        public decimal Edge { get; set; }
        public decimal MaxRoll { get; set; }

    }
}
