using Gambler.Bot.Common.Enums;
using Gambler.Bot.Common.Games;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Core.Events;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites;
using Gambler.Bot.Core.Tests.Code;

namespace Gambler.Bot.Core.Tests
{
    [TestCaseOrderer("Gambler.Bot.Core.Tests.Code.AlphabeticalOrderer", "Gambler.Bot.Core.Tests")]
    public abstract class DiceTests: IAsyncLifetime
    {
        protected BaseSite _site;

        public DiceTests(BaseSite site)
        {
           
            _site = site;
            _site.OnBrowserBypassRequired = _site_OnBrowserBypassRequired;
        }

        public async Task DisposeAsync()
        {
           //don't do shit here, leave my site connected please
        }

        public async Task InitializeAsync()
        {
            if (!_site.LoggedIn)
            {
                bool loggediin = false;
                bool finished = false;
                _site.LoginFinished += (sender, args) => { loggediin = args.Success; finished = true; };
                _site.LogIn(_site.Mirrors[0], BaseSiteTests.GetParams(_site.SiteName, "a4_LogInWit2faWhenRequiredShouldLogIn"));
                DateTime start = DateTime.Now;
                while (!finished && (DateTime.Now - start).TotalSeconds < 30)
                {
                    await Task.Delay(1000);
                }
                if (!loggediin)
                {
                    throw new Exception("Failed to log in to site");
                }
            }
        }

        private async Task<BrowserConfig> _site_OnBrowserBypassRequired(BypassRequiredArgs e)
        {
            BaseSiteTests.bypassthingy tmpthingy = new BaseSiteTests.bypassthingy { args = e };
            Thread tttttt = new Thread(new ParameterizedThreadStart(CreateBypassThread));
            tttttt.SetApartmentState(ApartmentState.STA);
            tttttt.Start(tmpthingy);
            while (tmpthingy.form == null)
            {
                Thread.Sleep(100);
            }

            e.Config = tmpthingy.form.GetBypass(e);

            tmpthingy.form.CloseForm();
            return e.Config;
        }

        [STAThread]
        private void CreateBypassThread(object config)
        {
            BaseSiteTests.bypassthingy e = (BaseSiteTests.bypassthingy)config;
            e.form = new BrowserBypass(e.args.URL);
            e.form.ShowDialog();
        }


        //Tests for dice?
        //bet with negative amount: -0.00001 // should trigger error event with appropriate message

        [Fact]
        public void BetNegativeAmount()
        {
            bool finished = false;
            string error = "";
            _site.Error += (sender, args) => { finished = true; error = args.Message; };
            _site.BetFinished += (sender, args) => { finished = true; };
            _site.PlaceBet(new PlaceDiceBet( -0.00001m,true, 49.5m));
            DateTime start = DateTime.Now;
            while (!finished && (DateTime.Now - start).TotalSeconds < 30)
            {
                Task.Delay(1000).Wait();
            }
            Assert.True(finished);
            Assert.NotNull( error);
            Assert.Contains("Bet cannot be < 0.", error);
        }


        //bet with invalid chance: 0%,0.1%;1%,99%,99.99%,100% // should trigger error event with appropriate message
        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        public void BetInvalidChance(decimal chance)
        {
            bool finished = false;
            string error = "";
            ErrorType errorType = ErrorType.Unknown;
            decimal balance = _site.Stats.Balance;
            _site.Error += (sender, args) => { finished = true; error = args.Message; errorType = args.Type; };
            _site.BetFinished += (sender, args) => { finished = true; };
            _site.PlaceBet(new PlaceDiceBet(0.000001m, true, chance));
            DateTime start = DateTime.Now;
            while (!finished && (DateTime.Now - start).TotalSeconds < 30)
            {
                Task.Delay(1000).Wait();
            }
            Assert.True(finished);
            Assert.NotNull(error);
            Assert.Equal(ErrorType.InvalidBet, errorType);
            if (chance==0)
            {
                Assert.Contains("Chance to win must be > 0", error);
            }
            Assert.Equal(balance.ToString("d8"), _site.Stats.Balance.ToString("d8"));
        }

        //bet with 0 amount if supported by site else should trigger error event with appropriate message
        [Fact]
        public void BetZeroSucceedIfAllowed()
        {
            bool finished = false;
            string error = null;           
            ErrorType errorType = ErrorType.Unknown;
            decimal balance = _site.Stats.Balance;
            DiceBet newbet = null;
            _site.Error += (sender, args) => { finished = true; error = args.Message; errorType = args.Type; };
            _site.BetFinished += (sender, args) => { finished = true; newbet = args.NewBet as DiceBet; };
            _site.PlaceBet(new PlaceDiceBet(00, true, 49.5m));
            DateTime start = DateTime.Now;
            while (!finished && (DateTime.Now - start).TotalSeconds < 30)
            {
                Task.Delay(1000).Wait();
            }
            Assert.True(finished);
            if (error==null && newbet!=null)
            {
                Assert.Null(error);
                Assert.Equal(0,newbet.TotalAmount);
                Assert.Equal(0, newbet.Profit);
                //the bet was a success. assert sucess conditions
            }
            else
            {
                //the bet failed, assert bet too low conditions
                Assert.Equal(ErrorType.BetTooLow, errorType);
            }
            
            Assert.Equal(balance, _site.Stats.Balance);
        }


        //bet with over site max profit amount (int.max) //should trigger error event with appropriate message
        [Fact]
        public void BetOverSiteMaxAmount()
        {
            bool finished = false;
            string error = "";
            ErrorType errorType = ErrorType.Unknown;
            decimal balance = _site.Stats.Balance;
            _site.Error += (sender, args) => { finished = true; error = args.Message; errorType = args.Type; };
            _site.BetFinished += (sender, args) => { finished = true; };
            _site.PlaceBet(new PlaceDiceBet(int.MaxValue, true, 49.5m));
            DateTime start = DateTime.Now;
            while (!finished && (DateTime.Now - start).TotalSeconds < 30)
            {
                Task.Delay(1000).Wait();
            }
            Assert.True(finished);
            Assert.NotNull(error);
            Assert.Contains<ErrorType>(errorType, new ErrorType[] { ErrorType.BalanceTooLow, ErrorType.InvalidBet });
            Assert.Equal(balance, _site.Stats.Balance);

        }

        //bet over user balance, below site max profit) //should trigger error event with appropriate message //bet with 0 amount if supported by site else should trigger error event with appropriate message
        [Fact]
        public void BetOverUserBalance()
        {
            bool finished = false;
            string error = "";
            ErrorType errorType = ErrorType.Unknown;
            decimal balance = _site.Stats.Balance;
            decimal betamount = balance * 1.1m;
            if (betamount<=0.00000001m)
            {
                betamount = 0.000001m;
            }
            _site.Error += (sender, args) => { finished = true; error = args.Message; errorType = args.Type; };
            _site.BetFinished += (sender, args) => { finished = true; };
            _site.PlaceBet(new PlaceDiceBet(betamount, true, 49.5m));
            DateTime start = DateTime.Now;
            while (!finished && (DateTime.Now - start).TotalSeconds < 30)
            {
                Task.Delay(1000).Wait();
            }
            Assert.True(finished);
            Assert.NotNull(error);
            Assert.Equal(ErrorType.BalanceTooLow, errorType);
            Assert.Equal(balance, _site.Stats.Balance);
        }

        //valid bet at different chances and currencies: (amounts 0.0000001, chance 5%-95% with 10% steps) // should fire bet finished event with appropriate bet values
        [Theory]
        [InlineData(0.000001,5,true)]
        [InlineData(0.000001, 15, false)]
        [InlineData(0.000001, 25, true)]
        [InlineData(0.000001, 35, false)]
        [InlineData(0.000001, 49.5, true)]
        [InlineData(0.000001, 49.5, false)]
        [InlineData(0.000001, 65, false)]
        [InlineData(0.000001, 75, true)]
        [InlineData(0.000001, 85, false)]
        [InlineData(0.000001, 95, true)]
        public void ValidBets(decimal amount,decimal chance, bool high)
        {
            bool finished = false;
            string error = null;
            ErrorType errorType = ErrorType.Unknown;
            decimal balance = _site.Stats.Balance;
            DiceBet resultingbet = null;
            _site.Error += (sender, args) => { finished = true; error = args.Message; errorType = args.Type; };
            _site.BetFinished += (sender, args) => { finished = true; resultingbet = args.NewBet as DiceBet; };
            _site.PlaceBet(new PlaceDiceBet(amount, high, chance));
            DateTime start = DateTime.Now;
            while (!finished && (DateTime.Now - start).TotalSeconds < 30)
            {
                Task.Delay(1000).Wait();
            }
            var dicesettings = (_site as iDice).DiceSettings;
            Assert.True(finished);
            Assert.Null(error);
            Assert.Equal(amount, resultingbet.TotalAmount);
            Assert.Equal(chance, resultingbet.Chance);
            Assert.Equal(high, resultingbet.High);
            bool ShouldBeWin = (((bool)high ? (decimal)resultingbet.Roll > (decimal)dicesettings.MaxRoll - (decimal)(chance) : (decimal)resultingbet.Roll < (decimal)(chance)));
            Assert.Equal(ShouldBeWin, resultingbet.IsWin);
            Assert.Equal(ShouldBeWin, resultingbet.GetWin(dicesettings));
            if (resultingbet.IsWin)
            {
                Assert.Equal(balance+resultingbet.Profit, _site.Stats.Balance);
                decimal AssumedProfit = Math.Floor(((((100 - dicesettings.Edge) / chance) * amount)-amount)*100000000m)/100000000m;
                Assert.Equal(AssumedProfit.ToString("0.00000000"), resultingbet.Profit.ToString("0.00000000"));
            }
            else
            {
                Assert.Equal(balance -resultingbet.TotalAmount, _site.Stats.Balance);
            }
            
            

            //assert the rest of the bet as far as possible
            //double check the chance, payout,profit, amount, high/low

        }
    }
}
