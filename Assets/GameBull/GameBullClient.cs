// GameBullClient.cs
// The ONE place that actually talks to the internet.
// Two methods: GetJson (read) and PostJson (send). Everything else in the SDK uses these.

using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace GameBull
{
    public static class GameBullClient
    {
        // Set by PostJson: the GameBull error code from the last failed POST (e.g. "play.no_lives"),
        // or "http_<status>" if the body couldn't be parsed. Reset to null after a successful POST.
        public static string LastErrorCode;

        // Monotonic counter so even same-tick / parallel GETs still get a unique URL.
        private static int _cacheBust;

        // GET: ask the server for data, get raw JSON text back.
        // 'bearerToken' is optional — pass the sessionToken when an endpoint needs auth (like /context).
        public static async Task<string> GetJson(string url, string bearerToken = null)
        {
            // Force a FRESH fetch: /context (lives), leaderboards and tournaments all need current data;
            // one-play-behind lives were a cached /context. Some WebGL browsers ignore no-cache headers
            // but never a unique URL — so do BOTH.
            url += (url.Contains("?") ? "&" : "?") + "_ts=" + System.DateTime.UtcNow.Ticks + "-" + (_cacheBust++);
            Debug.Log($"[LIVES] GetJson final url={url}");

            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(bearerToken))
                    req.SetRequestHeader("Authorization", "Bearer " + bearerToken);
                req.SetRequestHeader("Cache-Control", "no-cache");
                req.SetRequestHeader("Pragma", "no-cache");

                await SendAsync(req);

                if (req.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"[GameBull] GET failed {url} → {req.responseCode} {req.error}\n{req.downloadHandler.text}");
                    return null;
                }
                return req.downloadHandler.text;
            }
        }

        // POST: send JSON to the server (e.g. submit a score), get the reply JSON back.
        public static async Task<string> PostJson(string url, string jsonBody, string bearerToken = null)
        {
            using (UnityWebRequest req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
                req.uploadHandler   = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                if (!string.IsNullOrEmpty(bearerToken))
                    req.SetRequestHeader("Authorization", "Bearer " + bearerToken);

                await SendAsync(req);

                if (req.result != UnityWebRequest.Result.Success)
                {
                    LastErrorCode = ParseErrorCode(req.downloadHandler.text, req.responseCode);
                    Debug.LogError($"[GameBull] POST failed {url} → {req.responseCode} {req.error} [{LastErrorCode}]\n{req.downloadHandler.text}");
                    return null;
                }
                LastErrorCode = null;
                return req.downloadHandler.text;
            }
        }

        // Pull the GameBull error code from a failure body: {"error":{"code":"play.no_lives"}} or
        // {"code":"..."}. Falls back to "http_<status>" if the body isn't parseable JSON.
        private static string ParseErrorCode(string body, long statusCode)
        {
            if (!string.IsNullOrEmpty(body))
            {
                try
                {
                    var obj = Newtonsoft.Json.Linq.JObject.Parse(body);
                    string code = obj["error"]?["code"]?.ToString();
                    if (string.IsNullOrEmpty(code)) code = obj["code"]?.ToString();
                    if (!string.IsNullOrEmpty(code)) return code;
                }
                catch { /* not JSON — fall through to the status-based code */ }
            }
            return "http_" + statusCode;
        }

        // Small helper: turns Unity's "send and wait" into something we can 'await'.
        private static Task SendAsync(UnityWebRequest req)
        {
            var tcs = new TaskCompletionSource<bool>();
            var op = req.SendWebRequest();
            op.completed += _ => tcs.SetResult(true);
            return tcs.Task;
        }
    }
}
