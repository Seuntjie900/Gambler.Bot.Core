using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using Gambler.Bot.Core.SiteClient;

namespace Gambler.Bot.Core.Tests.SiteClient
{
    public class HttpClientSiteClientTests : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Uri _baseUri;
        private readonly Task _pump;

        public HttpClientSiteClientTests()
        {
            _baseUri = new Uri($"http://127.0.0.1:{GetFreePort()}/");
            _listener = new HttpListener();
            _listener.Prefixes.Add(_baseUri.ToString());
            _listener.Start();
            _pump = Task.Run(PumpRequests);
        }

        public void Dispose()
        {
            try { _listener.Stop(); }
            catch { }
            try { _listener.Close(); }
            catch { }
        }

        private async Task PumpRequests()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync();
                }
                catch
                {
                    break;
                }

                try
                {
                    string body;
                    using (var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                    {
                        body = reader.ReadToEnd();
                    }

                    if (ctx.Request.Url.AbsolutePath.EndsWith("/setcookie"))
                    {
                        ctx.Response.AddHeader("Set-Cookie", "testcookie=abc123; path=/");
                    }
                    if (ctx.Request.Url.AbsolutePath.EndsWith("/unavailable"))
                    {
                        ctx.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                    }

                    var sb = new StringBuilder();
                    sb.AppendLine($"METHOD={ctx.Request.HttpMethod}");
                    sb.AppendLine($"PATH={ctx.Request.Url.AbsolutePath}");
                    sb.AppendLine($"CONTENT-TYPE={ctx.Request.ContentType}");
                    foreach (string h in ctx.Request.Headers)
                    {
                        sb.AppendLine($"HDR {h}={ctx.Request.Headers[h]}");
                    }
                    sb.AppendLine($"BODY={body}");

                    byte[] buffer = Encoding.UTF8.GetBytes(sb.ToString());
                    ctx.Response.ContentType = "text/plain";
                    ctx.Response.ContentLength64 = buffer.Length;
                    await ctx.Response.OutputStream.WriteAsync(buffer);
                    ctx.Response.Close();
                }
                catch
                {
                    try { ctx.Response.Abort(); }
                    catch { }
                }
            }
        }

        private static int GetFreePort()
        {
            var tmp = new TcpListener(IPAddress.Loopback, 0);
            tmp.Start();
            int port = ((IPEndPoint)tmp.LocalEndpoint).Port;
            tmp.Stop();
            return port;
        }

        private ISiteClient CreateClient(Dictionary<string, string> headers = null)
        {
            return new HttpClientSiteClient(new SiteClientOptions
            {
                BaseAddress = new Uri(_baseUri, "api/"),
                Headers = headers,
                UserAgent = "SiteClientTest/1.0"
            });
        }

        [Fact]
        public async Task a_GetAsyncWithRelativeUriResolvesAgainstBaseAddressAndSendsHeaders()
        {
            using var client = CreateClient(new Dictionary<string, string> { ["X-Test-Header"] = "hello" });
            var response = await client.GetAsync("test/get");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string body = await response.Content.ReadAsStringAsync();
            Assert.Contains("METHOD=GET", body);
            Assert.Contains("PATH=/api/test/get", body);
            Assert.Contains("HDR X-Test-Header=hello", body);
            Assert.Contains("HDR User-Agent=SiteClientTest/1.0", body);
        }

        [Fact]
        public async Task b_PostAsyncWithStringContentSendsBody()
        {
            using var client = CreateClient();
            var content = new StringContent("{\"amount\":1}", Encoding.UTF8, "application/json");
            var response = await client.PostAsync("test/post", content);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string body = await response.Content.ReadAsStringAsync();
            Assert.Contains("METHOD=POST", body);
            Assert.Contains("CONTENT-TYPE=application/json", body);
            Assert.Contains("BODY={\"amount\":1}", body);
        }

        [Fact]
        public async Task c_PostAsyncWithFormUrlEncodedContentSendsFormBody()
        {
            using var client = CreateClient();
            var pairs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("username", "test"),
                new KeyValuePair<string, string>("password", "secret")
            };
            var response = await client.PostAsync("test/form", new FormUrlEncodedContent(pairs));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            string body = await response.Content.ReadAsStringAsync();
            Assert.Contains("CONTENT-TYPE=application/x-www-form-urlencoded", body);
            Assert.Contains("BODY=username=test&password=secret", body);
        }

        [Fact]
        public async Task d_GetStringAsyncReturnsBody()
        {
            using var client = CreateClient();
            string body = await client.GetStringAsync("test/getstring");

            Assert.Contains("METHOD=GET", body);
            Assert.Contains("PATH=/api/test/getstring", body);
        }

        [Fact]
        public async Task e_Non2xxStatusIsPassedThrough()
        {
            using var client = CreateClient();
            var response = await client.GetAsync("test/unavailable");

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.False(response.IsSuccessStatusCode);
            string body = await response.Content.ReadAsStringAsync();
            Assert.Contains("METHOD=GET", body);
        }

        [Fact]
        public async Task f_GetCookiesAsyncReturnsCookiesFromSetCookie()
        {
            using var client = CreateClient();
            await client.GetAsync("test/setcookie");

            var cookies = await client.GetCookiesAsync(_baseUri);
            Cookie cookie = cookies.GetCookies(_baseUri).Cast<Cookie>().FirstOrDefault(x => x.Name == "testcookie");
            Assert.NotNull(cookie);
            Assert.Equal("abc123", cookie.Value);
        }

        [Fact]
        public async Task g_SeededCookiesAreSentWithRequests()
        {
            var container = new CookieContainer();
            container.SetCookies(_baseUri, "seededcookie=seeded123; path=/");
            using var client = new HttpClientSiteClient(new SiteClientOptions
            {
                BaseAddress = new Uri(_baseUri, "api/"),
                Cookies = container
            });

            var response = await client.GetAsync("test/get");
            string body = await response.Content.ReadAsStringAsync();
            Assert.Contains("HDR Cookie=seededcookie=seeded123", body);
        }
    }
}
