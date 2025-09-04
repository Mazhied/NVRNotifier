using NVRNotifier.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Serilog;
using Microsoft.Extensions.Logging;
using Serilog.Extensions.Logging;
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
        private CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger<ZmWsClient> _logger;

        public event EventHandler<AlarmReceivedMessage?>? OnEventReceived;
        public event EventHandler<string>? OnError;
        public event EventHandler? OnConnected;
        public event EventHandler? OnDisconnected;


        public ZmWsClient(string host, string port, string user, string password, ILogger<ZmWsClient> logger)
        {
            _zmHost = host;
            _zmPort = port;
            _apiUser = user;
            _apiPassword = password;
            //_cancellationTokenSource = new CancellationTokenSource();

            _logger = logger;
        }

        public async Task ConnectAsync()
        {
            try
            {
                var uri = new Uri($"wss://{_zmHost}:{_zmPort}");
                _webSocket = new ClientWebSocket();
                _webSocket.Options.RemoteCertificateValidationCallback += (o, c, ch, er) => true;
                _cancellationTokenSource = new CancellationTokenSource();


                await _webSocket.ConnectAsync(uri, _cancellationTokenSource.Token);
                OnConnected?.Invoke(this, EventArgs.Empty);

                // Авторизуемся
                await AuthenticateAsync();

                // Запускаем прослушивание сообщений
                _ = Task.Run(ReceiveMessagesAsync, _cancellationTokenSource.Token);
            }
            catch (WebSocketException ex)
            {
                await DisconnectAsync();
                OnError?.Invoke(this, $"Ошибка при попытке установить соединение по вебсокету: {ex.Message}");
            }
            catch (Exception ex)
            {
                await DisconnectAsync();
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
                _logger.LogError($"Authentication failed: {authReceivedMessage?.Reason}");
                throw new Exception($"Authentication failed: {authReceivedMessage?.Reason}");
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

                    var rawStringMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    using var doc = JsonDocument.Parse(rawStringMessage);
                    string? eventType = doc.RootElement.GetProperty("event").GetString();
                    // Обрабатываем только alarm сообщения
                    if (eventType != "alarm")
                    {
                        continue;
                    }

                    var alarmMessage = JsonSerializer.Deserialize<AlarmReceivedMessage>(rawStringMessage);
                    
                    OnEventReceived?.Invoke(this, alarmMessage);
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
