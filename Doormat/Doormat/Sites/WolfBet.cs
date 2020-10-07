using DoormatCore.Games;
using DoormatCore.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;

namespace DoormatCore.Sites
{
    public class WolfBet : BaseSite, iDice
    {
        string accesstoken = "";
        public bool ispd = false;
        DateTime lastupdate = new DateTime();
        HttpClient Client;
        HttpClientHandler ClientHandlr;        
        string URL = "https://wolf.bet";
        public WolfBet()
        {
            StaticLoginParams = new LoginParameter[] { new LoginParameter("API Key", false, true, false, false) };
            this.MaxRoll = 99.99m;
            this.SiteAbbreviation = "WB";
            this.SiteName = "Wolf.Bet";
            this.SiteURL = "https://wolf.bet?c=Seuntjie";
            this.Stats = new SiteStats();
            this.TipUsingName = true;
            this.AutoInvest = false;
            this.AutoWithdraw = false;
            this.CanChangeSeed = false;
            this.CanChat = false;
            this.CanGetSeed = false;
            this.CanRegister = false;
            this.CanSetClientSeed = false;
            this.CanTip = false;
            this.CanVerify = false;
            this.Currencies = new string[] { "btc", "eth", "ltc", "trx", "bch", "doge", "xrp" };
            SupportedGames = new Games.Games[] { Games.Games.Dice };
            this.Currency = 0;
            this.DiceBetURL = "https://bit-exo.com/{0}";
            this.Edge = 1;
        }


        public override void SetProxy(ProxyDetails ProxyInfo)
        {
            
        }

        protected override void _Disconnect()
        {
            this.ispd = false;
        }

        protected override void _Login(LoginParamValue[] LoginParams)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
      | SecurityProtocolType.Tls11
      | SecurityProtocolType.Tls12;
            ClientHandlr = new HttpClientHandler { UseCookies = true, AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip};
            Client = new HttpClient(ClientHandlr) { BaseAddress = new Uri(URL + "/api/v1/") };
            Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("gzip"));
            Client.DefaultRequestHeaders.AcceptEncoding.Add(new System.Net.Http.Headers.StringWithQualityHeaderValue("deflate"));
            Client.DefaultRequestHeaders.Add("UserAgent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.117 Safari/537.36");
            Client.DefaultRequestHeaders.Add("Origin", "https://wolf.bet");
            Client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            try
            {
                string Password = LoginParams.First(m => m.Param?.Name?.ToLower() == "api key").Value;
                if (Password != null)
                {
                    Client.DefaultRequestHeaders.Add("authorization", "Bearer " + Password);
                }
                string sEmitResponse = Client.GetStringAsync("user/balances").Result;
                try
                {
                    WolfBetProfile tmpProfile = json.JsonDeserialize<WolfBetProfile>(sEmitResponse);
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
                        sEmitResponse = Client.GetStringAsync("user/stats/bets").Result;
                        WolfBetStats tmpStats = json.JsonDeserialize<WolfBetStats>(sEmitResponse);
                        SetStats(tmpStats);
                        ispd = true;
                        lastupdate = DateTime.Now;
                        new Thread(new ThreadStart(GetBalanceThread)).Start();
                        this.callLoginFinished(true);
                        return;
                    }
                }
                catch (Exception e)
                {
                    Logger.DumpLog(e);
                    Logger.DumpLog(sEmitResponse, 2);
                    callError(sEmitResponse, false, ErrorType.Unknown);
                    callNotify("Error: " + sEmitResponse);
                }
            }
            catch (Exception e)
            {
                Logger.DumpLog(e.ToString(), -1);
            }
            this.callLoginFinished(false);
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
                    Logger.DumpLog(e);
                }
                Thread.Sleep(100);
            }
        }
        protected override void _UpdateStats()
        {
            string sEmitResponse = Client.GetStringAsync("user/balances").Result;
            WolfBetProfile tmpProfile = json.JsonDeserialize<WolfBetProfile>(sEmitResponse);
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
                sEmitResponse = Client.GetStringAsync("user/stats/bets").Result;
                WolfBetStats tmpStats = json.JsonDeserialize<WolfBetStats>(sEmitResponse);
                SetStats(tmpStats);

            }
        }

        public void PlaceDiceBet(PlaceDiceBet BetDetails)
        {
            throw new NotImplementedException();
        }
        void SetStats(WolfBetStats Stats)
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
            public string xrp { get; set; }
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
            public WBStat xrp { get; set; }
        }

        public class WolfBetStats
        {
            public Dice dice { get; set; }
        }

        public class WolfPlaceBet
        {
            public string currency { get; set; }
            public string game { get; set; }
            public string amount { get; set; }
            public string rule { get; set; }
            public string multiplier { get; set; }
            public string bet_value { get; set; }
        }
        public class WBBet
        {
            public string hash { get; set; }
            public int nonce { get; set; }
            public string user_seed { get; set; }
            public string currency { get; set; }
            public string amount { get; set; }
            public string profit { get; set; }
            public string multiplier { get; set; }
            public string bet_value { get; set; }
            public string result_value { get; set; }
            public string state { get; set; }
            public int published_at { get; set; }
            public string server_seed_hashed { get; set; }
            public User user { get; set; }
            public Game game { get; set; }
        }

        public class UserBalance
        {
            public decimal amount { get; set; }
            public string currency { get; set; }
            public string withdraw_fee { get; set; }
            public string withdraw_minimum_amount { get; set; }
            public bool payment_id_required { get; set; }
        }

        public class WolfBetResult
        {
            public WBBet bet { get; set; }
            public UserBalance userBalance { get; set; }
        }
    }
}
