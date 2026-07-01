// GameBullEndpoints.cs
// One place for every GameBull URL. If the base URL ever changes,
// you change it HERE once and the whole SDK follows.

namespace GameBull
{
    public static class GameBullEndpoints
    {
        // The live staging server. (Swap to http://localhost:3000 for local backend testing.)
        public const string BaseUrl = "https://api-gamebull.hyperfunded.pro";

        // --- Branding / context ---
        // NOTE: the old public ".../config" is deprecated; the new one is ".../context" and needs the token.
        // {0} = gameId, {1} = tenantSlug, optional roomId (joiner path)
        public static string GameContext(string gameId, string tenantSlug, string roomId = null)
        {
            string url = $"{BaseUrl}/play/games/{gameId}/context?tenantSlug={tenantSlug}";
            if (!string.IsNullOrEmpty(roomId))
                url += $"&roomId={roomId}";
            return url;
        }

        // --- Score submission (one per mode) ---
        public static string SoloScore()
            => $"{BaseUrl}/play/scores";

        public static string RoomScore(string roomId)
            => $"{BaseUrl}/play/rooms/{roomId}/scores";

        public static string TournamentScore(string tournamentId)
            => $"{BaseUrl}/play/tournaments/{tournamentId}/scores";

        // --- Tournament rooms ---
        public static string CreateRoom()
            => $"{BaseUrl}/play/rooms";

        public static string JoinRoom(string roomId)
            => $"{BaseUrl}/play/rooms/{roomId}/join";

        // --- Open tournaments (public/play API) ---
        // NOTE: TournamentScore(string) already exists above and builds the same
        // ".../play/tournaments/{id}/scores" URL, so it is intentionally NOT duplicated here.
        public static string TournamentList(string tenantSlug) => $"{BaseUrl}/tenants/{tenantSlug}/tournaments";
        public static string TournamentDetail(string id)       => $"{BaseUrl}/play/tournaments/{id}";
        public static string TournamentJoin(string id)         => $"{BaseUrl}/play/tournaments/{id}/join";
        public static string TournamentSession(string id)      => $"{BaseUrl}/play/tournaments/{id}/sessions";
        public static string TournamentLeaderboard(string id)  => $"{BaseUrl}/play/tournaments/{id}/leaderboard";

        // --- My history (per-user) ---
        public static string MyRooms() => $"{BaseUrl}/me/rooms";
    }
}
