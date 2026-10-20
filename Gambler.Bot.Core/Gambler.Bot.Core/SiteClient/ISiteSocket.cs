using System;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// A transport-agnostic WebSocket used by sites. Implementations must not expose any
    /// third-party WebSocket types to callers.
    /// </summary>
    public interface ISiteSocket : IDisposable
    {
        /// <summary>The current <see cref="SiteSocketState"/> of the socket.</summary>
        SiteSocketState State { get; }

        /// <summary>
        /// Starts connecting to the socket. This is non-blocking; callers should poll
        /// <see cref="State"/> until it is <see cref="SiteSocketState.Open"/> or
        /// <see cref="SiteSocketState.Closed"/>.
        /// </summary>
        void Open();

        /// <summary>Closes the socket.</summary>
        void Close();

        /// <summary>Sends a text message on the socket.</summary>
        /// <param name="message">The text message to send.</param>
        void Send(string message);

        /// <summary>Raised when a text message is received.</summary>
        event EventHandler<SiteSocketMessageEventArgs> MessageReceived;

        /// <summary>Raised when a socket error occurs.</summary>
        event EventHandler<SiteSocketErrorEventArgs> Error;

        /// <summary>Raised when the socket has finished opening.</summary>
        event EventHandler Opened;

        /// <summary>Raised when the socket has been closed.</summary>
        event EventHandler Closed;
    }
}
