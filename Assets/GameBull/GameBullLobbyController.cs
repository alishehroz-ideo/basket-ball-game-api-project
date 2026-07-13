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
        public const int BUILD_VERSION = 21;
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
        // Fired by "back to menu" so the host game returns to ITS menu WITHOUT a scene reload.
        // (Reloading the gameplay scene breaks LapsFuse's next level build — HUD shows, board doesn't.)
        // The bridge wires this to FlowManager.SwitchMenu(MenuMain).
        public UnityEvent onReturnToMenu;
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
        public GameObject historyTournamentsTab;        // placeholder holder; the LIST is shared with the Rooms tab
        public TMPro.TMP_Text historyTournamentsPlaceholder; // (kept; empty state is shown as an in-list card instead)

        [Header("My History - room detail (leaderboard from history)")]
        public GameObject historyRoomDetailPanel;       // shows a room's leaderboard, separate from the post-game card
        public Transform  historyRoomDetailListRoot;    // ScrollView Content for rows
        public GameObject historyRoomDetailRowTemplate; // INACTIVE row: rank, name, score
        public UnityEngine.UI.Button historyRoomDetailBackButton; // Back -> returns to the Rooms list (history)

        [Header("GameBull - Message popup")]
        public GameObject messagePopupPanel;             // the panel root (starts INACTIVE)
        public TMPro.TMP_Text messagePopupTitle;
        public TMPro.TMP_Text messagePopupBody;
        public UnityEngine.UI.Button messagePopupOkButton;

        [Header("Scene control")]
        public string lobbySceneName = "Menu";       // BasketBall boot + lobby scene (Build Settings index 0)
        public string gameplaySceneName = "GamePlay"; // BasketBall gameplay scene (Build Settings index 1)
        public string splashSceneName = "";           // no separate splash scene in BasketBall (boot = Menu)
        public string backgroundObjectName = "background";   // SpriteRenderer in main, tinted to primary
        public string splashLogoObjectName = "logo";          // UI Image on AdController, swapped to logoUrl

        [Header("Splash logo (drag the Title Image here)")]
        public UnityEngine.UI.Image splashLogoImage;

        [Header("Primary color targets (drag card GameObjects with Image here)")]
        public GameObject[] primaryColorTargets;

        [Header("Main Panel v2")]
        public GameObject mainPanelV2;                 // panel root (starts ACTIVE)
        public TMPro.TMP_Text v2LivesText;             // heart lives counter (e.g. "5")
        public TMPro.TMP_Text v2NameText;              // user name
        public Transform  v2TournamentsListRoot;       // top scroll list Content
        public GameObject v2TournamentCardTemplate;    // INACTIVE card template
        public Transform  v2LeaderboardListRoot;       // bottom scroll list Content
        public GameObject v2LeaderboardRowTemplate;    // INACTIVE row template
        public UnityEngine.UI.Button v2JoinButton;     // Join Tournament button
        public TMPro.TMP_Text v2JoinCostText;          // "-1" / heart cost label on button
        // Tabs (LIST state)
        public UnityEngine.UI.Button v2TabAllButton;
        public UnityEngine.UI.Button v2TabActiveButton;
        public UnityEngine.UI.Button v2TabClosedButton;
        // LIST state container (tabs + tournament list live here)
        public GameObject v2ListState;
        // DETAIL state container (leaderboard + join/back live here)
        public GameObject v2DetailState;
        public UnityEngine.UI.Button v2BackButton;     // the small ✕ button
        // API-swappable game image + tab sprite-swap (selected/unselected on press)
        public UnityEngine.UI.Image v2GameImage;
        public UnityEngine.UI.Image v2TabAllImage;
        public UnityEngine.UI.Image v2TabActiveImage;
        public UnityEngine.UI.Image v2TabClosedImage;
        public Sprite v2TabSelectedSprite;
        public Sprite v2TabUnselectedSprite;
        public Sprite[] v2CharacterImages;   // ~10 avatars (Inspector); leaderboard row avatar picked by row position
        public Sprite v2MyRowSprite;         // green box: replaces the leaderboard row bar for the CURRENT player (isMe). Null -> keep default bar.

        [Header("GameBull - Game-End panel (v2 scorecard)")]
        public GameObject gameEndPanel;              // panel root (starts inactive)
        public TMPro.TMP_Text gameEndScoreText;      // FinalScoreText — this round's score
        public TMPro.TMP_Text gameEndRankText;       // RankText — RANK ONLY (number, e.g. "5")
        public TMPro.TMP_Text gameEndBestScoreText;  // BestScoreText — player's BEST score in this tournament
        public UnityEngine.UI.Button gameEndPlayAgainButton;
        public UnityEngine.UI.Button gameEndHomeButton;
        public UnityEngine.UI.Button gameEndLeaderboardButton;

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

        // true when the shared room-detail panel was opened from the Tournaments tab (routes its Back button).
        private bool _detailFromTournaments;

        // Main Panel v2 runtime state.
        private string _v2CurrentPhase;
        private string _v2SelectedId;
        // Per-rank prize tiers (prizeConfig) captured from the list, keyed by tournament id — so the
        // leaderboard (opened later by id, from a card tap OR the game-end "View Leaderboard") can show
        // each row's prize. The leaderboard API itself returns no prize info.
        private System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string,int>> _prizeByTid
            = new System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string,int>>();
        private int    _v2PopulateReq;   // re-entrancy guard: only the LATEST tournament populate renders
        private bool   _joining;         // true while a join (DoJoin) is in flight — blocks repopulate so the tapped card is never destroyed mid-join
        private bool   _v2ShownOnBoot;   // true once LoadContext showed v2 on boot — so Start() doesn't hide+reshow (a second populate)
        private bool   _v2ListReady;     // true after the FIRST populate renders — list stays hidden (untappable) until then

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
            // The GameBull canvas is born in the splash scene and persists everywhere, so gate
            // by scene: hide everything first, then apply only what this scene needs.
            HideAllPanels();
            if (scene.name == lobbySceneName)
            {
                if (string.IsNullOrEmpty(GameBull.GameBullBoot.RoomId) || _joinPanelShownOnce)
                    ShowLobbyHome();
                ApplySplashLogo();
            }
            else if (scene.name == splashSceneName)
            {
                ApplySplashLogo();
            }
            else if (scene.name == gameplaySceneName)
            {
                ApplyBackgroundColor();
            }
        }

        // Hide every GameBull panel. Used when entering any scene before applying per-scene UI.
        private void HideAllPanels()
        {
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
            if (splashLogoImage == null) return;
            splashLogoImage.gameObject.SetActive(false);   // hide until partner logo loads

            if (Context == null || Context.customization == null) return;
            string url = Context.customization.logoUrl;
            if (string.IsNullOrEmpty(url)) return;

            using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await System.Threading.Tasks.Task.Yield();

                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var tex = UnityEngine.Networking.DownloadHandlerTexture.GetContent(req);
                    splashLogoImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                    splashLogoImage.gameObject.SetActive(true);
                }
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

            // ONLY apply a partner-uploaded custom background image. If none was uploaded (or the
            // download didn't finish within the 5s wait above), leave the game's own background
            // completely untouched — NO primary-color tint. The old primary-color fallback dulled/
            // darkened the gameplay background, which we don't want.
            if (PartnerBackgroundSprite == null) yield break;

            var bgGO = GameObject.Find(backgroundObjectName);
            if (bgGO == null) yield break;

            var img = bgGO.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                img.sprite = PartnerBackgroundSprite;
                img.color  = Color.white;
                yield break;
            }

            var sr = bgGO.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sprite = PartnerBackgroundSprite;
                sr.color  = Color.white;
            }
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
        }

        // Find the partner's custom "ball" asset in context and download it into PartnerBallSprite,
        // which BallManager applies to the in-game ball SpriteRenderer.
        private async void LoadPartnerBallAsset()
        {
            if (Context == null || Context.customization == null || Context.customization.assets == null) return;
            string url = null;
            foreach (var a in Context.customization.assets)
                if (a != null && a.key == "ball") { url = a.url; break; }
            if (string.IsNullOrEmpty(url)) return;

            using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await System.Threading.Tasks.Task.Yield();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var tex = ((UnityEngine.Networking.DownloadHandlerTexture)req.downloadHandler).texture;
                    PartnerBallSprite = Sprite.Create(tex, new Rect(0,0,tex.width,tex.height), new Vector2(0.5f,0.5f), 100f);
                }
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
            if (string.IsNullOrEmpty(url)) return;

            using (var req = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await System.Threading.Tasks.Task.Yield();
                if (req.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var tex = ((UnityEngine.Networking.DownloadHandlerTexture)req.downloadHandler).texture;
                    PartnerBackgroundSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                }
            }
        }

        // Single source of truth for "go to lobby home": force-show the mode picker,
        // hide every other panel, restore the header, re-activate the mode buttons, reset play state.
        public void ShowLobbyHome()
        {
            // v2 is the single source of truth when present: route to it and skip the old mode picker.
            // This also fixes post-game return (game-over -> ShowLobbyHome now lands on v2).
            if (mainPanelV2 != null)
            {
                ShowMainPanelV2();   // shows v2, hides modePicker, fills header (cached), selects "All"
                _ = RefreshLives();  // post-game: refetch ♥; RefreshLives re-renders the v2 header when done
                return;
            }

            if (createRoomPanel)  createRoomPanel.SetActive(false);
            if (sharePanel)       sharePanel.SetActive(false);
            if (joinPanel)        joinPanel.SetActive(false);
            if (leaderboardPanel) leaderboardPanel.SetActive(false);
            if (tournamentListPanel)  tournamentListPanel.SetActive(false);
            if (tournamentScorePanel) tournamentScorePanel.SetActive(false);
            if (historyPanel) historyPanel.SetActive(false);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(false);
            if (modePickerPanel) modePickerPanel.SetActive(true);
            // The mode buttons get SetActive(false) in Start()/LoadContext() based on enabledModes,
            // and those run only once on this persistent controller — so re-activate them here.
            if (soloButton)       soloButton.gameObject.SetActive(HasMode("HEAD_TO_HEAD_1V1"));
            if (tournamentButton) tournamentButton.gameObject.SetActive(HasMode("TOURNAMENT_ROOM"));
            if (openTournamentButton) openTournamentButton.gameObject.SetActive(HasMode("OPEN_TOURNAMENT"));
            if (historyMenuButton) historyMenuButton.gameObject.SetActive(true);   // always available for logged-in users
            RenderHeader();   // shows CACHED greeting/lives immediately (no-op if null)
            // ...then refresh lives from the server so a post-game return shows the current count.
            Debug.Log("[LIVES] ShowLobbyHome — firing RefreshLives");
            _ = RefreshLives();

            // reset play state so the next game (solo or new room) is clean
            _playMode = PlayMode.None; _roomId = null; _playerId = null;
        }

        // Scene-return path uses identical logic to the menu button.
        public void ShowModePicker() { ShowLobbyHome(); }

        private void RenderHeader()
        {
            Debug.Log($"[LIVES] RenderHeader displaying count={Context?.lives?.count}");
            if (Context == null || Context.user == null) return;
            if (greetingText != null)
                greetingText.text = "Hi " + Context.user.displayName;
            if (livesText != null && Context.lives != null)
                livesText.text = "Lives: " + Context.lives.count + "/" + Context.lives.max;
        }

        // Re-fetch /context and re-render the header so the lives count is current. Called only at
        // transition points (after a session mint, and when the post-game scorecard is shown) — never per-frame.
        private async System.Threading.Tasks.Task RefreshLives()
        {
            Debug.Log($"[LIVES] RefreshLives START — current displayed Context.lives.count={Context?.lives?.count}");
            var ctx = await GameBullApi.GetContext();
            Debug.Log($"[LIVES] RefreshLives GOT server count={ctx?.lives?.count} (ctx null? {ctx==null})");
            if (ctx != null) { Context = ctx; RenderHeader(); if (mainPanelV2 != null) RenderV2Header(); }
            Debug.Log($"[LIVES] RefreshLives RENDERED count={Context?.lives?.count}");
        }

        // ---- Generic message popup (self-contained in the GameBull canvas) ----
        // Reusable for ANY warning/notice (out-of-lives, errors, etc.). Call from anywhere:
        //   GameBullLobbyController.Instance.ShowMessage("Title", "Body");
        public void ShowMessage(string title, string body)
        {
            ShowMessage(title, body, null, null);
        }
        public void ShowMessage(string title, string body, System.Action onOk)
        {
            ShowMessage(title, body, onOk, null);
        }

        // Context-aware overload: OK always closes the popup, then runs onOk (if any). Callers that
        // need OK to route somewhere (e.g. HOME after out-of-lives) pass a callback. Pass okLabel to
        // override the OK button's text (else it falls back to the default "わかりました").
        public void ShowMessage(string title, string body, System.Action onOk, string okLabel)
        {
            if (messagePopupPanel == null)
            {
                // Not wired yet — log instead of NRE so the game never crashes.
                Debug.LogWarning($"[GameBull] Message popup not wired. {title}: {body}");
                return;
            }
            if (messagePopupTitle) messagePopupTitle.text = title;
            if (messagePopupBody)  messagePopupBody.text  = body;

            // Rebind OK every time so callbacks from a previous ShowMessage never stack.
            if (messagePopupOkButton)
            {
                // Set the button label every call so a custom label never leaks into the next popup.
                var okLbl = messagePopupOkButton.GetComponentInChildren<TMPro.TMP_Text>(true);
                if (okLbl != null) okLbl.text = string.IsNullOrEmpty(okLabel) ? "わかりました" : okLabel;

                messagePopupOkButton.onClick.RemoveAllListeners();
                messagePopupOkButton.onClick.AddListener(HideMessage);
                if (onOk != null) messagePopupOkButton.onClick.AddListener(() => onOk());
            }

            messagePopupPanel.SetActive(true);
        }

        public void HideMessage()
        {
            if (messagePopupPanel) messagePopupPanel.SetActive(false);
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
                    var r = await GameBullApi.SubmitGlobalTournamentScore(_tournamentId, score, playTimeMs);
                    if (r != null)
                        Debug.Log($"[GameBull] Tournament score: {r.score} improved={r.improved} rank={r.rank}");
                    else
                        Debug.Log("[GameBull] Tournament score submit failed.");
                    if (r != null && !string.IsNullOrEmpty(r.entryId)) _tournamentEntryId = r.entryId;

                    // v2 game-end panel (replaces the old scorecard). Refresh lives + build the rank text
                    // exactly like the old scorecard (my-rank, with a submit-result fallback).
                    await RefreshLives();
                    var mine = await GameBullApi.GetGlobalMyRank();
                    // RANK: number only; "-" when unknown / 0.
                    int rankVal = (mine != null) ? mine.rank.GetValueOrDefault() : 0;
                    string rankStr = rankVal > 0 ? rankVal.ToString() : "-";
                    // BEST: server's recorded best; fall back to the submit result, then this round's score,
                    // so we never show a misleading 0 right after the player just scored.
                    int bestVal = (mine != null && mine.score.HasValue) ? mine.score.Value
                                : (r != null ? r.score : score);
                    string bestStr = bestVal.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                    ShowGameEndPanel(score, rankStr, bestStr);
                    // await ShowTournamentScoreCard(r);   // OLD scorecard — superseded by gameEndPanel (kept, not deleted)
                    return;   // don't redirect; Play Again / Home / Leaderboard handle it
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
            if (leaderboardPanel) leaderboardPanel.SetActive(false);
            _playMode = PlayMode.None; _roomId = null; _playerId = null;
            ReturnToLobbyNoReload();
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
            // clear old rows except template
            if (tournamentLbRoot != null)
                foreach (Transform c in tournamentLbRoot)
                    if (c.gameObject != tournamentLbRowTemplate) Destroy(c.gameObject);
            if (tournamentLbRowTemplate) tournamentLbRowTemplate.SetActive(false);

            // refresh the "where you stand" header too
            var mine = await GameBullApi.GetGlobalMyRank();
            if (tournamentScoreHeader != null && mine != null && mine.rank.GetValueOrDefault() > 0)
                tournamentScoreHeader.text = $"Your rank: #{mine.rank.Value}   Score: {mine.score.GetValueOrDefault()}";

            var lb = await GameBullApi.GetGlobalLeaderboard();
            if (lb != null && lb.items != null && tournamentLbRowTemplate != null && tournamentLbRoot != null)
            {
                foreach (var item in lb.items)
                {
                    GameObject row = Instantiate(tournamentLbRowTemplate, tournamentLbRoot);
                    row.SetActive(true);
                    var texts = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    if (texts.Length >= 3)
                    {
                        texts[0].text = item.rank.GetValueOrDefault() + ".";
                        texts[1].text = string.IsNullOrEmpty(item.alias) ? "Player" : item.alias;
                        texts[2].text = item.score.GetValueOrDefault().ToString();
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
            ReturnToLobbyNoReload();
        }

        // Return to the lobby WITHOUT reloading the gameplay scene. A LoadScene(scn_game) here re-inits
        // the scene and, together with the game-over Resources.UnloadUnusedAssets(), leaves LapsFuse
        // unable to build the NEXT level (HUD shows, board doesn't). Instead let the host restore its own
        // menu (onReturnToMenu -> FlowManager.SwitchMenu(MenuMain)) and just re-show the GameBull picker.
        private void ReturnToLobbyNoReload()
        {
            // BasketBall (multi-scene): return to the lobby scene; OnSceneLoaded re-shows the lobby
            // when scene.name == lobbySceneName. LapsFuse (single-scene, lobbySceneName="") falls
            // through to the original no-reload path (its onReturnToMenu hook restores the in-scene menu).
            var current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (!string.IsNullOrEmpty(lobbySceneName) && current != lobbySceneName)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(lobbySceneName);
                return;   // OnSceneLoaded re-shows the lobby when scene.name == lobbySceneName
            }
            onReturnToMenu?.Invoke();   // LapsFuse single-scene path (lobbySceneName empty)
            ShowLobbyHome();
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
            if (messagePopupOkButton)        messagePopupOkButton.onClick.AddListener(HideMessage);

            // Main Panel v2 buttons.
            // "全て" (All) tab removed — no v2TabAllButton wiring.
            if (v2TabActiveButton) v2TabActiveButton.onClick.AddListener(() => SelectV2Tab("active"));
            if (v2TabClosedButton) v2TabClosedButton.onClick.AddListener(() => SelectV2Tab("closed"));
            if (v2BackButton)      v2BackButton.onClick.AddListener(OnV2Back);
            if (v2JoinButton)      v2JoinButton.onClick.AddListener(OnV2Join);

            // Game-End panel buttons.
            if (gameEndPlayAgainButton)   gameEndPlayAgainButton.onClick.AddListener(OnGameEndPlayAgain);
            if (gameEndHomeButton)        gameEndHomeButton.onClick.AddListener(OnGameEndHome);
            if (gameEndLeaderboardButton) gameEndLeaderboardButton.onClick.AddListener(OnGameEndLeaderboard);
            var hbBtn = historyBackButton ? historyBackButton.GetComponent<UnityEngine.UI.Button>() : null;
            if (hbBtn) hbBtn.onClick.AddListener(OnHistoryClose);

            SetExpiry(300);   // default selection + highlight (5 min)

            // OnSceneLoaded does NOT fire for the scene this controller is born in, so apply the
            // per-scene gating once here for the current (starting) scene.
            string current = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (string.IsNullOrEmpty(GameBull.GameBullBoot.RoomId))
            {
                // If LoadContext already showed + populated v2 for THIS (lobby) scene, do NOT hide and
                // re-show it — that fires a SECOND populate on boot. Just apply the splash logo.
                // (LapsFuse: born scene != lobbyScene, so it falls to the else branch — behavior unchanged.)
                if (current == lobbySceneName && _v2ShownOnBoot)
                {
                    ApplySplashLogo();
                }
                else
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
            if (modePickerPanel) modePickerPanel.SetActive(false);
            if (joinPanel) joinPanel.SetActive(true);

            if (Context != null && Context.room != null)
            {
                if (joinRoomNameText) joinRoomNameText.text = $"Room: {Context.room.name}";
                int count = (Context.room.players != null) ? Context.room.players.Length : 0;
                if (joinPlayersText) joinPlayersText.text = $"Players: {count} / 4";
            }

            _joinPanelShownOnce = true;
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

        public async void OnSoloClicked()
        {
            _playMode = PlayMode.Solo;

            // v3.1 two-tier play: mint a play token (spends 1 life) BEFORE starting. No token → no play.
            var play = await GameBullApi.MintPlay();
            if (play == null || string.IsNullOrEmpty(play.playToken))
            {
                Debug.LogWarning("[GameBull] Couldn't start solo (out of lives or play-mint failed).");
                if (modePickerPanel) modePickerPanel.SetActive(true);   // stay on lobby, no crash
                return;
            }
            GameBullApi.CurrentPlayToken = play.playToken;
            GameBullApi.CurrentSeed      = play.seed;   // captured for correctness; LapsFuse uses its own RNG

            StartGameplay();   // unchanged — preserves the onStartSolo → Play_Game bridge exactly
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

            var list = await GameBullApi.ListGlobalTournaments("active");
            if (list == null || list.items == null || list.items.Length == 0)
            {
                Debug.Log("[GameBull] No open tournaments.");
                // optional: leave the list panel showing an empty state; for now just log
                return;
            }

            // The list endpoint returns ALL the tenant's games — keep only ours.
            var mine = FilterToThisGame(list.items);
            if (mine.Count == 0)
            {
                Debug.Log("[GameBull] No open tournaments for this game.");
                return;
            }

            foreach (var t in mine)
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

        public void OnJoinTournament(string tournamentId, string tournamentName)
        {
            // v3.1 two-tier: remember the id, then StartTournamentPlay mints a session (spends 1 life)
            // before starting. Score/leaderboard/my-rank then use the minted global token.
            _tournamentId = tournamentId;
            _tournamentEntryId = null;
            _playMode = PlayMode.Tournament;
            StartTournamentPlay();
        }

        private async void StartTournamentPlay()
        {
            // v3.1: mint the global-tournament session (SPENDS 1 LIFE) BEFORE starting. No token → no play.
            var sess = await GameBullApi.StartGlobalTournamentSession(_tournamentId);
            if (sess == null || string.IsNullOrEmpty(sess.sessionToken))
            {
                // Distinguish out-of-lives (403 play.no_lives) from a generic mint failure.
                if (GameBullClient.LastErrorCode == "play.no_lives")
                {
                    await RefreshLives();   // reflect the now-zero count in the header
                    // Play Again path: both gameEndPanel and mainPanelV2 were already hidden, so OK
                    // must route HOME (v2) or the player is stuck on a blank screen.
                    ShowMessage("ライフがありません",
                        "ホーム画面に戻り、ライフを獲得してください。",
                        ShowLobbyHome, "ホームへ戻る");
                }
                else
                {
                    Debug.LogWarning("[GameBull] Couldn't start tournament (session mint failed: " + GameBullClient.LastErrorCode + ").");
                }
                // Legacy flow only: re-show the old list. In the v2 flow (mainPanelV2 assigned) we stay on
                // v2 — no old tournamentListPanel — just the Out-of-Lives popup already shown above.
                if (mainPanelV2 == null && tournamentListPanel) tournamentListPanel.SetActive(true);
                return;
            }
            // CurrentGlobalToken + CurrentSeed were stored inside StartGlobalTournamentSession.
            _ = RefreshLives();   // (a) refresh lives after the mint (fire-and-forget; don't delay gameplay start)

            if (tournamentListPanel)  tournamentListPanel.SetActive(false);
            if (tournamentScorePanel) tournamentScorePanel.SetActive(false);
            if (modePickerPanel)      modePickerPanel.SetActive(false);
            // Single-scene games start gameplay via this hook; the scene load below is
            // skipped when gameplaySceneName is empty.
            onStartSolo?.Invoke();
            if (!string.IsNullOrEmpty(gameplaySceneName))
                UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
        }

        public void OnTournamentPlayAgain()
        {
            // play again, same tournament (no session to refresh)
            StartTournamentPlay();
        }

        private async System.Threading.Tasks.Task ShowTournamentScoreCard(TournamentScoreResult result)
        {
            // Show the card UP-FRONT so a slow/empty/failed fetch never leaves the player on a blank
            // game screen. (Previously an unhandled JSON error in the fetch below hid the panel entirely.)
            if (modePickerPanel)      modePickerPanel.SetActive(false);
            if (tournamentListPanel)  tournamentListPanel.SetActive(false);
            if (tournamentScorePanel) tournamentScorePanel.SetActive(true);

            await RefreshLives();   // (b) lives are current before the player can tap Play Again

            // Header: where THIS player stands (from /my-rank; falls back to the submit result).
            var mine = await GameBullApi.GetGlobalMyRank();
            if (tournamentScoreHeader != null)
            {
                if (mine != null && mine.rank.GetValueOrDefault() > 0)
                    tournamentScoreHeader.text = $"Your rank: #{mine.rank.Value}   Score: {mine.score.GetValueOrDefault()}";
                else if (result != null)
                    tournamentScoreHeader.text = $"Your score: {result.score}";
                else
                    tournamentScoreHeader.text = "Leaderboard";
            }

            // leaderboard for the token's tournament (no id — resolved from the JWT)
            if (tournamentLbRoot != null)
                foreach (Transform c in tournamentLbRoot)
                    if (c.gameObject != tournamentLbRowTemplate) Destroy(c.gameObject);
            if (tournamentLbRowTemplate) tournamentLbRowTemplate.SetActive(false);

            var lb = await GameBullApi.GetGlobalLeaderboard();
            if (lb != null && lb.items != null && tournamentLbRowTemplate != null && tournamentLbRoot != null)
            {
                foreach (var item in lb.items)
                {
                    GameObject row = Instantiate(tournamentLbRowTemplate, tournamentLbRoot);
                    row.SetActive(true);
                    var texts = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
                    if (texts.Length >= 3)
                    {
                        texts[0].text = item.rank.GetValueOrDefault() + ".";
                        texts[1].text = string.IsNullOrEmpty(item.alias) ? "Player" : item.alias;
                        texts[2].text = item.score.GetValueOrDefault().ToString();
                    }
                }
            }
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

        public async void ShowHistoryTournamentsTab()
        {
            // The shared history list lives INSIDE RoomsTab (historyRoomsListRoot is a descendant of
            // historyRoomsTab). Unity can't show a child while its ancestor is inactive, so we KEEP
            // historyRoomsTab active and just repopulate the SHARED list with tournament cards.
            if (historyRoomsTab) historyRoomsTab.SetActive(true);
            if (historyTournamentsTab) historyTournamentsTab.SetActive(false);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(false);

            // clear old cards (keep template inactive) — SHARED with the Rooms tab
            if (historyRoomsListRoot != null)
                foreach (Transform c in historyRoomsListRoot)
                    if (c.gameObject != historyRoomCardTemplate) Destroy(c.gameObject);
            if (historyRoomCardTemplate) historyRoomCardTemplate.SetActive(false);

            // active first, then past (boot token, no life cost); null-safe merge
            var active = await GameBullApi.ListGlobalTournaments("active");
            var past   = await GameBullApi.ListGlobalTournaments("past");
            var merged = new System.Collections.Generic.List<GlobalTournament>();
            if (active != null && active.items != null) merged.AddRange(active.items);
            if (past   != null && past.items   != null) merged.AddRange(past.items);

            // The list endpoint returns ALL the tenant's games — keep only THIS game's tournaments.
            merged = FilterToThisGame(merged);

            if (merged.Count == 0)
            {
                ShowHistoryMessageCard("No tournaments yet");
                return;
            }

            foreach (var t in merged)
            {
                if (historyRoomCardTemplate == null || historyRoomsListRoot == null) break;
                GameObject card = Instantiate(historyRoomCardTemplate, historyRoomsListRoot);
                card.SetActive(true);
                var texts = card.GetComponentsInChildren<TMPro.TMP_Text>(true);
                if (texts.Length >= 2)
                {
                    texts[0].text = string.IsNullOrEmpty(t.name) ? "Tournament" : t.name;
                    texts[1].text = $"{StatusLabel(t.status, t.startsAt, t.endsAt)}   {t.participantCount} players   {ShortDate(t.startsAt)}–{ShortDate(t.endsAt)}";
                }
                var btn = card.GetComponentInChildren<UnityEngine.UI.Button>(true);
                if (btn != null)
                {
                    btn.gameObject.SetActive(true);          // (re-enable in case a message card disabled it)
                    string tid = t.id;                       // capture per-card
                    bool isActive = EndsInFuture(t.endsAt);  // only the active one has a fetchable board
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => OnHistoryViewTournament(tid, isActive));
                }
            }
        }

        // One non-interactive info card in the SHARED history list (empty/loading states).
        private void ShowHistoryMessageCard(string message)
        {
            if (historyRoomCardTemplate == null || historyRoomsListRoot == null) return;
            GameObject card = Instantiate(historyRoomCardTemplate, historyRoomsListRoot);
            card.SetActive(true);
            var texts = card.GetComponentsInChildren<TMPro.TMP_Text>(true);
            if (texts.Length >= 1) texts[0].text = message;
            if (texts.Length >= 2) texts[1].text = "";
            var btn = card.GetComponentInChildren<UnityEngine.UI.Button>(true);
            if (btn != null) btn.gameObject.SetActive(false);   // no action on a message card
        }

        public async void OnHistoryViewRoom(string roomId)
        {
            // show the room's leaderboard inside the history (separate detail panel, with Back to list)
            if (historyRoomsTab) historyRoomsTab.SetActive(false);
            if (historyTournamentsTab) historyTournamentsTab.SetActive(false);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(true);
            _detailFromTournaments = false;   // Back returns to the Rooms tab

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
            if (_detailFromTournaments) ShowHistoryTournamentsTab();   // route Back to whichever tab opened it
            else ShowHistoryRoomsTab();
        }

        // View a tournament's board from history. Reuses the room-detail leaderboard panel.
        // Active tournament → boot-token leaderboard (no life cost). Past → no fetchable board (token-scoped API).
        public async void OnHistoryViewTournament(string tournamentId, bool isActive)
        {
            if (historyRoomsTab) historyRoomsTab.SetActive(false);
            if (historyTournamentsTab) historyTournamentsTab.SetActive(false);
            if (historyRoomDetailPanel) historyRoomDetailPanel.SetActive(true);
            _detailFromTournaments = true;   // Back returns to the Tournaments tab

            // clear old rows (keep template inactive)
            if (historyRoomDetailListRoot != null)
                foreach (Transform c in historyRoomDetailListRoot)
                    if (c.gameObject != historyRoomDetailRowTemplate) Destroy(c.gameObject);
            if (historyRoomDetailRowTemplate) historyRoomDetailRowTemplate.SetActive(false);

            // Single path for active AND past: the id-addressable board works for any published/ended
            // tournament with the boot token (no life cost). No active/past branching needed.
            var lb = await GameBullApi.GetGlobalLeaderboardById(tournamentId);
            if (lb == null || lb.items == null || lb.items.Length == 0)
            {
                ShowSingleDetailRow("No scores yet");
                return;
            }
            foreach (var item in lb.items)
            {
                GameObject row = Instantiate(historyRoomDetailRowTemplate, historyRoomDetailListRoot);
                row.SetActive(true);
                var texts = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
                if (texts.Length >= 3)
                {
                    texts[0].text = item.rank.GetValueOrDefault() + ".";
                    texts[1].text = (item.isMe ? "► " : "") + (string.IsNullOrEmpty(item.alias) ? "Player" : item.alias);
                    texts[2].text = item.score.GetValueOrDefault().ToString();
                }
            }
        }

        // Put a single message row in the room-detail list (empty / unavailable states).
        private void ShowSingleDetailRow(string message)
        {
            if (historyRoomDetailRowTemplate == null || historyRoomDetailListRoot == null) return;
            GameObject row = Instantiate(historyRoomDetailRowTemplate, historyRoomDetailListRoot);
            row.SetActive(true);
            var texts = row.GetComponentsInChildren<TMPro.TMP_Text>(true);
            if (texts.Length >= 3) { texts[0].text = ""; texts[1].text = message; texts[2].text = ""; }
            else if (texts.Length >= 1) texts[0].text = message;
        }

        // --- Tournament-history display helpers ---

        // The list endpoint returns EVERY tournament for the tenant, across ALL games. Keep only the
        // ones for THIS game (item.gameId == our GameBullBoot.GameId). Fallback: if our gameId is
        // unknown, show everything rather than hiding the whole list.
        private static System.Collections.Generic.List<GlobalTournament> FilterToThisGame(System.Collections.Generic.IEnumerable<GlobalTournament> items)
        {
            var result = new System.Collections.Generic.List<GlobalTournament>();
            if (items == null) return result;
            string myId = GameBullBoot.GameId;
            foreach (var t in items)
            {
                if (t == null) continue;
                if (string.IsNullOrEmpty(myId) || t.gameId == myId) result.Add(t);
            }
            return result;
        }

        private static bool EndsInFuture(string endsAtIso)
        {
            if (string.IsNullOrEmpty(endsAtIso)) return false;
            if (System.DateTime.TryParse(endsAtIso, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
                return dt > System.DateTime.UtcNow;
            return false;   // unparseable → treat as past (no board)
        }
        // True if startsAt is a valid timestamp in the FUTURE (tournament hasn't started yet).
        // Empty/unparseable startsAt -> false (treat as already started).
        private static bool StartsInFuture(string startsAtIso)
        {
            if (string.IsNullOrEmpty(startsAtIso)) return false;
            if (System.DateTime.TryParse(startsAtIso, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
                return dt > System.DateTime.UtcNow;
            return false;
        }
        private enum TournamentPhase { Upcoming, Active, Ended }

        // SINGLE source of truth for a tournament's phase — drives the label, the status-dot color,
        // and joinability, so all three always agree.
        private static TournamentPhase GetPhase(string status, string startsAtIso, string endsAtIso)
        {
            string s = (status ?? "").Trim().ToLowerInvariant();
            // Server explicitly marks it finished (e.g. status "ended"/"finalized") — respect that
            // over the timestamps.
            if (s == "ended" || s == "finished" || s == "finalized" || s == "closed"
                || s == "completed" || s == "complete" || s == "cancelled" || s == "canceled")
                return TournamentPhase.Ended;
            if (StartsInFuture(startsAtIso)) return TournamentPhase.Upcoming;   // not started yet
            return EndsInFuture(endsAtIso) ? TournamentPhase.Active : TournamentPhase.Ended;
        }

        private static string StatusLabel(string status, string startsAtIso, string endsAtIso)
        {
            switch (GetPhase(status, startsAtIso, endsAtIso))
            {
                case TournamentPhase.Upcoming: return "開始前";
                case TournamentPhase.Active:   return "アクティブ";
                default:                       return "終了";
            }
        }
        private static string ShortDate(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "";
            int t = iso.IndexOf('T');
            return t > 0 ? iso.Substring(0, t) : iso;   // yyyy-MM-dd portion of the ISO string
        }

        // Parse an ISO (UTC "Z") timestamp, convert to the player's LOCAL timezone (browser TZ in WebGL
        // — JST for JP users), and format "MM-DD HH:mm" (zero-padded), e.g. "2026-07-14T15:01Z" ->
        // "07-14 00:01" for JST-ish. Used for BOTH start and end times. Matches the admin dashboard.
        private static string FormatDateTime(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return "";
            System.DateTime dt;
            if (System.DateTime.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal |
                    System.Globalization.DateTimeStyles.AssumeUniversal, out dt))
            {
                var local = dt.ToLocalTime();
                return $"{local:MM-dd} {local:HH:mm}";   // zero-padded hour, e.g. "07-14 00:01"
            }
            return iso;   // fallback if parse fails
        }

        // Active-tab sort rank: Active first, then Upcoming, then Ended.
        private static int SortRank(GlobalTournament t)
        {
            var p = GetPhase(t.status, t.startsAt, t.endsAt);
            return p == TournamentPhase.Active ? 0 : p == TournamentPhase.Upcoming ? 1 : 2;
        }
        // Parse an ISO timestamp to UTC for sorting; unparseable -> MaxValue (sinks to the end).
        private static System.DateTime ParseUtc(string iso)
        {
            if (!string.IsNullOrEmpty(iso) && System.DateTime.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
                return dt;
            return System.DateTime.MaxValue;
        }

        // ============================================================================
        // Main Panel v2 — RUNTIME LOGIC (additive; does not touch the old lobby flow).
        // ============================================================================

        // Boot entry: show v2 as the main screen, fill the header, default to the "All" tab.
        public void ShowMainPanelV2()
        {
            if (mainPanelV2 == null) return;
            if (modePickerPanel) modePickerPanel.SetActive(false);   // v2 replaces the old mode picker
            mainPanelV2.SetActive(true);
            if (v2ListState)   v2ListState.SetActive(true);
            if (v2DetailState) v2DetailState.SetActive(false);
            RenderV2Header();
            // Hide the list (its Viewport) until the FIRST populate renders, so the player can't tap a
            // card during the boot load window (a repopulate would destroy the card and lose the life).
            if (!_v2ListReady && v2TournamentsListRoot != null && v2TournamentsListRoot.parent != null)
                v2TournamentsListRoot.parent.gameObject.SetActive(false);
            SelectV2Tab("active");   // "全て" (All) tab removed — Active is the default on open
        }

        // Reveal the tournament list (its Viewport) once the first populate has rendered, and force a
        // layout rebuild so rows lay out correctly after being shown. One-time (boot); no-op afterwards.
        private void RevealV2List()
        {
            _v2ListReady = true;
            if (v2TournamentsListRoot != null && v2TournamentsListRoot.parent != null)
            {
                var panel = v2TournamentsListRoot.parent.gameObject;
                if (!panel.activeSelf) panel.SetActive(true);
                var rt = v2TournamentsListRoot as RectTransform;
                if (rt != null) UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
            }
        }

        // Fill the v2 header (name + heart count) from the cached Context.
        private void RenderV2Header()
        {
            if (Context == null) return;
            if (v2NameText  != null && Context.user  != null) v2NameText.text  = Context.user.displayName;
            if (v2LivesText != null && Context.lives != null) v2LivesText.text = Context.lives.count.ToString();
        }

        // Tab select: swap the sprites, remember the phase, repopulate.
        public void SelectV2Tab(string phase)
        {
            _v2CurrentPhase = phase;
            // "全て" (All) tab removed. Only Active/Closed. v2TabAll* stay null-guarded elsewhere so the
            // deleted button/image can't cause NREs.
            if (v2TabActiveImage) v2TabActiveImage.sprite = (phase == "active") ? v2TabSelectedSprite : v2TabUnselectedSprite;
            if (v2TabClosedImage) v2TabClosedImage.sprite = (phase == "closed") ? v2TabSelectedSprite : v2TabUnselectedSprite;
            _ = PopulateV2Tournaments(phase);
        }

        // Populate the tournament list for the phase ("all" | "active" | "closed"), filtered to THIS game.
        private async System.Threading.Tasks.Task PopulateV2Tournaments(string phase)
        {
            if (v2TournamentsListRoot == null || v2TournamentCardTemplate == null) return;

            // A join is in flight — do NOT clear/repopulate the list, or we'd destroy the very card
            // whose Join button the player just tapped (which deducts a life without launching).
            // Populate resumes once the join resolves (DoJoin's finally clears _joining).
            if (_joining) return;

            // Re-entrancy guard: boot fires several SelectV2Tab()/populate calls that race (each
            // awaits, then appends -> net duplicate cards). Stamp this request; after each await bail
            // if a newer populate started, so only the LATEST populate renders. We deliberately do NOT
            // clear at method start (that would flicker the list empty on every racing call) — we clear
            // right before rendering below, so the surviving populate wins.
            int req = ++_v2PopulateReq;

            var merged = new System.Collections.Generic.List<GlobalTournament>();
            if (phase == "active")   // API's "active" already includes upcoming (not-yet-started) ones
            {
                var active = await GameBullApi.ListGlobalTournaments("active");
                if (req != _v2PopulateReq) return;   // superseded by a newer populate
                if (active != null && active.items != null) merged.AddRange(active.items);
            }
            if (phase == "closed")
            {
                var past = await GameBullApi.ListGlobalTournaments("past");
                if (req != _v2PopulateReq) return;   // superseded by a newer populate
                if (past != null && past.items != null) merged.AddRange(past.items);
            }
            if (req != _v2PopulateReq) return;       // superseded

            merged = FilterToThisGame(merged);

            // Dedupe by tournament id (safety net — e.g. the same tournament coming back from both
            // the active and past fetches on the "all" tab).
            var seen   = new System.Collections.Generic.HashSet<string>();
            var unique = new System.Collections.Generic.List<GlobalTournament>();
            foreach (var t in merged)
                if (t != null && !string.IsNullOrEmpty(t.id) && seen.Add(t.id))
                    unique.Add(t);
            merged = unique;

            // Active tab: NEVER show Ended tournaments (the API's ?phase=active may still return some).
            // Order: Active first, then Upcoming by soonest start time.
            if (phase == "active")
            {
                var vis = new System.Collections.Generic.List<GlobalTournament>();
                foreach (var t in merged)
                    if (GetPhase(t.status, t.startsAt, t.endsAt) != TournamentPhase.Ended) vis.Add(t);
                vis.Sort((a, b) =>
                {
                    int ra = SortRank(a), rb = SortRank(b);
                    if (ra != rb) return ra.CompareTo(rb);
                    return ParseUtc(a.startsAt).CompareTo(ParseUtc(b.startsAt));   // soonest start first
                });
                merged = vis;
            }

            // Clear RIGHT before rendering (not at method start), so the last populate wins even if an
            // earlier render already appended cards. ClearV2List destroys every clone, keeps the
            // template inactive.
            ClearV2List(v2TournamentsListRoot, v2TournamentCardTemplate);

            if (merged.Count == 0)
            {
                AddV2MessageRow(v2TournamentsListRoot, v2TournamentCardTemplate, "NameText", "No tournaments yet");
                RevealV2List();   // first render complete (empty) — show the list
                return;
            }

            foreach (var t in merged)
            {
                var card = Instantiate(v2TournamentCardTemplate, v2TournamentsListRoot);
                card.SetActive(true);

                // Remember this tournament's prize tiers by id, so its leaderboard rows can show per-rank prizes.
                if (!string.IsNullOrEmpty(t.id)) _prizeByTid[t.id] = t.prizeConfig;

                // ONE phase drives label, dot color, and joinability (always consistent).
                // (named tphase — 'phase' is the method's list-filter parameter)
                var tphase  = GetPhase(t.status, t.startsAt, t.endsAt);
                bool active = tphase == TournamentPhase.Active;   // joinable ONLY when active

                // Status text + status-dot color.
                var statusText = FindTmp(card, "StatusText");
                if (statusText != null) statusText.text = StatusLabel(t.status, t.startsAt, t.endsAt);
                var statusImage = FindImage(card, "StatusImage");
                if (statusImage != null)
                    statusImage.color = tphase == TournamentPhase.Upcoming ? new Color(1f,    0.85f, 0.20f, 1f)   // yellow
                                      : tphase == TournamentPhase.Active   ? new Color(0.30f, 0.80f, 0.30f, 1f)   // green
                                                                           : new Color(0.85f, 0.30f, 0.30f, 1f);  // red

                // Participants (Japanese) + a time label/value that SWITCHES by phase:
                //   Upcoming     -> "開始時間" + START time (do not show end time)
                //   Active/Ended -> "終了時間" + END time
                var countText = FindTmp(card, "CountText");
                if (countText != null) countText.text = t.participantCount + "人参加";
                bool upcoming = tphase == TournamentPhase.Upcoming;
                var dateLabel = FindTmp(card, "DateLable");   // static label — code now drives its text
                if (dateLabel != null) dateLabel.text = upcoming ? "開始時間" : "終了時間";
                var datesText = FindTmp(card, "DatesText");
                if (datesText != null) datesText.text = FormatDateTime(upcoming ? t.startsAt : t.endsAt);

                // Prize = sum of prizeConfig values. ALWAYS show both the amount and its "賞金:" label:
                // a prize -> "<sum>GB", no prize (GLORY_ONLY / empty / 0) -> "0" (no "GB" suffix).
                int prizeTotal = 0;
                if (t.prizeConfig != null)
                    foreach (var v in t.prizeConfig.Values) prizeTotal += v;
                var prizeText  = FindTmp(card, "TournamentPrizeText");
                var prizeLabel = FindTmp(card, "TournamentPrizeLable");   // template spelling: "Lable"
                if (prizeText != null)
                {
                    prizeText.gameObject.SetActive(true);
                    prizeText.text = (prizeTotal > 0) ? (prizeTotal + "GB") : "0";   // prize -> "400GB", none -> "0"
                }
                if (prizeLabel != null) prizeLabel.gameObject.SetActive(true);       // 賞金: label always visible

                // tool-built card has no Button — add one on the CLONE so the whole row is tappable.
                var btn = card.GetComponent<UnityEngine.UI.Button>();
                if (btn == null) btn = card.AddComponent<UnityEngine.UI.Button>();
                btn.enabled = true;
                string tid = t.id;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnV2TournamentTapped(tid, active));   // Join shown in detail ONLY when joinable

                // Per-row Join button (child Button "JoinButton") — direct join of THIS tournament.
                // As a CHILD button over the card's root Button, the event system gives it priority, so
                // its own tap is swallowed (the card bar-tap / leaderboard does NOT fire). Its Image has
                // raycastTarget=true on the template, so it receives the tap.
                var joinBtn = FindButton(card, "JoinButton");
                if (joinBtn != null)
                {
                    joinBtn.interactable = active;   // joinable ONLY when active; upcoming/ended -> false.
                                                     // (skin's Disabled Color = Normal, so it stays full-color.)
                    joinBtn.onClick.RemoveAllListeners();
                    joinBtn.onClick.AddListener(() => OnV2RowJoin(tid));   // 'tid' is the per-card local (closure-safe)
                }
            }

            RevealV2List();   // first render complete (cards) — show the list + rebuild layout
        }

        // Tap a card -> DETAIL state: show its leaderboard; Join is shown only for an ACTIVE tournament.
        public async void OnV2TournamentTapped(string id, bool isActive)
        {
            _v2SelectedId = id;
            // Per-rank prize map for THIS tournament (captured when the list was built).
            System.Collections.Generic.Dictionary<string,int> pc = null;
            _prizeByTid.TryGetValue(id, out pc);
            if (v2ListState)   v2ListState.SetActive(false);
            if (v2DetailState) v2DetailState.SetActive(true);
            if (v2JoinButton)
            {
                v2JoinButton.gameObject.SetActive(isActive);   // hidden for closed tournaments
                v2JoinButton.interactable = isActive;
            }

            if (v2LeaderboardListRoot == null || v2LeaderboardRowTemplate == null) return;
            ClearV2List(v2LeaderboardListRoot, v2LeaderboardRowTemplate);
            var lb = await GameBullApi.GetGlobalLeaderboardById(id);
            if (lb == null || lb.items == null || lb.items.Length == 0)
            {
                AddV2MessageRow(v2LeaderboardListRoot, v2LeaderboardRowTemplate, "PlayerNameText", "No scores yet");
                return;
            }
            int rowIndex = 1;
            foreach (var item in lb.items)
            {
                var row = Instantiate(v2LeaderboardRowTemplate, v2LeaderboardListRoot);
                row.SetActive(true);

                // Prefer the API's rank field when present/positive; else fall back to the 1-based row order.
                int rank = (item.rank.HasValue && item.rank.Value > 0) ? item.rank.Value : rowIndex;

                // Fill named children (null-guarded — skip any that's missing).
                var counterText = FindTmp(row, "PlayerCounterText");
                var nameText    = FindTmp(row, "PlayerNameText");
                var scoreText   = FindTmp(row, "PlayerScoreText");
                if (counterText != null)
                {
                    counterText.text  = rank.ToString();
                    counterText.color = rank == 1 ? new Color(1f, 0.84f, 0f)         // gold
                                      : rank == 2 ? new Color(0.75f, 0.75f, 0.75f)   // silver
                                      : rank == 3 ? new Color(0.80f, 0.50f, 0.20f)   // bronze
                                      :             Color.white;
                }
                // Name only — keep the existing isMe "► " highlight; do NOT change its color.
                if (nameText != null) nameText.text = (item.isMe ? "► " : "") + (string.IsNullOrEmpty(item.alias) ? "Player" : item.alias);
                if (scoreText != null)
                {
                    scoreText.text  = item.score.GetValueOrDefault().ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
                    scoreText.color = new Color(1f, 0.95f, 0.65f);   // slight yellow (all rows)
                }

                // Per-rank prize (GB points) from the tournament's prizeConfig — HIDDEN when this rank
                // has no prize (ranks beyond the prize tiers). Handles any depth (top 3, top 10, …).
                var gbText = FindTmp(row, "GB points");   // exact child name (note the space)
                if (gbText != null)
                {
                    int prize = 0;
                    if (pc != null) pc.TryGetValue(rank.ToString(), out prize);
                    bool hasPrize = prize > 0;
                    gbText.gameObject.SetActive(hasPrize);
                    if (hasPrize) gbText.text = prize + "GB";
                }

                // Avatar by row position: (rank-1) mod count. Skip if no avatars assigned.
                var charImage = FindImage(row, "characterimage");
                if (charImage != null && v2CharacterImages != null && v2CharacterImages.Length > 0)
                    charImage.sprite = v2CharacterImages[(rank - 1) % v2CharacterImages.Length];

                // Current player's row (isMe): swap the bar background to the green-box highlight sprite.
                // Only when a sprite is assigned; other rows keep the template's default bar. Rows are
                // fresh clones each populate (ClearV2List), so a non-me row never carries a leftover box.
                var rowBg = row.GetComponent<UnityEngine.UI.Image>();   // RowTemplate root = the bar Image
                if (item.isMe && v2MyRowSprite != null && rowBg != null)
                    rowBg.sprite = v2MyRowSprite;

                rowIndex++;
            }
        }

        // Back from DETAIL -> LIST (list is untouched, no repopulate).
        public void OnV2Back()
        {
            if (v2DetailState) v2DetailState.SetActive(false);
            if (v2ListState)   v2ListState.SetActive(true);
        }

        // Join the SELECTED tournament (Join button on the detail panel). Routes through DoJoin.
        public void OnV2Join()
        {
            if (string.IsNullOrEmpty(_v2SelectedId)) return;
            _tournamentId = _v2SelectedId;
            DoJoin(_v2SelectedId);
        }

        // Direct join from a tournament card's per-row Join button — join THAT tournament directly
        // (no leaderboard detail). Routes through the SAME DoJoin path.
        public void OnV2RowJoin(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            _v2SelectedId = id;
            _tournamentId = id;
            DoJoin(id);
        }

        // The ONE join implementation: mint a session (spends a life), then launch gameplay via the SAME
        // onStartSolo hook StartTournamentPlay uses. Score-submit targets _tournamentId (set by callers).
        // Out-of-lives -> popup whose OK returns to the lobby home; any other failure -> generic error popup.
        private async void DoJoin(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            // Hold _joining for the WHOLE join so a boot/tab repopulate can't clear the list (and the
            // tapped card) mid-flight. DoJoin lives on the persistent controller, so the launch below
            // completes even if the card GameObject is destroyed — the flag just protects the UI/list.
            _joining = true;
            try
            {
                var sess = await GameBullApi.StartGlobalTournamentSession(id);
                if (sess == null || string.IsNullOrEmpty(sess.sessionToken))
                {
                    if (GameBullClient.LastErrorCode == "play.no_lives")
                    {
                        await RefreshLives();
                        RenderV2Header();
                        ShowMessage("ライフがありません",
                            "ホーム画面に戻り、ライフを獲得してください。",
                            ShowLobbyHome, "ホームへ戻る");
                    }
                    else
                    {
                        ShowMessage("Error", "Couldn't start. Try again.");
                    }
                    return;
                }

                // Success: mint spent a life + stored CurrentGlobalToken. Point scoring at this tournament.
                _tournamentId = id;
                _tournamentEntryId = null;
                _playMode = PlayMode.Tournament;
                _ = RefreshLives();
                RenderV2Header();

                // Hide the v2 panel (and the old picker) and launch gameplay via the SAME hook.
                if (mainPanelV2)     mainPanelV2.SetActive(false);
                if (modePickerPanel) modePickerPanel.SetActive(false);
                onStartSolo?.Invoke();
                if (!string.IsNullOrEmpty(gameplaySceneName))
                    UnityEngine.SceneManagement.SceneManager.LoadScene(gameplaySceneName);
            }
            finally { _joining = false; }   // always clear, even on failure/exception
        }

        // Clear a v2 scroll list: destroy all rows except the template, keep the template inactive.
        private void ClearV2List(Transform root, GameObject template)
        {
            if (root == null) return;
            foreach (Transform c in root)
                if (c.gameObject != template) Destroy(c.gameObject);
            if (template) template.SetActive(false);
        }

        // Add a single non-interactive message row (empty states) cloned from the list's template,
        // so it inherits the template's width + layout. The message goes ONLY in the wide NAME field
        // (nameField, e.g. "NameText"/"PlayerNameText"); every other text is blanked. Writing to a
        // named field — not texts[0] — avoids dumping the message into the narrow Number/Counter
        // column, which made it wrap one character per line.
        private void AddV2MessageRow(Transform root, GameObject template, string nameField, string message)
        {
            if (root == null || template == null) return;
            var row = Instantiate(template, root);
            row.SetActive(true);

            // Text: put the message ONLY in the wide name field; blank every other TMP.
            foreach (var tmp in row.GetComponentsInChildren<TMPro.TMP_Text>(true))
                tmp.text = (tmp.gameObject.name == nameField) ? message : "";

            // Hide the whole Join button (its background + heart + label at once) so no leftover
            // green/heart button shows on the empty-state row. (It's a CHILD button, so the old
            // row.GetComponent<Button>() never caught it.)
            var joinBtn = FindButton(row, "JoinButton");
            if (joinBtn != null) joinBtn.gameObject.SetActive(false);

            // Hide EVERY child Image EXCEPT the row's own root background — catches the avatar
            // (characterimage), status dot, arrow, trophy, the button's heart, etc. Only the bar
            // background Image and the name text remain.
            var rootImage = row.GetComponent<UnityEngine.UI.Image>();
            foreach (var img in row.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (img != rootImage) img.enabled = false;

            // Make the row inert if the clone's root happens to carry a Button.
            var rootBtn = row.GetComponent<UnityEngine.UI.Button>();
            if (rootBtn != null) rootBtn.enabled = false;
        }

        // Find a TMP child by GameObject name anywhere in the clone (recursive; null if absent).
        private static TMPro.TMP_Text FindTmp(GameObject root, string name)
        {
            foreach (var tmp in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
                if (tmp.gameObject.name == name) return tmp;
            return null;
        }

        // Find a Button child by GameObject name anywhere in the clone (recursive; null if absent).
        private static UnityEngine.UI.Button FindButton(GameObject root, string name)
        {
            foreach (var b in root.GetComponentsInChildren<UnityEngine.UI.Button>(true))
                if (b.gameObject.name == name) return b;
            return null;
        }

        // Find an Image child by GameObject name anywhere in the clone (recursive; null if absent).
        private static UnityEngine.UI.Image FindImage(GameObject root, string name)
        {
            foreach (var img in root.GetComponentsInChildren<UnityEngine.UI.Image>(true))
                if (img.gameObject.name == name) return img;
            return null;
        }

        // ============================================================================
        // Game-End panel (v2 scorecard) — RUNTIME LOGIC (additive).
        // ============================================================================

        // Show the new game-end panel. score = comma-formatted final score; rankText = the
        // "Your rank: #N" string built the same way as the old scorecard.
        public void ShowGameEndPanel(int score, string rankText, string bestScoreText)
        {
            if (gameEndPanel == null) return;
            gameEndPanel.SetActive(true);   // opaque overlay; mainPanelV2 can stay behind
            if (gameEndScoreText     != null) gameEndScoreText.text     = score.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            if (gameEndRankText      != null) gameEndRankText.text      = rankText;       // RANK only
            if (gameEndBestScoreText != null) gameEndBestScoreText.text = bestScoreText;  // BEST score
        }

        // Play Again — reuse the SAME re-mint/relaunch path (StartTournamentPlay re-mints _tournamentId).
        public void OnGameEndPlayAgain()
        {
            if (gameEndPanel) gameEndPanel.SetActive(false);
            if (mainPanelV2)  mainPanelV2.SetActive(false);   // so v2 doesn't overlay the relaunched game
            StartTournamentPlay();
        }

        // Home — back to the lobby. Route through ReturnToLobbyNoReload so BasketBall (multi-scene)
        // loads the Menu scene (OnSceneLoaded then re-shows the lobby); LapsFuse (single-scene) still
        // just re-shows the picker in place.
        public void OnGameEndHome()
        {
            if (gameEndPanel) gameEndPanel.SetActive(false);
            ReturnToLobbyNoReload();
        }

        // View Leaderboard — open v2 on the SAME tournament's detail/leaderboard. Just-played => active.
        public void OnGameEndLeaderboard()
        {
            if (gameEndPanel) gameEndPanel.SetActive(false);
            ShowMainPanelV2();
            OnV2TournamentTapped(_tournamentId, true);
        }

        public async Task LoadContext()
        {
            Context = await GameBullApi.GetContext();

            if (Context == null)
            {
                if (greetingText) greetingText.text = "Couldn't load (offline?)";
                // For local/dev testing without a token, still show buttons so the UI is usable:
                if (soloButton)       soloButton.gameObject.SetActive(true);
                if (tournamentButton) tournamentButton.gameObject.SetActive(true);
                return;
            }

            // Joiner path: arrived via a share link (roomId in URL) and the server returned the room.
            bool isJoiner = !string.IsNullOrEmpty(GameBullBoot.RoomId) && Context != null && Context.room != null;
            if (isJoiner)
            {
                ShowJoinPanel();
                return;
            }

            // Greeting + lives
            RenderHeader();

            // Show only enabled modes
            bool solo = HasMode("HEAD_TO_HEAD_1V1");
            bool room = HasMode("TOURNAMENT_ROOM");
            if (soloButton)       soloButton.gameObject.SetActive(solo);
            if (tournamentButton) tournamentButton.gameObject.SetActive(room);
            if (openTournamentButton) openTournamentButton.gameObject.SetActive(HasMode("OPEN_TOURNAMENT"));
            if (historyMenuButton) historyMenuButton.gameObject.SetActive(true);   // always available for logged-in users

            // Drive the v2 main panel now that lives/name are available (additive — old picker still exists behind it).
            ShowMainPanelV2();
            _v2ShownOnBoot = true;   // so Start() doesn't hide + re-show (which fires a SECOND populate)
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
