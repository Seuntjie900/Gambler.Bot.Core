using System.Threading.Tasks;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// The default <see cref="ISiteClientFactory"/>. Creates <see cref="HttpClientSiteClient"/>
    /// instances (direct <see cref="System.Net.Http.HttpClient"/> + <c>WebSocket4Net</c>).
    /// </summary>
    public class HttpClientSiteClientFactory : ISiteClientFactory
    {
        public Task<ISiteClient> CreateClientAsync(SiteClientOptions options)
            => Task.FromResult<ISiteClient>(new HttpClientSiteClient(options));
    }
}
