﻿using Gambler.Bot.Common.Enums;
using Gambler.Bot.Common.Games;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Helpers;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites.Classes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Gambler.Bot.Core.Sites.Bitvest;
using static Gambler.Bot.Core.Sites.Stake;

namespace Gambler.Bot.Core.Sites
{
    public class PrimeDice : BaseSite, iDice
    {
        protected string URL = "/_api/graphql";
        protected string RolName = "primediceRoll";
        protected string GameName = "CasinoGamePrimedice";
        protected string StatGameName = "primedice";
        HttpClient Client;

        string accesstoken = "";
        DateTime LastSeedReset = new DateTime();
        public bool ispd = false;
        string username = "";
        long uid = 0;
        DateTime lastupdate = new DateTime();

        public PrimeDice(ILogger logger) : base(logger)
        {
            StaticLoginParams = new LoginParameter[] { new LoginParameter("API Key", true, true, false, true) };
            //this.MaxRoll = 99.99m;
            this.SiteAbbreviation = "PD";
            this.SiteName = "PrimeDice";
            this.SiteURL = "https://primedice.com?c=Seuntjie";
            this.Mirrors.Add("https://primedice.com");
            this.Mirrors.Add("https://primedice.blue");
            this.Mirrors.Add("https://primedice.games");
            AffiliateCode = "?c=Seuntjie";
            this.Stats = new SiteStats();
            this.TipUsingName = true;
            this.AutoInvest = false;
            this.AutoWithdraw = true;
            this.CanChangeSeed = true;
            this.CanChat = false;
            this.CanGetSeed = false;
            this.CanRegister = false;
            this.CanSetClientSeed = false;
            this.CanTip = true;
            this.CanVerify = true;
            this.SupportsBrowserLogin = true;
            AutoBank = true;
            this.Currencies = new string[] { "APE","BTC","ETH","BCH","EOS","BNB","BUSD","CRO","DAI","DOGE","LINK","LTC","POL","SAND","SHIB","SOL","TRUMP",
                "TRX","UNI","USDC","XRP","USDT", };
            SupportedGames = new Games[] { Games.Dice };
            CurrentCurrency = "btc";
            this.DiceBetURL = "https://primedice.com/bet/{0}";
            //this.Edge = 1;
            DiceSettings = new DiceConfig() { Edge = 1, MaxRoll = 99.99m };
            NonceBased = true;
        }

        public override void SetProxy(ProxyDetails ProxyInfo)
        {
            throw new NotImplementedException();
        }

        protected override void _Disconnect()
        {
            ispd = false;
            Client = null;

        }
        string userid = "";
        async Task OnWSConnected(GraphQL.Client.Http.GraphQLHttpClient client)
        {

        }


        public class PersonAndFilmsResponse
        {
        }

        protected override Task<bool> _Login(LoginParamValue[] LoginParams)
        {
            return _Login(LoginParams, 0);
        }
        protected async Task<bool> _Login(LoginParamValue[] LoginParams, int retry)
        {
            try
            {
                string APIKey = "";

                foreach (LoginParamValue x in LoginParams)
                {
                    if (x.Param.Name.ToLower() == "api key")
                        APIKey = x.Value;

                }
                //CookieContainer cookies = new CookieContainer();
                string requiredCookie = "__cf_bm";
                var cookies = CallBypassRequired(URLInUse + AffiliateCode, [requiredCookie], true, URL);

                HttpClientHandler handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                    UseCookies = true,
                    CookieContainer = cookies.Cookies,

                };
                Client = new HttpClient(handler);

                foreach (var x in cookies.Headers)
                {
                    try
                    {
                        if (x.Key.ToLower() == "content-type"
                            || x.Key.ToLower() == "cookie"
                            || x.Key.ToLower() == "authorization"
                            || x.Key.ToLower() == "x-access-token")
                            continue;
                        Client.DefaultRequestHeaders.Add(x.Key, x.Value);
                    }
                    catch (Exception ex)
                    {

                    }
                }
                Client.DefaultRequestHeaders.Add("X-Access-Token", APIKey);
                Client.DefaultRequestHeaders.Add("Authorization", "Bearer " + APIKey);

                GraphqlRequestPayload LoginReq = new GraphqlRequestPayload
                {
                    query = "query DiceBotLogin{user {activeServerSeed { seedHash seed nonce} activeClientSeed{seed} id balances{available{currency amount}} statistic {game bets wins losses betAmount profit currency}}}"
                        ,
                    operationName = "DiceBotLogin"
                };

                StringContent content = new StringContent(JsonSerializer.Serialize(LoginReq), Encoding.UTF8, "application/json");

                var resp = await Client.PostAsync(URLInUse + URL, content);
                string respostring = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode && retry < 5)
                {
                    CallCFCaptchaBypass(respostring);
                    await Task.Delay(Random.Next(50, 150) * retry);
                    return await _Login(LoginParams, ++retry);
                }
               
                if (!resp.IsSuccessStatusCode)
                {
                    await Task.Delay(106);

                }
                var Resp = JsonSerializer.Deserialize<Payload>(respostring);
                pdUser user = Resp.data.user;
                userid = user.id;
                if (string.IsNullOrWhiteSpace(userid))
                    callLoginFinished(false);
                else
                {
                    foreach (Statistic x in user.statistic)
                    {
                        if (x.currency.ToLower() == CurrentCurrency.ToLower() && x.game == StatGameName)
                        {
                            this.Stats.Bets = (int)x.bets;
                            this.Stats.Wins = (int)x.wins;
                            this.Stats.Losses = (int)x.losses;
                            this.Stats.Profit = x.profit ?? 0;
                            this.Stats.Wagered = x.amount ?? 0;

                            break;
                        }
                    }
                    foreach (Balance x in user.balances)
                    {
                        if (x.available.currency.ToLower() == CurrentCurrency.ToLower())
                        {
                            this.Stats.Balance = x.available.amount ?? 0;
                            break;
                        }
                    }

                    callLoginFinished(true);
                    return true;
                }

            }
            catch (WebException e)
            {
                _logger?.LogError(e.ToString());
                if (e.Response != null)
                {

                }
                callLoginFinished(false);
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
                callLoginFinished(false);
            }
            return false;
        }
        void GetBalanceThread()
        {
            try
            {
                while (ispd)
                {
                    if (userid != null && ((DateTime.Now - lastupdate).TotalSeconds >= 30 || ForceUpdateStats))
                    {
                        UpdateStats();

                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
            }
        }

        int retrycount = 0;
        DateTime Lastbet = DateTime.Now;

        public DiceConfig DiceSettings { get; set; }

        public async Task<DiceBet> PlaceDiceBet(PlaceDiceBet BetDetails)
        {
            try
            {
                decimal amount = BetDetails.Amount;
                decimal chance = BetDetails.Chance;
                bool High = BetDetails.High;
                /*if (amount < 10000 && (DateTime.Now - Lastbet).TotalMilliseconds < 500)
                {
                    Thread.Sleep((int)(500.0 - (DateTime.Now - Lastbet).TotalMilliseconds));
                }*/
                decimal tmpchance = High ? DiceSettings.MaxRoll - chance : chance;

                //string query = "mutation {" + RolName + "(amount:" + amount.ToString("0.00000000", System.Globalization.NumberFormatInfo.InvariantInfo) + ", target:" + tmpchance.ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo) + ",condition:" + (High ? "above" : "below") + ",currency:" + CurrentCurrency.ToLower() + ") { id iid nonce currency amount payout state { ... on " + GameName + " { result target condition } } createdAt serverSeed{seedHash seed nonce} clientSeed{seed} user{balances{available{amount currency}} statistic{game bets wins losses amount profit currency}}}}";
                //var primediceRoll = GQLClient.SendMutationAsync<dynamic>(new GraphQLRequest { Query = query }).Result;
                GraphqlRequestPayload betresult = new GraphqlRequestPayload
                {
                    query = "mutation DiceBotDiceBet($amount: Float! \r\n            $target: Float!\r\n            $condition: CasinoGamePrimediceConditionEnum!\r\n            $currency: CurrencyEnum!\r\n            $identifier: String!){ primediceRoll(amount: $amount, target: $target,condition: $condition,currency: $currency, identifier: $identifier) { id nonce currency amount payout state { ... on CasinoGamePrimedice { result target condition } } createdAt serverSeed{seedHash seed nonce} clientSeed{seed} user{balances{available{amount currency}} statistic{game bets wins losses betAmount profit currency}}}}",
                    variables = new
                    {
                        amount = amount,
                        target = tmpchance,
                        condition = (High ? "above" : "below"),
                        currency = CurrentCurrency.ToLower(),
                        identifier = Random.Next().ToString()
                    }
                    ,
                    operationName = "DiceBotDiceBet"
                };
                var response = await Client.PostAsync(URLInUse + URL, new StringContent(JsonSerializer.Serialize(betresult), Encoding.UTF8, "application/json"));
                var responsestring = await response.Content.ReadAsStringAsync();
                Payload ResponsePayload = System.Text.Json.JsonSerializer.Deserialize<Payload>(responsestring);
                if (ResponsePayload.errors != null && ResponsePayload.errors.Length > 0)
                {
                    string error = ResponsePayload.errors[0].message;
                    ErrorType errorType = ErrorType.Unknown;

                    if (error == ("Number too small."))
                    {
                        errorType = ErrorType.InvalidBet;
                    }
                    else if (error.StartsWith("Maximum bet exceeded"))
                    {
                        errorType = ErrorType.InvalidBet;
                    }
                    else if (error.StartsWith("You do not have enough balance to do that."))
                    {
                        errorType = ErrorType.BalanceTooLow;
                    }

                    callError(error, false, errorType);
                    return null;
                }
                RollDice tmp = ResponsePayload.data.primediceRoll;

                Lastbet = DateTime.Now;
                try
                {

                    lastupdate = DateTime.Now;
                    /*foreach (Statistic x in tmp.user?.statistic)
                    {
                        if (x.currency.ToLower() == CurrentCurrency.ToLower() && x.game == StatGameName)
                        {*/
                    DiceBet tmpbet = tmp.ToBet();
                    tmpbet.IsWin = tmpbet.GetWin(this.DiceSettings);
                    this.Stats.Bets++; ;
                    this.Stats.Wins += tmpbet.IsWin ? 1 : 0; ;
                    this.Stats.Losses += tmpbet.IsWin ? 0 : 1; ;
                    this.Stats.Profit += tmpbet.Profit;
                    this.Stats.Wagered += tmpbet.TotalAmount;

                    /*}
                }*/
                    /*foreach (Balance x in tmp.user.balances)
                    {
                        if (x.available.currency.ToLower() == CurrentCurrency.ToLower())
                        {*/
                    this.Stats.Balance += tmpbet.Profit;
                    /*break;
                }
            }*/


                    tmpbet.Guid = BetDetails.GUID;
                    callBetFinished(tmpbet);
                    retrycount = 0;
                    return tmpbet;
                }
                catch (Exception e)
                {
                    _logger?.LogError(e.ToString());
                    callNotify("Some kind of error happened. I don't really know graphql, so your guess as to what went wrong is as good as mine.");
                }
            }
            catch (Exception e2)
            {
                callNotify("Error occured while trying to bet, retrying in 30 seconds. Probably.");
                _logger?.LogError(e2.ToString());
            }
            return null;
        }

        protected override async Task<SiteStats> _UpdateStats()
        {
            try
            {
                ForceUpdateStats = false;
                lastupdate = DateTime.Now;

                GraphqlRequestPayload LoginReq = new GraphqlRequestPayload
                {
                    operationName = "DiceBotGetBalance",
                    query = "query DiceBotGetBalance{user {activeServerSeed { seedHash seed nonce} activeClientSeed{seed} id balances{available{currency amount}} statistic {game bets wins losses betAmount profit currency}}}"
                };
                var Resp = await Client.PostAsync("", new StringContent(JsonSerializer.Serialize(LoginReq), Encoding.UTF8, "application/json"));
                string respostring = await Resp.Content.ReadAsStringAsync();
                pdUser user = JsonSerializer.Deserialize<Payload>(respostring)?.data.user;
                //GraphQLResponse< pdUser> Resp = GQLClient.SendMutationAsync< pdUser>(LoginReq).Result;

                foreach (Statistic x in user.statistic)
                {
                    if (x.currency.ToLower() == CurrentCurrency.ToLower() && x.game == StatGameName)
                    {
                        this.Stats.Bets = (int)x.bets;
                        this.Stats.Wins = (int)x.wins;
                        this.Stats.Losses = (int)x.losses;
                        this.Stats.Profit = x.profit ?? 0;
                        this.Stats.Wagered = x.amount ?? 0;
                        break;
                    }
                }
                foreach (Balance x in user.balances)
                {
                    if (x.available.currency.ToLower() == CurrentCurrency.ToLower())
                    {
                        this.Stats.Balance = x.available.amount ?? 0;
                        break;
                    }
                }
                return Stats;
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
            }
            return null;
        }

        public override int _TimeToBet(PlaceBet BetDetails)
        {
            return 100 - (int)(DateTime.Now - Lastbet).TotalMilliseconds;

        }

        protected override async Task<SeedDetails> _ResetSeed()
        {
            try
            {
                string clientseed = GenerateNewClientSeed();
                GraphqlRequestPayload payload = new GraphqlRequestPayload
                {
                    operationName = "DiceBotRotateSeed",
                    query = "mutation DiceBotRotateSeed ($seed: String!){rotateServerSeed{ seed seedHash nonce } changeClientSeed(seed: $seed){seed}}",
                    variables = new
                    {
                        seed = clientseed
                    }
                };
                var response = await Client.PostAsync(URLInUse + URL, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
                var responsestring = await response.Content.ReadAsStringAsync();
                Payload ResponsePayload = System.Text.Json.JsonSerializer.Deserialize<Payload>(responsestring);
                if (ResponsePayload.errors != null && ResponsePayload.errors.Length > 0)
                {
                    callError("An error occured while trying to reset your seet: ", false, ErrorType.ResetSeed);
                    _logger.LogError(string.Join(Environment.NewLine, ResponsePayload.errors.Select(x => x.ToString())));

                    callResetSeedFinished(false, string.Join(Environment.NewLine, ResponsePayload.errors.Select(x => x.ToString())));
                    return null;
                }
                else
                {
                    callResetSeedFinished(true, ResponsePayload.data.rotateServerSeed.seedHash);
                    return new SeedDetails(ResponsePayload.data.changeClientSeed.seed, ResponsePayload.data.rotateServerSeed.seedHash);
                }
                
            }
            catch (Exception ex)
            {
                callError("An error occured while trying to bank your funds.", false, ErrorType.ResetSeed);
                _logger?.LogError(ex.ToString());
            }
            return null;
        }

        protected override async Task<bool> _Bank(decimal Amount)
        {
            try
            {
                GraphqlRequestPayload payload = new GraphqlRequestPayload
                {
                    query = "mutation CreateVaultDeposit($currency: CurrencyEnum!, $amount: Float!) {\n  createVaultDeposit(currency: $currency, amount: $amount) {\n    id\n    amount\n    currency\n    user {\n      id\n      balances {\n        available {\n          amount\n          currency\n          __typename\n        }\n        vault {\n          amount\n          currency\n          __typename\n        }\n        __typename\n      }\n      __typename\n    }\n    __typename\n  }\n}\n",
                    variables = new
                    {
                        currency = CurrentCurrency.ToLower(),
                        amount = Amount
                    }
                };
                var response = await Client.PostAsync(URLInUse + URL, new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json"));
                var responsestring = await response.Content.ReadAsStringAsync();
                Payload ResponsePayload = System.Text.Json.JsonSerializer.Deserialize<Payload>(responsestring);
                if (ResponsePayload.errors != null && ResponsePayload.errors.Length > 0)
                {
                    callError("An error occured while trying to bank your funds: ", false, ErrorType.Bank);
                    _logger.LogError(string.Join(Environment.NewLine, ResponsePayload.errors.Select(x => x.ToString())));
                    await UpdateStats();
                    callBankFinished(false, string.Join(Environment.NewLine, ResponsePayload.errors.Select(x => x.ToString())));
                    return false;
                }
                else
                {
                    Stats.Balance = ResponsePayload.data.createVaultDeposit.user.balances.FirstOrDefault(x => x.available.currency.ToLower() == CurrentCurrency.ToLower()).available.amount ?? 0;
                    callBankFinished(true, "");
                }
                return true;
            }
            catch (Exception ex)
            {
                callError("An error occured while trying to bank your funds.", false, ErrorType.Bank);
                _logger?.LogError(ex.ToString());
            }
            return false;
        }

        protected override IGameResult _GetLucky(string ServerSeed, string ClientSeed, int Nonce, Games Game)
        {
            string msg = ClientSeed + "-" + Nonce.ToString();
            string hex = Hash.HMAC512(msg, ServerSeed).ToLowerInvariant(); 
            int charstouse = 5;
            if (Game == Games.Dice)
            {
                for (int i = 0; i < hex.Length; i += charstouse)
                {

                    string s = hex.ToString().Substring(i, charstouse);

                    decimal lucky = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                    if (lucky < 1000000)
                    {
                        lucky %= 10000;
                        return new DiceResult { Roll = lucky / 100 };

                    }
                }
            }
            return null;
            
        }
        protected override Task<bool> _BrowserLogin()
        {
            return _BrowserLogin(0);
        }
        protected async Task<bool> _BrowserLogin(int retry)
        {
            
            try
            {
                var cookies = CallBypassRequired(URLInUse + AffiliateCode, ["session", "__cf_bm"], false, URL);
                string APIKey = cookies.Cookies.GetCookies(new Uri(URLInUse)).FirstOrDefault(x => x.Name.ToLower() == "session")?.Value;
                HttpClientHandler handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli | DecompressionMethods.All,
                    UseCookies = true,
                    CookieContainer = cookies.Cookies,
                };
                Client = new HttpClient(handler);
                Client.BaseAddress = new Uri(URLInUse + URL);
                foreach (var x in cookies.Headers)
                {
                    try
                    {
                        if (x.Key.ToLower() == "content-type"
                            || x.Key.ToLower() == "cookie"
                            || x.Key.ToLower() == "x-operation-name"
                            || x.Key.ToLower() == "x-operation-type")
                            continue;
                        Client.DefaultRequestHeaders.Add(x.Key, x.Value);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                GraphqlRequestPayload LoginReq = new GraphqlRequestPayload
                {
                    query = "query DiceBotLogin{user {activeServerSeed { seedHash seed nonce} activeClientSeed{seed} id balances{available{currency amount}} statistic {game bets wins losses betAmount profit currency}}}"
                        ,
                    operationName = "DiceBotLogin"
                };

                StringContent content = new StringContent(JsonSerializer.Serialize(LoginReq), Encoding.UTF8, "application/json");

                var resp = await Client.PostAsync(URLInUse + URL, content);
                string respostring = await resp.Content.ReadAsStringAsync();
                
                
                
                if  (!resp.IsSuccessStatusCode && retry++ < 5)
                {
                    CallCFCaptchaBypass(respostring);
                    await Task.Delay(Random.Next(50, 150) * retry);
                    return await _BrowserLogin(retry);
                }
                if (!resp.IsSuccessStatusCode)
                {
                    await Task.Delay(106);

                }
                var Resp = JsonSerializer.Deserialize<Payload>(respostring);
                pdUser user = Resp.data.user;
                userid = user.id;
                if (string.IsNullOrWhiteSpace(userid))
                    callLoginFinished(false);
                else
                {
                    foreach (Statistic x in user.statistic)
                    {
                        if (x.currency.ToLower() == CurrentCurrency.ToLower() && x.game == StatGameName)
                        {
                            this.Stats.Bets = (int)x.bets;
                            this.Stats.Wins = (int)x.wins;
                            this.Stats.Losses = (int)x.losses;
                            this.Stats.Profit = x.profit ?? 0;
                            this.Stats.Wagered = x.amount ?? 0;

                            break;
                        }
                    }
                    foreach (Balance x in user.balances)
                    {
                        if (x.available.currency.ToLower() == CurrentCurrency.ToLower())
                        {
                            this.Stats.Balance = x.available.amount ?? 0;
                            break;
                        }
                    }

                    callLoginFinished(true);
                    return true;
                }

            }
            catch (WebException e)
            {
                _logger?.LogError(e.ToString());
                if (e.Response != null)
                {

                }
                callLoginFinished(false);
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
                callLoginFinished(false);
            }
            return false;
        }
    

        public class Sender
        {
            public string name { get; set; }
            public string __typename { get; set; }
        }

        public class Receiver
        {
            public string name { get; set; }
            public string __typename { get; set; }
        }

        public class _Tip
        {
            public string id { get; set; }
            public decimal amount { get; set; }
            public string currency { get; set; }
            public Sender sender { get; set; }
            public Receiver receiver { get; set; }
            public string __typename { get; set; }
        }
        public class Data2
        {
            public string message { get; set; }
            public _Tip tip { get; set; }
            public string __typename { get; set; }
        }
        public class pdSeed
        {
            public string seedHash { get; set; }
            public string seed { get; set; }
            public int nonce { get; set; }
        }
        public class pdUser
        {
            public string id { get; set; }
            public string name { get; set; }
            public List<object> roles { get; set; }
            public string __typename { get; set; }
            public Balance balance { get; set; }
            public Balance[] balances { get; set; }
            public List<Statistic> statistic { get; set; }
            public pdSeed activeSeed { get; set; }
            public pdUser User { get; set; }
        }

        public class ChatMessages
        {
            public string id { get; set; }
            public Data2 data { get; set; }
            public string createdAt { get; set; }
            public pdUser user { get; set; }
            public string __typename { get; set; }
        }
        public class Chat
        {
            public string id { get; set; }
            public string __typename { get; set; }
        }
        public class Messages
        {
            public Chat chat { get; set; }
            public string id { get; set; }
            public Data2 data { get; set; }
            public string createdAt { get; set; }
            public pdUser user { get; set; }
            public string __typename { get; set; }
        }
        public class DiceState
        {
            public double result { get; set; }
            public double target { get; set; }
            public string condition { get; set; }

        }
        public class RollDice
        {
            public RollDice primediceRoll { get; set; }
            public string id { get; set; }
            public string iid { get; set; }
            public decimal result { get; set; }
            public decimal payoutMultiplier { get; set; }
            public decimal amount { get; set; }
            public decimal payout { get; set; }
            public string createdAt { get; set; }
            public string currency { get; set; }
            public pdUser user { get; set; }
            public string __typename { get; set; }
            public pdSeed serverSeed { get; set; }
            public pdSeed clientSeed { get; set; }
            public int nonce { get; set; }
            public DiceState state { get; set; }

            public DiceBet ToBet()
            {
                DiceBet bet = new DiceBet
                {
                    TotalAmount = amount,
                    Chance = state.condition.ToLower() == "above" ? 99.99m - (decimal)state.target : (decimal)state.target,
                    High = state.condition.ToLower() == "above",
                    Currency = currency,
                    DateValue = DateTime.Now,
                    BetID = id.ToString(),
                    Roll = (decimal)state.result,
                    ClientSeed = clientSeed.seed,
                    ServerHash = serverSeed.seedHash,
                    Nonce = nonce
                };

                //User tmpu = User.FindUser(bet.UserName);
                /*if (tmpu == null)
                    bet.uid = 0;
                else
                    bet.uid = (int)tmpu.Uid;*/
                bool win = (((bool)bet.High ? (decimal)bet.Roll > (decimal)99.99 - (decimal)(bet.Chance) : (decimal)bet.Roll < (decimal)(bet.Chance)));
                bet.Profit = win ? ((payout - amount)) : (-amount);
                return bet;
            }
        }
        public class GraphqlRequestPayload
        {
            public string operationName { get; set; }

            public string query { get; set; }

            public object variables { get; set; }

            public string identifier { get; set; }
        }
        public class Statistic
        {
            public string game { get; set; }
            public decimal? bets { get; set; }
            public decimal? wins { get; set; }
            public decimal? losses { get; set; }
            public decimal? amount { get; set; }
            public decimal? profit { get; set; }
            public string currency { get; set; }
            public string __typename { get; set; }
        }


        public class Rotateserverseed
        {
            public object seed { get; set; }
            public string seedHash { get; set; }
            public int nonce { get; set; }
        }

        public class Changeclientseed
        {
            public string seed { get; set; }
        }

        public class Data
        {
            public ChatMessages chatMessages { get; set; }
            public Messages messages { get; set; }
            public RollDice diceRoll { get; set; }
            public RollDice primediceRoll { get; set; }
            public pdUser user { get; set; }
            public RollDice bet { get; set; }
            public StakeVaultDepost createVaultDeposit { get; set; }
            public Rotateserverseed rotateServerSeed { get; set; }
            public Changeclientseed changeClientSeed { get; set; }
        }
        public class PDVaultDepost
        {
            public string currency { get; set; }
            public decimal amount { get; set; }
            public pdUser user { get; set; }
        }
        public class Payload
        {
            public Data data { get; set; }
            public PDError[] errors { get; set; }
        }


        public class PDError
        {
            public string[] path { get; set; }
            public string message { get; set; }
            public string errorType { get; set; }
        }

        public class RootObject
        {
            public string type { get; set; }
            public string id { get; set; }
            public Payload payload { get; set; }
        }
        public class Role
        {
            public string name { get; set; }
            public string __typename { get; set; }
        }
        public class Balance
        {
            public Available available { get; set; }
            public string __typename { get; set; }
        }
        public class Available
        {
            public decimal? amount { get; set; }
            public string currency { get; set; }
            public string __typename { get; set; }
        }
    }

}
