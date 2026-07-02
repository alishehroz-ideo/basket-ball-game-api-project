using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

namespace GameBull
{
    // Lives on the GameBullLobby root. Wires the mode-picker panel to real /context data at runtime.
    public class GameBullLobbyController : MonoBehaviour
    {
        public const int BUILD_VERSION = 20;
        private static int _loadContextCalls;
        private static bool _joinPanelShownOnce = false;

        // Single-instance reference; assigned in Awake().
        public static GameBullLobbyController Instance { get; private set; }

        // Downloaded partner ball image (from context customization.assets, key="ball"); applied by BallManager.
        public static Sprite PartnerBallSprite;

        // Downloaded partner background image (from context customization.assets, key="background"); applied to gameplay BG.
        public static Sprite PartnerBackgroundSprite;

        // True while the current run is a room/tournament game (vs. solo).
        public bool IsRoomGame => _playMode == PlayMode.Room;

        // True while the current run is an OPEN TOURNAMENT game (vs. solo/room).
        public bool IsTournamentGame => _playMode == PlayMode.Tournament;

        // Browser clipboard copy (WebGL only) — implemented in Plugins/GameBullClipboard.jslib.
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern void GameBullCopyToClipboard(string str);
#endif

        [Header("Panels")]
        public GameObject modePickerPanel;

        [Header("Mode picker texts")]
        public TextMeshProUGUI greetingText;
        public TextMeshProUGUI livesText;

        [Header("Mode buttons")]
        public Button soloButton;
        public Button tournamentButton;

        [Header("Game hooks")]
        public UnityEvent onStartSolo;
        public GameObject createRoomPanel;   // shown when Tournament is tapped (may be null until that panel exists)

        [Header("Create-room panel")]
        public TMPro.TMP_InputField nameInput;
        public UnityEngine.UI.Button expiry30Button;
        public UnityEngine.UI.Button expiry60Button;
        public UnityEngine.UI.Button createRoomButton;
        public UnityEngine.UI.Button createRoomBackButton;

        [Header("Share panel")]
        public GameObject sharePanel;
        public TMPro.TextMeshProUGUI roomNameText;
        public TMPro.TextMeshProUGUI joinUrlText;
        public UnityEngine.UI.Button copyButton;
        public TMPro.TextMeshProUGUI playersText;
        public UnityEngine.UI.Button playAsHostButton;
        public UnityEngine.UI.Button shareBackButton;

        [Header("Join panel")]
        public GameObject joinPanel;
        public TMPro.TextMeshProUGUI joinRoomNameText;
        public TMPro.TextMeshProUGUI joinPlayersText;
        public TMPro.TMP_InputField nicknameInput;
        public UnityEngine.UI.Button joinButton;

        [Header("Leaderboard report card")]
        public GameObject leaderboardPanel;       // the panel root (starts inactive)
        public Transform  leaderboardListRoot;    // ScrollView Content transform (rows go here)
        public GameObject leaderboardRowTemplate; // a row prefab with 3 TMP texts: rank, name, score
        public UnityEngine.UI.Button leaderboardMenuButton;
        public UnityEngine.UI.Button leaderboardRefreshButton;        // on the room report card

        [Header("Open Tournament - List panel")]
        public GameObject tournamentListPanel;        // starts inactive
        public Transform  tournamentListRoot;         // ScrollView Content
        public GameObject tournamentCardTemplate;     // a card with: name text, info text (time+players), Join button — INACTIVE template
        public UnityEngine.UI.Button tournamentListBackButton;

        [Header("Open Tournament - Score card panel")]
        public GameObject tournamentScorePanel;       // starts inactive
        public TMPro.TMP_Text tournamentScoreHeader;  // "Your score: X" + best/rank line
        public Transform  tournamentLbRoot;           // ScrollView Content for leaderboard rows
        public GameObject tournamentLbRowTemplate;    // row with 3 TMP texts: rank, name, score — INACTIVE template
        public UnityEngine.UI.Button tournamentPlayAgainButton;
        public UnityEngine.UI.Button tournamentMenuButton;
        public UnityEngine.UI.Button tournamentRefreshButton;         // on the tournament score card

        [Header("Open Tournament - Mode picker entry")]
        public UnityEngine.UI.Button openTournamentButton;   // "Join Open Tournaments" on the mode picker

        [Header("My History - parent + tabs")]
        public GameObject historyPanel;                 // parent, starts inactive
        public UnityEngine.UI.Button historyMenuButton; // "My History" button on the mode picker
        public UnityEngine.UI.Button tabRoomsButton;    // tab: Private Rooms
        public UnityEngine.UI.Button tabTournamentsButton; // tab: Open Tournaments
        public GameObject historyBackButton;            // Back (closes history -> mode picker); GameObject so we can use a Button on it

        [Header("My History - Rooms tab")]
        public GameObject historyRoomsTab;              // container shown when Rooms tab active
        public Transform  historyRoomsListRoot;         // ScrollView Content
        public GameObject historyRoomCardTemplate;      // INACTIVE card: name text, info text (score/players/state), a "View" button

        [Header("My History - Tournaments tab")]
        public GameObject historyTournamentsTab;        // container shown when Tournaments tab active
        public TMPro.TMP_Text historyTournamentsPlaceholder; // shows "No tournament history yet"

        [Header("My History - room detail (leaderboard from history)")]
        public GameObject historyRoomDetailPanel;       // shows a room's leaderboard, separate from the post-game card
        public Transform  historyRoomDetailListRoot;    // ScrollView Content for rows
        public GameObject historyRoomDetailRowTemplate; // INACTIVE row: rank, name, score
        public UnityEngine.UI.Button historyRoomDetailBackButton; // Back -> returns to the Rooms list (history)

        [Header("Scene control")]
        public string lobbySceneName = "home";   // scene where the lobby should show; empty = any non-gameplay scene
        public string gameplaySceneName = "main";   // the scene to load when a mode starts
        public string splashSceneName = "AdController";
        public string backgroundObjectName = "background";   // SpriteRenderer in main, tinted to primary
        public string splashLogoObjectName = "logo";          // UI Image on AdController, swapped to logoUrl

        [Header("Splash logo (drag the Title Image here)")]
        public UnityEngine.UI.Image splashLogoImage;

        [Header("Primary color targets (drag card GameObjects with Image here)")]
        public GameObject[] primaryColorTargets;

        // Filled from /context so other panels/logic can read it.
        public GameContext Context { get; private set; }

        private int _expirySeconds = 1800;   // default 30 min
        private CreateRoomResult _currentRoom;

        // Remembers the chosen mode + room identity so SubmitScore() can route correctly
        // after the gameplay scene loads (this controller persists via DontDestroyOnLoad).
        private enum PlayMode { None, Solo, Room, Tournament }
        private PlayMode _playMode = PlayMode.None;
        private string _roomId;
        private string _playerId;

        // Current open-tournament identity (persists via DontDestroyOnLoad into gameplay).
        private string _tournamentId;
        private string _tournamentSessionToken;
        private string _tournamentEntryId;

        void Awake()
        {
            Debug.Log("[GB-VERSION] Build v" + BUILD_VERSION);
            // Single-instance guard: if one already exists, destroy this duplicate.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Persist across scene loads (home -> main -> home) so the chosen mode/state survives.
            DontDestroyOnLoad(gameObject);

            // Re-show the mode picker whenever we (re)enter the lobby/home scene.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (Instance == this) Instance = null;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Debug.Log("[GB-LOG] OnSceneLoaded: scene=" + scene.name + " | lobbySceneName=" + lobbySceneName);

            // The GameBull canvas is born in the splash scene and persists everywhere, so gate
            // by scene: hide everything first, then apply only what this scene needs.
            HideAllPanels();
            if (scene.name == lobbySceneName)
            {
                if (string.IsNullOrEmpty(GameBull.GameBullBoot.RoomId) || _joinPanelShownOnce)
                {
                    Debug.Log("[GB-LOG] -> lobby scene, calling ShowLobbyHome() (joiner return=" + _joinPanelShownOnce + ")");
                    ShowLobbyHome();
                }
                else Debug.Log("[GB-LOG] -> lobby scene, fresh joiner — skipping ShowLobbyHome, letting LoadContext show join panel");
                ApplySplashLogo();
            }
            else if (scene.name == splashSceneName)
            {
                Debug.Log("[GB-LOG] -> splash scene, applying partner logo");
                ApplySplashLogo();
            }
            else if (scene.name == gameplaySceneName)
            {
                Debug.Log("[GB-LOG] -> gameplay scene, applying background color");
                ApplyBackgroundColor();
            }
            else Debug.Log("[GB-LOG] -> other scene, panels stay hidden");
        }

        // Hide every GameBull panel. Used when entering any scene before applying per-scene UI.
        private void HideAllPanels()
        {
            Debug.Log("[GB-HIDE] HideAllPanels CALLED by:\n" + System.Environment.StackTrace);
            if (modePickerPanel)        modePickerPanel.SetActive(false);
            if (createRoomPanel)        createRoomPanel.SetActive(false);
            if (sharePanel)             sharePanel.SetActive(false);
            if (joinPanel)              joinPanel.SetActive(false);
            if (leaderboardPanel)       leaderboardPanel.SetActive(false);
            if (tournamentListPanel)    tournamentListPanel.SetActive(false);
            if (tournamentScorePanel)   tournamentScorePanel.SetActive(false);
            if (historyPanel)           historyPanel.SetActive(false);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(false);
        }

        // Splash scene: keep the logo Image OFF until the partner logo successfully downloads,
        // so no default/placeholder ever shows. Only a successful download turns it on.
        // Uses the serialized splashLogoImage (dragged in the editor) instead of GameObject.Find.
        private async void ApplySplashLogo()
        {
            if (splashLogoImage == null) { Debug.Log("[GB-LOG] splashLogoImage not assigned"); return; }
            splashLogoImage.gameObject.SetActive(false);   // hide until partner logo loads

            if (Context == null || Context.customization == null) return;
            string url = Context.customization.logoUrl;
            if (string.IsNullOrEmpty(url)) { Debug.Log("[GB-LOG] no logoUrl, logo stays off"); return; }

            using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await System.Threading.Tasks.Task.Yield();

                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
                    splashLogoImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    splashLogoImage.gameObject.SetActive(true);
                    Debug.Log("[GB-LOG] splash logo set");
                }
                else Debug.Log("[GB-LOG] logo download failed");
            }
        }

        // Gameplay scene: apply partner background image (if uploaded) or tint with primary color as fallback.
        // Polls up to 5s for PartnerBackgroundSprite to finish downloading before falling back to color tint.
        private void ApplyBackgroundColor()
        {
            StartCoroutine(ApplyBackgroundCoroutine());
        }

        private System.Collections.IEnumerator ApplyBackgroundCoroutine()
        {
            float t = 0f;
            while (PartnerBackgroundSprite == null && t < 5f)
            {
                t += Time.deltaTime;
                yield return null;
            }

            var bgGO = GameObject.Find(backgroundObjectName);
            if (bgGO == null) { Debug.Log("[GB-LOG] BG object not found: " + backgroundObjectName); yield break; }

            string hex = (Context?.customization?.colors != null) ? Context.customization.colors.primary : null;
            Color primaryColor = Color.white;
            bool hasPrimary = !string.IsNullOrEmpty(hex) && UnityEngine.ColorUtility.TryParseHtmlString(hex, out primaryColor);

            var img = bgGO.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                if (PartnerBackgroundSprite != null)
                {
                    img.sprite = PartnerBackgroundSprite;
                    img.color  = Color.white;
                    Debug.Log("[GB-LOG] BG image replaced with custom asset (UI Image)");
                }
                else if (hasPrimary)
                {
                    img.color = primaryColor;
                    Debug.Log("[GB-LOG] BG (UI Image) tinted " + hex + " (no bg asset)");
                }
                yield break;
            }

            var sr = bgGO.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (PartnerBackgroundSprite != null)
                {
                    sr.sprite = PartnerBackgroundSprite;
                    sr.color  = Color.white;
                    Debug.Log("[GB-LOG] BG image replaced with custom asset (SpriteRenderer)");
                }
                else if (hasPrimary)
                {
                    sr.color = primaryColor;
                    Debug.Log("[GB-LOG] BG (SpriteRenderer) tinted " + hex + " (no bg asset)");
                }
                yield break;
            }

            Debug.Log("[GB-LOG] BG has no Image or SpriteRenderer");
        }

        // Tint every GameObject in primaryColorTargets (if it has an Image) with the partner primary color.
        private void ApplyPrimaryColorTargets()
        {
            if (Context == null || Context.customization == null || Context.customization.colors == null) return;
            string hex = Context.customization.colors.primary;
            if (string.IsNullOrEmpty(hex)) return;
            if (!UnityEngine.ColorUtility.TryParseHtmlString(hex, out Color c)) return;
            if (primaryColorTargets == null || primaryColorTargets.Length == 0) return;

            int applied = 0;
            foreach (var go in primaryColorTargets)
            {
                if (go == null) continue;
                var img = go.GetComponent<UnityEngine.UI.Image>();
                if (img == null) continue;
                img.color = c;
                applied++;
            }
            Debug.Log("[GB-LOG] Primary color applied to " + applied + " targets");
        }

        // Find the partner's custom "ball" asset in context and download it into PartnerBallSprite,
        // which BallManager applies to the in-game ball SpriteRenderer.
        private async void LoadPartnerBallAsset()
        {
            if (Context == null || Context.customization == null || Context.customization.assets == null) return;
            string url = null;
            foreach (var a in Context.customization.assets)
                if (a != null && a.key == "ball") { url = a.url; break; }
            if (string.IsNullOrEmpty(url)) { Debug.Log("[GB-LOG] No 'ball' asset."); return; }

            using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await System.Threading.Tasks.Task.Yield();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var tex = ((UnityEngine.Networking.DownloadHandlerTexture)req.downloadHandler).texture;
                    PartnerBallSprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f), 100f);
                    Debug.Log("[GB-LOG] Partner ball sprite loaded.");
                }
                else Debug.Log("[GB-LOG] Ball asset download failed: " + req.error);
            }
        }

        // Find the partner's custom "background" asset in context and download it into PartnerBackgroundSprite,
        // which ApplyBackgroundCoroutine applies to the gameplay scene background.
        private async void LoadPartnerBackgroundAsset()
        {
            if (Context == null || Context.customization == null || Context.customization.assets == null) return;
            string url = null;
            foreach (var a in Context.customization.assets)
                if (a != null && a.key == "background") { url = a.url; break; }
            if (string.IsNullOrEmpty(url)) { Debug.Log("[GB-LOG] No 'background' asset."); return; }

            using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await System.Threading.Tasks.Task.Yield();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var tex = ((UnityEngine.Networking.DownloadHandlerTexture)req.downloadHandler).texture;
                    PartnerBackgroundSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                    Debug.Log("[GB-LOG] Partner background sprite loaded.");
                }
                else Debug.Log("[GB-LOG] Background asset download failed: " + req.error);
            }
        }

        // Single source of truth for "go to lobby home": force-show the mode picker,
        // hide every other panel, restore the header, re-activate the mode buttons, reset play state.
        public void ShowLobbyHome()
        {
            Debug.Log("[GB-LOG] ShowLobbyHome CALLED. RoomId='" + GameBullBoot.RoomId + "' (joiner=" + (!string.IsNullOrEmpty(GameBullBoot.RoomId)) + ")");
            Debug.Log("[GB-LOG] ShowLobbyHome START");
            Debug.Log("[GB-LOG] modePickerPanel null? " + (modePickerPanel == null));
            if (createRoomPanel)  createRoomPanel.SetActive(false);
            if (sharePanel)       sharePanel.SetActive(false);
            if (joinPanel)        joinPanel.SetActive(false);
            if (leaderboardPanel) leaderboardPanel.SetActive(false);
            if (tournamentListPanel)  tournamentListPanel.SetActive(false);
            if (tournamentScorePanel) tournamentScorePanel.SetActive(false);
            if (historyPanel) historyPanel.SetActive(false);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(false);
            if (modePickerPanel) {
                modePickerPanel.SetActive(true);
                Debug.Log("[GB-LOG] modePickerPanel activeSelf=" + modePickerPanel.activeSelf + " activeInHierarchy=" + modePickerPanel.activeInHierarchy);
            }
            // The mode buttons get SetActive(false) in Start()/LoadContext() based on enabledModes,
            // and those run only once on this persistent controller — so re-activate them here.
            if (soloButton)       { soloButton.gameObject.SetActive(true);       Debug.Log("[GB-LOG] re-activated soloButton"); }
            if (tournamentButton) { tournamentButton.gameObject.SetActive(true); Debug.Log("[GB-LOG] re-activated tournamentButton"); }
            if (openTournamentButton) openTournamentButton.gameObject.SetActive(HasMode("OPEN_TOURNAMENT"));
            if (historyMenuButton) historyMenuButton.gameObject.SetActive(true);   // always available for logged-in users
            Debug.Log("[GB-LOG] soloButton null? " + (soloButton == null) + (soloButton ? " activeSelf=" + soloButton.gameObject.activeSelf : ""));
            Debug.Log("[GB-LOG] tournamentButton null? " + (tournamentButton == null) + (tournamentButton ? " activeSelf=" + tournamentButton.gameObject.activeSelf : ""));
            RenderHeader();   // restore greeting/lives from persisted Context (no-op if null)

            // reset play state so the next game (solo or new room) is clean
            _playMode = PlayMode.None; _roomId = null; _playerId = null;
            Debug.Log("[GB-LOG] ShowLobbyHome END");
        }

        // Scene-return path uses identical logic to the menu button.
        public void ShowModePicker() { ShowLobbyHome(); }

        private void RenderHeader()
        {
            if (Context == null || Context.user == null) return;
            if (greetingText != null)
                greetingText.text = "Hi " + Context.user.displayName;
            if (livesText != null && Context.lives != null)
                livesText.text = "Lives: " + Context.lives.count + "/" + Context.lives.max;
        }

        // Hides the whole lobby UI and loads the gameplay scene itself.
        // Scene loading no longer depends on an Inspector-wired reference (which goes
        // null after a scene reload because the wired object gets destroyed).
        private void StartGameplay()
        {
            // hide all lobby panels so nothing overlays the game
            if (modePickerPanel) modePickerPanel.SetActive(false);
            if (createRoomPanel) createRoomPanel.SetActive(false);
            if (sharePanel)      sharePanel.SetActive(false);
            if (joinPanel)       joinPanel.SetActive(false);

            onStartSolo?.Invoke();   // still fire the optional hook for any extra per-game setup

            if (!string.IsNullOrEmpty(gameplaySceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
        }

        // ---- Score submission ---------------------------------------------------
        // The SINGLE entry point the game calls at game-over. Routes to the correct
        // API based on the mode the player chose in the lobby (persisted via DontDestroyOnLoad).
        public async void SubmitScore(int score, int playTimeMs)
        {
            switch (_playMode)
            {
                case PlayMode.Solo:
                {
                    var r = await GameBullApi.SubmitSoloScore(score, playTimeMs);
                    Debug.Log(r != null
                        ? $"[GameBull] Solo score submitted: {r.value} (recorded={r.recorded})"
                        : "[GameBull] Solo score submit failed.");
                    break;
                }
                case PlayMode.Room:
                {
                    var r = await GameBullApi.SubmitRoomScore(_roomId, _playerId, score, playTimeMs);
                    Debug.Log(r != null
                        ? $"[GameBull] Room score submitted: {r.effectiveValue} (recorded={r.recorded})"
                        : "[GameBull] Room score submit failed.");
                    await ShowLeaderboard(_roomId);
                    return;   // don't RedirectBack; the Menu button handles leaving
                }
                case PlayMode.Tournament:
                {
                    var r = await GameBullApi.SubmitTournamentScoreV2(_tournamentId, _tournamentSessionToken, score, playTimeMs);
                    if (r != null)
                        Debug.Log($"[GameBull] Tournament score: {r.score} improved={r.improved} rank={r.rank}");
                    else
                        Debug.Log("[GameBull] Tournament score submit failed.");
                    if (r != null && !string.IsNullOrEmpty(r.entryId)) _tournamentEntryId = r.entryId;
                    await ShowTournamentScoreCard(r);
                    return;   // don't redirect; Play Again / Menu handle it
                }
                default:
                    Debug.LogWarning("[GameBull] SubmitScore called but no play mode was active — submitting as Solo fallback.");
                    var f = await GameBullApi.SubmitSoloScore(score, playTimeMs);
                    Debug.Log(f != null ? $"[GameBull] Fallback solo submitted: {f.value}" : "[GameBull] Fallback solo failed.");
                    break;
            }
            RedirectBack(score);
        }

        private void RedirectBack(int score)
        {
            string returnUrl = GameBullBoot.ReturnUrl;
            if (string.IsNullOrEmpty(returnUrl)) { Debug.Log("[GameBull] No returnUrl; staying in game."); return; }
            string sep = returnUrl.Contains("?") ? "&" : "?";
            Application.OpenURL($"{returnUrl}{sep}score={score}&result=submitted");
        }

        private async System.Threading.Tasks.Task ShowLeaderboard(string roomId)
        {
            // Clear rows from a previous tournament BEFORE fetching so a stale list never shows.
            // Keep the template, and make sure it is never displayed as a real row.
            if (leaderboardListRoot != null)
            {
                foreach (Transform child in leaderboardListRoot)
                    if (child.gameObject != leaderboardRowTemplate)
                        Destroy(child.gameObject);
            }
            if (leaderboardRowTemplate) leaderboardRowTemplate.SetActive(false);

            var lb = await GameBullApi.GetRoomLeaderboard(roomId);

            // Failed fetch or empty list -> don't show a blank card; fall back to lobby home.
            if (lb == null || lb.items == null || lb.items.Length == 0)
            {
                Debug.Log("[GameBull] No leaderboard data; returning to lobby.");
                ShowLobbyHome();
                return;
            }

            // Populate one row per item; each clone is activated individually.
            if (leaderboardRowTemplate != null && leaderboardListRoot != null)
            {
                foreach (var item in lb.items)
                {
                    GameObject row = Instantiate(leaderboardRowTemplate, leaderboardListRoot);
                    row.SetActive(true);
                    var texts = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    // expect 3 texts in order: rank, name, score
                    if (texts.Length >= 3)
                    {
                        texts[0].text = item.rank > 0 ? item.rank + "." : "-";
                        texts[1].text = string.IsNullOrEmpty(item.nickname) ? "Player" : item.nickname;
                        // still playing if no finishedAt
                        texts[2].text = string.IsNullOrEmpty(item.finishedAt) ? "Playing…" : item.score.ToString();
                    }
                }
            }

            // hide other panels, show the leaderboard
            if (modePickerPanel) modePickerPanel.SetActive(false);
            if (createRoomPanel) createRoomPanel.SetActive(false);
            if (sharePanel)      sharePanel.SetActive(false);
            if (joinPanel)       joinPanel.SetActive(false);
            if (leaderboardPanel) leaderboardPanel.SetActive(true);
        }

        public void OnLeaderboardMenu()
        {
            Debug.Log("[GB-LOG] OnLeaderboardMenu pressed. Loading home scene: " + lobbySceneName);
            if (leaderboardPanel) leaderboardPanel.SetActive(false);
            _playMode = PlayMode.None; _roomId = null; _playerId = null;
            UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }

        // Room card refresh: re-fetch + re-render the standings WITHOUT closing the card.
        // Self-contained (not a call to ShowLeaderboard) on purpose: ShowLeaderboard falls back to
        // ShowLobbyHome() on a transient empty/failed fetch, which would close the card and null _roomId.
        // Mirrors ShowLeaderboard's row rendering so refreshed rows look identical.
        public async void OnLeaderboardRefresh()
        {
            if (string.IsNullOrEmpty(_roomId)) return;

            // clear old rows except template
            if (leaderboardListRoot != null)
                foreach (Transform c in leaderboardListRoot)
                    if (c.gameObject != leaderboardRowTemplate) Destroy(c.gameObject);
            if (leaderboardRowTemplate) leaderboardRowTemplate.SetActive(false);

            var lb = await GameBullApi.GetRoomLeaderboard(_roomId);
            if (lb != null && lb.items != null && leaderboardRowTemplate != null && leaderboardListRoot != null)
            {
                foreach (var item in lb.items)
                {
                    GameObject row = Instantiate(leaderboardRowTemplate, leaderboardListRoot);
                    row.SetActive(true);
                    var texts = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    if (texts.Length >= 3)
                    {
                        texts[0].text = item.rank > 0 ? item.rank + "." : "-";
                        texts[1].text = string.IsNullOrEmpty(item.nickname) ? "Player" : item.nickname;
                        texts[2].text = string.IsNullOrEmpty(item.finishedAt) ? "Playing…" : item.score.ToString();
                    }
                }
            }
        }

        // Tournament card refresh: re-fetch leaderboard and re-render (header stays as-is; just refresh the standings).
        public async void OnTournamentRefresh()
        {
            if (string.IsNullOrEmpty(_tournamentId)) return;

            // clear old rows except template
            if (tournamentLbRoot != null)
                foreach (Transform c in tournamentLbRoot)
                    if (c.gameObject != tournamentLbRowTemplate) Destroy(c.gameObject);
            if (tournamentLbRowTemplate) tournamentLbRowTemplate.SetActive(false);

            var lb = await GameBullApi.GetTournamentLeaderboard(_tournamentId);
            if (lb != null && lb.items != null && tournamentLbRowTemplate != null && tournamentLbRoot != null)
            {
                foreach (var item in lb.items)
                {
                    GameObject row = Instantiate(tournamentLbRowTemplate, tournamentLbRoot);
                    row.SetActive(true);
                    var texts = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    if (texts.Length >= 3)
                    {
                        texts[0].text = item.rank + ".";
                        texts[1].text = string.IsNullOrEmpty(item.nickname) ? "Player" : item.nickname;
                        texts[2].text = item.score.ToString();
                    }
                }
            }
        }

        // Tournament Menu: leave the score card and return to the lobby by loading the home scene
        // (OnSceneLoaded -> ShowLobbyHome then shows the mode picker). Mirrors the room card's
        // OnLeaderboardMenu, which loads the scene rather than only toggling panels over gameplay.
        public void OnTournamentMenu()
        {
            if (tournamentScorePanel) tournamentScorePanel.SetActive(false);
            _playMode = PlayMode.None;
            _roomId = null; _playerId = null;
            _tournamentId = null; _tournamentSessionToken = null; _tournamentEntryId = null;
            UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
        }

        // Recompute the score-card header from the player's own leaderboard row, so a Refresh
        // shows the live best/rank instead of the frozen submit-time values.
        private void RenderTournamentHeaderFromLeaderboard(TournamentLeaderboard lb)
        {
            if (tournamentScoreHeader == null) return;
            if (lb == null || lb.items == null) return;
            TournamentLeaderboardItem mine = null;
            if (!string.IsNullOrEmpty(_tournamentEntryId))
                foreach (var it in lb.items)
                    if (it.entryId == _tournamentEntryId) { mine = it; break; }
            if (mine != null)
                tournamentScoreHeader.text = $"Your best: {mine.score}   Rank #{mine.rank}";
            else
                tournamentScoreHeader.text = "Your score is being ranked…";
        }

        [ContextMenu("Dev Submit Test Score (500)")]
        public void DevSubmitTestScore() => SubmitScore(500, 30000);

        async void Start()
        {
            // A duplicate scheduled for destruction in Awake() must not run Start().
            if (Instance != this) return;

            // hide buttons until we know which modes are enabled
            if (soloButton)       soloButton.gameObject.SetActive(false);
            if (tournamentButton) tournamentButton.gameObject.SetActive(false);
            if (openTournamentButton) openTournamentButton.gameObject.SetActive(false);

            if (greetingText) greetingText.text = "Loading…";
            if (livesText)    livesText.text    = "";

            await LoadContext();

            // Kick off partner asset downloads as soon as context is ready (fire-and-forget).
            LoadPartnerBallAsset();
            LoadPartnerBackgroundAsset();
            ApplyPrimaryColorTargets();

            // Wire button clicks once the lobby is loaded.
            if (soloButton)       soloButton.onClick.AddListener(OnSoloClicked);
            if (tournamentButton) tournamentButton.onClick.AddListener(OnTournamentClicked);

            // Create-room panel buttons.
            if (createRoomBackButton) createRoomBackButton.onClick.AddListener(OnCreateRoomBack);
            if (expiry30Button)       expiry30Button.onClick.AddListener(() => SetExpiry(300));    // 5 min
            if (expiry60Button)       expiry60Button.onClick.AddListener(() => SetExpiry(1800));   // 30 min
            if (createRoomButton)     createRoomButton.onClick.AddListener(OnCreateRoomConfirm);

            // Share panel buttons.
            if (copyButton)        copyButton.onClick.AddListener(OnCopyLink);
            if (shareBackButton)   shareBackButton.onClick.AddListener(OnShareBack);
            if (playAsHostButton)  playAsHostButton.onClick.AddListener(OnPlayAsHost);

            // Join panel button.
            if (joinButton) joinButton.onClick.AddListener(OnJoinConfirm);

            // Leaderboard report-card menu button.
            if (leaderboardMenuButton) leaderboardMenuButton.onClick.AddListener(OnLeaderboardMenu);

            // Open-tournament panels.
            if (openTournamentButton)      openTournamentButton.onClick.AddListener(OnOpenTournamentClicked);
            if (tournamentListBackButton)  tournamentListBackButton.onClick.AddListener(ShowLobbyHome);
            if (tournamentPlayAgainButton) tournamentPlayAgainButton.onClick.AddListener(OnTournamentPlayAgain);
            if (tournamentMenuButton)      tournamentMenuButton.onClick.AddListener(OnTournamentMenu);

            // Score-card refresh buttons (re-fetch + re-render standings, card stays open).
            if (leaderboardRefreshButton) leaderboardRefreshButton.onClick.AddListener(OnLeaderboardRefresh);
            if (tournamentRefreshButton)  tournamentRefreshButton.onClick.AddListener(OnTournamentRefresh);

            // My History panel + tabs + detail.
            if (historyMenuButton)           historyMenuButton.onClick.AddListener(OnHistoryClicked);
            if (tabRoomsButton)              tabRoomsButton.onClick.AddListener(ShowHistoryRoomsTab);
            if (tabTournamentsButton)        tabTournamentsButton.onClick.AddListener(ShowHistoryTournamentsTab);
            if (historyRoomDetailBackButton) historyRoomDetailBackButton.onClick.AddListener(OnHistoryRoomDetailBack);
            var hbBtn = historyBackButton ? historyBackButton.GetComponent<UnityEngine.UI.Button>() : null;
            if (hbBtn) hbBtn.onClick.AddListener(OnHistoryClose);

            SetExpiry(300);   // default selection + highlight (5 min)

            // OnSceneLoaded does NOT fire for the scene this controller is born in, so apply the
            // per-scene gating once here for the current (starting) scene.
            string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(GameBull.GameBullBoot.RoomId))
            {
                // Not a joiner: safe to reset panels and show the born-scene UI.
                HideAllPanels();
                if (current == lobbySceneName)
                {
                    ShowLobbyHome();
                    ApplySplashLogo();
                }
                else if (current == splashSceneName) ApplySplashLogo();
                else if (current == gameplaySceneName) ApplyBackgroundColor();
            }
            else
            {
                // Joiner: LoadContext already showed the join panel — do NOT hide panels.
                // Still apply the partner logo if on the lobby scene.
                if (current == lobbySceneName) ApplySplashLogo();
            }
        }

        private void SetExpiry(int seconds)
        {
            _expirySeconds = seconds;
            // simple highlight: selected button brighter, other dimmer
            HighlightExpiry();
        }

        private void HighlightExpiry()
        {
            var on  = new Color(0.16f, 0.45f, 0.92f, 1f);   // selected (blue)
            var off = new Color(0.25f, 0.25f, 0.30f, 1f);   // unselected (gray)
            var img30 = expiry30Button ? expiry30Button.GetComponent<UnityEngine.UI.Image>() : null;
            var img60 = expiry60Button ? expiry60Button.GetComponent<UnityEngine.UI.Image>() : null;
            if (img30) img30.color = (_expirySeconds == 300) ? on : off;
            if (img60) img60.color = (_expirySeconds == 1800) ? on : off;
        }

        public void OnCreateRoomBack()
        {
            if (createRoomPanel) createRoomPanel.SetActive(false);
            if (modePickerPanel) modePickerPanel.SetActive(true);
        }

        public async void OnCreateRoomConfirm()
        {
            // read inputs
            string roomName = (nameInput != null && !string.IsNullOrWhiteSpace(nameInput.text))
                              ? nameInput.text.Trim() : "Room";
            string hostName = (Context != null && Context.user != null) ? Context.user.displayName : "Host";

            // disable the button while the request is in flight (avoid double-create)
            if (createRoomButton) createRoomButton.interactable = false;

            CreateRoomResult room = await GameBullApi.CreateRoom(roomName, _expirySeconds, hostName);

            if (createRoomButton) createRoomButton.interactable = true;

            if (room == null)
            {
                Debug.LogError("[GameBull] CreateRoom failed (see earlier log). Likely no token/slug yet.");
                return;
            }

            Debug.Log($"[GameBull] Room created: id={room.roomId} joinUrl={room.joinUrl} seed={room.seed} hostPlayerId={room.hostPlayerId}");
            ShowSharePanel(room);
        }

        public void ShowSharePanel(CreateRoomResult room)
        {
            _currentRoom = room;
            _roomId   = room.roomId;
            _playerId = room.hostPlayerId;
            if (createRoomPanel) createRoomPanel.SetActive(false);
            if (sharePanel)      sharePanel.SetActive(true);
            if (roomNameText) roomNameText.text = $"Room: {room.roomName}";
            if (joinUrlText)  joinUrlText.text  = room.joinUrl;
            if (playersText)  playersText.text  = $"Players: 1 / {room.maxPlayers}";
        }

        public void OnCopyLink()
        {
            if (_currentRoom == null) return;
            string link = _currentRoom.joinUrl;
#if UNITY_WEBGL && !UNITY_EDITOR
            GameBullCopyToClipboard(link);
#else
            GUIUtility.systemCopyBuffer = link;
#endif
            Debug.Log("[GameBull] Join link copied: " + link);
        }

        public void OnShareBack()
        {
            if (sharePanel)      sharePanel.SetActive(false);
            if (modePickerPanel) modePickerPanel.SetActive(true);
        }

        public void OnPlayAsHost()
        {
            if (sharePanel) sharePanel.SetActive(false);
            _playMode = PlayMode.Room;
            StartGameplay();   // host starts gameplay just like solo; score submit uses the room later
        }

        [ContextMenu("Dev Preview Share Panel")]
        public void DevPreviewSharePanel()
        {
            ShowSharePanel(new CreateRoomResult {
                roomId = "dev-room-1234",
                roomName = "Preview Room",
                joinUrl = "https://user-gamebull.hyperfunded.pro/r/dev-room-1234",
                seed = "abc123",
                hostPlayerId = "host-1",
                maxPlayers = 4,
                expiresAt = ""
            });
        }

        private void ShowJoinPanel()
        {
            Debug.Log("[GB-LOG] ShowJoinPanel START");
            Debug.Log("[GB-LOG] joinPanel null? " + (joinPanel == null) + " name=" + (joinPanel != null ? joinPanel.name : "NULL"));
            Debug.Log("[GB-LOG] modePickerPanel null? " + (modePickerPanel == null));

            if (modePickerPanel)
            {
                Debug.Log("[GB-LOG] modePickerPanel BEFORE hide: activeSelf=" + modePickerPanel.activeSelf + " activeInHierarchy=" + modePickerPanel.activeInHierarchy);
                modePickerPanel.SetActive(false);
                Debug.Log("[GB-LOG] modePickerPanel AFTER hide: activeSelf=" + modePickerPanel.activeSelf + " activeInHierarchy=" + modePickerPanel.activeInHierarchy);
            }

            if (joinPanel)
            {
                Debug.Log("[GB-LOG] joinPanel BEFORE show: activeSelf=" + joinPanel.activeSelf + " activeInHierarchy=" + joinPanel.activeInHierarchy);
                joinPanel.SetActive(true);
                Debug.Log("[GB-LOG] joinPanel AFTER show: activeSelf=" + joinPanel.activeSelf + " activeInHierarchy=" + joinPanel.activeInHierarchy);
            }
            else
            {
                Debug.Log("[GB-LOG] joinPanel is NULL — panel cannot be shown. Check Inspector assignment on GameBullLobbyController.");
            }

            if (Context != null && Context.room != null)
            {
                if (joinRoomNameText) joinRoomNameText.text = $"Room: {Context.room.name}";
                int count = (Context.room.players != null) ? Context.room.players.Length : 0;
                if (joinPlayersText) joinPlayersText.text = $"Players: {count} / 4";
            }

            _joinPanelShownOnce = true;
            Debug.Log("[GB-LOG] ShowJoinPanel END | modePicker activeSelf=" + (modePickerPanel ? modePickerPanel.activeSelf.ToString() : "null") + " | joinPanel activeInHierarchy=" + (joinPanel ? joinPanel.activeInHierarchy.ToString() : "null"));
            StartCoroutine(WatchJoinPanel());
        }

        private System.Collections.IEnumerator WatchJoinPanel()
        {
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForSeconds(0.5f);
                string js = joinPanel != null ? (joinPanel.activeSelf + "/" + joinPanel.activeInHierarchy) : "NULL";
                var canv = GetComponentInParent<Canvas>();
                string cs = canv != null ? (canv.gameObject.activeInHierarchy + " sort=" + canv.sortingOrder + " enabled=" + canv.enabled) : "no-canvas";
                Debug.Log("[GB-WATCH] t+" + (i * 0.5f) + "s joinPanel(self/hier)=" + js + " | controllerGO active=" + gameObject.activeInHierarchy + " | canvas=" + cs);
            }
        }

        public async void OnJoinConfirm()
        {
            string nickname = (nicknameInput != null && !string.IsNullOrWhiteSpace(nicknameInput.text))
                              ? nicknameInput.text.Trim() : "Player";
            if (joinButton) joinButton.interactable = false;

            JoinRoomResult result = await GameBullApi.JoinRoom(GameBullBoot.RoomId, nickname);

            if (joinButton) joinButton.interactable = true;

            if (result == null)
            {
                Debug.LogError("[GameBull] JoinRoom failed (no token/slug yet, or room full/expired).");
                return;
            }

            Debug.Log($"[GameBull] Joined room. playerId={result.playerId} nickname={result.nickname} seed={result.seed}");
            _roomId   = GameBullBoot.RoomId;
            _playerId = result.playerId;
            _playMode = PlayMode.Room;
            StartGameplay();   // joiner starts gameplay; room score submit uses result.playerId later
        }

        [ContextMenu("Dev Preview Join Panel")]
        public void DevPreviewJoinPanel()
        {
            if (modePickerPanel) modePickerPanel.SetActive(false);
            if (joinPanel) joinPanel.SetActive(true);
            if (joinRoomNameText) joinRoomNameText.text = "Room: Preview Room";
            if (joinPlayersText)  joinPlayersText.text  = "Players: 1 / 4";
        }

        public void OnSoloClicked()
        {
            // Hide the lobby UI and load gameplay directly (no Inspector dependency).
            _playMode = PlayMode.Solo;
            StartGameplay();
        }

        public void OnTournamentClicked()
        {
            // Switch from the mode picker to the create-room panel (built in a later slice).
            if (modePickerPanel) modePickerPanel.SetActive(false);
            if (createRoomPanel) createRoomPanel.SetActive(true);
            // (If createRoomPanel is null for now, this just hides the picker — that's fine for this slice.)
        }

        // ---- Open Tournament flow ----------------------------------------------

        public async void OnOpenTournamentClicked()
        {
            // hide other panels, show the tournament list, populate it
            if (modePickerPanel) modePickerPanel.SetActive(false);
            if (tournamentListPanel) tournamentListPanel.SetActive(true);

            // clear old cards (keep template inactive)
            if (tournamentListRoot != null)
                foreach (Transform c in tournamentListRoot)
                    if (c.gameObject != tournamentCardTemplate) Destroy(c.gameObject);
            if (tournamentCardTemplate) tournamentCardTemplate.SetActive(false);

            var list = await GameBullApi.ListTournaments();
            if (list == null || list.items == null || list.items.Length == 0)
            {
                Debug.Log("[GameBull] No open tournaments.");
                // optional: leave the list panel showing an empty state; for now just log
                return;
            }

            foreach (var t in list.items)
            {
                GameObject card = Instantiate(tournamentCardTemplate, tournamentListRoot);
                card.SetActive(true);
                var texts = card.GetComponentsInChildren<TMPro.TMP_Text>(true);
                if (texts.Length >= 2)
                {
                    texts[0].text = string.IsNullOrEmpty(t.name) ? "Tournament" : t.name;
                    texts[1].text = t.participantCount + " players";  // (time-left can be added later)
                }
                var btn = card.GetComponentInChildren<UnityEngine.UI.Button>(true);
                if (btn != null)
                {
                    string tid = t.id;          // capture per-card
                    string tname = t.name;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnJoinTournament(tid, tname));
                }
            }
        }

        public async void OnJoinTournament(string tournamentId, string tournamentName)
        {
            _tournamentId = tournamentId;
            var join = await GameBullApi.JoinTournament(tournamentId, "Player");
            if (join == null) { Debug.LogError("[GameBull] Join tournament failed."); return; }
            _tournamentEntryId = join.entryId;

            _playMode = PlayMode.Tournament;
            await StartTournamentPlay();
        }

        private async System.Threading.Tasks.Task StartTournamentPlay()
        {
            var sess = await GameBullApi.StartTournamentSession(_tournamentId);
            if (sess == null) { Debug.LogError("[GameBull] Tournament session failed."); return; }
            _tournamentSessionToken = sess.sessionToken;

            // hide lobby panels and start gameplay
            if (tournamentListPanel)  tournamentListPanel.SetActive(false);
            if (tournamentScorePanel) tournamentScorePanel.SetActive(false);
            if (modePickerPanel)      modePickerPanel.SetActive(false);
            if (!string.IsNullOrEmpty(gameplaySceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
        }

        public void OnTournamentPlayAgain()
        {
            // play again uses a fresh session, same tournament
            _ = StartTournamentPlay();
        }

        private async System.Threading.Tasks.Task ShowTournamentScoreCard(TournamentScoreResult result)
        {
            // Header removed: the leaderboard list below already shows scores + ranks, so the
            // per-player "Your score / New best / Rank" line is redundant. Keep the existing element
            // as a static title (rather than leaving the editor's placeholder text or an empty gap).
            if (tournamentScoreHeader != null) tournamentScoreHeader.text = "Leaderboard";

            // leaderboard
            if (tournamentLbRoot != null)
                foreach (Transform c in tournamentLbRoot)
                    if (c.gameObject != tournamentLbRowTemplate) Destroy(c.gameObject);
            if (tournamentLbRowTemplate) tournamentLbRowTemplate.SetActive(false);

            var lb = await GameBullApi.GetTournamentLeaderboard(_tournamentId);
            if (lb != null && lb.items != null && tournamentLbRowTemplate != null && tournamentLbRoot != null)
            {
                foreach (var item in lb.items)
                {
                    GameObject row = Instantiate(tournamentLbRowTemplate, tournamentLbRoot);
                    row.SetActive(true);
                    var texts = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    if (texts.Length >= 3)
                    {
                        texts[0].text = item.rank + ".";
                        texts[1].text = string.IsNullOrEmpty(item.nickname) ? "Player" : item.nickname;
                        texts[2].text = item.score.ToString();
                    }
                }
            }

            if (modePickerPanel)      modePickerPanel.SetActive(false);
            if (tournamentListPanel)  tournamentListPanel.SetActive(false);
            if (tournamentScorePanel) tournamentScorePanel.SetActive(true);
        }

        // ---- My History flow ----------------------------------------------------

        public void OnHistoryClicked()
        {
            if (modePickerPanel) modePickerPanel.SetActive(false);
            if (historyPanel) historyPanel.SetActive(true);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(false);
            ShowHistoryRoomsTab(); // default tab
        }

        public void OnHistoryClose()
        {
            if (historyPanel) historyPanel.SetActive(false);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(false);
            if (modePickerPanel) modePickerPanel.SetActive(true);
        }

        public async void ShowHistoryRoomsTab()
        {
            if (historyRoomsTab) historyRoomsTab.SetActive(true);
            if (historyTournamentsTab) historyTournamentsTab.SetActive(false);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(false);

            // clear old cards
            if (historyRoomsListRoot != null)
                foreach (Transform c in historyRoomsListRoot)
                    if (c.gameObject != historyRoomCardTemplate) Destroy(c.gameObject);
            if (historyRoomCardTemplate) historyRoomCardTemplate.SetActive(false);

            var list = await GameBullApi.GetMyRooms();
            if (list == null || list.items == null || list.items.Length == 0)
            {
                Debug.Log("[GameBull] No room history.");
                return;
            }

            foreach (var room in list.items)
            {
                GameObject card = Instantiate(historyRoomCardTemplate, historyRoomsListRoot);
                card.SetActive(true);
                var texts = card.GetComponentsInChildren<TMPro.TMP_Text>(true);
                if (texts.Length >= 2)
                {
                    texts[0].text = string.IsNullOrEmpty(room.roomName) ? "Room" : room.roomName;
                    string hostTag = room.isHost ? "Host" : "Player";
                    texts[1].text = $"You: {room.myBestScore}   {room.playerCount} players   {room.state}   {hostTag}";
                }
                var btn = card.GetComponentInChildren<UnityEngine.UI.Button>(true);
                if (btn != null)
                {
                    string rid = room.roomId;
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnHistoryViewRoom(rid));
                }
            }
        }

        public void ShowHistoryTournamentsTab()
        {
            if (historyRoomsTab) historyRoomsTab.SetActive(false);
            if (historyTournamentsTab) historyTournamentsTab.SetActive(true);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(false);
            if (historyTournamentsPlaceholder)
                historyTournamentsPlaceholder.text = "No tournament history yet";
        }

        public async void OnHistoryViewRoom(string roomId)
        {
            // show the room's leaderboard inside the history (separate detail panel, with Back to list)
            if (historyRoomsTab) historyRoomsTab.SetActive(false);
            if (historyTournamentsTab) historyTournamentsTab.SetActive(false);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(true);

            if (historyRoomDetailListRoot != null)
                foreach (Transform c in historyRoomDetailListRoot)
                    if (c.gameObject != historyRoomDetailRowTemplate) Destroy(c.gameObject);
            if (historyRoomDetailRowTemplate) historyRoomDetailRowTemplate.SetActive(false);

            var lb = await GameBullApi.GetRoomLeaderboard(roomId);
            if (lb != null && lb.items != null && historyRoomDetailRowTemplate != null && historyRoomDetailListRoot != null)
            {
                foreach (var item in lb.items)
                {
                    GameObject row = Instantiate(historyRoomDetailRowTemplate, historyRoomDetailListRoot);
                    row.SetActive(true);
                    var texts = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    if (texts.Length >= 3)
                    {
                        texts[0].text = item.rank > 0 ? item.rank + "." : "-";
                        texts[1].text = string.IsNullOrEmpty(item.nickname) ? "Player" : item.nickname;
                        texts[2].text = string.IsNullOrEmpty(item.finishedAt) ? "Playing…" : item.score.ToString();
                    }
                }
            }
        }

        public void OnHistoryRoomDetailBack()
        {
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(false);
            ShowHistoryRoomsTab(); // back to the list
        }

        public async Task LoadContext()
        {
            Debug.Log("[GB-LC] LoadContext call #" + (++_loadContextCalls));
            Debug.Log("[GB-LOG] LoadContext START");
            Context = await GameBullApi.GetContext();

            if (Context == null)
            {
                Debug.Log("[GB-LOG] LoadContext: Context NULL -> showing both buttons (offline/dev)");
                if (greetingText) greetingText.text = "Couldn't load (offline?)";
                // For local/dev testing without a token, still show buttons so the UI is usable:
                if (soloButton)       { soloButton.gameObject.SetActive(true);       Debug.Log("[GB-LOG] LoadContext (null ctx) setting soloButton active=true"); }
                if (tournamentButton) { tournamentButton.gameObject.SetActive(true); Debug.Log("[GB-LOG] LoadContext (null ctx) setting tournamentButton active=true"); }
                return;
            }

            // Joiner path: arrived via a share link (roomId in URL) and the server returned the room.
            Debug.Log("[GB-LOG] LoadContext joiner check: RoomId='" + GameBullBoot.RoomId + "' Context.room null? " + (Context == null ? "ctx-null" : (Context.room == null).ToString()));
            bool isJoiner = !string.IsNullOrEmpty(GameBullBoot.RoomId) && Context != null && Context.room != null;
            if (isJoiner)
            {
                Debug.Log("[GB-LOG] -> showing JOIN panel");
                ShowJoinPanel();
                return;
            }

            // Greeting + lives
            RenderHeader();

            // Show only enabled modes
            bool solo = HasMode("HEAD_TO_HEAD_1V1");
            bool room = HasMode("TOURNAMENT_ROOM");
            Debug.Log("[GB-LOG] LoadContext enabledModes -> solo=" + solo + " room=" + room);
            if (soloButton)       { soloButton.gameObject.SetActive(solo);       Debug.Log("[GB-LOG] LoadContext setting soloButton active=" + solo); }
            if (tournamentButton) { tournamentButton.gameObject.SetActive(room); Debug.Log("[GB-LOG] LoadContext setting tournamentButton active=" + room); }
            if (openTournamentButton) openTournamentButton.gameObject.SetActive(HasMode("OPEN_TOURNAMENT"));
            if (historyMenuButton) historyMenuButton.gameObject.SetActive(true);   // always available for logged-in users
        }

        private bool HasMode(string mode)
        {
            if (Context == null || Context.enabledModes == null) return false;
            foreach (var m in Context.enabledModes)
                if (m == mode) return true;
            return false;
        }
    }
}
