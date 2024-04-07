﻿using DoormatCore.Games;
using DoormatCore.Sites;
using Newtonsoft.Json.Bson;
using OtpNet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoormatCore.Tests
{
    [TestCaseOrderer("DoormatCore.Tests.Code.AlphabeticalOrderer", "DoormatCore.Tests")]
    public abstract class DiceTests: IAsyncLifetime
    {
        BaseSite _site;

        public DiceTests(BaseSite site)
        {
           
            _site = site;
            _site.OnBrowserBypassRequired += _site_OnBrowserBypassRequired;
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
                _site.LogIn(BaseSiteTests.GetParams(_site.SiteName, "a4_LogInWit2faWhenRequiredShouldLogIn"));
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

        private void _site_OnBrowserBypassRequired(object? sender, BypassRequiredArgs e)
        {
            //yo how tf am I going to do this?
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
        [InlineData(0.1)]
        [InlineData(99)]
        [InlineData(99.99)]
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
            Assert.Equal(balance, _site.Stats.Balance);
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
            Assert.True(finished);
            Assert.Null(error);
            
            if (resultingbet.IsWin)
            {
                Assert.Equal(balance+resultingbet.Profit, _site.Stats.Balance);
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
