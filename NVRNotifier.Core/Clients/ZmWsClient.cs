using NVRNotifier.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace NVRNotifier.Core.Clients
{
    public class ZmWsClient : IDisposable
    {
        private ClientWebSocket _webSocket;
        private readonly string _apiUser;
        private readonly string _apiPassword;
        private readonly string _zmHost;
        private readonly string _zmPort;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public event EventHandler<ZmEventReceivedMessage>? OnEventReceived;
        public event EventHandler<string>? OnError;
        public event EventHandler? OnConnected;
        public event EventHandler? OnDisconnected;

        public ZmWsClient(string host, string port, string user, string password)
        {
            _zmHost = host;
            _zmPort = port;
            _apiUser = user;
            _apiPassword = password;
            _cancellationTokenSource = new CancellationTokenSource();
            _webSocket = new ClientWebSocket();
        }

        public async Task ConnectAsync()
        {
            try
            {
                var uri = new Uri($"wss://{_zmHost}:{_zmPort}");

                await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
                OnConnected?.Invoke(this, EventArgs.Empty);

                // Авторизуемся
                await AuthenticateAsync();

                // Запускаем прослушивание сообщений
                _ = Task.Run(ReceiveMessagesAsync, _cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Connection error: {ex.Message}");
            }
        }

        private async Task AuthenticateAsync()
        {
            var authMessage = new ZmAuthSentMessage()
            {
                Event = "auth",
                Data = new ZmAuthData()
                {
                    User = _apiUser,
                    Password = _apiPassword
                }
            };
            
            var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(authMessage);
            await _webSocket.SendAsync(
                new ArraySegment<byte>(jsonBytes),
                WebSocketMessageType.Text,
                true,
                _cancellationTokenSource.Token
            );

            
            var buffer = new byte[4096];
            var result = await _webSocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                _cancellationTokenSource.Token
            );

            var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var authReceivedMessage = JsonSerializer.Deserialize<ZmAuthReceivedMessage>(message);
            if (authReceivedMessage == null || authReceivedMessage.Status != "success")
            {
                throw new Exception("Authentication failed");
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[4096];

            try
            {
                while (_webSocket.State == WebSocketState.Open &&
                      !_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var result = await _webSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer),
                        _cancellationTokenSource.Token
                    );

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await _webSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure,
                            "Closed by server",
                            _cancellationTokenSource.Token
                        );
                        OnDisconnected?.Invoke(this, EventArgs.Empty);
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessMessage(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Receive error: {ex.Message}");
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                var zmEvent = JsonSerializer.Deserialize<ZmEventReceivedMessage>(message);

                if (zmEvent != null)
                {
                    OnEventReceived?.Invoke(this, zmEvent);

                    // Обработка конкретных типов событий
                    switch (zmEvent.Event)
                    {
                        case "EventStart":
                            Console.WriteLine($"Motion started: {zmEvent.EventData.Id}");
                            break;
                        case "EventEnd":
                            Console.WriteLine($"Motion ended: {zmEvent.EventData.Id}");
                            break;
                        case "Heartbeat":
                            // Игнорируем heartbeat сообщения
                            break;
                        default:
                            Console.WriteLine($"Unknown event type: {zmEvent.Event}");
                            break;
                    }
                }
            }
            catch (JsonException ex)
            {
                OnError?.Invoke(this, $"JSON parse error: {ex.Message}");
            }
        }

        private string GetBasicAuthHeader(string username, string password)
        {
            var credentials = $"{username}:{password}";
            var base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
            return $"Basic {base64Credentials}";
        }

        public async Task DisconnectAsync()
        {
            try
            {
                _cancellationTokenSource.Cancel();

                if (_webSocket.State == WebSocketState.Open)
                {
                    await _webSocket.CloseAsync(
                        WebSocketCloseStatus.NormalClosure,
                        "Client disconnect",
                        CancellationToken.None
                    );
                }

                _webSocket.Dispose();
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, $"Disconnect error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Cancel();
            _webSocket?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
}
