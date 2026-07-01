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
        // GET the per-tenant context (colors / logo / assets).
        // Uses the gameId + tenantSlug + sessionToken that GameBullBoot read from the URL.
        public static async Task<GameContext> GetContext()
        {
            string url = GameBullEndpoints.GameContext(GameBullBoot.GameId, GameBullBoot.TenantSlug, GameBullBoot.RoomId);
            string json = await GameBullClient.GetJson(url, GameBullBoot.SessionToken);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<GameContext>(json);
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
        public static async Task<ScoreResult> SubmitSoloScore(int score, int playTimeMs)
        {
            string url  = GameBullEndpoints.SoloScore();
            string body = JsonConvert.SerializeObject(new {
                sessionToken = GameBullBoot.SessionToken,
                value        = score,
                playTimeMs   = playTimeMs
            });
            string json = await GameBullClient.PostJson(url, body);
            if (string.IsNullOrEmpty(json)) return null;
            return JsonConvert.DeserializeObject<ScoreResult>(json);
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
