using Gambler.Bot.Common.Enums;
using Gambler.Bot.Common.Games;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Games.Limbo;
using Gambler.Bot.Common.Helpers;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites.Classes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Gambler.Bot.Core.Sites.Bitvest;

namespace Gambler.Bot.Core.Sites
{
    public class WolfBet : BaseSite, iDice, iLimbo
    {
        string accesstoken = "";
        public bool ispd = false;
        DateTime lastupdate = new DateTime();
        HttpClient Client;
        HttpClientHandler ClientHandlr;

        public DiceConfig DiceSettings { get; set; }
        public LimboConfig LimboSettings { get; set; }

        public WolfBet(ILogger logger) : base(logger)
        {

            configure();
        }

        void configure()
        {
            StaticLoginParams = new LoginParameter[] { new LoginParameter("API Key", false, true, false, false) };
            //this.MaxRoll = 99.99m;
            this.SiteAbbreviation = "WB";
            this.SiteName = "Wolfbet";
            this.SiteURL = "https://Wolfbet.com?c=Seuntjie";
            this.Mirrors.Add("https://Wolfbet.com");
            AffiliateCode = "?c=Seuntjie";
            this.Stats = new SiteStats();
            this.TipUsingName = true;
            this.AutoInvest = false;
            this.AutoWithdraw = false;
            this.CanChangeSeed = true;
            this.CanChat = false;
            this.CanGetSeed = false;
            this.CanRegister = false;
            this.CanSetClientSeed = true;
            this.CanTip = false;
            this.CanVerify = false;
            this.Currencies = new string[] { "ada","bch","bnb","bonk","btc","doge","dot","etc","eth","floki","ltc",
            "matic","optim","pepe","shib","sol","sushi","ton","trx","uni","usdt","xlm","xrp"};
            SupportedGames = new Games[] { Games.Dice, Games.Limbo };
            this.CurrentCurrency = "btc";
            this.DiceBetURL = "https://Wolfbet.com?c=Seuntjie/{0}";
            //this.Edge = 1;
            DiceSettings = new DiceConfig() { Edge = 1, MaxRoll = 99.99m };
            LimboSettings = new LimboConfig { Edge = 1, MaxPayout = 1000000m };
            NonceBased = true;

        }


        public override void SetProxy(ProxyDetails ProxyInfo)
        {

        }

        protected override void _Disconnect()
        {
            this.ispd = false;
            Client = null;
            ClientHandlr = null;
        }

        protected override async Task<bool> _Login(LoginParamValue[] LoginParams)
        {

            ClientHandlr = new HttpClientHandler { UseCookies = true, AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip };
            Client = new HttpClient(ClientHandlr) { BaseAddress = new Uri(URLInUse + "/api/v1/") };
            Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            Client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.117 Safari/537.36");
            Client.DefaultRequestHeaders.Add("Origin", URLInUse);
            Client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            try
            {
                string Password = LoginParams.First(m => m.Param?.Name?.ToLower() == "api key").Value;
                if (Password != null)
                {
                    Client.DefaultRequestHeaders.Add("authorization", "Bearer " + Password);
                }
                string sEmitResponse = await Client.GetStringAsync("user/balances");
                try
                {
                    WolfBetProfile tmpProfile = JsonSerializer.Deserialize<WolfBetProfile>(sEmitResponse);
                    if (tmpProfile.balances != null)
                    {
                        //set balance here
                        foreach (Balance x in tmpProfile.balances)
                        {
                            if (x.currency.ToLower() == CurrentCurrency.ToLower())
                            {
                                Stats.Balance = decimal.Parse(x.amount, System.Globalization.NumberFormatInfo.InvariantInfo);
                            }
                        }
                        //get stats
                        //set stats
                        sEmitResponse = await Client.GetStringAsync("user/stats/bets");
                        WolfBetStats tmpStats = JsonSerializer.Deserialize<WolfBetStats>(sEmitResponse);
                        SetStats(tmpStats);
                        ispd = true;
                        lastupdate = DateTime.Now;
                        new Thread(new ThreadStart(GetBalanceThread)).Start();
                        this.callLoginFinished(true);
                        return true;
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e.ToString());
                    _logger?.LogInformation(sEmitResponse);
                    callError(sEmitResponse, false, ErrorType.Unknown);
                    callNotify("Error: " + sEmitResponse);

                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());

            }
            this.callLoginFinished(false);
            return false;
        }

        void GetBalanceThread()
        {
            while (ispd)
            {
                try
                {
                    if (((DateTime.Now - lastupdate).TotalSeconds > 30 || ForceUpdateStats))
                    {
                        lastupdate = DateTime.Now;
                        ForceUpdateStats = false;
                        UpdateStats();
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e.ToString());
                }
                Thread.Sleep(100);
            }
        }
        protected override async Task<SiteStats> _UpdateStats()
        {
            try
            {
                string sEmitResponse = await Client.GetStringAsync("user/balances");
                WolfBetProfile tmpProfile = JsonSerializer.Deserialize<WolfBetProfile>(sEmitResponse);
                if (tmpProfile.user != null)
                {
                    //set balance here
                    foreach (Balance x in tmpProfile.user.balances)
                    {
                        if (x.currency.ToLower() == CurrentCurrency.ToLower())
                        {
                            Stats.Balance = decimal.Parse(x.amount, System.Globalization.NumberFormatInfo.InvariantInfo);
                        }
                    }
                    //get stats
                    //set stats
                    sEmitResponse = await Client.GetStringAsync("user/stats/bets");
                    WolfBetStats tmpStats = JsonSerializer.Deserialize<WolfBetStats>(sEmitResponse);
                    return SetStats(tmpStats);
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
            }
            return null;
        }

        public async Task<DiceBet> PlaceDiceBet(PlaceDiceBet BetDetails)
        {
            try
            {

                decimal tmpchance = Math.Round(BetDetails.Chance, 2);
                WolfPlaceDiceBet tmp = new WolfPlaceDiceBet
                {
                    amount = BetDetails.Amount.ToString("0.00000000", System.Globalization.NumberFormatInfo.InvariantInfo),
                    currency = CurrentCurrency,
                    rule = BetDetails.High ? "over" : "under",
                    multiplier = ((100m - DiceSettings.Edge) / tmpchance).ToString("0.####", System.Globalization.NumberFormatInfo.InvariantInfo),
                    bet_value = (BetDetails.High ? DiceSettings.MaxRoll - tmpchance : tmpchance).ToString("0.##", System.Globalization.NumberFormatInfo.InvariantInfo)                    
                };
                string LoginString = JsonSerializer.Serialize(tmp);
                HttpContent cont = new StringContent(LoginString);
                cont.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage resp2 = await Client.PostAsync("bet/place", cont);

                string sEmitResponse = resp2.Content.ReadAsStringAsync().Result;
                if (!resp2.IsSuccessStatusCode)
                {

                }

                try
                {
                    WolfDiceBetResult result = JsonSerializer.Deserialize<WolfDiceBetResult>(sEmitResponse);
                    if (result.bet != null)
                    {
                        DiceBet tmpRsult = new DiceBet()
                        {

                            TotalAmount = decimal.Parse(result.bet.amount, System.Globalization.NumberFormatInfo.InvariantInfo),
                            Chance = BetDetails.High ? DiceSettings.MaxRoll - decimal.Parse(result.bet.bet_value, System.Globalization.NumberFormatInfo.InvariantInfo) : decimal.Parse(result.bet.bet_value, System.Globalization.NumberFormatInfo.InvariantInfo),
                            ClientSeed = result.bet.user_seed,
                            DateValue = DateTime.Now,
                            Currency = CurrentCurrency,
                            Guid = BetDetails.GUID,
                            Nonce = result.bet.nonce,
                            BetID = result.bet.hash,
                            High = BetDetails.High,
                            Roll = decimal.Parse(result.bet.result_value, System.Globalization.NumberFormatInfo.InvariantInfo),
                            Profit = decimal.Parse(result.bet.profit, System.Globalization.NumberFormatInfo.InvariantInfo),
                            ServerHash = result.bet.server_seed_hashed
                        };
                        Stats.Bets++;
                        bool Win = (((bool)BetDetails.High ? tmpRsult.Roll > (decimal)DiceSettings.MaxRoll - (decimal)(tmpRsult.Chance) : (decimal)tmpRsult.Roll < (decimal)(tmpRsult.Chance)));
                        if (Win)
                            Stats.Wins++;
                        else Stats.Losses++;
                        Stats.Wagered += tmpRsult.TotalAmount;
                        Stats.Profit += tmpRsult.Profit;
                        Stats.Balance = decimal.Parse(result.userBalance.amount, CultureInfo.InvariantCulture);

                        callBetFinished(tmpRsult);
                        return tmpRsult;
                    }
                    else
                    {
                        _logger.LogError(sEmitResponse, -1);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString(), -1);
                    _logger.LogDebug(sEmitResponse, -1);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString(), -1);
            }
            return null;
        }
        SiteStats SetStats(WolfBetStats Stats)
        {
            try
            {
                PropertyInfo tmp = typeof(Dice).GetProperty(CurrentCurrency.ToLower());
                if (tmp != null)
                {
                    WBStat stat = tmp.GetValue(Stats.dice) as WBStat;
                    if (stat != null)
                    {
                        this.Stats.Bets = int.Parse(stat.total_bets);
                        this.Stats.Wins = int.Parse(stat.win);
                        this.Stats.Losses = int.Parse(stat.lose);
                        this.Stats.Wagered = decimal.Parse(stat.waggered, System.Globalization.NumberFormatInfo.InvariantInfo);
                        this.Stats.Profit = decimal.Parse(stat.profit, System.Globalization.NumberFormatInfo.InvariantInfo);

                    }
                }

            }
            catch
            {
                this.Stats.Bets = 0;
                this.Stats.Wins = 0;
                this.Stats.Losses = 0;
                this.Stats.Wagered = 0;
                this.Stats.Profit = 0;

            }
            return this.Stats;
        }

        public async Task<LimboBet> PlaceLimboBet(PlaceLimboBet bet)
        {
            try
            {

                
                WolfPlaceLimboBet tmp = new WolfPlaceLimboBet
                {
                    amount = bet.Amount.ToString("0.00000000", System.Globalization.NumberFormatInfo.InvariantInfo),
                    currency = CurrentCurrency,
                    multiplier = bet.Payout.ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo)                    
                };
                string LoginString = JsonSerializer.Serialize(tmp);
                HttpContent cont = new StringContent(LoginString);
                cont.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
                HttpResponseMessage resp2 = await Client.PostAsync("/api/v2/limbo/manual/play", cont);

                string sEmitResponse = resp2.Content.ReadAsStringAsync().Result;
                if (!resp2.IsSuccessStatusCode)
                {

                }

                try
                {
                    WolfLimboBetResult result = JsonSerializer.Deserialize<WolfLimboBetResult>(sEmitResponse);
                    if (result.bet != null)
                    {
                        LimboBet tmpRsult = new LimboBet()
                        {

                            TotalAmount = decimal.Parse(result.bet.amount, System.Globalization.NumberFormatInfo.InvariantInfo),
                            
                            ClientSeed = result.bet.user_seed,
                            DateValue = DateTime.Now,
                            Currency = CurrentCurrency,
                            Guid = bet.GUID,
                            Nonce = result.bet.nonce,
                            BetID = result.bet.hash,                            
                            Payout = decimal.Parse(result.bet.multiplier, System.Globalization.NumberFormatInfo.InvariantInfo),
                            Result = decimal.Parse(result.bet.result_value, System.Globalization.NumberFormatInfo.InvariantInfo),
                            Profit = decimal.Parse(result.bet.profit, System.Globalization.NumberFormatInfo.InvariantInfo),
                            ServerHash = result.bet.server_seed_hashed
                        };
                        Stats.Bets++;
                        tmpRsult.IsWin = tmpRsult.Result>= decimal.Parse(result.bet.multiplier, System.Globalization.NumberFormatInfo.InvariantInfo);
                        if (tmpRsult.IsWin)
                            Stats.Wins++;
                        else Stats.Losses++;
                        Stats.Wagered += tmpRsult.TotalAmount;
                        Stats.Profit += tmpRsult.Profit;
                        Stats.Balance = decimal.Parse(result.userBalance.amount, CultureInfo.InvariantCulture);

                        callBetFinished(tmpRsult);
                        return tmpRsult;
                    }
                    else
                    {
                        _logger.LogError(sEmitResponse, -1);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e.ToString(), -1);
                    _logger.LogDebug(sEmitResponse, -1);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString(), -1);
            }
            return null;
        }

        protected override async Task<SeedDetails> _ResetSeed()
        {
            var response = await Client.GetAsync("game/seed/refresh");
            string Resuult = await response.Content.ReadAsStringAsync();
           
            Resuult = await response.Content.ReadAsStringAsync();
            try
            {
                Game tmp = JsonSerializer.Deserialize<Game>(Resuult);
                if (tmp != null)
                {
                    SeedDetails tmpSeed = new SeedDetails();
                   
                    callResetSeedFinished(true, "");
                    return tmpSeed;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                callResetSeedFinished(false, e.ToString());
            }
            return null;
        }

        protected override IGameResult _GetLucky(string ServerSeed, string ClientSeed, int Nonce, Games Game)
        {
            
            if (Game == Games.Dice)
            {
                string msg = ClientSeed + "_" + Nonce.ToString();
                int charstouse = 5;
                string hex = Hash.HMAC256(msg, ServerSeed);
                for (int i = 0; i < hex.Length; i += charstouse)
                {

                    string s = hex.ToString().Substring(i, charstouse);

                    decimal lucky = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                    if (lucky < 1000000)
                        return new DiceResult { Roll = lucky % 10000 / 100m };
                }
            }
            else if (Game == Games.Limbo)
            {
                string msg = $"{ClientSeed}_{Nonce}_0";
                
                string hex = Hash.HMAC256(ServerSeed, msg);
                int charstouse = 2;
                decimal number = 0;
                for (int i = 0; i < 4; i++)
                {

                    string s = hex.ToString().Substring(i * charstouse, charstouse);
                    decimal part = Math.Round(((decimal)int.Parse(s, System.Globalization.NumberStyles.HexNumber)) / (decimal)(Math.Pow(256, i + 1)),10);
                    number += part;

                }
                number = number * 1e8m;
                decimal normalizedFloat = Math.Max(number, 0.01m);
                decimal floatPoint = 1e8m / normalizedFloat * (1 - LimboSettings.Edge / 100m);                
                decimal clampedFloatPoint = Math.Min(floatPoint, 9900000);         
                decimal crashPoint = Math.Floor(clampedFloatPoint * 100) / 100;               
                return new LimboResult { Result = Math.Max(crashPoint, 1) };
               
            }
                return null;
        }

        protected override Task<bool> _BrowserLogin()
        {
            throw new NotImplementedException();
        }

        public class WolfBetLogin
        {
            public string access_token { get; set; }
            public string token_type { get; set; }
            public string expires_in { get; set; }
        }
        public class Preferences
        {
            public bool public_total_profit { get; set; }
            public bool public_total_wagered { get; set; }
            public bool public_bets { get; set; }
        }

        public class Balance
        {
            public string amount { get; set; }
            public string currency { get; set; }
            public string withdraw_fee { get; set; }
            public string withdraw_minimum_amount { get; set; }
            public bool payment_id_required { get; set; }
        }

        public class Game2
        {
            public string name { get; set; }
        }

        public class Game
        {
            public string server_seed_hashed { get; set; }
            public Game2 game { get; set; }
        }

        public class User
        {
            public string login { get; set; }
            public string email { get; set; }
            public bool two_factor_authentication { get; set; }
            public bool has_email_to_verify { get; set; }
            public string last_nonce { get; set; }
            public string seed { get; set; }
            public string channel { get; set; }
            public string joined { get; set; }

            public List<Balance> balances { get; set; }
            public List<Game> games { get; set; }
        }



        public class History
        {
            public string amount { get; set; }
            public string currency { get; set; }
            public int step { get; set; }
            public long published_at { get; set; }
        }

        public class Values
        {
            public string btc { get; set; }
            public string eth { get; set; }
            public string ltc { get; set; }
            public string doge { get; set; }
            public string trx { get; set; }
            public string bch { get; set; }
        }

        public class Next
        {
            public decimal step { get; set; }
            public Values values { get; set; }
        }

        public class DailyStreak
        {
            public List<History> history { get; set; }
            public Next next { get; set; }
        }

        public class WolfBetProfile
        {
            public User user { get; set; }
            public List<Balance> balances { get; set; }

        }
        public class WBStat
        {
            public string total_bets { get; set; }
            public string win { get; set; }
            public string lose { get; set; }
            public string waggered { get; set; }
            public string currency { get; set; }
            public string profit { get; set; }
        }

        public class Dice
        {
            public WBStat doge { get; set; }
            public WBStat btc { get; set; }
            public WBStat eth { get; set; }
            public WBStat ltc { get; set; }
            public WBStat trx { get; set; }
            public WBStat bch { get; set; }
        }

        public class WolfBetStats
        {
            public Dice dice { get; set; }
        }

        public class WolfPlaceBet
        {
            public string currency { get; set; }
            public string amount { get; set; }
            public string game { get; set; }
            public int auto { get; set; } = 1;
        }

        public class WolfPlaceDiceBet: WolfPlaceBet
        {
            public WolfPlaceDiceBet()
            {
                game = "dice";
            }
            public string rule { get; set; }
            public string multiplier { get; set; }
            public string bet_value { get; set; }
        }

        public class WolfPlaceLimboBet:WolfPlaceBet
        {
            public WolfPlaceLimboBet()
            {
                game = "limbo";
            }
            public string multiplier { get; set; }
        }

        public class WBBaseBet
        {
            public string hash { get; set; }
            public int nonce { get; set; }
            public string user_seed { get; set; }
            public string currency { get; set; }
            public string amount { get; set; }
            public string profit { get; set; }
            public string state { get; set; }
            public long published_at { get; set; }
            public Game game { get; set; }
            public User user { get; set; }
        }
        public class WBDiceBet: WBBaseBet
        {
            public string multiplier { get; set; }
            public string bet_value { get; set; }
            public string result_value { get; set; }
            public string server_seed_hashed { get; set; }
        }
        public class WBLimboBet : WBBaseBet
        {
            public string multiplier { get; set; }            
            public string result_value { get; set; }
            public string server_seed_hashed { get; set; }
        }

        public class UserBalance
        {
            public string amount { get; set; }
            public string currency { get; set; }
            public string withdraw_fee { get; set; }
            public string withdraw_minimum_amount { get; set; }
            public bool payment_id_required { get; set; }
            
        }

        public class WolfBaseBetResult
        {
            
            public UserBalance userBalance { get; set; }
        }
        public class WolfDiceBetResult
        {
            public WBDiceBet bet { get; set; }
            public UserBalance userBalance { get; set; }
        }
        public class WolfLimboBetResult
        {
            public WBLimboBet bet { get; set; }
            public UserBalance userBalance { get; set; }
        }
    }
}
