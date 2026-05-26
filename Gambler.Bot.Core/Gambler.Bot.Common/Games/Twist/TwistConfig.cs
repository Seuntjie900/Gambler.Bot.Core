using Gambler.Bot.Common.Games.Dice;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gambler.Bot.Common.Games.Twist
{
    
    public class TwistConfig : IGameConfig
    {
        public decimal Edge { get; set; }
        public decimal MaxRoll { get; set; }

    }
}
