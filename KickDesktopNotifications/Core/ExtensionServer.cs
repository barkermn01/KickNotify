using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace KickDesktopNotifications.Core
{
    internal class ExtensionServer
    {
        private static ExtensionServer _instance;
        private HttpListener _listener;
        private CancellationTokenSource _cts;
        private const int Port = 32585;

        public static ExtensionServer GetInstance()
        {
            if (_instance == null)
            {
                _instance = new ExtensionServer();
            }
            return _instance;
        }

        /// <summary>
        /// Returns the current extension key, generating one if it doesn't exist.
        /// </summary>
        public static string GetOrCreateKey()
        {
            var store = DataStore.GetInstance().Store;
            if (string.IsNullOrEmpty(store.ExtensionKey))
            {
                store.ExtensionKey = GenerateKey();
                DataStore.GetInstance().Save();
            }
            return store.ExtensionKey;
        }

        /// <summary>
        /// Generates a new key, replacing any existing one.
        /// </summary>
        public static string RegenerateKey()
        {
            DataStore.GetInstance().Store.ExtensionKey = GenerateKey();
            DataStore.GetInstance().Save();
            return DataStore.GetInstance().Store.ExtensionKey;
        }

        private static string GenerateKey()
        {
            var bytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
            _listener.Prefixes.Add($"http://localhost:{Port}/");
            _listener.Start();
            Logger.GetInstance().WriteLine($"ExtensionServer WebSocket listening on port {Port}");
            Task.Run(() => AcceptLoop(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
            _listener?.Stop();
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var context = await _listener.GetContextAsync();

                    if (!context.Request.IsWebSocketRequest)
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                        continue;
                    }

                    // Validate the key from the query string before upgrading
                    var query = HttpUtility.ParseQueryString(context.Request.Url.Query);
                    string providedKey = query["key"];
                    string expectedKey = DataStore.GetInstance().Store.ExtensionKey;

                    if (string.IsNullOrEmpty(expectedKey) || providedKey != expectedKey)
                    {
                        Logger.GetInstance().WriteLine("ExtensionServer: Rejected WebSocket upgrade - invalid key");
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var body = Encoding.UTF8.GetBytes("{\"error\":\"invalid_key\"}");
                        context.Response.OutputStream.Write(body, 0, body.Length);
                        context.Response.Close();
                        continue;
                    }

                    var wsContext = await context.AcceptWebSocketAsync(null);
                    _ = Task.Run(() => HandleClient(wsContext.WebSocket, ct));
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    Logger.GetInstance().WriteLine($"ExtensionServer accept error: {ex.Message}");
                }
            }
        }

        private async Task HandleClient(WebSocket ws, CancellationToken ct)
        {
            var buffer = new byte[4096];
            try
            {
                while (ws.State == WebSocketState.Open && !ct.IsCancellationRequested)
                {
                    var result = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", ct);
                        break;
                    }

                    string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    string response = ProcessMessage(message);
                    var responseBytes = Encoding.UTF8.GetBytes(response);
                    await ws.SendAsync(new ArraySegment<byte>(responseBytes), WebSocketMessageType.Text, true, ct);
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                Logger.GetInstance().WriteLine($"ExtensionServer client error: {ex.Message}");
            }
        }

        private string ProcessMessage(string raw)
        {
            try
            {
                var msg = JsonSerializer.Deserialize<ExtensionMessage>(raw);
                if (msg == null || string.IsNullOrEmpty(msg.Action))
                {
                    return JsonSerializer.Serialize(new ExtensionResponse { Success = false, Error = "Invalid message" });
                }

                switch (msg.Action.ToLower())
                {
                    case "add_streamer":
                        return HandleAddStreamer(msg);
                    case "import_streamers":
                        return HandleImportStreamers(msg);
                    case "list_streamers":
                        return HandleListStreamers();
                    case "list_streamers_status":
                        return HandleListStreamersStatus();
                    default:
                        return JsonSerializer.Serialize(new ExtensionResponse { Success = false, Error = $"Unknown action: {msg.Action}" });
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().WriteLine($"ExtensionServer process error: {ex.Message}");
                return JsonSerializer.Serialize(new ExtensionResponse { Success = false, Error = "Internal error" });
            }
        }

        private string HandleAddStreamer(ExtensionMessage msg)
        {
            if (string.IsNullOrWhiteSpace(msg.Streamer))
            {
                return JsonSerializer.Serialize(new ExtensionResponse { Success = false, Error = "Streamer name required" });
            }

            string slug = msg.Streamer.Trim().ToLower();
            UIStreamer.GetCreateStreamer(slug);
            DataStore.GetInstance().Save();

            Logger.GetInstance().WriteLine($"ExtensionServer: Added streamer '{slug}'");
            return JsonSerializer.Serialize(new ExtensionResponse { Success = true, Message = $"Added {slug}" });
        }

        private string HandleImportStreamers(ExtensionMessage msg)
        {
            if (msg.Streamers == null || msg.Streamers.Count == 0)
            {
                return JsonSerializer.Serialize(new ExtensionResponse { Success = false, Error = "Streamers list required" });
            }

            int added = 0;
            foreach (var name in msg.Streamers)
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    string slug = name.Trim().ToLower();
                    UIStreamer.GetCreateStreamer(slug);
                    added++;
                }
            }
            DataStore.GetInstance().Save();

            Logger.GetInstance().WriteLine($"ExtensionServer: Imported {added} streamers");
            return JsonSerializer.Serialize(new ExtensionResponse { Success = true, Message = $"Imported {added} streamers" });
        }

        private string HandleListStreamers()
        {
            var streamers = DataStore.GetInstance().Store.SteamersToIgnore?.Streamers;
            var names = streamers?.Select(s => s.Name).ToList() ?? new List<string>();
            return JsonSerializer.Serialize(new ExtensionResponse { Success = true, Streamers = names });
        }

        private string HandleListStreamersStatus()
        {
            var streamers = DataStore.GetInstance().Store.SteamersToIgnore?.Streamers;
            if (streamers == null)
            {
                return JsonSerializer.Serialize(new ExtensionStatusResponse { Success = true, Channels = new List<ChannelStatus>() });
            }

            var states = KickFetcher.GetInstance().StreamerStates;
            var channels = streamers
                .Where(s => !s.IsIgnored)
                .Select(s =>
                {
                    bool isLive = states.ContainsKey(s.Name) && states[s.Name].IsLive;
                    return new ChannelStatus { Name = s.Name, IsLive = isLive };
                })
                .OrderByDescending(c => c.IsLive)
                .ThenBy(c => c.Name)
                .ToList();

            return JsonSerializer.Serialize(new ExtensionStatusResponse { Success = true, Channels = channels });
        }
    }

    internal class ExtensionMessage
    {
        [System.Text.Json.Serialization.JsonPropertyName("action")]
        public string Action { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("streamer")]
        public string Streamer { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("streamers")]
        public List<string> Streamers { get; set; }
    }

    internal class ExtensionResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("message")]
        public string Message { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("error")]
        public string Error { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("streamers")]
        public List<string> Streamers { get; set; }
    }

    internal class ChannelStatus
    {
        [System.Text.Json.Serialization.JsonPropertyName("name")]
        public string Name { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("is_live")]
        public bool IsLive { get; set; }
    }

    internal class ExtensionStatusResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("success")]
        public bool Success { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("channels")]
        public List<ChannelStatus> Channels { get; set; }
    }
}
