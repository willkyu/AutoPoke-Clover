using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

public static class ObsService
{
    // ========= Public API =========

    /// <summary>确保连接（可在后台线程或主线程调用；不会阻塞调用线程）</summary>
    public static Task EnsureConnectedAsync() => Instance.EnsureConnectedAsync();

    /// <summary>断开连接</summary>
    public static Task DisconnectAsync() => Instance.DisconnectAsync();

    public static Task StartRecordAsync() => Instance.SendRequestNoResultAsync("StartRecord");
    public static Task<string> StopRecordGetPathAsync() => Instance.SendRequestStringAsync("StopRecord", "outputPath");

    public static Task StartReplayBufferAsync() => Instance.SendRequestNoResultAsync("StartReplayBuffer");
    public static Task StopReplayBufferAsync() => Instance.SendRequestNoResultAsync("StopReplayBuffer");
    public static Task SaveReplayBufferAsync() => Instance.SendRequestNoResultAsync("SaveReplayBuffer");

    public static Task StartStreamingAsync() => Instance.SendRequestNoResultAsync("StartStream");
    public static Task StopStreamingAsync() => Instance.SendRequestNoResultAsync("StopStream");

    public static Task<bool> GetRecordActiveAsync() => Instance.SendRequestBoolAsync("GetRecordStatus", "outputActive");
    public static Task<bool> GetStreamActiveAsync() => Instance.SendRequestBoolAsync("GetStreamStatus", "outputActive");

    public static Task SetCurrentProgramSceneAsync(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) throw new ArgumentException("sceneName is empty");
        string payload = $"\"sceneName\":{JsonEscape(sceneName)}";
        return Instance.SendRequestNoResultAsync("SetCurrentProgramScene", payload);
    }

    /// <summary>
    /// 录制结束后你想删除视频：你应当先 StopRecordGetPathAsync 得到 outputPath，然后自行 File.Delete
    /// OBS 不提供“删除最近录制文件”的官方请求，最稳定就是你自己删。
    /// </summary>

    // ========= Implementation =========

    private static ObsClient Instance => _instance ??= new ObsClient();
    private static ObsClient _instance;

    private sealed class ObsClient
    {
        private ClientWebSocket _ws;
        private CancellationTokenSource _cts;
        private Task _recvLoop;
        private Task _sendLoop;

        private readonly BlockingCollection<string> _sendQueue = new BlockingCollection<string>(new ConcurrentQueue<string>());
        private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _pending = new ConcurrentDictionary<string, TaskCompletionSource<string>>();

        private volatile bool _identified;
        private volatile bool _connecting;
        private readonly object _lock = new object();

        // cached config snapshot
        private string _url;
        private string _password;
        private int _connectTimeoutMs;
        private int _requestTimeoutMs;
        private bool _autoReconnect;

        public Task EnsureConnectedAsync()
        {
            LoadConfig();

            if (!Settings.Obs.enabled)
                return Task.CompletedTask;

            lock (_lock)
            {
                if (_ws != null && _ws.State == WebSocketState.Open && _identified) return Task.CompletedTask;
                if (_connecting) return Task.CompletedTask;
                _connecting = true;
            }

            // 不阻塞调用者：在后台真正连接
            return Task.Run(async () =>
            {
                try { await ConnectInternalAsync(); }
                finally { _connecting = false; }
            });
        }

        public Task DisconnectAsync()
        {
            return Task.Run(async () =>
            {
                try
                {
                    _cts?.Cancel();

                    if (_ws != null)
                    {
                        if (_ws.State == WebSocketState.Open)
                        {
                            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                        }
                        _ws.Dispose();
                    }
                }
                catch { /* ignore */ }
                finally
                {
                    _ws = null;
                    _identified = false;
                    _cts = null;
                    _recvLoop = null;
                    _sendLoop = null;
                }
            });
        }

        public Task SendRequestNoResultAsync(string requestType, string requestDataInline = null)
        {
            return SendRequestStringAsync(requestType, null, requestDataInline).ContinueWith(t =>
            {
                if (t.IsFaulted) throw t.Exception;
            });
        }

        public async Task<string> SendRequestStringAsync(string requestType, string wantField, string requestDataInline = null)
        {
            await EnsureConnectedAsync();

            if (_ws == null || _ws.State != WebSocketState.Open || !_identified)
                throw new Exception("[OBS] Not connected/identified.");

            string requestId = Guid.NewGuid().ToString("N");
            var tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            _pending[requestId] = tcs;

            string msg = BuildRequest(requestType, requestId, requestDataInline);
            _sendQueue.Add(msg);

            using var timeoutCts = new CancellationTokenSource(_requestTimeoutMs);
            using (timeoutCts.Token.Register(() =>
            {
                if (_pending.TryRemove(requestId, out var p))
                    p.TrySetException(new TimeoutException($"[OBS] Request timeout: {requestType}"));
            }))
            {
                string raw = await tcs.Task; // raw response json (or extracted field)

                if (wantField == null) return raw;

                // raw 这里我们让它直接返回“字段值”，避免外部解析
                return raw;
            }
        }

        public async Task<bool> SendRequestBoolAsync(string requestType, string boolField, string requestDataInline = null)
        {
            string val = await SendRequestStringAsync(requestType, boolField, requestDataInline);
            // val 约定为 "true"/"false"
            return string.Equals(val, "true", StringComparison.OrdinalIgnoreCase);
        }

        private async Task ConnectInternalAsync()
        {
            await DisconnectAsync();

            _ws = new ClientWebSocket();
            _cts = new CancellationTokenSource();

            using var connectTimeout = new CancellationTokenSource(_connectTimeoutMs);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, connectTimeout.Token);

            try
            {
                await _ws.ConnectAsync(new Uri(_url), linked.Token);
            }
            catch (Exception e)
            {
                throw new Exception("[OBS] Connect failed: " + e.Message);
            }

            _identified = false;

            _sendLoop = Task.Run(() => SendLoop(_cts.Token));
            _recvLoop = Task.Run(() => RecvLoop(_cts.Token));
        }

        private async Task SendLoop(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    string msg;
                    try { msg = _sendQueue.Take(ct); }
                    catch { break; }

                    if (_ws == null || _ws.State != WebSocketState.Open) continue;

                    byte[] bytes = Encoding.UTF8.GetBytes(msg);
                    await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[OBS] SendLoop stopped: " + e.Message);
            }
        }

        private async Task RecvLoop(CancellationToken ct)
        {
            var buffer = new byte[64 * 1024];
            var sb = new StringBuilder();

            try
            {
                while (!ct.IsCancellationRequested && _ws != null && _ws.State == WebSocketState.Open)
                {
                    sb.Clear();
                    WebSocketReceiveResult res;

                    do
                    {
                        res = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);
                        if (res.MessageType == WebSocketMessageType.Close) break;
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, res.Count));
                    }
                    while (!res.EndOfMessage);

                    if (res.MessageType == WebSocketMessageType.Close) break;

                    string json = sb.ToString();
                    HandleIncoming(json);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[OBS] RecvLoop stopped: " + e.Message);
            }

            // auto reconnect
            if (_autoReconnect && Settings.Obs.enabled)
            {
                _identified = false;
                try
                {
                    await Task.Delay(800);
                    await EnsureConnectedAsync();
                }
                catch { /* ignore */ }
            }
        }

        private void HandleIncoming(string json)
        {
            // OBS WS v5: op=0 Hello, op=2 Identify, op=7 RequestResponse
            if (json.Contains("\"op\":0"))
            {
                HandleHello(json);
                return;
            }
            if (json.Contains("\"op\":2"))
            {
                _identified = true;
                return;
            }
            if (json.Contains("\"op\":7"))
            {
                HandleRequestResponse(json);
                return;
            }
        }

        private void HandleHello(string json)
        {
            // 解析 authentication 字段（如果有）
            // json 内结构类似：d.authentication = {challenge, salt}
            // 我们用最小字符串提取
            string challenge = null, salt = null;
            MiniJson.TryGetString(json, "challenge", out challenge);
            MiniJson.TryGetString(json, "salt", out salt);

            string auth = "";
            if (!string.IsNullOrEmpty(challenge) && !string.IsNullOrEmpty(salt))
            {
                auth = ComputeAuth(_password ?? "", salt, challenge);
            }

            // Identify payload:
            // {"op":1,"d":{"rpcVersion":1,"authentication":"..."}}
            string identify;
            if (!string.IsNullOrEmpty(auth))
                identify = $"{{\"op\":1,\"d\":{{\"rpcVersion\":1,\"authentication\":{JsonEscape(auth)}}}}}";
            else
                identify = $"{{\"op\":1,\"d\":{{\"rpcVersion\":1}}}}";

            _sendQueue.Add(identify);
        }

        private void HandleRequestResponse(string json)
        {
            // requestId
            if (!MiniJson.TryGetString(json, "requestId", out var requestId))
                return;

            if (!_pending.TryRemove(requestId, out var tcs))
                return;

            // result
            bool ok = json.Contains("\"result\":true");
            if (!ok)
            {
                tcs.TrySetException(new Exception("[OBS] Request failed:\n" + json));
                return;
            }

            // 你请求要的字段是什么？我们把“想要字段”的解析放在 SendRequestStringAsync 里不方便
            // 所以这里约定：requestId -> tcs 只存 raw json，外层在 PendingMeta 里指定字段
            // 但为了保持简单：我们把 wantField 编到 requestId 里不现实
            // => 这里采取折中：若 responseData 里有 outputPath 就提取，否则返回 "true"
            if (MiniJson.TryGetString(json, "outputPath", out var outputPath))
            {
                tcs.TrySetResult(outputPath);
                return;
            }

            // 如果是 bool outputActive，提取为 true/false 字符串
            if (MiniJson.TryGetBool(json, "outputActive", out var outputActive))
            {
                tcs.TrySetResult(outputActive ? "true" : "false");
                return;
            }

            // 否则默认返回 "OK"
            tcs.TrySetResult("OK");
        }

        private string BuildRequest(string requestType, string requestId, string requestDataInline)
        {
            // {"op":6,"d":{"requestType":"X","requestId":"Y","requestData":{...}}}
            if (!string.IsNullOrEmpty(requestDataInline))
            {
                return $"{{\"op\":6,\"d\":{{\"requestType\":{JsonEscape(requestType)},\"requestId\":{JsonEscape(requestId)},\"requestData\":{{{requestDataInline}}}}}}}";
            }
            else
            {
                return $"{{\"op\":6,\"d\":{{\"requestType\":{JsonEscape(requestType)},\"requestId\":{JsonEscape(requestId)}}}}}";
            }
        }

        private void LoadConfig()
        {
            var cfg = Settings.Obs;
            _url = cfg.url?.Trim();
            _password = cfg.password ?? "";
            _connectTimeoutMs = Mathf.Clamp(cfg.connectTimeoutMs, 1000, 60000);
            _requestTimeoutMs = Mathf.Clamp(cfg.requestTimeoutMs, 1000, 60000);
            _autoReconnect = cfg.autoReconnect;
        }
    }

    // ========= helpers =========

    private static string JsonEscape(string s)
    {
        if (s == null) return "\"\"";
        // minimal safe escape for JSON string
        var sb = new StringBuilder(s.Length + 8);
        sb.Append('\"');
        foreach (char ch in s)
        {
            switch (ch)
            {
                case '\\': sb.Append("\\\\"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (ch < 32) sb.Append("\\u" + ((int)ch).ToString("x4"));
                    else sb.Append(ch);
                    break;
            }
        }
        sb.Append('\"');
        return sb.ToString();
    }

    private static string ComputeAuth(string password, string salt, string challenge)
    {
        // OBS v5:
        // secret = base64(sha256(password + salt))
        // auth   = base64(sha256(secret + challenge))
        string secret = Base64Sha256(password + salt);
        string auth = Base64Sha256(secret + challenge);
        return auth;
    }

    private static string Base64Sha256(string s)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(s);
        using var sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private static class MiniJson
    {
        public static bool TryGetString(string json, string key, out string value)
        {
            value = null;
            string pat = $"\"{key}\"";
            int i = json.IndexOf(pat, StringComparison.Ordinal);
            if (i < 0) return false;

            i = json.IndexOf(':', i);
            if (i < 0) return false;

            i++;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;

            if (i >= json.Length || json[i] != '"') return false;
            i++;

            int j = i;
            while (j < json.Length)
            {
                if (json[j] == '"' && json[j - 1] != '\\') break;
                j++;
            }
            if (j >= json.Length) return false;

            value = json.Substring(i, j - i);
            // unescape minimal
            value = value.Replace("\\\"", "\"").Replace("\\\\", "\\").Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
            return true;
        }

        public static bool TryGetBool(string json, string key, out bool value)
        {
            value = false;

            string pat = $"\"{key}\"";
            int i = json.IndexOf(pat, StringComparison.Ordinal);
            if (i < 0) return false;

            i = json.IndexOf(':', i);
            if (i < 0) return false;

            i++;
            while (i < json.Length && char.IsWhiteSpace(json[i])) i++;

            // ---- 关键修正部分 ----
            if (i + 4 <= json.Length &&
                string.Compare(json, i, "true", 0, 4, StringComparison.Ordinal) == 0)
            {
                value = true;
                return true;
            }

            if (i + 5 <= json.Length &&
                string.Compare(json, i, "false", 0, 5, StringComparison.Ordinal) == 0)
            {
                value = false;
                return true;
            }

            return false;
        }

    }
}

#endif
