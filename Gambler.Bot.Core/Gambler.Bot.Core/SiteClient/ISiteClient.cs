using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// A transport-agnostic HTTP client used by sites. The surface mirrors the
    /// <see cref="HttpClient"/> members the sites rely on, so call sites change minimally.
    /// </summary>
    public interface ISiteClient : IDisposable
    {
        /// <summary>The base address used to resolve relative request URIs.</summary>
        Uri BaseAddress { get; set; }

        /// <summary>
        /// The default request headers. Adding to this collection applies the header to all
        /// subsequent requests (equivalent to <see cref="HttpClient.DefaultRequestHeaders"/>).
        /// </summary>
        IDictionary<string, string> DefaultHeaders { get; }

        /// <summary>Issues an HTTP GET request.</summary>
        Task<HttpResponseMessage> GetAsync(string uri, CancellationToken ct = default);

        /// <summary>Issues an HTTP POST request with the given content.</summary>
        Task<HttpResponseMessage> PostAsync(string uri, HttpContent content, CancellationToken ct = default);

        /// <summary>Issues an arbitrary HTTP request.</summary>
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default);

        /// <summary>Issues an HTTP GET request and returns the body as a string.</summary>
        Task<string> GetStringAsync(string uri, CancellationToken ct = default);

        /// <summary>Creates a new WebSocket from this client.</summary>
        /// <param name="options">The socket options.</param>
        ISiteSocket CreateSocket(SiteSocketOptions options);

        /// <summary>
        /// Returns the cookie container associated with this client (after any Set-Cookie
        /// responses received).
        /// </summary>
        Task<CookieContainer> GetCookiesAsync(Uri uri);
    }
}
