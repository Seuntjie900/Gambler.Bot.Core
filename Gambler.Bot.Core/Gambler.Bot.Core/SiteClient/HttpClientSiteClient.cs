using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// The default <see cref="ISiteClient"/> implementation, backed by <see cref="HttpClient"/>
    /// (via <see cref="SocketsHttpHandler"/>) and <c>WebSocket4Net</c> for sockets. This wraps the
    /// existing direct-connection behavior of the sites.
    /// </summary>
    public class HttpClientSiteClient : ISiteClient
    {
        private readonly SocketsHttpHandler _handler;
        private readonly HttpClient _client;
        private readonly HttpHeadersDictionary _defaultHeaders;
        private bool _disposed;

        public HttpClientSiteClient(SiteClientOptions options)
        {
            options ??= new SiteClientOptions();

            _handler = new SocketsHttpHandler
            {
                UseCookies = true,
                CookieContainer = options.Cookies ?? new CookieContainer(),
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
                AllowAutoRedirect = true,
                SslOptions = new System.Net.Security.SslClientAuthenticationOptions
                {
                    EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
                }
            };

            // Proxy support is out of scope for now (SetProxy remains NotImplemented).
            // if (options.Proxy != null) { ... }

            _client = new HttpClient(_handler, disposeHandler: true)
            {
                BaseAddress = options.BaseAddress
            };

            if (!string.IsNullOrWhiteSpace(options.UserAgent))
            {
                _client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            }

            if (options.Headers != null)
            {
                foreach (var header in options.Headers)
                {
                    try
                    {
                        _client.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
                    }
                    catch
                    {
                        // Ignore headers that the client rejects (restricted/invalid names).
                    }
                }
            }

            _defaultHeaders = new HttpHeadersDictionary(_client.DefaultRequestHeaders);
        }

        public Uri BaseAddress
        {
            get => _client.BaseAddress;
            set => _client.BaseAddress = value;
        }

        public IDictionary<string, string> DefaultHeaders => _defaultHeaders;

        public Task<HttpResponseMessage> GetAsync(string uri, CancellationToken ct = default)
            => _client.GetAsync(uri, ct);

        public Task<HttpResponseMessage> PostAsync(string uri, HttpContent content, CancellationToken ct = default)
            => _client.PostAsync(uri, content, ct);

        public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default)
            => _client.SendAsync(request, ct);

        public Task<string> GetStringAsync(string uri, CancellationToken ct = default)
            => _client.GetStringAsync(uri, ct);

        public ISiteSocket CreateSocket(SiteSocketOptions options)
            => new WebSocket4NetSiteSocket(options);

        public Task<CookieContainer> GetCookiesAsync(Uri uri)
            => Task.FromResult(_handler.CookieContainer);

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            _client.Dispose();
        }
    }
}
