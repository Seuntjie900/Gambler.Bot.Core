using Gambler.Bot.Common.Enums;
using Gambler.Bot.Common.Games;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Games.RangeDice;
using Gambler.Bot.Common.Helpers;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites.Classes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Gambler.Bot.Core.Sites.Bitvest;

namespace Gambler.Bot.Core.Sites
{
    public class WinDice : BaseSite, iDice, iRangeDice
    {
        string accesstoken = "";
        DateTime LastSeedReset = new DateTime();
        public bool ispd = false;
        string username = "";
        long uid = 0;
        DateTime lastupdate = new DateTime();
        HttpClient Client;
        HttpClientHandler ClientHandlr;        
        Random R = new Random();
        WDGetSeed currentseed;

        public DiceConfig DiceSettings { get; set; }
        public RangeDiceConfig RangeDiceSettings { get; set; }

        public WinDice(ILogger logger) : base(logger)
        {
            StaticLoginParams = new LoginParameter[] { new LoginParameter("API Key", true, true, false, false) };
            //this.MaxRoll = 99.99m;
            this.SiteAbbreviation = "WD";
            this.SiteName = "WinDice";
            this.SiteURL = "https://windice.io/?r=08406hjdd";
            this.Mirrors.Add("https://windice.io");
            AffiliateCode = "/?r=08406hjdd";
            this.Stats = new SiteStats();
            this.TipUsingName = true;
            this.AutoInvest = false;
            this.AutoWithdraw = false;
            AutoBank = true;
            this.CanChangeSeed = true;
            this.CanChat = false;
            this.CanGetSeed = false;
            this.CanRegister = false;
            this.CanSetClientSeed = false;
            this.CanTip = false;
            this.CanVerify = false;
            this.Currencies = new string[] { "USDT","BTC","ETH","TRX","LTC","DOGE","BCH","XRP","BNB","WIN","TON" };
            SupportedGames = new Games[] { Games.Dice, Games.RangeDice };
            CurrentCurrency ="btc";
            this.DiceBetURL = "https://windice.io/api/v1/api/getBet?hash={0}";
            //this.Edge = 1;
            DiceSettings = new DiceConfig() { Edge = 1, MaxRoll = 99.99m };
            RangeDiceSettings = new RangeDiceConfig { Edge = 1, MaxRoll = 99.99m, SupportsDouble = false };
            NonceBased = true;
        }

        public async Task<DiceBet> PlaceDiceBet(PlaceDiceBet BetDetails)
        {
            _logger.LogDebug("WinDice placing dice bet");
            decimal low = 0;
            decimal high = 0;
            if (BetDetails.High)
            {
                high = DiceSettings.MaxRoll * 100;
                low = (DiceSettings.MaxRoll - BetDetails.Chance) * 100 + 1;
            }
            else
            {
                high = BetDetails.Chance * 100 - 1;
                low = 0;
            }
            string loginjson = JsonSerializer.Serialize<WDPlaceBet>(new WDPlaceBet()
            {
                curr = CurrentCurrency.ToLower(),
                bet = BetDetails.Amount,
                game = "in",
                high = (int)high,
                low = (int)low
            });

            HttpContent cont = new StringContent(loginjson);
            cont.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage resp2 = await Client.PostAsync("roll", cont);

            if (resp2.IsSuccessStatusCode)
            {
                string response = await resp2.Content.ReadAsStringAsync();
                _logger.LogDebug("WinDice bet result:" + response);
                WDBaseResponse statusMessage = JsonSerializer.Deserialize<WDBaseResponse>(response);
                if (statusMessage.status == "error")
                {
                    ErrorType type = ErrorType.Unknown;
                    switch (statusMessage.message)
                    {
                        case "Minimal chance for non-deposit bets is 0.2%": 
                            
                        case "Chance min 0.010000 / max 98.000000":

                            type = ErrorType.InvalidBet;
                            break;
                        case "No balance":

                            type = ErrorType.BalanceTooLow;

                            break;
                        
                        default:
                            type = ErrorType.Unknown;
                            break;
                    }
                    if (type == ErrorType.Unknown)
                    {
                        if (statusMessage.message.StartsWith("Maximum win "))
                            type = ErrorType.InvalidBet;
                        if (statusMessage.message.StartsWith("Minimum bet "))
                            type = ErrorType.BetTooLow;
                        
                    }
                    callError(statusMessage.message, false, type);
                    return null;
                }
                WDBet tmpBalance = JsonSerializer.Deserialize<WDBet>(response);
                if (tmpBalance.status == "success")
                {
                    DiceBet Result = new DiceBet()
                    {
                        TotalAmount = BetDetails.Amount,
                        DateValue = DateTime.Now,
                        Chance = tmpBalance.data.chance,
                        ClientSeed = currentseed.client,
                        Currency = CurrentCurrency,
                        Guid = BetDetails.GUID,
                        BetID = tmpBalance.data.hash,
                        High = BetDetails.High,
                        Nonce = tmpBalance.data.nonce,
                        Profit = tmpBalance.data.win - tmpBalance.data.bet,
                        Roll = tmpBalance.data.result / 100m,
                        ServerHash = currentseed.hash
                    };
                    Stats.Bets++;
                    bool Win = (((bool)BetDetails.High ? (decimal)Result.Roll > (decimal)DiceSettings.MaxRoll - (decimal)(BetDetails.Chance) : (decimal)Result.Roll < (decimal)(BetDetails.Chance)));
                    if (Win)
                        Stats.Wins++;
                    else Stats.Losses++;
                    Stats.Wagered += BetDetails.Amount;
                    Stats.Profit += Result.Profit;
                    Stats.Balance += Result.Profit;
                    callBetFinished(Result);
                    return Result;
                }
                else
                {
                    callNotify(tmpBalance.message);
                    callError(tmpBalance.message, false, ErrorType.Unknown);
                }
            }
            return null;
        }

        public override void SetProxy(ProxyDetails ProxyInfo)
        {
            throw new NotImplementedException();
        }

        protected override void _Disconnect()
        {
            ispd = false;
            Client = null;
            ClientHandlr = null;
        }

        protected override async Task<bool> _Login(LoginParamValue[] LoginParams)
        {
            _logger.LogDebug("WinDice Logging in");
            ClientHandlr = new HttpClientHandler { UseCookies = true, AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip };
            Client = new HttpClient(ClientHandlr) { BaseAddress = new Uri($"{URLInUse}/api/v1/api/") };
            Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            Client.DefaultRequestHeaders.Add("UserAgent", "DiceBot");
            Client.DefaultRequestHeaders.Add("Authorization", LoginParams.First(m=>m.Param?.Name?.ToLower()=="api key")?.Value);
            try
            {
                if (await getbalance())
                {
                    await Task.Delay(50);
                    await getstats();

                    await Task.Delay(50);
                    await getseed();

                    await Task.Delay(50);
                    ispd = true;
                    lastupdate = DateTime.Now;

                    new Thread(new ThreadStart(GetBalanceThread)).Start();
                    //lasthash = tmpblogin.server_hash;
                    callLoginFinished(true);
                    return true;
                }
                else
                {
                    callLoginFinished(false);
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
                callLoginFinished(false);
                return false;
            }
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
                        getbalance();
                        getstats();
                    }
                }
                catch (Exception e)
                {
                    _logger?.LogError(e.ToString());
                }
                Thread.Sleep(1000);
            }
        }
        async Task<bool> getbalance()
        {
            _logger.LogDebug("WinDice getbalance");
            string response = await Client.GetStringAsync("user");
            WDUserResponse tmpBalance = JsonSerializer.Deserialize<WDUserResponse>(response);
            if (tmpBalance.data != null)
            {
                PropertyInfo tmp = typeof(WDBalance).GetProperty(CurrentCurrency.ToLower());
                if (tmp != null)
                {
                    decimal balance = (decimal)tmp.GetValue(tmpBalance.data.balance);
                    Stats.Balance = balance;                    
                }
            }
            return tmpBalance.status == "success";
        }
        async Task<bool> getstats()
        {
            _logger.LogDebug("WinDice GetStats");
            string response = await Client.GetStringAsync("stats");
            WDStatsResponse tmpBalance = JsonSerializer.Deserialize<WDStatsResponse>(response);
            if (tmpBalance.data != null)
            {
                foreach (WDStatistic x in tmpBalance.data.statistics)
                {
                    if (x.curr == CurrentCurrency)
                    {
                        Stats.Wagered = x.bet;
                        Stats.Profit = x.profit;
                        
                        break;
                    }
                }
                Stats.Bets = tmpBalance.data.stats.bets;
                Stats.Wins = tmpBalance.data.stats.wins;
                Stats.Losses = tmpBalance.data.stats.loses;                
            }
            return tmpBalance.status == "success";
        }
        async Task<bool> getseed()
        {
            _logger.LogDebug("WinDice GetSeed");
            string response = await Client.GetStringAsync("seed");
            WDGetSeed tmpBalance = JsonSerializer.Deserialize<WDGetSeed>(response);
            if (tmpBalance.data != null)
            {
                currentseed = tmpBalance.data;
            }
            return tmpBalance.status == "success";
        }
        protected override async Task<SiteStats> _UpdateStats()
        {
            await getbalance();
            if (await getstats())
            {
                return Stats;
            }
            else
            {
                return null;
            }
        }

        protected override async Task<SeedDetails> _ResetSeed()
        {
            string seed = "";
            while (seed.Length < 12)
            {
                seed += R.Next(0, int.MaxValue).ToString();
            }
            string loginjson = JsonSerializer.Serialize<WDResetSeed>(new WDResetSeed()
            {
                value = seed
            });

            HttpContent cont = new StringContent(loginjson);
            cont.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage resp2 = await Client.PostAsync("seed", cont);

            if (resp2.IsSuccessStatusCode)
            {
                string response = await resp2.Content.ReadAsStringAsync();
                WDGetSeed tmpBalance = JsonSerializer.Deserialize<WDGetSeed>(response);
                string currentseed = "";
                if (tmpBalance.status == "success")
                    currentseed = await getcurrentSeed();
                callResetSeedFinished(true, currentseed);
                return new SeedDetails(this.currentseed.client, this.currentseed.hash);
                
            }
            else
            {
                string errorresponse = await resp2.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to reset seed: {errorresponse}");
                callError("Failed to reset seed", false, ErrorType.ResetSeed);
                callResetSeedFinished(false, errorresponse);
                return null;
            }
        }

        protected async Task<string> getcurrentSeed()
        {
           
            string response = Client.GetStringAsync("seed").Result;
            WDGetSeed tmpBalance = JsonSerializer.Deserialize<WDGetSeed>(response);
            if (tmpBalance.data != null)
            {
                currentseed = tmpBalance.data;
            }
            return currentseed.client;
        }

        protected override async Task<bool> _Bank(decimal Amount)
        {
            try
            {
                bool result = false;
                HttpContent val = (HttpContent)new StringContent(JsonSerializer.Serialize(new WDVault
                {
                    curr = base.CurrentCurrency,
                    amount = Math.Round((Amount * 1000m), 5)
                }));
                val.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                HttpResponseMessage result2 = await Client.PostAsync("/api/v1/user/bank/send", val);
                if (result2.IsSuccessStatusCode)
                {
                    WDVaultResponse wDVaultResponse = JsonSerializer.Deserialize<WDVaultResponse>(await result2.Content.ReadAsStringAsync());
                    if (wDVaultResponse.status == "success")
                    {
                        foreach (var balance in wDVaultResponse.data.balances)
                        {
                            if (balance.curr.Equals(CurrentCurrency, StringComparison.InvariantCultureIgnoreCase))
                            {                                
                                Stats.Balance = balance.amount;
                                callStatsUpdated(Stats);
                                callBankFinished(true,"");
                                return true;
                            }
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to bank :{wDVaultResponse.message}");
                        callError($"Failed to bank funds: {wDVaultResponse.message}", false, ErrorType.Bank);
                        callBankFinished(false, wDVaultResponse.message);
                    }
                }  
                else
                {
                    string errorresponse = await result2.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                callError("Failed to bank funds", false, ErrorType.Bank);
            }
            return false;
        }

        protected override IGameResult _GetLucky(string ServerSeed, string ClientSeed, int Nonce, Games Game)
        {
            string msg = ServerSeed + ClientSeed + Nonce; 
            int charstouse = 5;
            string hex = Hash.SHA512(msg);
            if (Game == Games.Dice)
            {
                for (int i = 0; i < hex.Length; i += charstouse)
                {

                    string s = hex.ToString().Substring(i, charstouse);

                    decimal lucky = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                    if (lucky < 1000000)
                        return new DiceResult { Roll = lucky % 10000 / 100m };
                }
            }
            return null;
        }

        protected override Task<bool> _BrowserLogin()
        {
            throw new NotImplementedException();
        }

        public async Task<RangeDiceBet> PlaceRangeDiceBet(PlaceRangeDiceBet BetDetails)
        {
            _logger.LogDebug("WinDice placing dice bet");
           
            string loginjson = JsonSerializer.Serialize<WDPlaceBet>(new WDPlaceBet()
            {
                curr = CurrentCurrency.ToLower(),
                bet = BetDetails.Amount,
                game = BetDetails.Type.ToString().ToLower(),
                high =(int)(BetDetails.Max*100),
                low = (int)(BetDetails.Min*100),
                 
            });

            HttpContent cont = new StringContent(loginjson);
            cont.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            HttpResponseMessage resp2 = await Client.PostAsync("roll", cont);

            if (resp2.IsSuccessStatusCode)
            {
                string response = await resp2.Content.ReadAsStringAsync();
                _logger.LogDebug("WinDice bet result:" + response);
                WDBaseResponse statusMessage = JsonSerializer.Deserialize<WDBaseResponse>(response);
                if (statusMessage.status == "error")
                {
                    ErrorType type = ErrorType.Unknown;
                    switch (statusMessage.message)
                    {
                        case "Minimal chance for non-deposit bets is 0.2%":

                        case "Chance min 0.010000 / max 98.000000":

                            type = ErrorType.InvalidBet;
                            break;
                        case "No balance":

                            type = ErrorType.BalanceTooLow;

                            break;

                        default:
                            type = ErrorType.Unknown;
                            break;
                    }
                    if (type == ErrorType.Unknown)
                    {
                        if (statusMessage.message.StartsWith("Maximum win "))
                            type = ErrorType.InvalidBet;
                        if (statusMessage.message.StartsWith("Minimum bet "))
                            type = ErrorType.BetTooLow;

                    }
                    callError(statusMessage.message, false, type);
                    return null;
                }
                WDBet tmpBalance = JsonSerializer.Deserialize<WDBet>(response);
                if (tmpBalance.status == "success")
                {
                    RangeDiceBet Result = new RangeDiceBet()
                    {
                        TotalAmount = BetDetails.Amount,
                        DateValue = DateTime.Now,
                        ClientSeed = currentseed.client,
                        Currency = CurrentCurrency,
                        Guid = BetDetails.GUID,
                        BetID = tmpBalance.data.hash,
                        Nonce = tmpBalance.data.nonce,
                        Profit = tmpBalance.data.win - tmpBalance.data.bet,
                        Roll = tmpBalance.data.result / 100m,
                        ServerHash = currentseed.hash,
                        Type = BetDetails.Type,
                         Min = tmpBalance.data.pointLow/100m,
                          Max = tmpBalance.data.pointHigh/100m,
                            
                    };
                    Stats.Bets++;
                    Result.IsWin  = Result.GetWin(RangeDiceSettings);
                    if (Result.IsWin)
                        Stats.Wins++;
                    else 
                        Stats.Losses++;
                    Stats.Wagered += BetDetails.Amount;
                    Stats.Profit += Result.Profit;
                    Stats.Balance += Result.Profit;
                    callBetFinished(Result);
                    return Result;
                }
                else
                {
                    callNotify(tmpBalance.message);
                    callError(tmpBalance.message, false, ErrorType.Unknown);
                }
            }
            return null;
        }

        public class WDCurrencyBalance
        {
            public string curr { get; set; }

            public decimal amount { get; set; }
        }
        public class WDBalances
        {
            public List<WDCurrencyBalance> balances { get; set; }

            public List<WDCurrencyBalance> bank { get; set; }
        }
        public class WDVaultResponse : WDBaseResponse
        {
            public WDBalances data { get; set; }
        }
        public class WDBaseResponse
        {
            public string status { get; set; }
            public string message { get; set; }
        }
        public class WDVault
        {
            public string curr { get; set; }

            public decimal amount { get; set; }
        }
        public class WDUserResponse : WDBaseResponse
        {
            public WDUserResponse data { get; set; }
            public string hash { get; set; }
            public string username { get; set; }
            public string avatar { get; set; }
            public int rating { get; set; }
            public int reg_time { get; set; }
            public bool hide_profit { get; set; }
            public bool hide_bet { get; set; }
            public WDBalance balance { get; set; }
        }
        public class WDBalance
        {
            public decimal btc { get; set; }
            public decimal eth { get; set; }
            public decimal ltc { get; set; }
            public decimal doge { get; set; }
            public decimal bch { get; set; }
            public decimal ton { get; set; }
            public decimal trx { get; set; }
            public decimal usdt { get; set; }
            public decimal win { get; set; }
            public decimal xrp { get; set; }
            public decimal bnb { get; set; }
        }
        public class WDPlaceBet
        {
            public string curr { get; set; }
            public decimal bet { get; set; }
            public string game { get; set; }
            public int low { get; set; }
            public int high { get; set; }
        }
        public class WDBet : WDBaseResponse
        {
            

            public WDBet data { get; set; }
            public string hash { get; set; }
            public string userHash { get; set; }
            public string username { get; set; }
            public int nonce { get; set; }
            public string curr { get; set; }
            public decimal bet { get; set; }
            public decimal win { get; set; }
            public decimal jackpot { get; set; }
            public decimal pointLow { get; set; }
            public decimal pointHigh { get; set; }
            public string game { get; set; }
            public decimal chance { get; set; }
            public decimal payout { get; set; }
            public decimal result { get; set; }
            public decimal time { get; set; }
            public bool isHigh { get; set; }
        }
        public class WDStatistic
        {
            public string curr { get; set; }
            public decimal bet { get; set; }
            public decimal profit { get; set; }
        }

        public class WDStats
        {
            public int wins { get; set; }
            public int loses { get; set; }
            public int bets { get; set; }
            public int chat { get; set; }
            public int online { get; set; }
        }

        public class WDStatsResponse : WDBaseResponse
        {
            public WDStatsResponse data { get; set; }
            public WDStatistic[] statistics { get; set; }
            public WDStats stats { get; set; }
        }
        public class WDGetSeed : WDBaseResponse
        {
            public WDGetSeed data { get; set; }
            public string client { get; set; }
            public string hash { get; set; }
            public string newHash { get; set; }
            public int nonce { get; set; }
        }
        public class WDResetSeed
        {
            public string value { get; set; }

        }
        public class WDResetResult : WDBaseResponse
        {
            public WDResetResult data { get; set; }
            public string client { get; set; }
            public string hash { get; set; }
            public long nonce { get; set; }
        }
    }
}
