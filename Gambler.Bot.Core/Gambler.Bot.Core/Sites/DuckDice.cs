using Gambler.Bot.Common.Enums;
using Gambler.Bot.Common.Games;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Games.RangeDice;
using Gambler.Bot.Common.Helpers;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites.Classes;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static Gambler.Bot.Core.Sites.Bitvest;
using static Gambler.Bot.Core.Sites.NitrogenSports;

namespace Gambler.Bot.Core.Sites
{
    public class DuckDice : BaseSite, iDice, iRangeDice
    {
        string accesstoken = "";
        DateTime LastSeedReset = new DateTime();
        public bool ispd = false;
        string username = "";
        long uid = 0;
        DateTime lastupdate = new DateTime();
        HttpClient Client;
        HttpClientHandler ClientHandlr;//
        public static string[] cCurrencies = new string[] { "USDT", "BTC", "LTC", "TRX", "DECOY", "DOGE", "XRP", "ETH", "XLM",
            "BCH","BNB","SHIB","USDC","ADA","DASH","SOL","ATOM","ETC","EOS","XMR","BTTC","POL","ZEC","DOT","RVN","LINK","DAI",
            "TUSD","AVAX","NEAR","ZEN","AAVE","ENA","UNI","TON","FDUSD","TRUMP","WBTC","INR","PKR","USD","VND","GHS","KZT","BDT",
        "KGS","CAD","UZS","AZN","CLP","IDR","KES","MXN","MYR","NGN","THB"};
        string apiversion = "1.1.1";
        QuackSeed currentseed = null;

        public DiceConfig DiceSettings { get; set; }
        public RangeDiceConfig RangeDiceSettings { get; set; }

        public DuckDice(ILogger logger) : base(logger)
        {
            StaticLoginParams = new LoginParameter[] { new LoginParameter("API Key", true, true, false, true) };
            //this.MaxRoll = 99.99m;
            this.SiteAbbreviation = "DD";
            this.SiteName = "DuckDice";
            this.SiteURL = "https://duckdice.io/?c=53ea652da4";
            this.Mirrors.Add("https://duckdice.io");
            this.Mirrors.Add("https://duckdice.me");
            this.Mirrors.Add("https://duckdice.net");
            this.GameModes.Add("Faucet");
            AffiliateCode = "?c=53ea652da4";
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
            this.CanVerify = true;
            this.Currencies = cCurrencies;
            SupportedGames = new Games[] { Games.Dice, Games.RangeDice };
            CurrentCurrency = "btc";
            this.DiceBetURL = "https://duckdice.io/Bets/{0}";
            //this.Edge = 1m;
            DiceSettings = new DiceConfig() { Edge = 1, MaxRoll = 99.99m };
            RangeDiceSettings = new RangeDiceConfig() { Edge = 1, MaxRoll = 99.99m, SupportsDouble = false };
            NonceBased = true;
            SupportsBrowserLogin = true;
        }

        void GetBalanceThread()
        {

            while (ispd)
            {
                if (accesstoken != "" && ((DateTime.Now - lastupdate).TotalSeconds > 60 || ForceUpdateStats))
                {
                    lastupdate = DateTime.Now;
                    UpdateStats();

                }
                Thread.Sleep(1000);
            }

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
            try
            {
                accesstoken = LoginParams[0].Value;


                var handler = new SocketsHttpHandler
                {
                    SslOptions = new SslClientAuthenticationOptions
                    {
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,

                    },
                    UseCookies = true,
                    AllowAutoRedirect = true,


                };

                var cookies = await CallBypassRequired(URLInUse + AffiliateCode, ["__cf_bm"]);
                var authcookie = cookies.Cookies.GetCookies(new Uri(URLInUse)).FirstOrDefault(x => x.Name == "_at");
                if (authcookie != null)
                {
                    authcookie.Expired = true;
                }
                //handler = new HttpClientHandler
                //{
                //    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                //    UseCookies = true,
                //    CookieContainer = cookies.Cookies,

                //};
                Client = new HttpClient(handler) { BaseAddress = new Uri(URLInUse + "/api/") }; ;
                foreach (var x in cookies.Headers)
                {
                    try
                    {
                        if (x.Key.ToLower() == "content-type"
                            || x.Key.ToLower() == "cookie"
                            || x.Key.ToLower() == "authorization"
                            )
                            continue;
                        Client.DefaultRequestHeaders.Add(x.Key, x.Value);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                var EmitResponse = await Client.GetAsync("load/" + CurrentCurrency + (string.IsNullOrWhiteSpace(accesstoken) ? "" : "?api_key=" + accesstoken));
                var sEmitResponse = await EmitResponse.Content.ReadAsStringAsync();
                int retriees = 0;
                while (!EmitResponse.IsSuccessStatusCode && retriees++ < 5)
                {
                    await CallCFCaptchaBypass(sEmitResponse);
                    await Task.Delay(Random.Next(50, 150) * retriees);
                    EmitResponse = await Client.GetAsync("load/" + CurrentCurrency + (string.IsNullOrWhiteSpace(accesstoken) ? "" : "?api_key=" + accesstoken));
                    sEmitResponse = await EmitResponse.Content.ReadAsStringAsync();
                }

                if (EmitResponse.IsSuccessStatusCode)
                {
                    Quackbalance balance = JsonSerializer.Deserialize<Quackbalance>(sEmitResponse);
                    sEmitResponse = await Client.GetStringAsync("bot/stats/" + CurrentCurrency + (string.IsNullOrWhiteSpace(accesstoken) ? "" : "?api_key=" + accesstoken));
                    QuackStatsDetails _Stats = JsonSerializer.Deserialize<QuackStatsDetails>(sEmitResponse);
                    /*sEmitResponse = await Client.GetStringAsync("randomize" + (string.IsNullOrWhiteSpace(accesstoken)?"": "?api_key=" + accesstoken));
                    currentseed = JsonSerializer.Deserialize<QuackSeed>(sEmitResponse).current;*/
                    if (balance != null && _Stats != null)
                    {
                        if (this.SelectedGameMode == "Normal")
                        {
                            Stats.Balance = decimal.Parse(balance.user.balances.main, System.Globalization.NumberFormatInfo.InvariantInfo);
                        }
                        else
                        {
                            Stats.Balance = decimal.Parse(balance.user.balances.faucet, System.Globalization.NumberFormatInfo.InvariantInfo);
                        }
                        Stats.Balance = decimal.Parse(balance.user.balances.main, System.Globalization.NumberFormatInfo.InvariantInfo);
                        Stats.Profit = decimal.Parse(_Stats.profit, System.Globalization.NumberFormatInfo.InvariantInfo);
                        Stats.Wagered = decimal.Parse(_Stats.volume, System.Globalization.NumberFormatInfo.InvariantInfo);
                        Stats.Bets = _Stats.bets;
                        Stats.Wins = _Stats.wins;
                        Stats.Losses = _Stats.bets - _Stats.wins;


                        ispd = true;
                        lastupdate = DateTime.Now;
                        new Thread(new ThreadStart(GetBalanceThread)).Start();
                        callLoginFinished(true);
                        return true;
                    }
                }
                else
                {
                    string url = $"{URLInUse}/api/load/{CurrentCurrency}?api_key={accesstoken}";
                    string result = await ExecJS($"let response = await fetch(\"{url}\");let data = await response.json();return data;");

                    string response = await EmitResponse.Content.ReadAsStringAsync();
                    callLoginFinished(false);
                    return false;
                }
            }
            catch (Exception e)
            {
                callLoginFinished(false);
                return false;
            }
            callLoginFinished(false);
            return false;
        }

        protected override async Task<bool> _BrowserLogin()
        {
            try
            {
                var cookies = await CallBypassRequired(URLInUse + "/dice" + AffiliateCode, ["_at", "__cf_bm"], false, "/api/load/bets");
                //accesstoken = cookies.Cookies.GetCookies(new Uri(URLInUse)).FirstOrDefault(x=>x.Name=="_at")?.Value;
                HttpClientHandler handler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli | DecompressionMethods.All,
                    UseCookies = true,
                    CookieContainer = cookies.Cookies
                };
                Client = new HttpClient(handler) { BaseAddress = new Uri(URLInUse + "/api/") }; ;

                foreach (var x in cookies.Headers)
                {
                    try
                    {
                        if (x.Key.ToLower() == "content-type" || x.Key.ToLower() == "cookie" || x.Key.ToLower() == "accept" || x.Key.ToLower() == "sentry-trace")
                            continue;
                        Client.DefaultRequestHeaders.Add(x.Key, x.Value);
                    }
                    catch (Exception ex)
                    {

                    }
                }/*
                  
                  */
                Client.DefaultRequestHeaders.Add("sec-fetch-dest", "empty");
                Client.DefaultRequestHeaders.Add("sec-fetch-mode", "cors");
                Client.DefaultRequestHeaders.Add("sec-fetch-site", "same-origin");
                //Client.DefaultRequestHeaders.Add("content-type", "application/json");
                var tmprequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(URLInUse + "/load/" + CurrentCurrency.ToLower()),
                    Headers = { }
                };
                var EmitResponse = await Client.SendAsync(tmprequest);
                string sEmitResponse = await EmitResponse.Content.ReadAsStringAsync();
                int retriees = 0;
                while (!EmitResponse.IsSuccessStatusCode && retriees++ < 5)
                {
                    await CallCFCaptchaBypass(sEmitResponse);
                    await Task.Delay(Random.Next(50, 150) * retriees);
                    EmitResponse = await Client.GetAsync("load/" + CurrentCurrency);
                    sEmitResponse = await EmitResponse.Content.ReadAsStringAsync();
                }

                if (EmitResponse.IsSuccessStatusCode)
                {
                    Quackbalance balance = JsonSerializer.Deserialize<Quackbalance>(sEmitResponse);
                    sEmitResponse = await Client.GetStringAsync("stat/" + CurrentCurrency);
                    QuackStatsDetails _Stats = JsonSerializer.Deserialize<QuackStatsDetails>(sEmitResponse);
                    sEmitResponse = await Client.GetStringAsync("randomize");
                    currentseed = JsonSerializer.Deserialize<QuackSeed>(sEmitResponse).current;
                    if (balance != null && _Stats != null)
                    {
                        if (this.SelectedGameMode == "Normal")
                        {
                            Stats.Balance = decimal.Parse(balance.user.balances.main, System.Globalization.NumberFormatInfo.InvariantInfo);
                        }
                        else
                        {
                            Stats.Balance = decimal.Parse(balance.user.balances.faucet, System.Globalization.NumberFormatInfo.InvariantInfo);
                        }
                        Stats.Balance = decimal.Parse(balance.user.balances.main, System.Globalization.NumberFormatInfo.InvariantInfo);
                        Stats.Profit = decimal.Parse(_Stats.profit, System.Globalization.NumberFormatInfo.InvariantInfo);
                        Stats.Wagered = decimal.Parse(_Stats.volume, System.Globalization.NumberFormatInfo.InvariantInfo);
                        Stats.Bets = _Stats.bets;
                        Stats.Wins = _Stats.wins;
                        Stats.Losses = _Stats.bets - _Stats.wins;


                        ispd = true;
                        lastupdate = DateTime.Now;
                        new Thread(new ThreadStart(GetBalanceThread)).Start();
                        callLoginFinished(true);
                        return true;
                    }
                }
                else
                {
                    /*
                     *async function fetchAsync (url) {
  let response = await fetch(url);
  let data = await response.json();
  return data;
}
                     * 
                     */
                    string url = $"{URLInUse}/api/load/{CurrentCurrency}";
                    string result = await ExecJS($"let response = await fetch(\"{url}\");let data = await response.json();return data;");
                    string response = await EmitResponse.Content.ReadAsStringAsync();
                    callLoginFinished(false);
                    return false;
                }
            }
            catch (Exception e)
            {
                callLoginFinished(false);
                return false;
            }
            callLoginFinished(false);
            return false;
        }

        protected override async Task<SiteStats> _UpdateStats()
        {
            try
            {

                string sEmitResponse = await Client.GetStringAsync("load/" + CurrentCurrency + (string.IsNullOrWhiteSpace(accesstoken) ? "" : "?api_key=" + accesstoken));
                Quackbalance balance = JsonSerializer.Deserialize<Quackbalance>(sEmitResponse);
                if (this.SelectedGameMode == "Normal")
                {
                    Stats.Balance = decimal.Parse(balance.user.balances.main, System.Globalization.NumberFormatInfo.InvariantInfo);

                }
                else
                {
                    Stats.Balance = decimal.Parse(balance.user.balances.faucet, System.Globalization.NumberFormatInfo.InvariantInfo);

                }
                sEmitResponse = await Client.GetStringAsync("stat/" + CurrentCurrency + (string.IsNullOrWhiteSpace(accesstoken) ? "" : "?api_key=" + accesstoken));
                QuackStatsDetails _Stats = JsonSerializer.Deserialize<QuackStatsDetails>(sEmitResponse);
                Stats.Profit = decimal.Parse(_Stats.profit, System.Globalization.NumberFormatInfo.InvariantInfo);
                Stats.Wagered = decimal.Parse(_Stats.volume, System.Globalization.NumberFormatInfo.InvariantInfo);
                Stats.Bets = _Stats.bets;
                Stats.Wins = _Stats.wins;
                Stats.Losses = _Stats.bets - _Stats.wins;
                return Stats;
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
            }
            return null;
        }

        public async Task<DiceBet> PlaceDiceBet(PlaceDiceBet BetDetails)
        {

            decimal amount = BetDetails.Amount;
            decimal chance = BetDetails.Chance;
            bool High = BetDetails.High;
            StringContent Content = new StringContent(string.Format(System.Globalization.NumberFormatInfo.InvariantInfo, "{{\"amount\":\"{0:0.00000000}\",\"symbol\":\"{1}\",\"chance\":{2:0.00},\"isHigh\":{3},\"faucet\":{4}}}", amount, CurrentCurrency, chance, High ? "true" : "false", (SelectedGameMode == "Faucet").ToString().ToLower()), Encoding.UTF8, "application/json");
            try
            {
                var response = await Client.PostAsync("play" + (string.IsNullOrWhiteSpace(accesstoken) ? "" : "?api_key=" + accesstoken), Content);
                string sEmitResponse = await response.Content.ReadAsStringAsync();
                QuackBet newbet = JsonSerializer.Deserialize<QuackBet>(sEmitResponse);
                if (newbet.error != null || newbet.errors != null)
                {
                    ErrorType type = ErrorType.Unknown;
                    string msg = newbet.error;

                    if (newbet.error != null)
                    {
                        if (newbet.error == "You have insufficient balance.")
                            type = ErrorType.BalanceTooLow;
                        else if (newbet.error.StartsWith("The minimum bet is"))
                            type = ErrorType.BetTooLow;
                    }
                    else
                    {
                        if (newbet.errors?.chance?.FirstOrDefault(x => x.StartsWith("The chance may not be greater than")) != null)
                        {
                            type = ErrorType.InvalidBet;
                            msg = newbet.errors.chance.FirstOrDefault();

                        }
                    }

                    callError(msg, false, type);
                    return null;
                }
                DiceBet tmp = new DiceBet
                {
                    TotalAmount = decimal.Parse(newbet.bet.betAmount, System.Globalization.NumberFormatInfo.InvariantInfo),
                    Chance = newbet.bet.chance,
                    //ClientSeed = currentseed.clientSeed,
                    Currency = CurrentCurrency,
                    DateValue = DateTime.Now,
                    High = High,
                    //Nonce = currentseed.nonce++,
                    Profit = decimal.Parse(newbet.bet.profit, System.Globalization.NumberFormatInfo.InvariantInfo),
                    Roll = newbet.bet.number / 100,
                    //ServerHash = currentseed.serverSeedHash,
                    BetID = newbet.bet.hash,
                    Guid = BetDetails.GUID
                };
                lastupdate = DateTime.Now;
                Stats.Profit = decimal.Parse(newbet.user.profit, System.Globalization.NumberFormatInfo.InvariantInfo);
                Stats.Wagered = decimal.Parse(newbet.user.volume, System.Globalization.NumberFormatInfo.InvariantInfo);
                Stats.Balance = decimal.Parse(newbet.user.balance, System.Globalization.NumberFormatInfo.InvariantInfo);
                Stats.Wins = newbet.user.wins;
                Stats.Bets = newbet.user.bets;
                Stats.Losses = newbet.user.bets - newbet.user.wins;
                callBetFinished(tmp);
                return tmp;
            }
            catch (Exception e)
            {
                callError("There was an error placing your bet.", false, ErrorType.Unknown);
                _logger?.LogError(e.ToString());
            }
            return null;
        }

        protected override async Task<SeedDetails> _ResetSeed()
        {
            try
            {
                var seed = GenerateNewClientSeed();
                StringContent Content = new StringContent(string.Format(System.Globalization.NumberFormatInfo.InvariantInfo, "{{\"clientSeed\":\"{0}\"}}", seed), Encoding.UTF8, "application/json");
                var response = await Client.PostAsync("randomize" + (string.IsNullOrWhiteSpace(accesstoken) ? "" : "?api_key=" + accesstoken), Content);
                string sresponse = await response.Content.ReadAsStringAsync();
                var responseseed = JsonSerializer.Deserialize<QuackSeed>(sresponse);
                currentseed = responseseed.current;
                return new SeedDetails
                {
                    Nonce = 0,
                    ServerHash = currentseed.serverSeedHash,
                    ClientSeed = seed
                };

            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                return null;
            }
        }

        protected override async Task<bool> _Bank(decimal Amount)
        {
            QuackBank bnk = new QuackBank { amount = Amount, symbol = CurrentCurrency.ToUpper() };
            StringContent Content = new StringContent(JsonSerializer.Serialize(bnk), Encoding.UTF8, "application/json");
            try
            {
                var response = await Client.PostAsync("bank/deposit" + (string.IsNullOrWhiteSpace(accesstoken) ? "" : "?api_key=" + accesstoken), Content);
                string sEmitResponse = await response.Content.ReadAsStringAsync();
                if (response.IsSuccessStatusCode)
                {
                    QuackBankResponse resp = JsonSerializer.Deserialize<QuackBankResponse>(sEmitResponse);
                    Stats.Balance = decimal.Parse(resp.balance, System.Globalization.NumberFormatInfo.InvariantInfo);
                    callStatsUpdated(Stats);
                    callBankFinished(true, "");
                    return true;
                }
                else
                {
                    callError(sEmitResponse, false, ErrorType.Bank);
                    callBankFinished(false, "");
                    return false;
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e.ToString());
                callError("Failed to bank funds.", false, ErrorType.Bank);
            }
            return false;
        }

        protected override IGameResult _GetLucky(string ServerSeed, string ClientSeed, int Nonce, Games Game)
        {
            string hex = Hash.SHA512(ServerSeed + ClientSeed + Nonce.ToString());
            int charstouse = 5;
            for (int i = 0; i < hex.Length; i += charstouse)
            {

                string s = hex.ToString().Substring(i, charstouse);

                decimal lucky = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                if (lucky < 1000000)
                {
                    decimal tmp = (lucky % 10000) / 100m;
                    return new DiceResult { Roll = tmp };
                }
            }
            return null;
        }

        public async Task<RangeDiceBet> PlaceRangeDiceBet(PlaceRangeDiceBet BetDetails)
        {
            decimal amount = BetDetails.Amount;

            duckPlaceRangeBet placebet = new duckPlaceRangeBet { amount = BetDetails.Amount.ToString("0.00000000", System.Globalization.NumberFormatInfo.InvariantInfo), isIn = BetDetails.Type == RangeDiceType.In, symbol = CurrentCurrency, };
            placebet.range = new int[] { (int)(BetDetails.Min * 100), (int)(BetDetails.Max * 100) };
            StringContent Content = new StringContent(JsonSerializer.Serialize(placebet), Encoding.UTF8, "application/json");
            try
            {
                var response = await Client.PostAsync("range-dice/play" + (string.IsNullOrWhiteSpace(accesstoken) ? "" : "?api_key=" + accesstoken), Content);
                string sEmitResponse = await response.Content.ReadAsStringAsync();
                QuakRangeBetResponse newbet = JsonSerializer.Deserialize<QuakRangeBetResponse>(sEmitResponse);
                if (newbet.error != null || newbet.errors != null)
                {
                    ErrorType type = ErrorType.Unknown;
                    string msg = newbet.error;

                    if (newbet.error != null)
                    {
                        if (newbet.error == "You have insufficient balance.")
                            type = ErrorType.BalanceTooLow;
                        else if (newbet.error.StartsWith("The minimum bet is"))
                            type = ErrorType.BetTooLow;
                    }
                    else
                    {
                        if (newbet.errors?.chance?.FirstOrDefault(x => x.StartsWith("The chance may not be greater than")) != null)
                        {
                            type = ErrorType.InvalidBet;
                            msg = newbet.errors.chance.FirstOrDefault();

                        }
                    }

                    callError(msg, false, type);
                    return null;
                }
                var range = newbet.bet.choiceOption.Split(",");
                RangeDiceBet tmp = new RangeDiceBet
                {
                    TotalAmount = decimal.Parse(newbet.bet.betAmount, System.Globalization.NumberFormatInfo.InvariantInfo),
                    //Chance = newbet.bet.chance,
                    //ClientSeed = currentseed.clientSeed,
                     Date = newbet.bet.createdAt,
                      Edge = RangeDiceSettings.Edge, 
                       Min = int.Parse(range[0])/100m,
                       Max = int.Parse(range[1]) / 100m,
                        Nonce = newbet.bet.nonce,
                          Type = BetDetails.Type,
                           
                    Currency = CurrentCurrency,
                    DateValue = DateTime.Now,
                    //High = High,
                    //Nonce = currentseed.nonce++,
                    Profit = decimal.Parse(newbet.bet.profit, System.Globalization.NumberFormatInfo.InvariantInfo),
                    Roll = newbet.bet.number / 100m,
                    //ServerHash = currentseed.serverSeedHash,
                    BetID = newbet.bet.hash,
                    Guid = BetDetails.GUID
                };
                tmp.IsWin = tmp.GetWin(RangeDiceSettings);
                lastupdate = DateTime.Now;
                Stats.Profit = decimal.Parse(newbet.user.profit, System.Globalization.NumberFormatInfo.InvariantInfo);
                Stats.Wagered = decimal.Parse(newbet.user.volume, System.Globalization.NumberFormatInfo.InvariantInfo);
                Stats.Balance = decimal.Parse(newbet.user.balance, System.Globalization.NumberFormatInfo.InvariantInfo);
                Stats.Wins = newbet.user.wins;
                Stats.Bets = newbet.user.bets;
                Stats.Losses = newbet.user.bets - newbet.user.wins;
                callBetFinished(tmp);
                return tmp;
            }
            catch (Exception e)
            {
                callError("There was an error placing your bet.", false, ErrorType.Unknown);
                _logger?.LogError(e.ToString());
            }
            return null;
        }

        public class QuackLogin
        {
            public string token { get; set; }
        }

        public class QuackErrors
        {
            public string[] chance { get; set; }

        }
        public class QuackStats
        {

            public QuackStats user { get; set; }
            public string hash { get; set; }
            public string username { get; set; }
            public string balance { get; set; }
            public QuackStats session { get; set; }
            public int bets { get; set; }
            public int wins { get; set; }
            public string volume { get; set; }
            public string profit { get; set; }
            public Absolutelevel absoluteLevel { get; set; }

        }
        public class QuackStatsDetails
        {
            public int bets { get; set; }
            public int wins { get; set; }
            public string profit { get; set; }
            public string volume { get; set; }
        }
        public class QuackBet
        {
            public string error { get; set; }
            public QuackErrors errors { get; set; }
            public QuackBet bet { get; set; }
            public QuackStats user { get; set; }
            public string hash { get; set; }
            public string symbol { get; set; }
            public bool result { get; set; }
            public bool isHigh { get; set; }
            public decimal number { get; set; }
            public decimal threshold { get; set; }
            public decimal chance { get; set; }
            public decimal payout { get; set; }
            public string betAmount { get; set; }
            public string winAmount { get; set; }
            public string profit { get; set; }
            public long nonce { get; set; }

        }
        public class QuackSeed
        {
            public QuackSeed current { get; set; }
            public string clientSeed { get; set; }
            public long nonce { get; set; }
            public string serverSeedHash { get; set; }
        }
        public class QuackWithdraw
        {
            public string error { get; set; }

        }


        public class Quackbalance
        {
            public QuackUser user { get; set; }
            public QuackBet[] bets { get; set; }
        }

        public class QuackUser
        {
            public Balances balances { get; set; }
            public object[] presets { get; set; }
            public string hash { get; set; }
            public string username { get; set; }
        }

        public class Balances
        {
            public string main { get; set; }
            public string faucet { get; set; }
        }
        public class QuackBank
        {
            public decimal amount { get; set; }
            public string symbol { get; set; }
        }
        public class QuackBankResponse
        {
            public string amount { get; set; }
            public string symbol { get; set; }
            public string balance { get; set; }
            public string bankBalance { get; set; }
        }

        public class duckPlaceRangeBet
        {
            public string amount { get; set; }
            public string symbol { get; set; }
            public bool isIn { get; set; }
            public int[] range { get; set; }
            public object tleHash { get; set; }
            public object userWageringBonusHash { get; set; }
        }


        public class QuakRangeBetResponse
        {

            public string error { get; set; }
            public QuackErrors errors { get; set; }
            public QuakRangeBet bet { get; set; }
            public bool isJackpot { get; set; }
            public object jackpotStatus { get; set; }
            public object jackpot { get; set; }
            public QuackStats user { get; set; }
            public object context { get; set; }
        }

        public class QuakRangeBet
        {
            public string hash { get; set; }
            public string symbol { get; set; }
            public int nonce { get; set; }
            public bool result { get; set; }
            public string choice { get; set; }
            public string choiceOption { get; set; }
            public decimal number { get; set; }
            public decimal chance { get; set; }
            public decimal payout { get; set; }
            public string betAmount { get; set; }
            public string winAmount { get; set; }
            public string profit { get; set; }
            public int created { get; set; }
            public string gameMode { get; set; }
            public object tle { get; set; }
            public object mined { get; set; }
            public Game game { get; set; }
            public decimal createdAt { get; set; }
            public bool isJackpot { get; set; }
            public QuackStats user { get; set; }
        }

        public class Game
        {
            public string name { get; set; }
            public string slug { get; set; }
        }

        

        public class Absolutelevel
        {
            public decimal level { get; set; }
            public decimal xp { get; set; }
            public decimal xpNext { get; set; }
            public decimal xpPrev { get; set; }
            public string levelRank { get; set; }
        }



    }
}
