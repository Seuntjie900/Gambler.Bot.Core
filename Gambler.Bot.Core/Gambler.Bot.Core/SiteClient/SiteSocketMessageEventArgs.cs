using System;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// Event arguments for <see cref="ISiteSocket.MessageReceived"/>.
    /// </summary>
    public class SiteSocketMessageEventArgs : EventArgs
    {
        /// <summary>The text message that was received.</summary>
        public string Message { get; }

        public SiteSocketMessageEventArgs(string message)
        {
            Message = message;
        }
    }
}
