using System;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// Event arguments for <see cref="ISiteSocket.Error"/>.
    /// </summary>
    public class SiteSocketErrorEventArgs : EventArgs
    {
        /// <summary>The exception that caused the error.</summary>
        public Exception Exception { get; }

        public SiteSocketErrorEventArgs(Exception exception)
        {
            Exception = exception;
        }
    }
}
