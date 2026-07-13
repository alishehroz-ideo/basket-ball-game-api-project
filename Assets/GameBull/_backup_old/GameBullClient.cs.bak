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
        // GET: ask the server for data, get raw JSON text back.
        // 'bearerToken' is optional — pass the sessionToken when an endpoint needs auth (like /context).
        public static async Task<string> GetJson(string url, string bearerToken = null)
        {
            using (UnityWebRequest req = UnityWebRequest.Get(url))
            {
                if (!string.IsNullOrEmpty(bearerToken))
                    req.SetRequestHeader("Authorization", "Bearer " + bearerToken);

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
                    Debug.LogError($"[GameBull] POST failed {url} → {req.responseCode} {req.error}\n{req.downloadHandler.text}");
                    return null;
                }
                return req.downloadHandler.text;
            }
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
