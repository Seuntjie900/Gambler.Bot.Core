using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Games.Limbo;
using Gambler.Bot.Common.Games.Twist;
using Gambler.Bot.Core.Sites;
using Gambler.Bot.Core.Tests.Code;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace Gambler.Bot.Core.Tests.BitslerT
{
    [TestCaseOrderer("Gambler.Bot.Core.Tests.Code.AlphabeticalOrderer", "Gambler.Bot.Core.Tests")]
    public class BitslerTests : BaseSiteTests, IClassFixture<BitslerFixture>
    {
        public BitslerTests(BitslerFixture fixure) : base(fixure.site)
        {

        }
        
        [Fact]
        public override void a3_LogInWith2faWhenNotRequiredShouldLogIn()
        {
            //test s not applicable
            Assert.True(true);
        }
        //Validate the roll verifier for dice
        [Fact]
        public void ValidateDiceBet()
        {
            string server = "f98342c962759ec75d79fc89fb9a2c89b1bfa332f34568350ac45e42aa4c2b9e5d2c524b6015dcb729dfaa08c5881d220eb462aac9c4cf83ee1c1c26346f2e5f";
            string client = "8U9Of8OXUFhGyYmJ";
            int nonce = 13;
            decimal roll = 31.12m;

            var result = this._site.GetLucky(server, client, nonce, Gambler.Bot.Common.Games.Games.Dice);

            Assert.NotNull(result);
            Assert.NotNull((result as DiceResult));
            Assert.Equal(roll, (result as DiceResult).Roll);
        }

        //Validate the roll verifier for Limbo
        [Fact]
        public void ValidateLimboBet()
        {
            string server = "f98342c962759ec75d79fc89fb9a2c89b1bfa332f34568350ac45e42aa4c2b9e5d2c524b6015dcb729dfaa08c5881d220eb462aac9c4cf83ee1c1c26346f2e5f";
            string client = "8U9Of8OXUFhGyYmJ";
            int nonce = 14;
            decimal roll = 2.58m;

            var result = this._site.GetLucky(server, client, nonce, Gambler.Bot.Common.Games.Games.Limbo);

            Assert.NotNull(result);
            Assert.NotNull((result as LimboResult));
            Assert.Equal(roll, (result as LimboResult).Result);
        }

        //Validate the roll verifier for Twist
        [Fact]
        public void ValidateTwistBet()
        {
            string server = "f98342c962759ec75d79fc89fb9a2c89b1bfa332f34568350ac45e42aa4c2b9e5d2c524b6015dcb729dfaa08c5881d220eb462aac9c4cf83ee1c1c26346f2e5f";
            string client = "8U9Of8OXUFhGyYmJ";
            int nonce = 15;
            decimal roll =10m;

            var result = this._site.GetLucky(server, client, nonce, Gambler.Bot.Common.Games.Games.Twist);

            Assert.NotNull(result);
            Assert.NotNull((result as TwistResult));
            Assert.Equal(roll, (result as TwistResult).Roll);
        }
    }
}
