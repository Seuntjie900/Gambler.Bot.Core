using Gambler.Bot.Common.Games;
using Gambler.Bot.Common.Games.Crash;
using Gambler.Bot.Common.Games.Dice;
using Gambler.Bot.Common.Games.Limbo;
using Gambler.Bot.Common.Helpers;
using Gambler.Bot.Core.Helpers;
using Gambler.Bot.Core.Sites.Classes;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WebSocket4Net;

namespace Gambler.Bot.Core.Sites
{
    public class BCHGames : BaseSite, iDice, iLimbo, iCrash
    {
        WebSocket Sock;
        public DiceConfig DiceSettings { get; set; }
        public LimboConfig LimboSettings { get; set; }
        public CrashConfig CrashSettings { get; set; }

        public BCHGames(ILogger logger) : base(logger)
        {
            StaticLoginParams = new LoginParameter[] { new LoginParameter("API Key", true, true, false, true),};
            IsEnabled = false;
            //this.MaxRoll = 99.99m;
            this.SiteAbbreviation = "BCH";
            this.SiteName = "BCHGames";
            this.SiteURL = "https://bch.games/play/Seuntjie";
            this.Stats = new SiteStats();
            this.TipUsingName = true;
            this.AutoInvest = false;
            this.AutoWithdraw = true;
            this.CanChangeSeed = true;
            this.CanChat = false;
            this.CanGetSeed = true;
            this.CanRegister = false;
            this.CanSetClientSeed = true;
            this.CanTip = true;
            this.CanVerify = true;
            this.Currencies = new string[] { "bch" };

            
            SupportedGames = new Games[] { Games.Dice };
            CurrentCurrency = "bch";
            this.DiceBetURL = "https://bch.games/bet/{0}";
            //this.Edge = 1;
            DiceSettings = new DiceConfig() { Edge = 2, MaxRoll = 99.99m };
            LimboSettings = new LimboConfig() { Edge = 2, MaxPayout = 1000000 };
            CrashSettings = new CrashConfig() { Edge = 1, IsMultiplayer = true };
            NonceBased = true;
            this.Mirrors.Add("https://bch.games");
            AffiliateCode = "/play/Seuntjie";
            SupportsBrowserLogin = true;
            SupportsNormalLogin = false;
        }

        public Task<DiceBet> PlaceDiceBet(PlaceDiceBet BetDetails)
        {
            throw new NotImplementedException();
        }

        public Task<LimboBet> PlaceLimboBet(PlaceLimboBet bet)
        {
            throw new NotImplementedException();
        }

        public override void SetProxy(ProxyDetails ProxyInfo)
        {
            throw new NotImplementedException();
        }

        protected override void _Disconnect()
        {
            throw new NotImplementedException();
        }

        protected override Task<bool> _Login(LoginParamValue[] LoginParams)
        {
            throw new NotImplementedException();
        }

        protected override Task<SiteStats> _UpdateStats()
        {
            throw new NotImplementedException();
        }

        public Task<CrashBet> PlaceCrashBet(PlaceCrashBet BetDetails)
        {
            throw new NotImplementedException();
        }

        protected override IGameResult _GetLucky(string ServerSeed, string ClientSeed, int Nonce, Games Game)
        {
            throw new NotImplementedException();
        }
        ConcurrentDictionary<Guid,string> messages = new ConcurrentDictionary<Guid, string>();
        protected override async Task<bool> _BrowserLogin()
        {
            try
            {
                var cookies = await CallBypassRequired(URLInUse + AffiliateCode, ["cf_clearance="], false,"/sounds/bet3.mp3", "localStorage.getItem('auth_token_v1')");
                string authtoken = cookies.scriptResponse.Replace("\"", "").Replace("\\","");
                string cfuid = "";
                if (string.IsNullOrEmpty(authtoken))
                {
                    callLoginFinished(false);
                    return false;
                }
                else
                {
                    foreach (Cookie c in cookies.Cookies.GetCookies(new Uri($"{URLInUse}")))
                    {
                        if (c.Name == "cf_clearance")
                        {
                            cfuid = c.Value;
                            break;
                        }
                    }
                    List<KeyValuePair<string, string>> wscookies = new List<KeyValuePair<string, string>>();
                    wscookies.Add(new KeyValuePair<string, string>("cf_clearance", cfuid));

                    await OpenSocket(wscookies, cookies.Headers.ToList(), cookies.UserAgent);
                    if (Sock.State == WebSocketState.Open)
                    {

                    }
                    else
                    {
                        callLoginFinished(false);
                        return false;
                    }
                    //open websocket here
                    Guid currentGuid = Guid.NewGuid();
                    GraphqlRequestPayload LoginReq = new GraphqlRequestPayload
                    {
                        query =
                        $"{{\"id\":\"{currentGuid.ToString()}\",\"type\":\"subscribe\",\"payload\":{{\"query\":\"{{authenticate(authToken:\"{authtoken}\"){{_id username authToken email twoFactorEnabled role countryBlock __typename}}}}\",\"variables\":{{ }}}}}}",
                        operationName = "authenticate"
                    };
                    messages.AddOrUpdate(currentGuid, "", (key, oldValue) => "");
                    Sock.Send(JsonSerializer.Serialize(LoginReq));
                    await Task.Run(() =>
                    {
                        while (string.IsNullOrWhiteSpace(messages[currentGuid]))
                        {
                            Thread.Sleep(1);
                        }
                    });
                    
                    messages.Remove(currentGuid, out string response);
                    //get user details from response
                    callLoginFinished(true);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex.ToString());
                callLoginFinished(false);
                return false;   
            }
        }

        async Task OpenSocket(List<KeyValuePair<string,string>> wscookies, List<KeyValuePair<string, string>> headers, string UserAgent)
        {
            Sock = new WebSocket($"{URLInUse.Replace("https", "wss")}/api/graphql",
                   null,
                   wscookies,
                   headers,
                   UserAgent,
                   $"{URLInUse}"/*,
                    WebSocketVersion.None*/);
            Sock.Opened += Sock_Opened; ;
            Sock.Error += Sock_Error;
            Sock.MessageReceived += Sock_MessageReceived;
            Sock.Closed += Sock_Closed;

            Sock.Open();
            await Task.Run(() =>
            {
                while (Sock.State == WebSocketState.Connecting)
                {
                    Thread.Sleep(300);
                    //Response = Client.GetStringAsync("https://gs.ethercrash.io/socket.io/?EIO=3&sid=" + io + "&transport=polling&t=" + json.CurrentDate()).Result;

                }
            });
            
        }

        private void Sock_Closed(object sender, EventArgs e)
        {
            _logger.LogDebug("closed");
        }

        private void Sock_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            _logger.LogInformation(e.Message);
        }

        private void Sock_Error(object sender, SuperSocket.ClientEngine.ErrorEventArgs e)
        {
            _logger.LogError(e.Exception.ToString());
        }

        private void Sock_Opened(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }
    }

    public class GraphqlRequestPayload
    {
        public string operationName { get; set; }

        public string query { get; set; }

        public object variables { get; set; }

        public string identifier { get; set; }
    }
}

