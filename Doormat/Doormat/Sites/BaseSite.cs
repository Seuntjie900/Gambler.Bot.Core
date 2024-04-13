﻿using DoormatCore.Games;
using DoormatCore.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DoormatCore.Sites
{
    public abstract class BaseSite
    {

        public List<SiteAction> ActiveActions { get; set; } = new List<SiteAction>();


        public LoginParameter[] StaticLoginParams = new LoginParameter[] { new LoginParameter("Username", false, true, false, false), new LoginParameter("Password", true, true, false, true), new LoginParameter("Two Factor Code", false, false, true, true,true) };
        
        public LoginParameter[] LoginParams { get { return StaticLoginParams; } }
        #region Properties
        /// <summary>
        /// Specifies wether the user can register a new account on the website using the bot.
        /// </summary>
        public bool CanRegister { get; protected set; }

        /// <summary>
        /// Specifies wether the bot is able to withdraw from the specified site.
        /// </summary>
        public bool AutoWithdraw { get; protected set; }

        /// <summary>
        /// Specifies whether the bot can invest coins into the site, if the site supports the feature.
        /// </summary>
        public bool AutoInvest { get; protected set; }

        /// <summary>
        /// Specifies whether the bot can reset the seed for the player.
        /// </summary>
        public bool CanChangeSeed { get; protected set; }

        /// <summary>
        /// Specifies Whether the bot can set the client seed for the current or next seed.
        /// </summary>
        public bool CanSetClientSeed { get; protected set; }

        /// <summary>
        /// Specifies whether the bot can send a tip to another player, if the site supports the feature.
        /// </summary>
        public bool CanTip { get; protected set; }

        /// <summary>
        /// Specify whether tipping on the site uses a username (true, string) or a userID (false, int)
        /// </summary>
        public bool TipUsingName { get; protected set; }

        /// <summary>
        /// Specify whether the bot can fetch the server seed for a specific bet
        /// </summary>
        public bool CanGetSeed { get; protected set; }

        /// <summary>
        /// True if the bot is busy getting the server seed for a specific bet
        /// </summary>
        public bool GettingSeed { get; protected set; }

        /// <summary>
        /// Specifies whether the roll verifier for the site is implemented and working.
        /// </summary>
        public bool CanVerify { get; protected set; }

        /// <summary>
        /// The Reflink URL of the site
        /// </summary>
        public string SiteURL { get; protected set; }

        /// <summary>
        /// The Name of the site
        /// </summary>
        public string SiteName { get; protected set; }

        /// <summary>
        /// The URL where more details for a bet can be seen, using string.format formatting, where {0} is the betID.
        /// </summary>
        public string DiceBetURL { get; protected set; }

        /// <summary>
        /// The index of the list of supported currencies for the current currency
        /// </summary>
        public int Currency { get; set; }

        /// <summary>
        /// The name/abbreviation of the currency currently in use
        /// </summary>
        public string CurrentCurrency { get { return Currencies[Currency]; } }

        /// <summary>
        /// The maximum roll allowed at the site. Usually 99.99. Used to determine whether the roll is a win
        /// </summary>
        public decimal MaxRoll { get; protected set; }

        /// <summary>
        /// The house edge for the site. Used to determine payout and profit for bets and simulations
        /// </summary>
        public decimal Edge { get; protected set; }

        /// <summary>
        /// List of currencies supported by the site
        /// </summary>
        public string[] Currencies { get; protected set; }

        /// <summary>
        /// Indicates whether the bot can connect to and use the chat on the site
        /// </summary>
        public bool CanChat { get; protected set; }

        /// <summary>
        /// Site Statistics about the user 
        /// </summary>
        public SiteStats Stats { get; protected set; }

        /// <summary>
        /// Indicates whether the user is logged in to the site
        /// </summary>
        public bool LoggedIn { get; set; }

        SiteDetails siteDetails = null;

        /// <summary>
        /// Provides information to the user/implementer about the site, such as features available, edge, max roll etc.
        /// </summary>
        public SiteDetails SiteDetails {
            get
            {
                if (siteDetails==null)
                {
                    siteDetails = new SiteDetails {
                         caninvest=AutoInvest,
                          canresetseed=CanChangeSeed,
                           cantip=CanTip,
                            canwithdraw=AutoWithdraw,
                             edge=Edge,
                              maxroll=MaxRoll,
                               name=SiteName,
                                siteurl=SiteURL,
                                 tipusingname=TipUsingName,
                                  Currencies=CopyHelper.CreateCopy(Currencies.GetType(), Currencies) as string[],
                    };
                }
                return siteDetails;
            }
        }

        /// <summary>
        /// Abbreviation for the site to be used for display purposes
        /// </summary>
        public string SiteAbbreviation { get; set; }

        /// <summary>
        /// Cryptographically secure random number generator with extension functions for random strings and numbers
        /// </summary>
        public Helpers.Random R { get; internal set; } = new Helpers.Random();
        #endregion

        /// <summary>
        /// Forces the bot to update stats on the next timed updated
        /// </summary>
        public bool ForceUpdateStats { get; protected set; }

        /// <summary>
        /// The last time the site statistics were updated
        /// </summary>
        public DateTime LastStats { get; set; } = DateTime.Now;

        /// <summary>
        /// List of supported games for the site
        /// </summary>
        public Games.Games[] SupportedGames { get; set; } = new Games.Games[] { Games.Games.Dice };

        #region Required Methods

        /// <summary>
        /// Interface with site to handle login.
        /// </summary>
        /// <param name="LoginParams">The login details required for logging in. Typically username, passwordm, 2fa in that order, or API Key</param>
        protected abstract Task<bool> _Login(LoginParamValue[] LoginParams);

        /// <summary>
        /// Logs the user into the site if correct details were provided
        /// </summary>
        /// <param name="LoginParams">The login details required for logging in. Typically username, passwordm, 2fa in that order, or API Key</param>
        public async Task<bool> LogIn(LoginParamValue[] LoginParams)
        {
            bool success = false;
            await Task.Run(async ()=> { success = await _Login(LoginParams); });
            if (success)
            {
                await UpdateStats();
            }
            return success;
            
        }

       
        /// <summary>
        /// Interface with site to disconnect and dispose of applicable objects
        /// </summary>
        protected abstract void _Disconnect();

        /// <summary>
        /// Disconnect from the site, if connected
        /// </summary>
        public void Disconnect()
        {
            LoggedIn = false;
            _Disconnect();
        }

        /// <summary>
        /// Set the proxy for the connection to the site
        /// </summary>
        /// <param name="ProxyInfo"></param>
        public abstract void SetProxy(Helpers.ProxyDetails ProxyInfo);

        /// <summary>
        /// Update the site statistics for whatever reason.
        /// </summary>
        public async Task<SiteStats> UpdateStats()
        {
            ForceUpdateStats = false;
            SiteStats stats = null;
            await Task.Run(async () => stats= await _UpdateStats());

            StatsUpdated?.Invoke(this, new StatsUpdatedEventArgs(this.Stats));
            return stats;
        }

        /// <summary>
        /// Interface with the site to get the latest user stats
        /// </summary>
        protected abstract Task<SiteStats> _UpdateStats();
        #endregion

        #region Betting methods
        public async Task <Bet> PlaceBet(PlaceBet BetDetails)
        {
            Bet result = null;
            await Task.Run(async () =>
            {
                if (BetDetails is PlaceDiceBet dicebet && this is iDice DiceSite)
                {
                    if (dicebet.Amount < 0)
                    {
                        callError("Bet cannot be < 0.", false, ErrorType.BetTooLow);
                        return;
                    }
                    else if (dicebet.Chance <= 0)
                    {
                        callError("Chance to win must be > 0", false, ErrorType.InvalidBet);
                        return;
                    }
                    callNotify($"Placing Dice Bet: {dicebet.Amount:0.00######} as {dicebet.Chance:0.0000}% chance to win, {(dicebet.High ? "High" : "Low")}");
                    result = await DiceSite.PlaceDiceBet(dicebet);
                }
                if (BetDetails is PlaceCrashBet crashBet && this is iCrash crashsite)
                {
                    if (crashBet.TotalAmount < 0)
                    {
                        callError("Bet cannot be < 0.", false, ErrorType.BetTooLow);
                        return;
                    }
                    result = await crashsite.PlaceCrashBet(BetDetails as PlaceCrashBet);
                }
                if (BetDetails is PlacePlinkoBet plinkoBet && this is iPlinko PlinkoSite)
                {
                    if (plinkoBet.TotalAmount < 0)
                    {
                        callError("Bet cannot be < 0.", false, ErrorType.BetTooLow);
                        return;
                    }
                    result = await PlinkoSite.PlacePlinkoBet(BetDetails as PlacePlinkoBet);
                }
                if (BetDetails is PlaceRouletteBet rouletteBet && this is iRoulette RouletteSite)
                {
                    if (rouletteBet.TotalAmount < 0)
                    {
                        callError("Bet cannot be < 0.", false, ErrorType.BetTooLow);
                        return;
                    }
                    result = await RouletteSite.PlaceRouletteBet(BetDetails as PlaceRouletteBet);
                }
            });
            return result;
        } 
        
      
        #endregion

        #region Extention Methods
        public async Task<SeedDetails> ResetSeed(string ClientSeed = null)
        {
            SeedDetails seedDetails = null;
            if (CanChangeSeed)
            {
                ActiveActions.Add(SiteAction.ResetSeed);
                callNotify("Resetting seed.");
                await Task.Run(async () => 
                {
                    seedDetails = await _ResetSeed();                    
                });
                if (CanSetClientSeed)
                {
                    string client = await SetClientSeed(ClientSeed);
                    if (!string.IsNullOrWhiteSpace(client))
                        seedDetails.ClientSeed = client;
                }
            }
            else
                callError("Reset Seed not allowed!", false, ErrorType.NotImplemented);
            return seedDetails;
        }
        protected virtual async Task<SeedDetails> _ResetSeed() 
        {
            callError("Reset Seed not implemented", false, ErrorType.NotImplemented); 
            return null; 
        }

        public async Task<string> SetClientSeed(string ClientSeed)
        {
            string result = null;
            if (CanSetClientSeed)
            {
                await Task.Run(async () =>
                {
                    result = await _SetClientSeed(ClientSeed);
                });
            }
            else
                callError("Setting Client Seed not allowed!", false, ErrorType.NotImplemented);
            return null;
        }
        protected virtual async Task<string> _SetClientSeed(string ClientSeed) { return null; }

        public async Task<bool> Invest(decimal Amount)
        {
            bool success = false;
            if (AutoInvest)
            {
                ActiveActions.Add(SiteAction.Invest);
                callNotify($"Investing {Amount} {CurrentCurrency}");
                await Task.Run(async () =>
                {                    
                    success = await _Invest(Amount);
                });
                
                await UpdateStats();
            }
            else
                callError("Investing not allowed!", false, ErrorType.NotImplemented);
            return success;
        }
        protected virtual async Task<bool> _Invest(decimal Amount) { return false; }

        public async Task<bool> Donate(decimal Amount)
        {
            bool success = false;
            //ActiveActions.Add(TriggerAction.Donate);
            if (AutoWithdraw || CanTip)
            {
                await Task.Run(async () =>
                {
                    success = await _Donate(Amount);
                });

                callNotify($"Donating {Amount} {CurrentCurrency}");
                
                await UpdateStats();
            }
            else
                callError("Donations not Implemented!", false, ErrorType.NotImplemented);
            return success;
        }
        protected virtual async Task<bool> _Donate(decimal Amount) { return false; }

        public async Task<bool> Withdraw(string Address, decimal Amount)
        {
            bool success  = false;
            if (AutoWithdraw)
            {
                ActiveActions.Add(SiteAction.Withdraw);
                callNotify($"Withdrawing {Amount} {CurrentCurrency} to {Address}");
                await Task.Run(async () =>
                {
                    success = await _Withdraw(Address, Amount);
                });
                
                await UpdateStats();
            }
            else
                callError("Withdrawing not allowed!", false, ErrorType.NotImplemented);
            return success;
        }
        protected virtual async Task<bool> _Withdraw(string Address, decimal Amount) 
        { 
            callError("Withdrawing not implemented", false, ErrorType.Withdrawal);
            return false;
        }

        public async Task<bool> Register(string Username, string Password)
        {
            bool Success = false;
            if (CanRegister)
            {

                await Task.Run(async () =>
                {
                    Success = await _Register(Username, Password);
                });
                
                await UpdateStats();
                RegisterFinished?.Invoke(this, new GenericEventArgs { Success = Success });

            }
            else
                callError("Registering not allowed!", false, ErrorType.NotImplemented);
            return Success;

        }
        protected virtual async Task<bool> _Register(string Username, string Password) { return false; }

        public decimal GetLucky(string Hash, string ServerSeed, string ClientSeed, int Nonce)
        {
            return _GetLucky(Hash, ServerSeed, ClientSeed, Nonce);
        }
        protected virtual decimal _GetLucky(string Hash, string ServerSeed, string ClientSeed, int Nonce)
        {
            HMACSHA512 betgenerator = new HMACSHA512();

            int charstouse = 5;
            List<byte> serverb = new List<byte>();

            for (int i = 0; i < ServerSeed.Length; i++)
            {
                serverb.Add(Convert.ToByte(ServerSeed[i]));
            }

            betgenerator.Key = serverb.ToArray();

            List<byte> buffer = new List<byte>();
            string msg = /*nonce.ToString() + ":" + */ClientSeed + ":" + Nonce.ToString();
            foreach (char c in msg)
            {
                buffer.Add(Convert.ToByte(c));
            }

            byte[] hash = betgenerator.ComputeHash(buffer.ToArray());

            StringBuilder hex = new StringBuilder(hash.Length * 2);
            foreach (byte b in hash)
                hex.AppendFormat("{0:x2}", b);


            for (int i = 0; i < hex.Length; i += charstouse)
            {

                string s = hex.ToString().Substring(i, charstouse);

                decimal lucky = int.Parse(s, System.Globalization.NumberStyles.HexNumber);
                if (lucky < 1000000)
                    return (decimal)(lucky / 10000m);
            }
            return 0;
        }

        public virtual string GetHash(string ServerSeed)
        {
            return Hash.SHA256(ServerSeed);
        }
        public virtual string GenerateNewClientSeed()
        {
            string ClientSeed = R.Next(0, int.MaxValue).ToString();
            return ClientSeed;
        }

        public async Task<string> GetSeed(string BetID)
        {
            string seed = null;
            if (CanGetSeed)
            {
               await Task.Run(async () =>
                {
                    seed = await _GetSeed(BetID);
                    callNotify($"Got seed for bet {BetID}");
                    callGameMessage(seed);
                });
            }
            else
            {

                callError("Getting server seed not allowed!", false, ErrorType.NotImplemented);
                return "-1";
            }
            return seed;
        }
        protected virtual async Task<string> _GetSeed(string BetID) { return "-1"; }

        public async Task<bool> SendTip(string Username, decimal Amount)
        {
            bool success = false;
            if (CanTip)
            {
                ActiveActions.Add(SiteAction.Tip);
                callNotify($"Tipping {Amount} {CurrentCurrency} to {Username}");
                await Task.Run(async () =>
                {
                    success = await _SendTip(Username, Amount);
                });
            }
            else
                callError("Tipping not allowed!", false, ErrorType.NotImplemented);
            return success;
        }
        protected virtual async Task<bool> _SendTip(string Username, decimal Amount) { return false; }

        public void SendChat(string Message)
        {
            if (CanChat)
            {
                _SendChat(Message);
            }
            else
                callError("Chatting not allowed!", false, ErrorType.NotImplemented);
        }
        protected virtual void _SendChat(string Message) { }

        public virtual int _TimeToBet(PlaceBet BetDetails)
        {
            return -1;
        }

        public int TimeToBet(PlaceBet BetDetails)
        {
            return _TimeToBet(BetDetails);
        }

        private bool nonceBased;

        public bool NonceBased
        {
            get { return nonceBased; }
            set { nonceBased = value; }
        }

        #endregion

        #region Events
        public delegate void dStatsUpdated(object sender, StatsUpdatedEventArgs e);
        public delegate void dBetFinished(object sender, BetFinisedEventArgs e);
        public delegate void dLoginFinished(object sender, LoginFinishedEventArgs e);
        public delegate void dRegisterFinished(object sender, GenericEventArgs e);
        public delegate void dError(object sender, ErrorEventArgs e);
        public delegate void dNotify(object sender, GenericEventArgs e);
        public delegate void dGameMessage(object sender, GenericEventArgs e);
        public delegate void dAction(object sender, GenericEventArgs e);
        public delegate void dChat(object sender, GenericEventArgs e);
        
        public event dStatsUpdated StatsUpdated;
        public event dBetFinished BetFinished;
        public event dLoginFinished LoginFinished;
        public event dRegisterFinished RegisterFinished;
        public event dError Error;
        public event dNotify Notify;
        public event dAction Action;
        public event dChat ChatReceived;
        public event dAction OnWithdrawalFinished;
        public event dAction OnTipFinished;
        public event dAction OnResetSeedFinished;
        public event dAction OnDonationFinished;
        public event dAction OnInvestFinished;
        public event dGameMessage OnGameMessage;
        public event EventHandler<BypassRequiredArgs> OnBrowserBypassRequired;

        protected void callStatsUpdated(SiteStats Stats)
        {
            if (StatsUpdated != null)
            {
                StatsUpdated(this, new StatsUpdatedEventArgs(Stats));
            }
        }
        protected void callBetFinished(Bet NewBet)
        {
            if (NewBet is DiceBet dicebet)
            {
                dicebet.IsWin = dicebet.GetWin(this);
                dicebet.CalculateWinnableType(this);                
            }
            if (BetFinished != null)
            {
                BetFinished(this, new BetFinisedEventArgs(NewBet));
            }
        }
        protected void callLoginFinished(bool Success)
        {
            if (LoginFinished != null)
            {
                LoggedIn = Success;
                LoginFinished(this, new LoginFinishedEventArgs(Success, this.Stats));
            }
        }
        protected void callRegisterFinished(bool Success)
        {

            RegisterFinished?.Invoke(this, new GenericEventArgs { Success = Success });

        }
        protected void callError(string Message, bool Fatal, ErrorType type)
        {
            if (Error != null)
            {
                Error(this, new ErrorEventArgs { Message = Message, Fatal = Fatal, Type= type });
            }
        }
        protected void callNotify(string Message)
        {
            if (Notify != null)
            {
                Notify(this, new GenericEventArgs { Message = Message });
            }
        }
        protected void callGameMessage(string Message)
        {            
            OnGameMessage?.Invoke(this, new GenericEventArgs { Message = Message });            
        }
        protected void callAction(string CurrentAction)
        {
            if (Action != null)
            {
                Action(this, new GenericEventArgs { Message = CurrentAction });
            }
        }
        protected void callChatReceived(string Message)
        {
            if (ChatReceived != null)
            {
                ChatReceived(this, new GenericEventArgs { Message = Message });
            }
        }
        protected void callWithdrawalFinished(bool Success, string Message)
        {
            if (ActiveActions.Contains(SiteAction.Withdraw))
                ActiveActions.Remove(SiteAction.Withdraw);
            ForceUpdateStats = true;
            OnWithdrawalFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }
        protected void callTipFinished(bool Success, string Message)
        {
            if (ActiveActions.Contains(SiteAction.Tip))
                ActiveActions.Remove(SiteAction.Tip);
            ForceUpdateStats = true;
            OnTipFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }
        protected void callResetSeedFinished(bool Success, string Message)
        {
            if (ActiveActions.Contains(SiteAction.ResetSeed))
                ActiveActions.Remove(SiteAction.ResetSeed);
            OnResetSeedFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }
        protected void callDonationFinished(bool Success, string Message)
        {   
            OnWithdrawalFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }
        protected void callInvestFinished(bool Success, string Message)
        {
            if (ActiveActions.Contains(SiteAction.Invest))
                ActiveActions.Remove(SiteAction.Invest);
            ForceUpdateStats = true;
            OnInvestFinished?.Invoke(this, new GenericEventArgs { Success = Success, Message = Message });
        }
        protected BrowserConfig CallBypassRequired(string URL)
        {
            var args = new BypassRequiredArgs { URL = URL };
            OnBrowserBypassRequired?.Invoke(this, args);
            
            return args.Config;
        }
        #endregion
      
       
    }
    public class LoginParameter
    {
        public LoginParameter(string Name, bool Masked, bool Required, bool ClearafterEnter, bool Clearafterlogin, bool ismfa = false)
        {
            this.Name = Name;
            this.Masked = Masked;
            this.Required = Required;
            this.ClearAfterEnter = ClearafterEnter;
            this.ClearAfterLogin = Clearafterlogin;
            this.IsMFA = IsMFA;
        }

        public LoginParameter()
        {
        }

        public string Name { get; set; }
        public bool Masked { get; set; }
        public bool Required { get; set; }
        public bool ClearAfterEnter { get; set; }
        public bool ClearAfterLogin { get; set; }
        public bool IsMFA { get; set; }
    }

    public class LoginParamValue
    {

        public int ParameterId { get; set; }
        public LoginParameter Param { get; set; }
        public string Value { get; set; }
    }
    public enum ErrorType
    {
        InvalidBet,
        BalanceTooLow,
        BetTooLow,
        ResetSeed,
        Withdrawal,
        Tip,
        NotImplemented,
        Other,
        BetMismatch,
        Unknown
    }

    public class ErrorEventArgs: EventArgs
    {
        public bool Fatal { get; set; }
        public ErrorType Type { get; set; }
        public string Message { get; set; }
        public bool Handled { get; set; }
    }

    public class StatsUpdatedEventArgs : EventArgs
    {
        public SiteStats NewStats { get; set; }
        public StatsUpdatedEventArgs(SiteStats Stats)
        {
            this.NewStats = Stats;
        }
    }
    public class BetFinisedEventArgs : EventArgs
    {
        public Bet NewBet { get; set; }
        public BetFinisedEventArgs(Bet Bet)
        {
            NewBet = Bet;
        }
    }
    public class LoginFinishedEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public SiteStats Stats { get; set; }
        public LoginFinishedEventArgs(bool Success, SiteStats Stats)
        {
            this.Success = Success;
            this.Stats = Stats;
        }
    }
    public class GenericEventArgs : EventArgs
    {
        public string Message { get; set; }
        public bool Success { get; set; }
        public bool Fatal { get; set; }

    }

    public class BypassRequiredArgs:EventArgs
    {
        public string URL { get; set; }
        public BrowserConfig Config { get; set; }
    }
    
    public class SiteStats
    {
        public Currency Currency { get; set; }
        public decimal Balance { get; set; }        
        public Games.Games Game { get; set; }
        public decimal Wagered { get; set; }
        public decimal Profit { get; set; }
        public long Bets { get; set; }
        public long Wins { get; set; }
        public long Losses { get; set; }
    }
    public class Currency 
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public byte[] Icon { get; set; }
    }
    
    public class SiteDetails
    {
        public string name { get; set; }
        public decimal edge { get; set; }
        public decimal maxroll { get; set; }
        public bool cantip { get; set; }
        public bool tipusingname { get; set; }
        public bool canwithdraw { get; set; }
        public bool canresetseed { get; set; }
        public bool caninvest { get; set; }
        public string siteurl { get; set; }
        public string[] Currencies { get; set; }
        public string[] Games { get; set; }

    }

    public class SeedDetails
    {
        public string ClientSeed { get; set; }
        public string ServerSeed { get; set; }
        public string ServerHash { get; set; }
        public string PreviousServer { get; set; }
        public string PreviousClient { get; set; }
        public string PreviousHash { get; set; }
        public long? Nonce { get; set; }

        public SeedDetails()
        {
            
        }

        public SeedDetails(string Client, string Hash)
        {
            ClientSeed = Client;
            ServerHash = Hash;
        }
    }
}
