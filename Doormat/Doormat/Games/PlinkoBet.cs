﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Gambler.Bot.Core.Sites;

namespace Gambler.Bot.Core.Games
{
    [MoonSharp.Interpreter.MoonSharpUserData]
    public class PlinkoBet : Bet
    {
        public override PlaceBet CreateRetry()
        {
            throw new NotImplementedException();
        }

        public override bool GetWin(BaseSite Site)
        {
            throw new NotImplementedException();
        }
    }
    [MoonSharp.Interpreter.MoonSharpUserData]
    public class PlacePlinkoBet : PlaceBet
    {

    }

    public interface iPlinko
    {
        Task<PlinkoBet> PlacePlinkoBet(PlacePlinkoBet BetDetails);
    }
}
