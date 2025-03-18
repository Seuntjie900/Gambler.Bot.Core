﻿using Gambler.Bot.Common.Games.Dice;
using System;
using System.Linq;

namespace Gambler.Bot.Common.Games.Limbo
{
    public class Twist : IGameConfig
    {
        public decimal Edge { get; set; }
        public decimal MinChance { get; set; }
    }
}
