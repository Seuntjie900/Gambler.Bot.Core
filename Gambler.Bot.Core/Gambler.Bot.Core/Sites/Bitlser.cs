using Gambler.Bot.Common.Enums;
using Gambler.Bot.Common.Games;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Games.Limbo;
using Gambler.Bot.Common.Games.Twist;
using Gambler.Bot.Common.Helpers;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites.Classes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using static Gambler.Bot.Core.Sites.Bitvest;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Gambler.Bot.Core.Sites
{
    public class Bitsler : BaseSite, iDice, iTwist, iLimbo
    {
        bool IsBitsler = false;
        string accesstoken = "";
        string sessiontoken = "";
        DateTime LastSeedReset = new DateTime();

        string username = "";
        long uid = 0;
        DateTime lastupdate = new DateTime();
        HttpClient Client;
        public static string[] sCurrencies = new string[] { "ada", "arb", "avax", "bch", "bnb", "brl", "btc", "btg", "btslr", "busd", "dai", "dash", "dgb", "doge", "eos", "etc", "eth", "ethw", "fdusd","link"
        ,"ltc","matic","neo","op","pol","qtum","shib","sol","ton","trx","usdc","usdt","xlm","xrp","zec"};
        HttpClientHandler ClientHandlr;

        public DiceConfig DiceSettings { get; set; }
        public LimboConfig LimboSettings { get; set; }
        public TwistConfig TwistSettings { get; set; }

        public Bitsler(ILogger logger) : base(logger)
        {
            StaticLoginParams = new LoginParameter[] { new LoginParameter("Username", false, true, false, false)
                , new LoginParameter("Password", true, true, false, true)
                , new LoginParameter("API Key", true, true, false, true)
                , new LoginParameter("2FA Code", false, false, true, true, true) };
            Currencies = sCurrencies;
            DiceBetURL = "https://www.bitsler.com/?ref=seuntjie/";
            SiteURL = "https://www.bitsler.com/?ref=seuntjie";
            this.Mirrors.Add("https://www.bitsler.com");
            Mirrors.Add("https://bitsler.io");
            AffiliateCode = "?ref=seuntjie";
            //this.MaxRoll = 99.99m;
            this.SiteAbbreviation = "BS";
            this.SiteName = "Bitsler";
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
            NonceBased = true;
            SupportedGames = new Games[] { Games.Dice, Games.Twist, Games.Limbo };
            this.CurrentCurrency = "btc";
            this.DiceBetURL = "https://bitvest.io/bet/{0}";
            //this.Edge = 1;
            DiceSettings = new DiceConfig() { Edge = 1, MaxRoll = 99.99m };
            TwistSettings = new TwistConfig() { Edge = 2, MaxRoll = 99m };
            LimboSettings = new LimboConfig() { Edge = 2, MaxPayout = 1000000 };
        }


        void GetBalanceThread()
        {
            while (IsBitsler)
            {
                if ((DateTime.Now - lastupdate).TotalSeconds > 60 || ForceUpdateStats)
                {
                    lastupdate = DateTime.Now;
                    UpdateStats();
                }
                Thread.Sleep(1000);
            }
        }

        private async Task<bool> RefreshToken()
        {
            if (string.IsNullOrWhiteSpace(sessiontoken))
                return false;

            List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
            pairs.Add(new KeyValuePair<string, string>("session_token", sessiontoken));
            FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
            HttpResponseMessage tmpresp = await Client.PostAsync("api/new-token", Content);
            byte[] bytes = await tmpresp.Content.ReadAsByteArrayAsync();
            string sEmitResponse = await tmpresp.Content.ReadAsStringAsync();

            //getuserstats 
            bsLogin bsbase = JsonSerializer.Deserialize<bsLogin>(sEmitResponse.Replace("\"return\":", "\"_return\":"));


            if (bsbase?.success ?? false)
            {
                accesstoken = bsbase.access_token;
                return true;
            }
            return false;
        }

        public async Task<DiceBet> PlaceDiceBet(PlaceDiceBet BetObj)
        {
            try
            {
                if (BetObj.Chance > 99.99m)
                {
                    callError("Chance must be less than 99.99", false, ErrorType.InvalidBet);
                    return null;
                }


                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                /*access_token
type:dice
amount:0.00000001
condition:< or >
game:49.5
devise:btc*/
                pairs.Add(new KeyValuePair<string, string>("access_token", accesstoken));
                //pairs.Add(new KeyValuePair<string, string>("type", "dice"));
                pairs.Add(new KeyValuePair<string, string>("amount", BetObj.Amount.ToString("0.00000000", System.Globalization.NumberFormatInfo.InvariantInfo)));
                pairs.Add(new KeyValuePair<string, string>("over", BetObj.High.ToString().ToLower()));
                pairs.Add(new KeyValuePair<string, string>("target", !BetObj.High ? BetObj.Chance.ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo) : (DiceSettings.MaxRoll - BetObj.Chance).ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo)));
                pairs.Add(new KeyValuePair<string, string>("currency", CurrentCurrency));
                pairs.Add(new KeyValuePair<string, string>("api_key", "0b2edbfe44e98df79665e52896c22987445683e78"));
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                HttpResponseMessage tmpmsg = await Client.PostAsync("api/bet-dice", Content);
                string sEmitResponse = await tmpmsg.Content.ReadAsStringAsync();

                bsBet bsbase = null;
                try
                {
                    bsbase = JsonSerializer.Deserialize<bsBetwithintid>(sEmitResponse.Replace("\"return\":", "\"_return\":"));
                }
                catch (Exception e)
                {

                }

                if (bsbase != null)
                    // if (bsbase._return != null)
                    if (bsbase.success)
                    {
                        Stats.Balance = decimal.Parse(bsbase.new_balance, System.Globalization.NumberFormatInfo.InvariantInfo);
                        lastupdate = DateTime.Now;
                        DiceBet tmp = bsbase.ToBet();
                        tmp.High = BetObj.High;
                        tmp.Chance = BetObj.Chance;
                        tmp.Guid = BetObj.GUID;
                        tmp.Currency = CurrentCurrency;
                        Stats.Profit += (decimal)tmp.Profit;
                        Stats.Wagered += (decimal)tmp.TotalAmount;
                        tmp.DateValue = DateTime.Now;
                        bool win = false;
                        if ((tmp.Roll > 99.99m - tmp.Chance && tmp.High) || (tmp.Roll < tmp.Chance && !tmp.High))
                        {
                            win = true;
                        }
                        //set win
                        if (win)
                            Stats.Wins++;
                        else
                            Stats.Losses++;
                        Stats.Bets++;
                        LastBetAmount = (double)BetObj.Amount;
                        LastBet = DateTime.Now;
                        callBetFinished(tmp);
                        return tmp;
                    }
                    else
                    {
                        if (bsbase.error != null)
                        {
                            _logger.LogDebug(bsbase.error);
                            ErrorType type = ErrorType.Unknown;
                            if (bsbase.error == "token_invalid")
                            {
                                if (await RefreshToken())
                                {
                                    return await PlaceDiceBet(BetObj);
                                }
                                else
                                {
                                    type = ErrorType.Other;
                                }
                            }
                            if (bsbase.error.StartsWith("Maximum bet"))
                            {
                                type = ErrorType.InvalidBet;
                            }
                            else if (bsbase.error == "[amount]: This value should be positive.")
                            {
                                type = ErrorType.BetTooLow;
                            }
                            else if (bsbase.error.Contains("Bet in progress, please wait few seconds and retry."))
                            {

                            }
                            else if (bsbase.error == "amount_balance")
                                type = ErrorType.BalanceTooLow;
                            else
                            {

                            }
                            callError(bsbase.error, false, type);
                            return null;
                        }
                    }
                //

            }
            catch (AggregateException e)
            {
                callError("An Unknown error has ocurred.", false, ErrorType.Unknown);
                callNotify("An Unknown error has ocurred.");
            }
            catch (Exception e)
            {
                callError("An Unknown error has ocurred.", false, ErrorType.Unknown);
                callNotify("An Unknown error has ocurred.");
            }
            return null;
        }



        DateTime LastReset = new DateTime();
        protected override async Task<SeedDetails> _ResetSeed()
        {
            try
            {
                if ((DateTime.Now - LastReset).TotalMinutes >= 3)
                {
                    List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                    pairs.Add(new KeyValuePair<string, string>("access_token", accesstoken));
                    pairs.Add(new KeyValuePair<string, string>("username", username));
                    string clientseed = GenerateNewClientSeed();
                    pairs.Add(new KeyValuePair<string, string>("seed_client", clientseed));
                    FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                    var response = await Client.PostAsync("api/change-seeds", Content);
                    string sEmitResponse = await response.Content.ReadAsStringAsync();
                    bsResetSeed bsbase = JsonSerializer.Deserialize<bsResetSeed>(sEmitResponse.Replace("\"return\":", "\"_return\":"));
                    if (bsbase.success)
                    {
                        return new SeedDetails
                        {
                            ClientSeed = clientseed,
                            Nonce = 0,
                            PreviousClient = bsbase.previous_client,
                            PreviousHash = bsbase.previous_hash,
                            ServerHash = bsbase.current_hash,
                            PreviousServer = bsbase.previous_seed,
                            ServerSeed = bsbase.next_hash
                        };
                    }
                    else
                    {
                        if (bsbase.error == "token_invalid")
                        {
                            if (await RefreshToken())
                            {
                                return await _ResetSeed();
                            }
                            else
                            {
                                callError("Session invalid", false, ErrorType.ResetSeed);
                            }
                        }
                    }
                    //sqlite_helper.InsertSeed(bsbase._return.last_seeds_revealed.seed_server_hashed, bsbase._return.last_seeds_revealed.seed_server_revealed);

                    //sqlite_helper.InsertSeed(bsbase._return.last_seeds_revealed.seed_server, bsbase._return.last_seeds_revealed.seed_server_revealed);
                }
                else
                {
                    callError("Too soon to reset seed", false, ErrorType.ResetSeed);
                }
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {

                    string sEmitResponse = new StreamReader(e.Response.GetResponseStream()).ReadToEnd();
                    callNotify(sEmitResponse);
                    if (e.Message.Contains("429"))
                    {
                        Thread.Sleep(2000);
                        return await _ResetSeed();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
                callError("Failed to reset seed", false, ErrorType.ResetSeed);

            }
            Thread.Sleep(51);
            return null;
        }


        protected override async Task<bool> _Login(LoginParamValue[] LoginParams)
        {
            string Username = "";
            string Password = "";
            string APIKey = "";
            string twofa = "";
            foreach (LoginParamValue x in LoginParams)
            {
                if (x.Param.Name.ToLower() == "username")
                    Username = x.Value;
                if (x.Param.Name.ToLower() == "password")
                    Password = x.Value;
                if (x.Param.Name.ToLower() == "2fa code")
                    twofa = x.Value;
                if (x.Param.Name.ToLower() == "api key")
                    APIKey = x.Value?.Trim();
            }
            ClientHandlr = new HttpClientHandler { UseCookies = true, AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip };
            Client = new HttpClient(ClientHandlr) { BaseAddress = new Uri(URLInUse) };
            Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            Client.DefaultRequestHeaders.Add("User-Agent", "DiceBot");

            try
            {

                HttpResponseMessage resp = await Client.GetAsync(URLInUse);
                string s1 = "";


                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                pairs.Add(new KeyValuePair<string, string>("username", Username));
                pairs.Add(new KeyValuePair<string, string>("password", Password));
                //pairs.Add(new KeyValuePair<string, string>("api_key", "0b2edbfe44e98df79665e52896c22987445683e78"));
                if (!string.IsNullOrWhiteSpace(twofa))
                {
                    pairs.Add(new KeyValuePair<string, string>("two_factor", twofa));
                }
                pairs.Add(new KeyValuePair<string, string>("api_key", APIKey));
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                HttpResponseMessage tmpresp = await Client.PostAsync("api/login", Content);

                byte[] bytes = await tmpresp.Content.ReadAsByteArrayAsync();
                string sEmitResponse = await tmpresp.Content.ReadAsStringAsync();

                //getuserstats 
                bsLogin bsbase = JsonSerializer.Deserialize<bsLogin>(sEmitResponse.Replace("\"return\":", "\"_return\":"));


                if (bsbase?.success ?? false)
                {
                    accesstoken = bsbase.access_token;
                    sessiontoken = bsbase.session_token;
                    IsBitsler = true;
                    lastupdate = DateTime.Now;


                    pairs = new List<KeyValuePair<string, string>>();
                    pairs.Add(new KeyValuePair<string, string>("access_token", accesstoken));
                    Content = new FormUrlEncodedContent(pairs);
                    resp = await Client.PostAsync("api/getuserstats", Content);

                    sEmitResponse = await resp.Content.ReadAsStringAsync();
                    JsonElement bsstatsbase = JsonSerializer.Deserialize<dynamic>(sEmitResponse.Replace("\"return\":", "\"_return\":"));
                    if ((object)bsstatsbase != null)

                        if (bsstatsbase.GetProperty("success").GetBoolean())
                        {
                            GetStatsFromStatsBase(bsstatsbase);
                            this.username = Username;
                        }
                        else
                        {
                            if (bsstatsbase.GetProperty("error").GetString() != null)
                            {
                                callNotify(bsstatsbase.GetProperty("error").GetString());
                            }
                        }

                    IsBitsler = true;
                    Thread t = new Thread(GetBalanceThread);
                    t.Start();
                    callLoginFinished(true);
                    return true;
                }
                else
                {
                    if (bsbase.error != null)
                        callNotify(bsbase.error);
                    callLoginFinished(false);
                }

            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
                callLoginFinished(false);
            }
            return false;
        }

        DateTime LastBet = DateTime.Now;
        double LastBetAmount = 0;

        public override int _TimeToBet(PlaceBet BetDetails)
        {
            //return true;
            decimal amount = BetDetails.Amount;
            int type_delay = 0;

            if (CurrentCurrency.ToLower() == "btc")
            {
                if (LastBetAmount <= 0.00000005 || (double)amount <= 0.00000005)
                    type_delay = 1;
                else
                    type_delay = 2;
            }
            else if (CurrentCurrency.ToLower() == "eth")
            {
                if (LastBetAmount <= 0.00000250 || (double)amount <= 0.00000250)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "ltc")
            {
                if (LastBetAmount <= 0.00001000 || (double)amount <= 0.00001000)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "doge")
            {
                if (LastBetAmount <= 5.00000000 || (double)amount <= 5.00000000)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "burst")
            {
                if (LastBetAmount <= 5.00000000 || (double)amount <= 5.00000000)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "bch")
            {
                if (LastBetAmount <= 0.00000025 || (double)amount <= 0.00000025)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "dash")
            {
                if (LastBetAmount <= 0.00000025 || (double)amount <= 0.00000025)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "zec")
            {
                if (LastBetAmount <= 0.00000025 || (double)amount <= 0.00000025)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "xmr")
            {
                if (LastBetAmount <= 0.00000025 || (double)amount <= 0.00000025)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "etc")
            {
                if (LastBetAmount <= 0.00000025 || (double)amount <= 0.00000025)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "neo")
            {
                if (LastBetAmount <= 0.00000025 || (double)amount <= 0.00000025)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "strat")
            {
                if (LastBetAmount <= 0.00000025 || (double)amount <= 0.00000025)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "kmd")
            {
                if (LastBetAmount <= 0.00000025 || (double)amount <= 0.00000025)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            else if (CurrentCurrency.ToLower() == "xrp")
            {
                if (LastBetAmount <= 0.00000025 || (double)amount <= 0.00000025)
                    type_delay = 1;
                else
                    type_delay = 2;

            }
            int delay = 0;
            if (type_delay == 1)
                delay = 300;
            else if (type_delay == 2)
                delay = 200;
            else
                delay = 200;

            return delay - (int)((DateTime.Now - LastBet).TotalMilliseconds);
        }



        public static IGameResult sGetLucky(string server, string client, int nonce, Games game)
        {
            string hex = Hash.HMAC512($"{client},{nonce}", server);
            if (game == Games.Dice || game == Games.Twist)
            {
                int charstouse = 5;
                int offset = 0;
                decimal Lucky = 0;
                do
                {
                    string s = hex.ToString().Substring(offset, charstouse);
                    Lucky = long.Parse(s, System.Globalization.NumberStyles.HexNumber);
                    offset += charstouse;
                } while (Lucky > 999999);
                if (game == Games.Dice)
                {
                    Lucky = (Lucky % 10000.0m) / 100.0m;

                    return new DiceResult { Roll = Lucky };
                }
                else if (game == Games.Twist)
                {
                    Lucky = Lucky % 100;
                    return new TwistResult { Roll = Lucky };
                }
            }
            else if (game == Games.Limbo)
            {
                string part = hex.Substring(0, 52 / 4);
                decimal number = long.Parse(part, System.Globalization.NumberStyles.HexNumber) / (decimal)Math.Pow(2, 52);

                decimal multiplier = Math.Floor(98 / (1 - number)) / 100;

                return new LimboResult { Result = Math.Max(1, Math.Min(multiplier, 1000000)) };
            }
            return null;
        }

        protected override IGameResult _GetLucky(string server, string client, int nonce, Games game)
        {

            return sGetLucky(server, client, nonce, game);
        }


        protected override void _Disconnect()
        {
            this.IsBitsler = false;
            Client = null;
            ClientHandlr = null;
        }

        public override void SetProxy(ProxyDetails ProxyInfo)
        {
            throw new NotImplementedException();
        }

        void GetStatsFromStatsBase(JsonElement bsstatsbase)
        {
            //if (bsstatsbase is ExpandoObject exp)
            {
                //Stats.Balance = decimal.Parse(bsstatsbase.GetProperty($"{CurrentCurrency.ToLower()}_balance").GetString(), NumberFormatInfo.InvariantInfo);
                Stats.Profit = decimal.Parse(bsstatsbase.GetProperty($"{CurrentCurrency.ToLower()}_profit").GetString(), NumberFormatInfo.InvariantInfo);
                Stats.Wagered = decimal.Parse(bsstatsbase.GetProperty($"{CurrentCurrency.ToLower()}_wagered").GetString(), NumberFormatInfo.InvariantInfo);
                Stats.Bets = bsstatsbase.GetProperty($"bets").GetInt32();
                Stats.Wins = bsstatsbase.GetProperty($"wins").GetInt32();
                Stats.Losses = bsstatsbase.GetProperty($"losses").GetInt32();
            }
            return;


        }

        protected override async Task<bool> _Bank(decimal Amount)
        {
            try
            {
                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();

                pairs.Add(new KeyValuePair<string, string>("access_token", accesstoken));
                pairs.Add(new KeyValuePair<string, string>("amount", Amount.ToString("0.00000000", System.Globalization.NumberFormatInfo.InvariantInfo)));
                pairs.Add(new KeyValuePair<string, string>("jp_optin", "0"));
                pairs.Add(new KeyValuePair<string, string>("currency", CurrentCurrency));
                pairs.Add(new KeyValuePair<string, string>("to", "true"));
                pairs.Add(new KeyValuePair<string, string>("api_key", "0b2edbfe44e98df79665e52896c22987445683e78"));
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                HttpResponseMessage tmpmsg = await Client.PostAsync("/api/vault-transaction", Content);
                string sEmitResponse = await tmpmsg.Content.ReadAsStringAsync();
                bsLogin result = JsonSerializer.Deserialize<bsLogin>(sEmitResponse);
                if (tmpmsg.IsSuccessStatusCode && result.success)
                {
                    Stats.Balance -= Amount;
                    callStatsUpdated(Stats);
                    callBankFinished(true, "");
                    return true;
                }
                else
                {
                    callError("Could not bank funds: " + result.error, false, ErrorType.Bank);
                    _logger.LogError(result.error);
                    callBankFinished(false, result.error);
                    return false;
                }
                //

            }
            catch (AggregateException e)
            {
                callError("An Unknown error has ocurred.", false, ErrorType.Bank);
                callNotify("An Unknown error has ocurred.");
            }
            catch (Exception e)
            {
                callError("An Unknown error has ocurred.", false, ErrorType.Bank);
                callNotify("An Unknown error has ocurred.");
            }
            return false;

        }
        protected override async Task<SiteStats> _UpdateStats()
        {
            try
            {
                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                pairs.Add(new KeyValuePair<string, string>("access_token", accesstoken));
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                HttpResponseMessage resp = await Client.PostAsync("api/getuserstats", Content);

                string s1 = "";
                string sEmitResponse = "";// resp.Content.ReadAsStringAsync().Result;

                if (resp.IsSuccessStatusCode)
                {
                    sEmitResponse = await resp.Content.ReadAsStringAsync();
                    if (sEmitResponse.Contains("token_invalid"))
                    {
                        if (await RefreshToken())
                        {
                            return await _UpdateStats();
                        }
                        else
                        {
                            callError("Session invalid", false, ErrorType.ResetSeed);
                        }
                    }
                }
                else
                {
                    sEmitResponse = "";
                    if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {
                        s1 = await resp.Content.ReadAsStringAsync();
                    }
                    else
                    {

                    }
                }
                if (sEmitResponse != "")
                {
                    JsonElement bsstatsbase = JsonSerializer.Deserialize<dynamic>(sEmitResponse.Replace("\"return\":", "\"_return\":"));
                    if ((object)bsstatsbase != null)
                        //if (bsstatsbase._return != null)
                        if (bsstatsbase.GetProperty("success").GetBoolean())
                        {
                            GetStatsFromStatsBase(bsstatsbase);
                        }
                        else
                        {
                            if (bsstatsbase.GetProperty("error").GetString() != null)
                            {
                                callNotify(bsstatsbase.GetProperty("error").GetString());
                            }
                        }
                }
                return Stats;
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
                callError("Failed to update stats", false, ErrorType.Other);
                return null;
            }
        }
        public async Task<TwistBet> PlaceTwistBet(PlaceTwistBet bet)
        {
            try
            {
                if (bet.Chance > 98m)
                {
                    callError("Chance must be less than 99.99", false, ErrorType.InvalidBet);
                    return null;
                }
                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                /*access_token
type:dice
amount:0.00000001
condition:< or >
game:49.5
devise:btc*/
                pairs.Add(new KeyValuePair<string, string>("access_token", accesstoken));
                //pairs.Add(new KeyValuePair<string, string>("type", "dice"));
                pairs.Add(new KeyValuePair<string, string>("amount", bet.Amount.ToString("0.00000000", System.Globalization.NumberFormatInfo.InvariantInfo)));
                pairs.Add(new KeyValuePair<string, string>("over", "false"));
                pairs.Add(new KeyValuePair<string, string>("target", !bet.High ? bet.Chance.ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo) : (TwistSettings.MaxRoll - bet.Chance).ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo)));
                pairs.Add(new KeyValuePair<string, string>("currency", CurrentCurrency));
                pairs.Add(new KeyValuePair<string, string>("api_key", "0b2edbfe44e98df79665e52896c22987445683e78"));
                pairs.Add(new KeyValuePair<string, string>("jp_optin", "0"));
                pairs.Add(new KeyValuePair<string, string>("auto", "false"));
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                HttpResponseMessage tmpmsg = await Client.PostAsync("api/bet-twist", Content);
                string sEmitResponse = await tmpmsg.Content.ReadAsStringAsync();

                bsBet bsbase = null;
                try
                {
                    bsbase = JsonSerializer.Deserialize<bsBetwithstringid>(sEmitResponse.Replace("\"return\":", "\"_return\":"));
                }
                catch (Exception e)
                {

                }

                if (bsbase != null)
                    // if (bsbase._return != null)
                    if (bsbase.success)
                    {
                        Stats.Balance = decimal.Parse(bsbase.new_balance, System.Globalization.NumberFormatInfo.InvariantInfo);
                        lastupdate = DateTime.Now;
                        TwistBet tmp = bsbase.ToTwistBet();
                        tmp.Currency = CurrentCurrency;
                        tmp.High = bet.High;
                        tmp.Chance = bet.Chance;
                        tmp.Guid = bet.GUID;
                        Stats.Profit += (decimal)tmp.Profit;
                        Stats.Wagered += (decimal)tmp.TotalAmount;
                        tmp.DateValue = DateTime.Now;
                        tmp.IsWin = tmp.GetWin(TwistSettings);

                        //set win
                        if (tmp.IsWin)
                            Stats.Wins++;
                        else
                            Stats.Losses++;
                        Stats.Bets++;
                        LastBetAmount = (double)bet.Amount;
                        LastBet = DateTime.Now;
                        callBetFinished(tmp);
                        return tmp;
                    }
                    else
                    {
                        if (bsbase.error != null)
                        {
                            ErrorType type = ErrorType.Unknown;
                            if (bsbase.error == "token_invalid")
                            {
                                if (await RefreshToken())
                                {
                                    return await PlaceTwistBet(bet);
                                }
                                else
                                {
                                    type = ErrorType.Other;
                                }
                            }
                            if (bsbase.error.StartsWith("Maximum bet"))
                            {
                                type = ErrorType.InvalidBet;
                            }
                            else if (bsbase.error == "Bet amount not valid")
                            {
                                type = ErrorType.BetTooLow;
                            }
                            else if (bsbase.error.Contains("Bet in progress, please wait few seconds and retry."))
                            {

                            }
                            else if (bsbase.error == "Insufficient fund")
                                type = ErrorType.BalanceTooLow;
                            else
                            {

                            }
                            callError(bsbase.error, false, type);
                            return null;
                        }
                    }
                //

            }
            catch (AggregateException e)
            {
                callError("An Unknown error has ocurred.", false, ErrorType.Unknown);
                callNotify("An Unknown error has ocurred.");
            }
            catch (Exception e)
            {
                callError("An Unknown error has ocurred.", false, ErrorType.Unknown);
                callNotify("An Unknown error has ocurred.");
            }
            return null;
        }
        public async Task<LimboBet> PlaceLimboBet(PlaceLimboBet bet)
        {
            try
            {
                if (bet.Payout > LimboSettings.MaxPayout)
                {
                    callError("Payout must be less than " + LimboSettings.MaxPayout, false, ErrorType.InvalidBet);
                    return null;
                }


                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                /*access_token
type:dice
amount:0.00000001
condition:< or >
game:49.5
devise:btc*/
                pairs.Add(new KeyValuePair<string, string>("access_token", accesstoken));
                //pairs.Add(new KeyValuePair<string, string>("type", "dice"));
                pairs.Add(new KeyValuePair<string, string>("amount", bet.Amount.ToString("0.00000000", System.Globalization.NumberFormatInfo.InvariantInfo)));

                pairs.Add(new KeyValuePair<string, string>("payout", bet.Payout.ToString("0.00", System.Globalization.NumberFormatInfo.InvariantInfo)));
                pairs.Add(new KeyValuePair<string, string>("currency", CurrentCurrency));
                pairs.Add(new KeyValuePair<string, string>("api_key", "0b2edbfe44e98df79665e52896c22987445683e78"));
                pairs.Add(new KeyValuePair<string, string>("jp_optin", "0"));
                pairs.Add(new KeyValuePair<string, string>("auto", "false"));
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                HttpResponseMessage tmpmsg = await Client.PostAsync("api/bet-boom", Content);
                string sEmitResponse = await tmpmsg.Content.ReadAsStringAsync();

                bsBet bsbase = null;
                try
                {
                    bsbase = JsonSerializer.Deserialize<bsBetwithstringid>(sEmitResponse.Replace("\"return\":", "\"_return\":"));
                }
                catch (Exception e)
                {

                }

                if (bsbase != null)
                    // if (bsbase._return != null)
                    if (bsbase.success)
                    {
                        Stats.Balance = decimal.Parse(bsbase.new_balance, System.Globalization.NumberFormatInfo.InvariantInfo);
                        lastupdate = DateTime.Now;
                        LimboBet tmp = bsbase.ToLimboBet();
                        tmp.Currency = CurrentCurrency;
                        tmp.Payout = bet.Payout;
                        tmp.Guid = bet.GUID;
                        Stats.Profit += (decimal)tmp.Profit;
                        Stats.Wagered += (decimal)tmp.TotalAmount;
                        tmp.DateValue = DateTime.Now;
                        tmp.IsWin = tmp.GetWin(LimboSettings);

                        //set win
                        if (tmp.IsWin)
                            Stats.Wins++;
                        else
                            Stats.Losses++;
                        Stats.Bets++;
                        LastBetAmount = (double)bet.Amount;
                        LastBet = DateTime.Now;
                        callBetFinished(tmp);
                        return tmp;
                    }
                    else
                    {
                        if (bsbase.error != null)
                        {
                            ErrorType type = ErrorType.Unknown;
                            if (bsbase.error == "token_invalid")
                            {
                                if (await RefreshToken())
                                {
                                    return await PlaceLimboBet(bet);
                                }
                                else
                                {
                                    type = ErrorType.Other;
                                }
                            }
                            if (bsbase.error.StartsWith("Maximum bet"))
                            {
                                type = ErrorType.InvalidBet;
                            }
                            else if (bsbase.error == "Bet amount not valid")
                            {
                                type = ErrorType.BetTooLow;
                            }
                            else if (bsbase.error.Contains("Bet in progress, please wait few seconds and retry."))
                            {

                            }
                            else if (bsbase.error == "Insufficient fund")
                                type = ErrorType.BalanceTooLow;
                            else
                            {

                            }
                            callError(bsbase.error, false, type);
                            return null;
                        }
                    }
                //

            }
            catch (AggregateException e)
            {
                callError("An Unknown error has ocurred.", false, ErrorType.Unknown);
                callNotify("An Unknown error has ocurred.");
            }
            catch (Exception e)
            {
                callError("An Unknown error has ocurred.", false, ErrorType.Unknown);
                callNotify("An Unknown error has ocurred.");
            }
            return null;
        }

        protected override Task<bool> _BrowserLogin()
        {
            throw new NotImplementedException();
        }

        public class bsLogin
        {
            public bool success { get; set; }
            public string access_token { get; set; }
            public string session_token { get; set; }
            public string error { get; set; }
        }

        public class BsLoginBase
        {
            public bsLogin _return { get; set; }
        }
        //"{\"return\":{\"success\":\"true\",\"balance\":1.0e-5,\"wagered\":0,\"profit\":0,\"bets\":\"0\",\"wins\":\"0\",\"losses\":\"0\"}}"
        //{"return":{"success":"true","bets":0,"wins":0,"losses":0,"btc_profit":"0.00000000","btc_wagered":"0.00000000","btc_balance":"0.00000000","eth_profit":"0.00000000","eth_wagered":"0.00000000","eth_balance":"0.00000000","ltc_profit":"0.00000000","ltc_wagered":"0.00000000","ltc_balance":"0.00000000","bch_profit":"0.00000000","bch_wagered":"0.00000000","bch_balance":"0.00000000","doge_profit":"0.00000000","doge_wagered":"0.00000000","doge_balance":"0.00000000","dash_profit":"0.00000000","dash_wagered":"0.00000000","dash_balance":"0.00000000","zec_profit":"0.00000000","zec_wagered":"0.00000000","zec_balance":"0.00000000","burst_profit":"0.00000000","burst_wagered":"0.00000000","burst_balance":"0.00000000"}}


        public class bsStats
        {
            public string error { get; set; }
            public bool success { get; set; }
            public int bets { get; set; }
            public int wins { get; set; }
            public int losses { get; set; }
            public string btc_profit { get; set; }
            public string btc_wagered { get; set; }
            public string eth_profit { get; set; }
            public string eth_wagered { get; set; }
            public string xrp_profit { get; set; }
            public string xrp_wagered { get; set; }
            public string ltc_profit { get; set; }
            public string ltc_wagered { get; set; }
            public string doge_profit { get; set; }
            public string doge_wagered { get; set; }
            public string etc_profit { get; set; }
            public string etc_wagered { get; set; }
            public string bnb_profit { get; set; }
            public string bnb_wagered { get; set; }
            public string busd_profit { get; set; }
            public string busd_wagered { get; set; }
            public string usdc_profit { get; set; }
            public string usdc_wagered { get; set; }
            public string sol_profit { get; set; }
            public string sol_wagered { get; set; }
            public string ada_profit { get; set; }
            public string ada_wagered { get; set; }
            public string bch_profit { get; set; }
            public string bch_wagered { get; set; }
            public string dash_profit { get; set; }
            public string dash_wagered { get; set; }
            public string btg_profit { get; set; }
            public string btg_wagered { get; set; }
            public string zec_profit { get; set; }
            public string zec_wagered { get; set; }
            public string dgb_profit { get; set; }
            public string dgb_wagered { get; set; }
            public string eos_profit { get; set; }
            public string eos_wagered { get; set; }
            public string xlm_profit { get; set; }
            public string xlm_wagered { get; set; }
            public string trx_profit { get; set; }
            public string trx_wagered { get; set; }
            public string neo_profit { get; set; }
            public string neo_wagered { get; set; }
            public string qtum_profit { get; set; }
            public string qtum_wagered { get; set; }
            public string usdt_profit { get; set; }
            public string usdt_wagered { get; set; }
            public string ethw_profit { get; set; }
            public string ethw_wagered { get; set; }
            public string matic_profit { get; set; }
            public string matic_wagered { get; set; }
            public string shib_profit { get; set; }
            public string shib_wagered { get; set; }
            public string link_profit { get; set; }
            public string link_wagered { get; set; }
            public string dai_profit { get; set; }
            public string dai_wagered { get; set; }
            public string ton_profit { get; set; }
            public string ton_wagered { get; set; }
            public string avax_profit { get; set; }
            public string avax_wagered { get; set; }
            public string btslr_profit { get; set; }
            public string btslr_wagered { get; set; }
            public string brl_profit { get; set; }
            public string brl_wagered { get; set; }
            public string fdusd_profit { get; set; }
            public string fdusd_wagered { get; set; }
            public string btc_balance { get; set; }
            public string eth_balance { get; set; }
            public string ltc_balance { get; set; }
            public string doge_balance { get; set; }
            public string bch_balance { get; set; }
            public string etc_balance { get; set; }
            public string zec_balance { get; set; }
            public string neo_balance { get; set; }
            public string dgb_balance { get; set; }
            public string btg_balance { get; set; }
            public string qtum_balance { get; set; }
            public string bnb_balance { get; set; }
            public string ethw_balance { get; set; }
            public string sol_balance { get; set; }
            public string ada_balance { get; set; }
            public string usdt_balance { get; set; }
            public string trx_balance { get; set; }
            public string matic_balance { get; set; }
            public string xlm_balance { get; set; }
            public string xrp_balance { get; set; }
            public string dash_balance { get; set; }
            public string eos_balance { get; set; }
        }



        public class bsBetBase
        {
            public bsBet _return { get; set; }
        }



        public class bsBetwithstringid:bsBet
        {
            public string id { get; set; }

            public override string GetId()
            {
                return id;
            }
        }
        public class bsBetwithintid : bsBet
        {
            public long id { get; set; }
            public override string GetId()
            {
                return id.ToString();
            }
        }
        public abstract class bsBet
        {
            public string error { get; set; }
            public bool success { get; set; }
            public decimal result { get; set; }
            public string server_seed { get; set; }
            public string client_seed { get; set; }
            public long nonce { get; set; }
           
            public int timestamp { get; set; }
            public string amount { get; set; }
            public string currency { get; set; }
            public decimal payout { get; set; }
            public string profit { get; set; }
            public decimal xp { get; set; }
            public decimal xp_add { get; set; }
            public string username { get; set; }
            public string new_balance { get; set; }
            public object[] notifications { get; set; }
            public abstract string GetId();

            public DiceBet ToBet()
            {
                DiceBet tmp = new DiceBet
                {
                    TotalAmount = decimal.Parse(amount, System.Globalization.NumberFormatInfo.InvariantInfo),
                    DateValue = DateTime.Now,
                    BetID = GetId(),
                    Profit = decimal.Parse(profit, System.Globalization.NumberFormatInfo.InvariantInfo),
                    Roll = (decimal)result,

                    Nonce = nonce,
                    ServerHash = server_seed,
                    ClientSeed = client_seed
                };
                return tmp;
            }
            public TwistBet ToTwistBet()
            {
                TwistBet tmp = new TwistBet
                {
                    TotalAmount = decimal.Parse(amount, System.Globalization.NumberFormatInfo.InvariantInfo),
                    DateValue = DateTime.Now,
                    BetID = GetId(),
                    Profit = decimal.Parse(profit, System.Globalization.NumberFormatInfo.InvariantInfo),
                    Roll = (decimal)result,

                    Nonce = nonce,
                    ServerHash = server_seed,
                    ClientSeed = client_seed
                };
                return tmp;
            }
            public LimboBet ToLimboBet()
            {
                LimboBet tmp = new LimboBet
                {
                    TotalAmount = decimal.Parse(amount, System.Globalization.NumberFormatInfo.InvariantInfo),
                    DateValue = DateTime.Now,
                    BetID = GetId(),
                    Profit = decimal.Parse(profit, System.Globalization.NumberFormatInfo.InvariantInfo),
                    Result = (decimal)result,

                    Nonce = nonce,
                    ServerHash = server_seed,
                    ClientSeed = client_seed
                };
                return tmp;
            }
        }

        
        public class bsResetSeed
        {
            public string previous_hash { get; set; }
            public string previous_seed { get; set; }
            public string previous_client { get; set; }
            public string previous_total { get; set; }
            public string current_client { get; set; }
            public string current_hash { get; set; }
            public string next_hash { get; set; }
            public bool success { get; set; }
            public string error { get; set; }
        }

    }
}
