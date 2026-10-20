using System;
using WebSocket4Net;

namespace Gambler.Bot.Core.SiteClient
{
    /// <summary>
    /// An <see cref="ISiteSocket"/> backed by <c>WebSocket4Net.WebSocket</c>. This maps the
    /// third-party socket's state and events onto the Core-owned <see cref="ISiteSocket"/> surface so
    /// sites never see WebSocket4Net types.
    /// </summary>
    public class WebSocket4NetSiteSocket : ISiteSocket
    {
        private readonly WebSocket _ws;
        private readonly EventHandler _openedHandler;
        private readonly EventHandler _closedHandler;
        private readonly EventHandler<SuperSocket.ClientEngine.ErrorEventArgs> _errorHandler;
        private readonly EventHandler<MessageReceivedEventArgs> _messageHandler;
        private bool _disposed;

        public WebSocket4NetSiteSocket(SiteSocketOptions options)
        {
            options ??= new SiteSocketOptions { Url = new Uri("ws://localhost") };

            _ws = new WebSocket(
                options.Url.ToString(),
                options.Protocol,
                options.Cookies,
                options.Headers,
                options.UserAgent,
                options.Origin,
                WebSocketVersion.Rfc6455,
                null);

            _openedHandler = (s, e) => Opened?.Invoke(this, EventArgs.Empty);
            _closedHandler = (s, e) => Closed?.Invoke(this, EventArgs.Empty);
            _errorHandler = (s, e) => Error?.Invoke(this, new SiteSocketErrorEventArgs(e.Exception));
            _messageHandler = (s, e) => MessageReceived?.Invoke(this, new SiteSocketMessageEventArgs(e.Message));

            _ws.Opened += _openedHandler;
            _ws.Closed += _closedHandler;
            _ws.Error += _errorHandler;
            _ws.MessageReceived += _messageHandler;
        }

        public event EventHandler<SiteSocketMessageEventArgs> MessageReceived;
        public event EventHandler<SiteSocketErrorEventArgs> Error;
        public event EventHandler Opened;
        public event EventHandler Closed;

        public SiteSocketState State
        {
            get
            {
                switch (_ws.State)
                {
                    case WebSocketState.Connecting: return SiteSocketState.Connecting;
                    case WebSocketState.Open: return SiteSocketState.Open;
                    case WebSocketState.Closing: return SiteSocketState.Closing;
                    default: return SiteSocketState.Closed;
                }
            }
        }

        public void Open() => _ws.Open();

        public void Close()
        {
            try { _ws.Close(); }
            catch
            {
                // Already closed or never opened; ignore.
            }
        }

        public void Send(string message) => _ws.Send(message);

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _ws.Opened -= _openedHandler;
            _ws.Closed -= _closedHandler;
            _ws.Error -= _errorHandler;
            _ws.MessageReceived -= _messageHandler;

            try { _ws.Close(); }
            catch
            {
                // Ignore close failures during disposal.
            }
        }
    }
}
