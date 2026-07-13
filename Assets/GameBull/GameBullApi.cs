// GameBullApi.cs
// The "front door" — clean named methods the game calls.
// Each method uses GameBullClient (the fetcher) and parses JSON with Newtonsoft.
// The game NEVER touches URLs or UnityWebRequest — it just calls these.

using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace GameBull
{
    // ---------- Response shapes (what the server sends back) ----------

    // ---- /context response (the one boot call) ----

    public class GameContext
    {
        public User                 user;
        public Lives                lives;
        public int                  points;
        public Customization        customization;
        public string[]             enabledModes;
        public ActiveOpenTournament activeOpenTournament;   // null if none
        public Room                 room;                   // null unless joiner (URL had roomId)
    }

    public class User
    {
        public string displayName;
        public string globalUserId;
    }

    public class Lives
    {
        public int    count;
        public int    max;
        public string nextRefillAt;            // nullable ISO timestamp
        public int    refillIntervalSeconds;
    }

    public class Customization
    {
        public Colors  colors;
        public string  logoUrl;
        public Asset[] assets;
    }

    public class Colors
    {
        public string primary;
        public string secondary;
    }

    public class Asset
    {
        public string key;
        public string url;
        public string mimeType;
        public int    bytes;
    }

    public class ActiveOpenTournament
    {
        public string id;
        public string name;
        public string endsAt;
        public int    participantCount;
    }

    public class RoomPlayer
    {
        public string playerId;
        public string nickname;
        public int    score;
        public string finishedAt;
    }

    public class Room
    {
        public string       id;
        public string       name;
        public string       state;
        public string       expiresAt;
        public RoomPlayer[] players;     // players already in the room (objects, not strings)
    }

    public class ScoreResult
    {
        public string scoreId;
        public int    value;       // solo + tournament responses use "value"
        public int    score;       // room responses use "score"
        public bool   recorded;
        public string submittedAt;
        public string finishedAt;  // room responses include finishedAt

        // Whichever the endpoint populated (room sets score, solo/tournament set value)
        public int effectiveValue => value != 0 ? value : score;
    }

    // POST /play/plays response (v3.1) — mint a play token before the round (spends 1 life).
    [System.Serializable] public class PlayResult {
        public string playToken;
        public string seed;
        public string mode;
        public string expiresAt;
    }

    public class RoomLeaderboard
    {
        public LeaderboardItem[] items;
        public bool   hasFinalScores;
        public string state;
        public string expiresAt;
    }

    public class LeaderboardItem
    {
        public string playerId;
        public string nickname;
        public int    score;
        public int    rank;
        public string finishedAt;   // null/empty if still playing
    }

    // Returned by POST /play/rooms (host creates a room)
    public class CreateRoomResult
    {
        public string roomId;
        public string roomName;
        public string joinUrl;
        public string seed;
        public string hostPlayerId;
        public string hostNickname;
        public int    maxPlayers;
        public string expiresAt;
    }

    // Returned by POST /play/rooms/:roomId/join (friend joins)
    public class JoinRoomResult
    {
        public string playerId;
        public string nickname;
        public string seed;
        public string expiresAt;
    }

    // ---------- Open Tournament API response shapes (match Swagger schema) ----------

    [System.Serializable] public class OpenTournament {
        public string id;
        public string tenantId;
        public string gameId;
        public string name;
        public string description;
        public string startsAt;
        public string endsAt;
        public int?   maxPlayers;
        public PrizeTier[] prizeTiers;
        public string status;
        public int?   participantCount;
        public string createdAt;
    }
    [System.Serializable] public class PrizeTier {
        public int?   rank;
        public string label;
        public string giftId;
    }
    [System.Serializable] public class OpenTournamentList {
        public OpenTournament[] items;
    }
    [System.Serializable] public class TournamentJoinResult {
        public string entryId;
        public string nickname;
        public string joinedAt;
    }
    [System.Serializable] public class TournamentSession {
        public string sessionToken;
        public string seed;
        public string mode;
        public string expiresAt;
    }
    [System.Serializable] public class TournamentScoreResult {
        public string entryId;
        public int    score;
        public bool   improved;
        public int    rank;
        public string finishedAt;
    }
    [System.Serializable] public class TournamentLeaderboard {
        public TournamentLeaderboardItem[] items;
        public string status;
        public int    participantCount;
        public string startsAt;
        public string endsAt;
    }
    [System.Serializable] public class TournamentLeaderboardItem {
        public string entryId;
        public string nickname;
        public int    score;
        public int    rank;
        public string finishedAt;
    }

    // ---------- Global Tournament API response shapes (NEW /game/global-tournaments) ----------

    [System.Serializable] public class GlobalTournament {
        public string id;
        public string gameId;
        public string name;
        public string description;
        public string startsAt;
        public string endsAt;
        public string prizeModel;                                              // e.g. "TENANT_GB_POINTS" | "GLORY_ONLY"
        public System.Collections.Generic.Dictionary<string,int> prizeConfig;  // e.g. {"1":100,"2":50,"3":29}; empty for GLORY_ONLY
        public string status;
        public string finalizedAt;
        public int    participantCount;
    }
    [System.Serializable] public class GlobalTournamentList {
        public GlobalTournament[] items;
    }
    // Leaderboard shape differs from the old one: (rank, alias, score, isMe) — no entryId/finishedAt.
    // rank/score are nullable: the server sends null for players who haven't scored yet.
    [System.Serializable] public class GlobalLeaderboardItem {
        public int?   rank;
        public string alias;
        public int?   score;
        public bool   isMe;
    }
    [System.Serializable] public class GlobalLeaderboard {
        public GlobalLeaderboardItem[] items;
    }
    // GET /game/global-tournaments/my-rank — caller's own standing. rank/score are null until the player scores.
    [System.Serializable] public class GlobalMyRank {
        public int?   rank;
        public string alias;
        public int?   score;
        public bool   isMe;
    }

    // ---------- My history (/me/rooms) response shapes (match live schema) ----------

    [System.Serializable] public class MyRoom {
        public string roomId;
        public string roomName;
        public string gameId;
        public string state;
        public string expiresAt;
        public string createdAt;
        public string hostNickname;
        public int    playerCount;
        public bool   isHost;
        public string myPlayerId;
        public int    myBestScore;
    }
    [System.Serializable] public class MyRoomsList {
        public MyRoom[] items;
        public string nextCursor;
    }

    // ---------- The API methods ----------

    public static class GameBullApi
    {
        // v3.1 two-tier play: the play token minted by MintPlay(), used to submit the solo score.
        public static string CurrentPlayToken;
        public static string CurrentSeed;
        // v3.1 global tournaments: token from StartGlobalTournamentSession — used for the tournament
        // score submit and the leaderboard/my-rank reads (NOT the boot token).
        public static string CurrentGlobalToken;

        // GET the per-tenant context (colors / logo / assets).
        // Uses the gameId + tenantSlug + sessionToken that GameBullBoot read from the URL.
        public static async Task<GameContext> GetContext()
        {
            string url = GameBullEndpoints.GameContext(GameBullBoot.GameId, GameBullBoot.TenantSlug, GameBullBoot.RoomId);
            string json = await GameBullClient.GetJson(url, GameBullBoot.SessionToken);
            if (string.IsNullOrEmpty(json)) return null;
            var result = JsonConvert.DeserializeObject<GameContext>(json);
            Debug.Log($"[LIVES] GetContext url={url} → server lives.count={result?.lives?.count}");
            return result;
        }

        // GET the public room leaderboard (no auth needed — leaderboard is public).
        public static async Task<RoomLeaderboard> GetRoomLeaderboard(string roomId)
        {
            string url  = $"{GameBullEndpoints.BaseUrl}/play/rooms/{roomId}/leaderboard";
            string json = await GameBullClient.GetJson(url, null);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<RoomLeaderboard>(json);
        }

        // POST a solo score (mode HEAD_TO_HEAD_1V1). Token goes in the BODY for GameBull.
        // v3.1: the token is the PLAY token (from MintPlay), NOT the boot token.
        public static async Task<ScoreResult> SubmitSoloScore(int score, int playTimeMs)
        {
            string url  = GameBullEndpoints.SoloScore();
            string body = JsonConvert.SerializeObject(new {
                sessionToken = CurrentPlayToken,   // v3.1: score with the PLAY token, not the boot token
                value        = score,
                playTimeMs   = playTimeMs
            });
            string json = await GameBullClient.PostJson(url, body);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<ScoreResult>(json);
        }

        // v3.1: mint a play token (POST /play/plays) BEFORE the round. Spends exactly 1 life.
        // Token goes in the BODY as bootToken (no Bearer), same style as SubmitSoloScore.
        // Returns null on any failure (incl. play.no_lives) — caller must NOT start the game.
        public static async Task<PlayResult> MintPlay()
        {
            string url  = GameBullEndpoints.Plays();
            string body = JsonConvert.SerializeObject(new { bootToken = GameBullBoot.SessionToken });
            string json = await GameBullClient.PostJson(url, body);   // 2-arg, no Bearer
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<PlayResult>(json); }
            catch (System.Exception e) { Debug.LogWarning("[GameBull] Play mint parse failed: " + e.Message + "\n" + json); return null; }
        }

        // POST a 4-player room score (mode TOURNAMENT_ROOM). Uses playerId from the URL.
        public static async Task<ScoreResult> SubmitRoomScore(int score, int playTimeMs)
        {
            string url  = GameBullEndpoints.RoomScore(GameBullBoot.RoomId);
            string body = JsonConvert.SerializeObject(new {
                playerId   = GameBullBoot.PlayerId,
                score      = score,
                playTimeMs = playTimeMs
            });
            string json = await GameBullClient.PostJson(url, body);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<ScoreResult>(json);
        }

        // Overload: explicit roomId + playerId (used when the lobby remembers them itself
        // instead of reading from the URL).
        public static async Task<ScoreResult> SubmitRoomScore(string roomId, string playerId, int score, int playTimeMs)
        {
            string url  = GameBullEndpoints.RoomScore(roomId);
            string body = JsonConvert.SerializeObject(new { playerId = playerId, score = score, playTimeMs = playTimeMs });
            string json = await GameBullClient.PostJson(url, body);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<ScoreResult>(json);
        }

        // POST an open-tournament score (mode OPEN_TOURNAMENT). Token in body.
        public static async Task<ScoreResult> SubmitTournamentScore(int score, int playTimeMs)
        {
            string url  = GameBullEndpoints.TournamentScore(GameBullBoot.OpenTournamentId);
            string body = JsonConvert.SerializeObject(new {
                sessionToken = GameBullBoot.SessionToken,
                value        = score,
                playTimeMs   = playTimeMs
            });
            string json = await GameBullClient.PostJson(url, body);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<ScoreResult>(json);
        }

        // HOST: create a 4-player tournament room. Returns roomId + joinUrl (share link) + seed + hostPlayerId.
        public static async Task<CreateRoomResult> CreateRoom(string roomName, int expiresInSeconds, string hostNickname)
        {
            string url  = GameBullEndpoints.CreateRoom();
            string body = JsonConvert.SerializeObject(new {
                gameId           = GameBullBoot.GameId,
                roomName         = roomName,
                expiresInSeconds = expiresInSeconds,
                hostNickname     = hostNickname
            });
            string json = await GameBullClient.PostJson(url, body, GameBullBoot.SessionToken);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<CreateRoomResult>(json);
        }

        // JOINER: join a room by id (the friend's game has roomId from the URL). Returns this player's playerId + seed.
        public static async Task<JoinRoomResult> JoinRoom(string roomId, string nickname)
        {
            string url  = GameBullEndpoints.JoinRoom(roomId);
            string body = JsonConvert.SerializeObject(new {
                nickname = nickname
            });
            string json = await GameBullClient.PostJson(url, body, GameBullBoot.SessionToken);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<JoinRoomResult>(json);
        }

        // ---------- Open Tournament API ----------

        public static async Task<OpenTournamentList> ListTournaments()
        {
            string url = GameBullEndpoints.TournamentList(GameBullBoot.TenantSlug);
            string json = await GameBullClient.GetJson(url, GameBullBoot.SessionToken);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<OpenTournamentList>(json);
        }

        public static async Task<TournamentJoinResult> JoinTournament(string id, string nickname)
        {
            string url = GameBullEndpoints.TournamentJoin(id);
            string body = JsonConvert.SerializeObject(new { nickname = nickname });
            string json = await GameBullClient.PostJson(url, body, GameBullBoot.SessionToken);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<TournamentJoinResult>(json);
        }

        public static async Task<TournamentSession> StartTournamentSession(string id)
        {
            string url = GameBullEndpoints.TournamentSession(id);
            string json = await GameBullClient.PostJson(url, "{}", GameBullBoot.SessionToken);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<TournamentSession>(json);
        }

        // NOTE: score body uses "value" (NOT "score"); response uses "score". Uses the tournament session token.
        public static async Task<TournamentScoreResult> SubmitTournamentScoreV2(string id, string tournamentSessionToken, int value, int playTimeMs)
        {
            string url = GameBullEndpoints.TournamentScore(id);
            string body = JsonConvert.SerializeObject(new { sessionToken = tournamentSessionToken, value = value, playTimeMs = playTimeMs });
            string json = await GameBullClient.PostJson(url, body);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<TournamentScoreResult>(json);
        }

        public static async Task<TournamentLeaderboard> GetTournamentLeaderboard(string id)
        {
            string url = GameBullEndpoints.TournamentLeaderboard(id);
            string json = await GameBullClient.GetJson(url, null);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<TournamentLeaderboard>(json);
        }

        // ---------- Global Tournament API (v3.1 two-tier) ----------
        // LIST uses the boot token. SESSION mint (spends 1 life) returns the GLOBAL token, which then
        // authenticates the score submit + leaderboard/my-rank reads.

        // LIST stays on the boot token (correct as-is).
        public static async Task<GlobalTournamentList> ListGlobalTournaments(string phase = "active")
        {
            string url  = GameBullEndpoints.GlobalTournamentList(phase);
            string json = await GameBullClient.GetJson(url, GameBullBoot.SessionToken);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<GlobalTournamentList>(json);
        }

        // v3.1: mint a global-tournament session (POST /play/global-tournaments/{id}/sessions). SPENDS 1 LIFE.
        // bootToken is sent as the Bearer AUTH header (confirmed via curl), empty JSON body. On success,
        // stores the returned token as CurrentGlobalToken (used by score/leaderboard/my-rank) + the seed.
        // Returns null on failure (incl. play.no_lives) — caller must NOT start the game.
        public static async Task<TournamentSession> StartGlobalTournamentSession(string id)
        {
            Debug.Log("[LIVES] mint session — about to spend a life");
            string url  = GameBullEndpoints.GlobalTournamentSession(id);
            string json = await GameBullClient.PostJson(url, "{}", GameBullBoot.SessionToken);   // bootToken as Bearer header
            Debug.Log($"[LIVES] mint returned (session null? {string.IsNullOrEmpty(json)}) — a life should now be spent server-side");
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                var sess = JsonConvert.DeserializeObject<TournamentSession>(json);
                if (sess != null && !string.IsNullOrEmpty(sess.sessionToken))
                {
                    CurrentGlobalToken = sess.sessionToken;
                    CurrentSeed        = sess.seed;
                }
                return sess;
            }
            catch (System.Exception e) { Debug.LogWarning("[GameBull] Global session parse failed: " + e.Message + "\n" + json); return null; }
        }

        // POST the final score to /play/global-tournaments/{id}/scores. Token goes in the BODY as
        // sessionToken = the GLOBAL token from StartGlobalTournamentSession (NOT the boot/play token). No Bearer.
        public static async Task<TournamentScoreResult> SubmitGlobalTournamentScore(string tournamentId, int value, int playTimeMs)
        {
            string url  = GameBullEndpoints.GlobalTournamentScore(tournamentId);
            string body = JsonConvert.SerializeObject(new { sessionToken = CurrentGlobalToken, value = value, playTimeMs = playTimeMs });
            string json = await GameBullClient.PostJson(url, body);   // token in body, no Bearer
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<TournamentScoreResult>(json); }
            catch (System.Exception e) { Debug.LogWarning("[GameBull] Score reply parse failed: " + e.Message); return null; }
        }

        // GET the leaderboard for the token's tournament (no id — server resolves it from the token).
        // in-game passes the global (session) token; My History passes useBootToken:true (boot token, no life cost).
        public static async Task<GlobalLeaderboard> GetGlobalLeaderboard(bool useBootToken = false)
        {
            string url  = GameBullEndpoints.GlobalTournamentLeaderboard();
            string json = await GameBullClient.GetJson(url, useBootToken ? GameBullBoot.SessionToken : CurrentGlobalToken);
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<GlobalLeaderboard>(json); }
            catch (System.Exception e) { Debug.LogWarning("[GameBull] Leaderboard parse failed: " + e.Message + "\n" + json); return null; }
        }

        // GET a SPECIFIC tournament's board by id (boot token, no life cost). Used by My History for
        // both active and past tournaments — /{id}/leaderboard works for any published/ended one.
        public static async Task<GlobalLeaderboard> GetGlobalLeaderboardById(string id)
        {
            string url  = GameBullEndpoints.GlobalTournamentLeaderboardById(id);
            string json = await GameBullClient.GetJson(url, GameBullBoot.SessionToken);   // BOOT token
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<GlobalLeaderboard>(json); }
            catch (System.Exception e) { Debug.LogWarning("[GameBull] Leaderboard-by-id parse failed: " + e.Message + "\n" + json); return null; }
        }

        // GET the caller's own rank for the global token's tournament (rank/score null until they score).
        public static async Task<GlobalMyRank> GetGlobalMyRank()
        {
            string url  = GameBullEndpoints.GlobalTournamentMyRank();
            string json = await GameBullClient.GetJson(url, CurrentGlobalToken);   // Bearer = GLOBAL token
            if (string.IsNullOrEmpty(json)) return null;
            try { return JsonConvert.DeserializeObject<GlobalMyRank>(json); }
            catch (System.Exception e) { Debug.LogWarning("[GameBull] My-rank parse failed: " + e.Message + "\n" + json); return null; }
        }

        // GET the current user's room history (needs the Bearer session token).
        public static async Task<MyRoomsList> GetMyRooms()
        {
            string url = GameBullEndpoints.MyRooms();
            string json = await GameBullClient.GetJson(url, GameBullBoot.SessionToken);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<MyRoomsList>(json);
        }
    }
}
