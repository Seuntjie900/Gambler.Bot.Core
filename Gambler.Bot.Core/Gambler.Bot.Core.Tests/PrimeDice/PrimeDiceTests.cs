﻿using Gambler.Bot.Core.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Gambler.Bot.Core.Tests.PrimeDiceT
{
    [TestCaseOrderer("Gambler.Bot.Core.Tests.Code.AlphabeticalOrderer", "Gambler.Bot.Core.Tests")]
    public class PrimeDiceTests : BaseSiteTests, IClassFixture<PrimeDiceFixture>
    {
        public PrimeDiceTests(PrimeDiceFixture fixure) : base(fixure.site)
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
    }
}
