using System.Threading.Tasks;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// Creates <see cref="ISiteClient"/> instances. Factories are async so that browser-based
    /// implementations can perform initial navigation before returning the client.
    /// </summary>
    public interface ISiteClientFactory
    {
        /// <summary>Creates a new client from the given options.</summary>
        Task<ISiteClient> CreateClientAsync(SiteClientOptions options);
    }
}
