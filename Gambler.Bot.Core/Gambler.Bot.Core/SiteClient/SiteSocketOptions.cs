using System;
using System.Collections.Generic;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// Options used to create an <see cref="ISiteSocket"/> via <see cref="ISiteClient.CreateSocket"/>.
    /// </summary>
    public class SiteSocketOptions
    {
        /// <summary>The WebSocket (ws/wss) URL to connect to.</summary>
        public Uri Url { get; set; }

        /// <summary>The subprotocol to request (e.g. "wamp"), or <c>null</c>.</summary>
        public string Protocol { get; set; }

        /// <summary>Cookies to send with the handshake.</summary>
        public List<KeyValuePair<string, string>> Cookies { get; set; }

        /// <summary>Extra headers to send with the handshake.</summary>
        public List<KeyValuePair<string, string>> Headers { get; set; }

        /// <summary>The User-Agent to send with the handshake.</summary>
        public string UserAgent { get; set; }

        /// <summary>The Origin header value, if any.</summary>
        public string Origin { get; set; }
    }
}
