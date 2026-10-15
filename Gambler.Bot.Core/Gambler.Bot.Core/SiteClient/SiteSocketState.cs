namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// The connection state of a <see cref="ISiteSocket"/>.
    /// </summary>
    public enum SiteSocketState
    {
        /// <summary>The socket is establishing a connection.</summary>
        Connecting,
        /// <summary>The socket is connected and can send/receive.</summary>
        Open,
        /// <summary>The socket is in the process of closing.</summary>
        Closing,
        /// <summary>The socket is closed.</summary>
        Closed
    }
}
