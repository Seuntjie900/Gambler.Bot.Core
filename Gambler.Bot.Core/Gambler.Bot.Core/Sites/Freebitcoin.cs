using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Gambler.Bot.Common.Enums;
using Gambler.Bot.Common.Games;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Helpers;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites.Classes;
using Gambler.Bot.Core.SiteClient;
using Microsoft.Extensions.Logging;

namespace Gambler.Bot.Core.Sites
{
    public class Freebitcoin : BaseSite, iDice
    {
        string accesstoken = "";
        DateTime LastSeedReset = new DateTime();
        public bool ispd = false;
        string username = "";
        long uid = 0;
        DateTime lastupdate = new DateTime();


        public Freebitcoin(ILogger logger) : base(logger)
        {
            StaticLoginParams = new LoginParameter[] { new LoginParameter("Username", false, true, false, false), new LoginParameter("Password", true, true, false, true), new LoginParameter("2FA Code", false, false, true, true, true) };
            //this.MaxRoll = 100m;
            this.SiteAbbreviation = "FBtc";
            this.SiteName = "FreeBitcoin";
            this.SiteURL = "https://freebitco.in/?r=2310118";
            this.Mirrors.Add("https://freebitco.in");
            AffiliateCode = "?r=2310118";
            this.Stats = new SiteStats();
            this.TipUsingName = true;
            this.AutoInvest = false;
            this.AutoWithdraw = false;
            this.CanChangeSeed = false;
            this.CanChat = false;
            this.CanGetSeed = true;
            this.CanRegister = false;
            this.CanSetClientSeed = false;
            this.CanTip = true;
            this.CanVerify = true;
            Currencies = new string[] {"Btc" };
            
            SupportedGames = new Games[] { Games.Dice };
            CurrentCurrency ="btc";
            this.DiceBetURL = "https://freebitco.in/?r=2310118&bet={0}";
            //this.Edge = 5m;
            DiceSettings = new DiceConfig() { Edge = 5, MaxRoll = 100m };
            SupportsNormalLogin = false;
            SupportsBrowserLogin = true;
        }


        public override void SetProxy(ProxyDetails ProxyInfo)
        {
            throw new NotImplementedException();
        }

        protected override void _Disconnect()
        {
            ispd = false;
        }
        CookieContainer Cookies = new CookieContainer();
        string csrf = "";
        string address = "";
        protected override async Task<bool> _Login(LoginParamValue[] LoginParams)
        {
            string Username = "";
            string Password = "";
            string otp = "";
            foreach (LoginParamValue x in LoginParams)
            {
                if (x.Param.Name.ToLower() == "username")
                    Username = x.Value;
                if (x.Param.Name.ToLower() == "password")
                    Password = x.Value;
                if (x.Param.Name.ToLower() == "2fa code")
                    otp = x.Value;
            }
            Client = await CreateClientAsync(new SiteClientOptions { BaseAddress = new Uri(URLInUse), Cookies = Cookies });
            try
            {
                string s1 = "";
                HttpResponseMessage resp = await Client.GetAsync("");
                /*if (resp.IsSuccessStatusCode)
                {
                    s1 = await resp.Content.ReadAsStringAsync();
                }
                else
                {
                    if (resp.StatusCode == HttpStatusCode.ServiceUnavailable)
                    {*/
                        s1 = await resp.Content.ReadAsStringAsync();
                        //cflevel = 0;
                        
                        var thing = CallBypassRequired(this.SiteURL, ["__cf_bm"]);

                /*
                    }
                }*/
                foreach (Cookie x in Cookies.GetCookies(new Uri(URLInUse)))
                {
                    if (x.Name == "csrf_token")
                    {
                        csrf = x.Value;
                    }
                }
                List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
                pairs.Add(new KeyValuePair<string, string>("csrf_token", csrf));
                pairs.Add(new KeyValuePair<string, string>("op", "login_new"));
                pairs.Add(new KeyValuePair<string, string>("btc_address", Username));
                pairs.Add(new KeyValuePair<string, string>("password", Password));
                pairs.Add(new KeyValuePair<string, string>("tfa_code", otp));
                FormUrlEncodedContent Content = new FormUrlEncodedContent(pairs);
                var EmitResponse = await Client.PostAsync("" + accesstoken, Content);
                int retriees = 0;
                while (!EmitResponse.IsSuccessStatusCode && retriees++ < 5)
                {
                    string sresponse = await EmitResponse.Content.ReadAsStringAsync();
                    await Task.Delay(Random.Next(50, 150) * retriees);
                    Thread.Sleep(100);
                    EmitResponse = await Client.PostAsync("" + accesstoken, Content);
                }
                if (EmitResponse.IsSuccessStatusCode)
                {
                    string s = await EmitResponse.Content.ReadAsStringAsync();
                    string[] messages = s.Split(':');
                    if (messages.Length > 2)
                    {
                        address = messages[1];
                        accesstoken = messages[2];
                        Cookies.Add(new Cookie("btc_address", address, "/", URLInUse));
                        Cookies.Add(new Cookie("password", accesstoken, "/", URLInUse));
                        Cookies.Add(new Cookie("have_account", "1", "/", URLInUse));

                        s =await Client.GetStringAsync($"{URLInUse}/cgi-bin/api.pl?op=get_user_stats");
                        FreebtcStats stats = JsonSerializer.Deserialize<FreebtcStats>(s);
                        if (stats != null)
                        {
                            Stats.Balance = stats.balance / 100000000m;
                            Stats.Bets = (int)stats.rolls_played;
                            Stats.Wins = Stats.Losses = 0;
                            Stats.Profit = stats.dice_profit / 100000000m;
                            Stats.Wagered = stats.wagered / 100000000m;
                           
                            lastupdate = DateTime.Now;
                            ispd = true;
                            Thread t = new Thread(GetBalanceThread);
                            t.Start();
                            callLoginFinished(true);
                            return true;
                        }
                        callLoginFinished(false);
                        return false;

                    }
                    callLoginFinished(false);
                    return false;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
            }
            callLoginFinished(false);
            return false;
        }

        protected override async Task<SiteStats> _UpdateStats()
        {
            try
            {
                lastupdate = DateTime.Now;
                string s = await Client.GetStringAsync($"{URLInUse}/cgi-bin/api.pl?op=get_user_stats");
                FreebtcStats stats = JsonSerializer.Deserialize<FreebtcStats>(s);
                if (stats != null)
                {
                    Stats.Balance = stats.balance / 100000000m;
                    Stats.Bets = (int)stats.rolls_played;
                    //wins = losses = 0;
                    Stats.Profit = stats.dice_profit / 100000000m;
                    Stats.Wagered = stats.wagered / 100000000m;
                    return Stats;
                }

            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
            }
            return null;
        }


        void GetBalanceThread()
        {
            while (ispd)
            {
                if ((DateTime.Now - lastupdate).TotalSeconds > 30)
                {
                    lastupdate = DateTime.Now;
                    UpdateStats();
                }
                Thread.Sleep(100);
            }
        }
        string clientseed = "";

        public DiceConfig DiceSettings { get; set; }

        public override string GenerateNewClientSeed()
        {
            string seed = "";
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZqwertyuiopasdfghjklzxcvbnm1234567890";
            while (seed.Length < 16)
            {
                seed += chars[Random.Next(0, chars.Length)];
            }
            return seed;
        }

        public async Task<DiceBet> PlaceDiceBet(PlaceDiceBet BetDetails)
        {
            try
            {
               
                bool High = BetDetails.High;
                decimal amount = BetDetails.Amount;
                decimal chance = BetDetails.Chance;
                
                clientseed = GenerateNewClientSeed();
                string Params = string.Format(System.Globalization.NumberFormatInfo.InvariantInfo, "m={0}&client_seed={1}&jackpot=0&stake={2}&multiplier={3}&rand={5}&csrf_token={4}",
                    High ? "hi" : "lo", clientseed, amount, (100m - DiceSettings.Edge) / chance, csrf, Random.Next(0, 9999999) / 10000000);

                var betresult =await Client.GetAsync($"{URLInUse}/cgi-bin/bet.pl?" + Params);
                if (betresult.IsSuccessStatusCode)
                {

                    string Result = await betresult.Content.ReadAsStringAsync();
                    string[] msgs = Result.Split(':');
                    if (msgs.Length > 2)
                    {

                        /*
                            1. Success code (s1)
                            2. Result (w/l)
                            3. Rolled number
                            4. User balance
                            5. Amount won or lost (always positive). If 2. is l, then amount is subtracted from balance else if w it is added.
                            6. Redundant (can ignore)
                            7. Server seed hash for next roll
                            8. Client seed of previous roll
                            9. Nonce for next roll
                            10. Server seed for previous roll
                            11. Server seed hash for previous roll
                            12. Client seed again (can ignore)
                            13. Previous nonce
                            14. Jackpot result (1 if won 0 if not won)
                            15. Redundant (can ignore)
                            16. Jackpot amount won (0 if lost)
                            17. Bonus account balance after bet
                            18. Bonus account wager remaining
                            19. Max. amount of bonus eligible
                            20. Max bet
                            21. Account balance before bet
                            22. Account balance after bet
                            23. Bonus account balance before bet
                            24. Bonus account balance after bet
                         */
                        DiceBet tmp = new DiceBet
                        {
                            Guid = BetDetails.GUID,
                            TotalAmount = amount,
                            DateValue = DateTime.Now,
                            Chance = chance,
                            ClientSeed = msgs[7],
                            High = High,
                           BetID = Stats.Bets.ToString(),
                            Profit = msgs[1] == "w" ? decimal.Parse(msgs[4]) : -decimal.Parse(msgs[4], System.Globalization.NumberFormatInfo.InvariantInfo),
                            Nonce = long.Parse(msgs[12], System.Globalization.NumberFormatInfo.InvariantInfo),
                            ServerHash = msgs[10],
                            ServerSeed = msgs[9],
                            Roll = decimal.Parse(msgs[2], System.Globalization.NumberFormatInfo.InvariantInfo) / 100.0m,
                            Currency = CurrentCurrency

                        };
                        tmp.IsWin = tmp.GetWin(this.DiceSettings);
                        Stats.Balance = decimal.Parse(msgs[3], System.Globalization.NumberFormatInfo.InvariantInfo);
                        if (msgs[1] == "w")
                            Stats.Wins++;
                        else Stats.Losses++;
                        Stats.Bets++;
                        Stats.Wagered += amount;
                        Stats.Profit += tmp.Profit;
                        callBetFinished(tmp);
                        return tmp;
                    }
                    else if (msgs.Length > 0)
                    {
                        //20 - too low balance
                        if (msgs.Length > 1)

                        {
                            if (msgs[1] == "20")
                            {
                                callError("Balance too low.",true, ErrorType.BalanceTooLow);
                            }
                        }
                        else
                        {
                            callError("Site returned unknown error. Retrying in 30 seconds.", true, ErrorType.Unknown);
                        }
                    }
                    else
                    {
                        callError("Site returned unknown error. Retrying in 30 seconds.",true, ErrorType.Unknown);
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
                callError("An internal error occured. Retrying in 30 seconds.",true, ErrorType.Unknown);
            }
            return null;
        }

        protected override IGameResult _GetLucky(string ServerSeed, string ClientSeed, int Nonce, Games Game)
        {
            ServerSeed = Nonce + ":" + ServerSeed + ":" + Nonce;
            string msg = Nonce + ":" + ClientSeed + ":" + Nonce;
            string hex = Hash.HMAC512(ServerSeed, msg);
            int charstouse = 8;
            if (Game == Games.Dice)
            {
                for (int i = 0; i < hex.Length; i += charstouse)
                {

                    string s = hex.ToString().Substring(i, charstouse);

                    decimal lucky = long.Parse(s, System.Globalization.NumberStyles.HexNumber);
                    /*if (lucky < 1000000)
                        return lucky / 10000;*/
                    lucky = Math.Round(lucky / 429496.7295m);
                    return new DiceResult { Roll = lucky / 100 };
                }
            }
            return null;
        }

        protected override async Task<bool> _BrowserLogin()
        {
            
            try
            {

                var cookies = await CallBypassRequired(URLInUse + AffiliateCode, ["cf_clearance","password"], false, "/cgi-bin/");

                Client = await CreateClientAsync(new SiteClientOptions { BaseAddress = new Uri(URLInUse), Cookies = cookies.Cookies });

                foreach (var x in cookies.Headers)
                {
                    try
                    {
                        if (x.Key.ToLower() == "content-type"
                            || x.Key.ToLower() == "cookie"
                            || x.Key.ToLower() == "authorization"
                            || x.Key.ToLower() == "x-access-token")
                            continue;
                        Client.DefaultHeaders.Add(x.Key, x.Value);
                    }
                    catch (Exception ex)
                    {

                    }
                }

                var response = await Client.GetAsync($"{URLInUse}/cgi-bin/api.pl?op=get_user_stats");
                string responsestring = await response.Content.ReadAsStringAsync();
                int retriees = 0;
                while (!response.IsSuccessStatusCode && retriees++ < 5)
                {
                    await CallCFCaptchaBypass(responsestring);
                    await Task.Delay(Random.Next(50, 150) * retriees);
                    response = await Client.GetAsync($"{URLInUse}/cgi-bin/api.pl?op=get_user_stats");
                    responsestring = await response.Content.ReadAsStringAsync();
                }
                FreebtcStats stats = JsonSerializer.Deserialize<FreebtcStats>(responsestring);
                if (stats != null)
                {
                    Stats.Balance = stats.balance / 100000000m;
                    Stats.Bets = (int)stats.rolls_played;
                    Stats.Wins = Stats.Losses = 0;
                    Stats.Profit = stats.dice_profit / 100000000m;
                    Stats.Wagered = stats.wagered / 100000000m;

                    lastupdate = DateTime.Now;
                    ispd = true;
                    Thread t = new Thread(GetBalanceThread);
                    t.Start();
                    callLoginFinished(true);
                    return true;
                }
                callLoginFinished(false);
                return false;

                
                callLoginFinished(false);
                return false;
                
            }
            catch (Exception e)
            {
                _logger?.LogError(e.ToString());
            }
            callLoginFinished(false);
            return false;
        }

        public class FreebtcStats
        {
            public long wagered { get; set; }
            public long rolls_played { get; set; }
            public decimal lottery_spent { get; set; }
            public string status { get; set; }
            public decimal jackpot_winnings { get; set; }
            public decimal jackpot_spent { get; set; }
            public decimal reward_points { get; set; }
            public decimal balance { get; set; }
            public decimal total_winnings { get; set; }
            public decimal dice_profit { get; set; }
        }
    }
}
