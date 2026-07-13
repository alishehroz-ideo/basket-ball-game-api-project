// GameBullBoot.cs
// Reads the boot info the host page puts in the URL, automatically on game start,
// and stores it so the rest of the SDK can use it. This is step 1 of the flow.

using System.Collections.Generic;
using UnityEngine;

namespace GameBull
{
    public static class GameBullBoot
    {
        // The values read from the URL. Other scripts read these.
        public static string TenantSlug   { get; private set; }
        public static string SessionToken { get; private set; }
        public static string Seed         { get; private set; }
        public static string Mode         { get; private set; }
        public static string GameId       { get; private set; }
        public static string ReturnUrl    { get; private set; }
        public static string RoomId       { get; private set; }
        public static string PlayerId     { get; private set; }
        public static string OpenTournamentId { get; private set; }

        public static bool Ready { get; private set; }

        // Runs automatically when the game starts — no need to call it or place anything in a scene.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            string url = Application.absoluteURL;   // full page URL incl. ?params (works on WebGL)
            var q = ParseQuery(url);

            TenantSlug       = Get(q, "tenantSlug");
            SessionToken     = Get(q, "sessionToken");
            Seed             = Get(q, "seed");
            Mode             = Get(q, "mode");
            GameId           = Get(q, "gameId");
            ReturnUrl        = Get(q, "returnUrl");
            RoomId           = Get(q, "roomId");
            PlayerId         = Get(q, "playerId");
            OpenTournamentId = Get(q, "openTournamentId");

            Ready = true;

            Debug.Log(
                "[GameBull] Boot params read:\n" +
                $"  tenantSlug={TenantSlug}\n" +
                $"  sessionToken={(string.IsNullOrEmpty(SessionToken) ? "(none)" : SessionToken.Substring(0, Mathf.Min(12, SessionToken.Length)) + "…")}\n" +
                $"  seed={Seed}\n  mode={Mode}\n  gameId={GameId}\n  returnUrl={ReturnUrl}\n" +
                $"  roomId={RoomId}\n  playerId={PlayerId}\n  openTournamentId={OpenTournamentId}");
        }

        private static string Get(Dictionary<string, string> q, string key)
            => q.TryGetValue(key, out var v) ? v : null;

        // Minimal query-string parser (no external libs). Handles ?a=1&b=2 and URL-decoding.
        private static Dictionary<string, string> ParseQuery(string url)
        {
            var result = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(url)) return result;

            int qIndex = url.IndexOf('?');
            if (qIndex < 0 || qIndex == url.Length - 1) return result;

            string query = url.Substring(qIndex + 1);
            foreach (string pair in query.Split('&'))
            {
                if (string.IsNullOrEmpty(pair)) continue;
                int eq = pair.IndexOf('=');
                if (eq < 0) { result[UnityWebRequestUnescape(pair)] = ""; continue; }
                string key = UnityWebRequestUnescape(pair.Substring(0, eq));
                string val = UnityWebRequestUnescape(pair.Substring(eq + 1));
                result[key] = val;
            }
            return result;
        }

        private static string UnityWebRequestUnescape(string s)
            => UnityEngine.Networking.UnityWebRequest.UnEscapeURL(s);
    }
}
