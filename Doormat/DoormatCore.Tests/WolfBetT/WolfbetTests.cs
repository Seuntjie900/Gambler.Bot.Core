﻿using DoormatCore.Sites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace DoormatCore.Tests.WolfBetT
{
    [TestCaseOrderer("DoormatCore.Tests.Code.AlphabeticalOrderer", "DoormatCore.Tests")]
    public class WolfbetTests : BaseSiteTests, IClassFixture<WolfBet>
    {
        public WolfbetTests(WolfBet fixure) : base(fixure)
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
