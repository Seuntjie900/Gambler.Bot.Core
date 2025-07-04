﻿using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Core.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Gambler.Bot.Core.Tests.WinDiceT
{
    public class WindiceTests : BaseSiteTests, IClassFixture<WindiceFixture>
    {
        public WindiceTests(WindiceFixture fixure) : base(fixure.site)
        {

        }

        [Fact]
        public override void a2_LogInWithout2faWhenRequiredShouldNotLogIn()
        {
            //test s not applicable
            Assert.True(true);
        }
        [Fact]
        public override void a3_LogInWith2faWhenNotRequiredShouldLogIn()
        {
            //test s not applicable
            Assert.True(true);
        }
        [Fact]
        public override void a4_LogInWit2faWhenRequiredShouldLogIn()
        {
            //test s not applicable
            Assert.True(true);
        }

        //Validate the roll verifier for dice
        [Fact]
        public void ValidateDiceBet()
        {
            string server = "z88Lr44PbhBOYFSGRblK52fda9lWTV934eLokE9cQclXMRf43OjwuYJ5jJoP8Ps3";
            string client = "16931020091461886569";
            int nonce = 4698;
            decimal roll = 62.00m;

            var result = this._site.GetLucky(server, client, nonce, Gambler.Bot.Common.Games.Games.Dice);

            Assert.NotNull(result);
            Assert.NotNull((result as DiceResult));
            Assert.Equal(roll, (result as DiceResult).Roll);
        }
    }
}
