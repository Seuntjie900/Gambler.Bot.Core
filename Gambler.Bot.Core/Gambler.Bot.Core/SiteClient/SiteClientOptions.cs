using System;
using System.Collections.Generic;
using System.Net;
using Gambler.Bot.Core.Helpers;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// Options used to create an <see cref="ISiteClient"/>.
    /// </summary>
    public class SiteClientOptions
    {
        /// <summary>The base address used to resolve relative request URIs.</summary>
        public Uri BaseAddress { get; set; }

        /// <summary>
        /// The cookie container to seed the client with (and to read back from via
        /// <see cref="ISiteClient.GetCookiesAsync"/>). When <c>null</c>, an empty container is used.
        /// </summary>
        public CookieContainer Cookies { get; set; }

        /// <summary>Default request headers to apply to every request.</summary>
        public IDictionary<string, string> Headers { get; set; }

        /// <summary>The User-Agent header value, if any.</summary>
        public string UserAgent { get; set; }

        /// <summary>Optional proxy details. Proxy support is out of scope for now.</summary>
        public ProxyDetails Proxy { get; set; }
    }
}
