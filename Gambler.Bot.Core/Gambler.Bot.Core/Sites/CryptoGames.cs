using Gambler.Bot.Common.Enums;
using Gambler.Bot.Common.Games;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Helpers;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites.Classes;
using Gambler.Bot.Core.SiteClient;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Gambler.Bot.Core.Sites
{
    public class CryptoGames : BaseSite, iDice
    {
        string accesstoken = "";

        public bool iscg = false;
        string username = "";
        long uid = 0;
        DateTime lastupdate = new DateTime();
        public static string[] sCurrencies = new string[] { "BTC", "Doge", "ETH",  "Bch", "PLAY", "LTC",  "ETC","USDC","USDT","SOL","BNB","POL","PEPE","SHIB","XRP" };
        string CurrenyHash = "";

        public DiceConfig DiceSettings { get; set; }

        public CryptoGames(ILogger logger) : base(logger)
        {
            StaticLoginParams = new LoginParameter[] { new LoginParameter("API Key", false, true, false, false) };
            //this.MaxRoll = 99.999m;
            this.SiteAbbreviation = "CG";
            this.SiteName = "CryptoGames";
            this.SiteURL = "https://www.crypto.games?i=KaSwpL1Bky";
            this.Mirrors.Add("https://www.crypto.games");
            AffiliateCode = "?i=KaSwpL1Bk";
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
            this.CanVerify = true;
            this.Currencies = sCurrencies;
            SupportedGames = new Games[] { Games.Dice };
            CurrentCurrency ="btc";
            this.DiceBetURL = "https://www.crypto.games/fair.aspx?coin=BTC&type=3&id={0}";
            //this.Edge = 0.8m;
            NonceBased = true;
            DiceSettings = new DiceConfig() { Edge = 0.8m, MaxRoll = 99.99m };
        }


        public override void SetProxy(ProxyDetails ProxyInfo)
        {
            throw new NotImplementedException();
        }

        protected override void _Disconnect()
        {
            iscg = false;
        }

        protected override async Task<bool> _Login(LoginParamValue[] LoginParams)
        {
            Client = await CreateClientAsync(new SiteClientOptions { BaseAddress = new Uri($"{URLInUse.Replace("www","api")}/v1/") });
            try
            {
                accesstoken = LoginParams[0].Value;

                string sEmitResponse = await Client.GetStringAsync("user/" + CurrentCurrency + "/" + accesstoken);
                cgUser tmpBal = JsonSerializer.Deserialize<cgUser>(sEmitResponse);
                sEmitResponse = await Client.GetStringAsync("nextseed/" + CurrentCurrency + "/" + accesstoken);
                cgNextSeed tmpSeed = JsonSerializer.Deserialize<cgNextSeed>(sEmitResponse);
                CurrenyHash = tmpSeed.NextServerSeedHash;
                Stats.Balance = tmpBal.Balance;
                Stats.Wagered = tmpBal.Wagered;
                Stats.Profit = tmpBal.Profit;
                Stats.Bets = tmpBal.TotalBets;
                //Get stats
                //assign vals to stats
                
                Thread t = new Thread(GetBalanceThread);
                iscg = true;
                t.Start();

                callLoginFinished(true);
                return true;
            }
            catch (AggregateException e)
            {
                _logger?.LogError(e.ToString());
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
                while (iscg)
                {
                    if (accesstoken != "" && ((DateTime.Now - lastupdate).TotalSeconds > 60 || ForceUpdateStats))
                    {
                        lastupdate = DateTime.Now;
                        UpdateStats();
                    }
                    Thread.Sleep(1000);
                }
            }
            catch { }
        }
        protected override async Task<SiteStats> _UpdateStats()
        {
            try
            {

                string sEmitResponse = await Client.GetStringAsync("user/" + CurrentCurrency + "/" + accesstoken);
                cgUser tmpBal = JsonSerializer.Deserialize<cgUser>(sEmitResponse);
                Stats.Balance = tmpBal.Balance;
                Stats.Wagered = tmpBal.Wagered;
                Stats.Profit = tmpBal.Profit;
                Stats.Bets = tmpBal.TotalBets;
                return Stats;
            }
            catch { }
            return null;
        }

        public async Task<DiceBet> PlaceDiceBet(PlaceDiceBet BetDetails)
        {

            string Clients = GenerateNewClientSeed();
            decimal payout = decimal.Parse(((100m - DiceSettings.Edge) / (decimal)BetDetails.Chance).ToString("0.0000"));
            cgPlaceBet tmpPlaceBet = new cgPlaceBet() { Bet = BetDetails.Amount, ClientSeed = Clients, UnderOver = BetDetails.High, Payout = (decimal)payout };

            string post = JsonSerializer.Serialize<cgPlaceBet>(tmpPlaceBet);
            HttpContent cont = new StringContent(post);
            cont.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            /*List<KeyValuePair<string, string>> pairs = new List<KeyValuePair<string, string>>();
            pairs.Add(new KeyValuePair<string, string>("value", post));
            //pairs.Add(new KeyValuePair<string, string>("affiliate", "seuntjie"));
            FormUrlEncodedContent cont = new FormUrlEncodedContent(pairs);*/

            try
            {

                var response = await Client.PostAsync("placebet/" + CurrentCurrency + "/" + accesstoken, cont);
                string sEmitResponse = await response.Content.ReadAsStringAsync();
                cgGetBet Response = JsonSerializer.Deserialize<cgGetBet>(sEmitResponse);
                if (Response.Message != "" && Response.Message != null)
                {
                    ErrorType ertype = ErrorType.Unknown;
                    switch (Response.Message)
                    {
                        case "Invalid payout. Should be larger than 1.02": ertype = ErrorType.InvalidBet; break;
                        case "Bet was larger than your balance.": ertype = ErrorType.BalanceTooLow; break;
                        case "Invalid bet amount": ertype = ErrorType.BetTooLow; break;
                    }
                    if (ertype == ErrorType.Unknown)
                    {
                        if (Response.Message.StartsWith("Your bet amount was to large. Max win amount is set to"))
                            ertype = ErrorType.InvalidBet;
                    }
                    callError(Response.Message,true, ertype);
                    return null;
                }
                DiceBet bet = new DiceBet()
                {
                    Guid = BetDetails.GUID,
                    TotalAmount = (decimal)BetDetails.Amount,
                    Profit = (decimal)Response.Profit,
                    Roll = (decimal)Response.Roll,
                    Chance = decimal.Parse(Response.Target.Substring(3), System.Globalization.NumberFormatInfo.InvariantInfo),
                    DateValue = DateTime.Now,
                    ClientSeed = Clients,
                    Currency = CurrentCurrency,
                    BetID = Response.BetId.ToString(),
                    High = Response.Target.Contains(">"),
                    ServerHash = CurrenyHash,
                    Nonce = -1,
                    ServerSeed = Response.ServerSeed
                };
                if (bet.High)
                    bet.Chance = (decimal)DiceSettings.MaxRoll - bet.Chance;
                this.CurrenyHash = Response.NextServerSeedHash;
                bool Win = (((bool)bet.High ? (decimal)bet.Roll > (decimal)DiceSettings.MaxRoll - (decimal)(bet.Chance) : (decimal)bet.Roll < (decimal)(bet.Chance)));
                if (Win)
                    Stats.Wins++;
                else
                    Stats.Losses++;
                Stats.Bets++;
                Stats.Wagered += bet.TotalAmount;
                Stats.Balance += Response.Profit;
                Stats.Profit += Response.Profit;
                callBetFinished(bet);
                return bet;
            }
            catch (Exception e)
            { 
                _logger?.LogError(e.ToString());
            }
            return null;
        }

        protected override IGameResult _GetLucky(string ServerSeed, string ClientSeed, int Nonce, Games Game)
        {
            string hex = Hash.SHA512(ServerSeed + ClientSeed);
            int charstouse = 5;

            if (Game == Games.Dice)
            {

                for (int i = 0; i < hex.Length; i += charstouse)
                {

                    string s = hex.ToString().Substring(i, charstouse);

                    decimal lucky = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                    if (lucky < 1000000)
                    {
                        //return lucky / 10000;
                        string tmps = lucky.ToString("000000").Substring(lucky.ToString("000000").Length - 5);
                        return new DiceResult { Roll = decimal.Parse(tmps) / 1000.0m };
                    }
                }
            }
            return null;
        }

        protected override Task<bool> _BrowserLogin()
        {
            throw new NotImplementedException();
        }

        public class cgBalance
        {
            public decimal Balance { get; set; }
        }
        public class cgPlaceBet
        {
            public decimal Bet { get; set; }
            public decimal Payout { get; set; }
            public bool UnderOver { get; set; }
            public string ClientSeed { get; set; }
        }
        public class cgGetBet
        {
            public long BetId { get; set; }
            public decimal Roll { get; set; }
            public string ClientSeed { get; set; }
            public string Target { get; set; }
            public decimal Profit { get; set; }
            public string NextServerSeedHash { get; set; }
            public string ServerSeed { get; set; }
            public string Message { get; set; }
        }
        public class cgUser
        {
            public string Nickname { get; set; }
            public decimal Balance { get; set; }
            public string Coin { get; set; }
            public int TotalBets { get; set; }
            public decimal Profit { get; set; }
            public decimal Wagered { get; set; }
        }
        public class cgNextSeed
        {
            public string NextServerSeedHash { get; set; }
        }


    }
}
