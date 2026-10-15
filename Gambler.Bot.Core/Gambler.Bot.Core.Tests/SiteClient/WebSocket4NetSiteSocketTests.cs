using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using Gambler.Bot.Core.SiteClient;

namespace Gambler.Bot.Core.Tests.SiteClient
{
    public class WebSocket4NetSiteSocketTests : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Uri _url;
        private readonly List<WebSocket> _serverSockets = new();

        public WebSocket4NetSiteSocketTests()
        {
            int port = GetFreePort();
            _url = new Uri($"ws://127.0.0.1:{port}/");
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();
            _ = Task.Run(PumpServer);
        }

        public void Dispose()
        {
            lock (_serverSockets)
            {
                foreach (var ws in _serverSockets)
                {
                    try { ws.Abort(); }
                    catch { }
                }
                _serverSockets.Clear();
            }
            try { _listener.Stop(); }
            catch { }
            try { _listener.Close(); }
            catch { }
        }

        private async Task PumpServer()
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
                    if (!ctx.Request.IsWebSocketRequest)
                    {
                        ctx.Response.StatusCode = 400;
                        ctx.Response.Close();
                        continue;
                    }

                    var wsContext = await ctx.AcceptWebSocketAsync(null);
                    var ws = wsContext.WebSocket;
                    lock (_serverSockets)
                    {
                        _serverSockets.Add(ws);
                    }
                    _ = Task.Run(() => ServeSocket(ws));
                }
                catch
                {
                    try { ctx.Response.Abort(); }
                    catch { }
                }
            }
        }

        private async Task ServeSocket(WebSocket ws)
        {
            var buffer = new byte[8192];
            try
            {
                while (ws.State == WebSocketState.Open)
                {
                    var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await ws.SendAsync(Encoding.UTF8.GetBytes(message + "-echo"), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        break;
                    }
                }
            }
            catch
            {
                // Client disconnected; nothing to do.
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

        private ISiteSocket CreateSocket()
        {
            return new WebSocket4NetSiteSocket(new SiteSocketOptions { Url = _url });
        }

        private static async Task WaitAsync(TaskCompletionSource<bool> tcs, int seconds = 10)
        {
            var completed = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(seconds)));
            Assert.Same(tcs.Task, completed);
        }

        [Fact]
        public async Task a_OpeningSocketShouldRaiseOpenedAndReachOpenState()
        {
            using var socket = CreateSocket();
            var opened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            socket.Opened += (s, e) => opened.TrySetResult(true);

            socket.Open();
            await WaitAsync(opened);

            Assert.Equal(SiteSocketState.Open, socket.State);
        }

        [Fact]
        public async Task b_SendReceiveRoundTrip()
        {
            using var socket = CreateSocket();
            var opened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var received = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            socket.Opened += (s, e) => opened.TrySetResult(true);
            socket.MessageReceived += (s, e) => received.TrySetResult(e.Message);

            socket.Open();
            await WaitAsync(opened);

            socket.Send("hello");
            string message = await received.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("hello-echo", message);
        }

        [Fact]
        public async Task c_ServerClosingSocketShouldRaiseClosed()
        {
            using var socket = CreateSocket();
            var opened = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var closed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            socket.Opened += (s, e) => opened.TrySetResult(true);
            socket.Closed += (s, e) => closed.TrySetResult(true);

            socket.Open();
            await WaitAsync(opened);

            WebSocket serverSocket;
            lock (_serverSockets)
            {
                serverSocket = _serverSockets.LastOrDefault(x => x.State == WebSocketState.Open);
            }
            Assert.NotNull(serverSocket);
            await serverSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "server closing", CancellationToken.None);

            await WaitAsync(closed);
        }

        [Fact]
        public async Task d_FailedConnectionShouldRaiseError()
        {
            int port = GetFreePort();
            var badListener = new HttpListener();
            badListener.Prefixes.Add($"http://127.0.0.1:{port}/");
            badListener.Start();
            _ = Task.Run(async () =>
            {
                try
                {
                    var ctx = await badListener.GetContextAsync();
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                }
                catch
                {
                    // Listener stopped.
                }
            });

            using var socket = new WebSocket4NetSiteSocket(new SiteSocketOptions { Url = new Uri($"ws://127.0.0.1:{port}/") });
            var error = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
            socket.Error += (s, e) => error.TrySetResult(e.Exception);

            socket.Open();
            Exception exception = await error.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.NotNull(exception);

            badListener.Stop();
        }
    }
}
