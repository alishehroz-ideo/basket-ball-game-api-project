using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace GameBull.EditorTools
{
    // Editor window that will (step by step) generate the GameBull lobby UI into a scene.
    public class GameBullSetupWindow : EditorWindow
    {
        [MenuItem("GameBull/Setup Lobby")]
        public static void Open()
        {
            var window = GetWindow<GameBullSetupWindow>("GameBull Setup");
            window.minSize = new Vector2(360, 240);
            window.Show();
        }

        void OnGUI()
        {
            GUILayout.Space(10);
            EditorGUILayout.LabelField("GameBull — Lobby Setup", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "This tool will generate the GameBull lobby UI (mode picker, create-room, share, join) into your chosen scene, step by step.",
                MessageType.Info);

            GUILayout.Space(10);

            if (GUILayout.Button("Create Lobby Canvas", GUILayout.Height(40)))
            {
                CreateLobbyCanvas();
            }
        }

        // Builds  GameBullLobby > Canvas (Canvas + CanvasScaler + GraphicRaycaster)
        //                          > ModePickerPanel (full-screen Image backdrop)
        // into the currently open scene, plus an EventSystem if one is missing.
        private void CreateLobbyCanvas()
        {
            var existing = GameObject.Find("GameBullLobby");
            if (existing != null)
            {
                bool recreate = EditorUtility.DisplayDialog(
                    "GameBull",
                    "A \"GameBullLobby\" object already exists in this scene.\n\nDelete it and recreate?",
                    "Delete & Recreate",
                    "Cancel");
                if (!recreate) return;
            }

            // Group every change so a single Ctrl+Z reverts the whole creation.
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName("Create GameBull Lobby");
            int undoGroup = Undo.GetCurrentGroup();

            if (existing != null)
                Undo.DestroyObjectImmediate(existing);

            // Root: plain empty GameObject. Registering it for undo also removes its
            // children (Canvas, ModePickerPanel) when undone.
            var root = new GameObject("GameBullLobby");
            Undo.RegisterCreatedObjectUndo(root, "Create GameBull Lobby");

            // Canvas + CanvasScaler + GraphicRaycaster
            var canvasGO = new GameObject("Canvas", typeof(RectTransform));
            canvasGO.transform.SetParent(root.transform, false);

            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;   // sit on top of the game

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720f, 1280f);  // portrait
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ModePickerPanel: stretched full-screen with a semi-transparent black backdrop.
            var panelGO = new GameObject("ModePickerPanel", typeof(RectTransform));
            panelGO.transform.SetParent(canvasGO.transform, false);

            var panelRT = (RectTransform)panelGO.transform;
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            var backdrop = panelGO.AddComponent<Image>();
            backdrop.color = new Color(0f, 0f, 0f, 0.6f);

            // ---- Mode-picker card (layout only; no button logic yet) ----
            // Centered vertical card with a flat dark background.
            var cardGO = new GameObject("Card", typeof(RectTransform));
            cardGO.transform.SetParent(panelGO.transform, false);

            var cardRT = (RectTransform)cardGO.transform;
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(600f, 800f);
            cardRT.anchoredPosition = Vector2.zero;

            var cardBg = cardGO.AddComponent<Image>();
            cardBg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            var layout = cardGO.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 40, 40);
            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // 1) Title
            CreateText(cardGO.transform, "TitleText", "GameBull",
                48f, true, Color.white, 70f);

            // 2) Greeting (placeholder text)
            var greetingTMP = CreateText(cardGO.transform, "GreetingText", "Hi, Player",
                30f, false, new Color(0.80f, 0.80f, 0.80f, 1f), 50f);

            // 3) Lives (placeholder text)
            var livesTMP = CreateText(cardGO.transform, "LivesText", "Lives: 5 / 5",
                26f, false, new Color(1f, 0.82f, 0.30f, 1f), 45f);

            // 4) Flexible spacer pushes the buttons toward the bottom.
            var spacerGO = new GameObject("Spacer", typeof(RectTransform));
            spacerGO.transform.SetParent(cardGO.transform, false);
            var spacerLE = spacerGO.AddComponent<LayoutElement>();
            spacerLE.flexibleHeight = 1f;

            // 5) Solo button (blue)
            var soloButton = CreateButton(cardGO.transform, "SoloButton", "Solo Play",
                new Color(0.16f, 0.45f, 0.92f, 1f), 90f, 30f);

            // 6) Tournament button (purple)
            var tournamentButton = CreateButton(cardGO.transform, "TournamentButton", "Create Tournament",
                new Color(0.55f, 0.30f, 0.85f, 1f), 90f, 30f);

            // 7) Open Tournaments button (orange) — entry point to the open-tournament list
            var openTournamentButton = CreateButton(cardGO.transform, "OpenTournamentButton", "Join Open Tournaments",
                new Color(0.90f, 0.55f, 0.15f, 1f), 90f, 30f);

            // 8) My History button (teal) — opens the My History panel
            var historyMenuButton = CreateButton(cardGO.transform, "HistoryMenuButton", "My History",
                new Color(0.20f, 0.60f, 0.60f, 1f), 90f, 30f);

            // ---- Create-room panel: sibling of ModePickerPanel, hidden until Tournament is tapped ----
            var createRoomPanelGO = new GameObject("CreateRoomPanel", typeof(RectTransform));
            createRoomPanelGO.transform.SetParent(canvasGO.transform, false);

            var roomPanelRT = (RectTransform)createRoomPanelGO.transform;
            roomPanelRT.anchorMin = Vector2.zero;
            roomPanelRT.anchorMax = Vector2.one;
            roomPanelRT.offsetMin = Vector2.zero;
            roomPanelRT.offsetMax = Vector2.zero;

            var roomBackdrop = createRoomPanelGO.AddComponent<Image>();
            roomBackdrop.color = new Color(0f, 0f, 0f, 0.6f);

            // Centered card (mirrors the mode-picker card).
            var roomCardGO = new GameObject("Card", typeof(RectTransform));
            roomCardGO.transform.SetParent(createRoomPanelGO.transform, false);

            var roomCardRT = (RectTransform)roomCardGO.transform;
            roomCardRT.anchorMin = new Vector2(0.5f, 0.5f);
            roomCardRT.anchorMax = new Vector2(0.5f, 0.5f);
            roomCardRT.pivot = new Vector2(0.5f, 0.5f);
            roomCardRT.sizeDelta = new Vector2(600f, 800f);
            roomCardRT.anchoredPosition = Vector2.zero;

            var roomCardBg = roomCardGO.AddComponent<Image>();
            roomCardBg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            var roomLayout = roomCardGO.AddComponent<VerticalLayoutGroup>();
            roomLayout.padding = new RectOffset(40, 40, 40, 40);
            roomLayout.spacing = 24f;
            roomLayout.childAlignment = TextAnchor.UpperCenter;
            roomLayout.childControlWidth = true;
            roomLayout.childControlHeight = true;
            roomLayout.childForceExpandWidth = true;
            roomLayout.childForceExpandHeight = false;

            // 1) Title
            CreateText(roomCardGO.transform, "Title", "Create Tournament",
                40f, true, Color.white, 70f);

            // 2) "Room name" label (left-aligned)
            CreateText(roomCardGO.transform, "RoomNameLabel", "Room name",
                24f, false, new Color(0.80f, 0.80f, 0.80f, 1f), 35f, TextAlignmentOptions.Left);

            // 3) Room-name input field
            var nameInput = CreateInputField(roomCardGO.transform, "NameInput", "e.g. Friday Fun", 70f);

            // 4) "Expires in" label (left-aligned)
            CreateText(roomCardGO.transform, "ExpiresLabel", "Expires in",
                24f, false, new Color(0.80f, 0.80f, 0.80f, 1f), 35f, TextAlignmentOptions.Left);

            // 5) Expiry row: two side-by-side toggle-style buttons (selection logic comes later).
            var expiryRowGO = new GameObject("ExpiryRow", typeof(RectTransform));
            expiryRowGO.transform.SetParent(roomCardGO.transform, false);

            var expiryHLG = expiryRowGO.AddComponent<HorizontalLayoutGroup>();
            expiryHLG.spacing = 16f;
            expiryHLG.childControlWidth = true;
            expiryHLG.childForceExpandWidth = true;
            expiryHLG.childControlHeight = true;
            expiryHLG.childForceExpandHeight = true;

            var expiryRowLE = expiryRowGO.AddComponent<LayoutElement>();
            expiryRowLE.minHeight = 70f;
            expiryRowLE.preferredHeight = 70f;

            // Neutral slate (unselected-toggle look); real selection visuals come later.
            var expiry30Button = CreateButton(expiryRowGO.transform, "Expiry30Button", "5 min",
                new Color(0.20f, 0.23f, 0.29f, 1f), 70f, 26f);
            var expiry60Button = CreateButton(expiryRowGO.transform, "Expiry60Button", "30 min",
                new Color(0.20f, 0.23f, 0.29f, 1f), 70f, 26f);

            // 6) Flexible spacer
            var roomSpacerGO = new GameObject("Spacer", typeof(RectTransform));
            roomSpacerGO.transform.SetParent(roomCardGO.transform, false);
            roomSpacerGO.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // 7) Create Room (green)
            var createRoomButton = CreateButton(roomCardGO.transform, "CreateButton", "Create Room",
                new Color(0.16f, 0.70f, 0.40f, 1f), 90f, 30f);

            // 8) Back (gray)
            var createRoomBackButton = CreateButton(roomCardGO.transform, "BackButton", "Back",
                new Color(0.30f, 0.30f, 0.35f, 1f), 70f, 26f);

            createRoomPanelGO.SetActive(false);   // hidden until Tournament is tapped

            // ---- Share panel: third sibling under the Canvas, shown after a room is created ----
            var sharePanelGO = new GameObject("SharePanel", typeof(RectTransform));
            sharePanelGO.transform.SetParent(canvasGO.transform, false);

            var sharePanelRT = (RectTransform)sharePanelGO.transform;
            sharePanelRT.anchorMin = Vector2.zero;
            sharePanelRT.anchorMax = Vector2.one;
            sharePanelRT.offsetMin = Vector2.zero;
            sharePanelRT.offsetMax = Vector2.zero;

            var shareBackdrop = sharePanelGO.AddComponent<Image>();
            shareBackdrop.color = new Color(0f, 0f, 0f, 0.6f);

            // Centered card (mirrors the other cards; spacing 20).
            var shareCardGO = new GameObject("Card", typeof(RectTransform));
            shareCardGO.transform.SetParent(sharePanelGO.transform, false);

            var shareCardRT = (RectTransform)shareCardGO.transform;
            shareCardRT.anchorMin = new Vector2(0.5f, 0.5f);
            shareCardRT.anchorMax = new Vector2(0.5f, 0.5f);
            shareCardRT.pivot = new Vector2(0.5f, 0.5f);
            shareCardRT.sizeDelta = new Vector2(600f, 800f);
            shareCardRT.anchoredPosition = Vector2.zero;

            var shareCardBg = shareCardGO.AddComponent<Image>();
            shareCardBg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            var shareLayout = shareCardGO.AddComponent<VerticalLayoutGroup>();
            shareLayout.padding = new RectOffset(40, 40, 40, 40);
            shareLayout.spacing = 20f;
            shareLayout.childAlignment = TextAnchor.UpperCenter;
            shareLayout.childControlWidth = true;
            shareLayout.childControlHeight = true;
            shareLayout.childForceExpandWidth = true;
            shareLayout.childForceExpandHeight = false;

            // 1) Title
            CreateText(shareCardGO.transform, "Title", "Room Created!",
                40f, true, Color.white, 70f);

            // 2) Room name
            var roomNameText = CreateText(shareCardGO.transform, "RoomNameText", "Room: —",
                26f, false, new Color(0.80f, 0.80f, 0.80f, 1f), 45f);

            // 3) "Share this link:" label (left)
            CreateText(shareCardGO.transform, "ShareLinkLabel", "Share this link:",
                22f, false, new Color(0.70f, 0.70f, 0.70f, 1f), 35f, TextAlignmentOptions.Left);

            // 4) Join URL: a box (bg image) with wrapping text inside (TMP wraps by default).
            var joinUrlBoxGO = new GameObject("JoinUrlBox", typeof(RectTransform));
            joinUrlBoxGO.transform.SetParent(shareCardGO.transform, false);
            var joinUrlBg = joinUrlBoxGO.AddComponent<Image>();
            joinUrlBg.color = new Color(1f, 1f, 1f, 0.08f);
            var joinUrlBoxLE = joinUrlBoxGO.AddComponent<LayoutElement>();
            joinUrlBoxLE.minHeight = 120f;
            joinUrlBoxLE.preferredHeight = 120f;

            var joinUrlTextGO = new GameObject("JoinUrlText", typeof(RectTransform));
            joinUrlTextGO.transform.SetParent(joinUrlBoxGO.transform, false);
            var joinUrlTextRT = (RectTransform)joinUrlTextGO.transform;
            joinUrlTextRT.anchorMin = Vector2.zero;
            joinUrlTextRT.anchorMax = Vector2.one;
            joinUrlTextRT.offsetMin = new Vector2(12f, 8f);
            joinUrlTextRT.offsetMax = new Vector2(-12f, -8f);
            var joinUrlText = joinUrlTextGO.AddComponent<TextMeshProUGUI>();
            joinUrlText.text = "—";
            joinUrlText.fontSize = 20f;
            joinUrlText.alignment = TextAlignmentOptions.Center;
            joinUrlText.color = Color.white;

            // 5) Copy button (blue)
            var copyButton = CreateButton(shareCardGO.transform, "CopyButton", "Copy Link",
                new Color(0.16f, 0.45f, 0.92f, 1f), 80f, 28f);

            // 6) Players
            var playersText = CreateText(shareCardGO.transform, "PlayersText", "Players: 1 / 4",
                22f, false, new Color(0.70f, 0.70f, 0.70f, 1f), 40f);

            // 7) Flexible spacer
            var shareSpacerGO = new GameObject("Spacer", typeof(RectTransform));
            shareSpacerGO.transform.SetParent(shareCardGO.transform, false);
            shareSpacerGO.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // 8) Play as Host (green)
            var playAsHostButton = CreateButton(shareCardGO.transform, "PlayAsHostButton", "Play as Host",
                new Color(0.16f, 0.70f, 0.40f, 1f), 90f, 30f);

            // 9) Back to Lobby (gray)
            var shareBackButton = CreateButton(shareCardGO.transform, "ShareBackButton", "Back to Lobby",
                new Color(0.30f, 0.30f, 0.35f, 1f), 70f, 26f);

            sharePanelGO.SetActive(false);   // shown only after a room is created

            // ---- Join panel: 4th sibling under the Canvas, shown when booted via a share link ----
            var joinPanelGO = new GameObject("JoinPanel", typeof(RectTransform));
            joinPanelGO.transform.SetParent(canvasGO.transform, false);

            var joinPanelRT = (RectTransform)joinPanelGO.transform;
            joinPanelRT.anchorMin = Vector2.zero;
            joinPanelRT.anchorMax = Vector2.one;
            joinPanelRT.offsetMin = Vector2.zero;
            joinPanelRT.offsetMax = Vector2.zero;

            var joinBackdrop = joinPanelGO.AddComponent<Image>();
            joinBackdrop.color = new Color(0f, 0f, 0f, 0.6f);

            // Centered card (mirrors the other cards; spacing 20).
            var joinCardGO = new GameObject("Card", typeof(RectTransform));
            joinCardGO.transform.SetParent(joinPanelGO.transform, false);

            var joinCardRT = (RectTransform)joinCardGO.transform;
            joinCardRT.anchorMin = new Vector2(0.5f, 0.5f);
            joinCardRT.anchorMax = new Vector2(0.5f, 0.5f);
            joinCardRT.pivot = new Vector2(0.5f, 0.5f);
            joinCardRT.sizeDelta = new Vector2(600f, 800f);
            joinCardRT.anchoredPosition = Vector2.zero;

            var joinCardBg = joinCardGO.AddComponent<Image>();
            joinCardBg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            var joinLayout = joinCardGO.AddComponent<VerticalLayoutGroup>();
            joinLayout.padding = new RectOffset(40, 40, 40, 40);
            joinLayout.spacing = 20f;
            joinLayout.childAlignment = TextAnchor.UpperCenter;
            joinLayout.childControlWidth = true;
            joinLayout.childControlHeight = true;
            joinLayout.childForceExpandWidth = true;
            joinLayout.childForceExpandHeight = false;

            // 1) Title
            CreateText(joinCardGO.transform, "Title", "Join Room",
                40f, true, Color.white, 70f);

            // 2) Room name
            var joinRoomNameText = CreateText(joinCardGO.transform, "JoinRoomNameText", "Room: —",
                28f, false, Color.white, 50f);

            // 3) Players
            var joinPlayersText = CreateText(joinCardGO.transform, "JoinPlayersText", "Players: —",
                22f, false, new Color(0.80f, 0.80f, 0.80f, 1f), 45f);

            // 4) "Your nickname" label (left)
            CreateText(joinCardGO.transform, "NicknameLabel", "Your nickname",
                24f, false, new Color(0.70f, 0.70f, 0.70f, 1f), 35f, TextAlignmentOptions.Left);

            // 5) Nickname input
            var nicknameInput = CreateInputField(joinCardGO.transform, "NicknameInput", "Enter a nickname", 70f);

            // 6) Flexible spacer
            var joinSpacerGO = new GameObject("Spacer", typeof(RectTransform));
            joinSpacerGO.transform.SetParent(joinCardGO.transform, false);
            joinSpacerGO.AddComponent<LayoutElement>().flexibleHeight = 1f;

            // 7) Join + Play (green)
            var joinButton = CreateButton(joinCardGO.transform, "JoinButton", "Join + Play",
                new Color(0.16f, 0.70f, 0.40f, 1f), 90f, 30f);

            joinPanelGO.SetActive(false);   // shown only for joiners (roomId in URL)

            // ---- Leaderboard report card: 5th sibling under the Canvas, shown after a ROOM game ----
            var leaderboardPanelGO = new GameObject("LeaderboardPanel", typeof(RectTransform));
            leaderboardPanelGO.transform.SetParent(canvasGO.transform, false);

            var lbPanelRT = (RectTransform)leaderboardPanelGO.transform;
            lbPanelRT.anchorMin = Vector2.zero;
            lbPanelRT.anchorMax = Vector2.one;
            lbPanelRT.offsetMin = Vector2.zero;
            lbPanelRT.offsetMax = Vector2.zero;

            var lbBackdrop = leaderboardPanelGO.AddComponent<Image>();
            lbBackdrop.color = new Color(0f, 0f, 0f, 0.6f);

            // Centered card (mirrors the other cards).
            var lbCardGO = new GameObject("Card", typeof(RectTransform));
            lbCardGO.transform.SetParent(leaderboardPanelGO.transform, false);

            var lbCardRT = (RectTransform)lbCardGO.transform;
            lbCardRT.anchorMin = new Vector2(0.5f, 0.5f);
            lbCardRT.anchorMax = new Vector2(0.5f, 0.5f);
            lbCardRT.pivot = new Vector2(0.5f, 0.5f);
            lbCardRT.sizeDelta = new Vector2(600f, 800f);
            lbCardRT.anchoredPosition = Vector2.zero;

            var lbCardBg = lbCardGO.AddComponent<Image>();
            lbCardBg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            var lbLayout = lbCardGO.AddComponent<VerticalLayoutGroup>();
            lbLayout.padding = new RectOffset(40, 40, 40, 40);
            lbLayout.spacing = 20f;
            lbLayout.childAlignment = TextAnchor.UpperCenter;
            lbLayout.childControlWidth = true;
            lbLayout.childControlHeight = true;
            lbLayout.childForceExpandWidth = true;
            lbLayout.childForceExpandHeight = false;

            // 1) Title
            CreateText(lbCardGO.transform, "Title", "Leaderboard",
                40f, true, Color.white, 70f);

            // 2) ScrollView (vertical) that fills the remaining card height.
            var lbScrollGO = new GameObject("ScrollView", typeof(RectTransform));
            lbScrollGO.transform.SetParent(lbCardGO.transform, false);
            var lbScrollBg = lbScrollGO.AddComponent<Image>();
            lbScrollBg.color = new Color(1f, 1f, 1f, 0.05f);   // also gives the scroll area something to drag on
            var lbScrollRect = lbScrollGO.AddComponent<ScrollRect>();
            lbScrollRect.horizontal = false;
            lbScrollRect.vertical = true;
            lbScrollRect.movementType = ScrollRect.MovementType.Clamped;
            lbScrollRect.scrollSensitivity = 20f;
            var lbScrollLE = lbScrollGO.AddComponent<LayoutElement>();
            lbScrollLE.flexibleHeight = 1f;   // take all leftover card height (pins Menu to the bottom)
            lbScrollLE.minHeight = 400f;

            // Viewport (clips the content)
            var lbViewportGO = new GameObject("Viewport", typeof(RectTransform));
            lbViewportGO.transform.SetParent(lbScrollGO.transform, false);
            StretchFull((RectTransform)lbViewportGO.transform);
            lbViewportGO.AddComponent<RectMask2D>();

            // Content (rows go here) — grows with its children via ContentSizeFitter.
            var lbContentGO = new GameObject("Content", typeof(RectTransform));
            lbContentGO.transform.SetParent(lbViewportGO.transform, false);
            var lbContentRT = (RectTransform)lbContentGO.transform;
            lbContentRT.anchorMin = new Vector2(0f, 1f);
            lbContentRT.anchorMax = new Vector2(1f, 1f);
            lbContentRT.pivot = new Vector2(0.5f, 1f);
            lbContentRT.anchoredPosition = Vector2.zero;
            lbContentRT.sizeDelta = Vector2.zero;

            var lbContentVLG = lbContentGO.AddComponent<VerticalLayoutGroup>();
            lbContentVLG.padding = new RectOffset(8, 8, 8, 8);
            lbContentVLG.spacing = 8f;
            lbContentVLG.childAlignment = TextAnchor.UpperCenter;
            lbContentVLG.childControlWidth = true;
            lbContentVLG.childControlHeight = true;
            lbContentVLG.childForceExpandWidth = true;
            lbContentVLG.childForceExpandHeight = false;

            var lbContentCSF = lbContentGO.AddComponent<ContentSizeFitter>();
            lbContentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            lbScrollRect.viewport = (RectTransform)lbViewportGO.transform;
            lbScrollRect.content  = lbContentRT;

            // Row template: horizontal row with 3 TMP columns (rank, name, score). Inactive = template.
            var lbRowGO = new GameObject("RowTemplate", typeof(RectTransform));
            lbRowGO.transform.SetParent(lbContentGO.transform, false);
            var lbRowBg = lbRowGO.AddComponent<Image>();
            lbRowBg.color = new Color(1f, 1f, 1f, 0.04f);
            var lbRowHLG = lbRowGO.AddComponent<HorizontalLayoutGroup>();
            lbRowHLG.padding = new RectOffset(8, 8, 4, 4);
            lbRowHLG.spacing = 10f;
            lbRowHLG.childAlignment = TextAnchor.MiddleLeft;
            lbRowHLG.childControlWidth = true;
            lbRowHLG.childControlHeight = true;
            lbRowHLG.childForceExpandWidth = false;
            lbRowHLG.childForceExpandHeight = true;
            var lbRowLE = lbRowGO.AddComponent<LayoutElement>();
            lbRowLE.minHeight = 56f;
            lbRowLE.preferredHeight = 56f;

            // Column 1: rank (narrow, centered)
            var lbRankText = CreateText(lbRowGO.transform, "RankText", "1.",
                26f, true, Color.white, 56f, TextAlignmentOptions.Center);
            var lbRankLE = lbRankText.GetComponent<LayoutElement>();
            lbRankLE.minWidth = 70f;
            lbRankLE.preferredWidth = 70f;

            // Column 2: name (flexible, left)
            var lbNameText = CreateText(lbRowGO.transform, "NameText", "Player",
                26f, false, Color.white, 56f, TextAlignmentOptions.Left);
            lbNameText.GetComponent<LayoutElement>().flexibleWidth = 1f;

            // Column 3: score (medium, right)
            var lbScoreText = CreateText(lbRowGO.transform, "ScoreText", "0",
                26f, true, new Color(1f, 0.82f, 0.30f, 1f), 56f, TextAlignmentOptions.Right);
            var lbScoreLE = lbScoreText.GetComponent<LayoutElement>();
            lbScoreLE.minWidth = 120f;
            lbScoreLE.preferredWidth = 140f;

            lbRowGO.SetActive(false);   // template — clones are made at runtime by the controller

            // 3) Refresh button (blue) — re-fetch + re-render the standings (card stays open)
            var leaderboardRefreshButton = CreateButton(lbCardGO.transform, "RefreshButton", "Refresh",
                new Color(0.16f, 0.45f, 0.92f, 1f), 80f, 28f);

            // 4) Menu button (gray) — returns home
            var leaderboardMenuButton = CreateButton(lbCardGO.transform, "MenuButton", "Menu",
                new Color(0.30f, 0.30f, 0.35f, 1f), 90f, 30f);

            leaderboardPanelGO.SetActive(false);   // shown only after a ROOM game ends

            // ---- Open Tournament: LIST panel (6th sibling under the Canvas) ----
            var tListPanelGO = new GameObject("TournamentListPanel", typeof(RectTransform));
            tListPanelGO.transform.SetParent(canvasGO.transform, false);

            var tListPanelRT = (RectTransform)tListPanelGO.transform;
            tListPanelRT.anchorMin = Vector2.zero;
            tListPanelRT.anchorMax = Vector2.one;
            tListPanelRT.offsetMin = Vector2.zero;
            tListPanelRT.offsetMax = Vector2.zero;

            var tListBackdrop = tListPanelGO.AddComponent<Image>();
            tListBackdrop.color = new Color(0f, 0f, 0f, 0.6f);

            // Centered card (mirrors the other cards).
            var tListCardGO = new GameObject("Card", typeof(RectTransform));
            tListCardGO.transform.SetParent(tListPanelGO.transform, false);

            var tListCardRT = (RectTransform)tListCardGO.transform;
            tListCardRT.anchorMin = new Vector2(0.5f, 0.5f);
            tListCardRT.anchorMax = new Vector2(0.5f, 0.5f);
            tListCardRT.pivot = new Vector2(0.5f, 0.5f);
            tListCardRT.sizeDelta = new Vector2(600f, 800f);
            tListCardRT.anchoredPosition = Vector2.zero;

            var tListCardBg = tListCardGO.AddComponent<Image>();
            tListCardBg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            var tListLayout = tListCardGO.AddComponent<VerticalLayoutGroup>();
            tListLayout.padding = new RectOffset(40, 40, 40, 40);
            tListLayout.spacing = 20f;
            tListLayout.childAlignment = TextAnchor.UpperCenter;
            tListLayout.childControlWidth = true;
            tListLayout.childControlHeight = true;
            tListLayout.childForceExpandWidth = true;
            tListLayout.childForceExpandHeight = false;

            // 1) Title
            CreateText(tListCardGO.transform, "Title", "Tournaments",
                40f, true, Color.white, 70f);

            // 2) ScrollView (vertical) — fills the remaining card height.
            var tListScrollGO = new GameObject("ScrollView", typeof(RectTransform));
            tListScrollGO.transform.SetParent(tListCardGO.transform, false);
            var tListScrollBg = tListScrollGO.AddComponent<Image>();
            tListScrollBg.color = new Color(1f, 1f, 1f, 0.05f);
            var tListScrollRect = tListScrollGO.AddComponent<ScrollRect>();
            tListScrollRect.horizontal = false;
            tListScrollRect.vertical = true;
            tListScrollRect.movementType = ScrollRect.MovementType.Clamped;
            tListScrollRect.scrollSensitivity = 20f;
            var tListScrollLE = tListScrollGO.AddComponent<LayoutElement>();
            tListScrollLE.flexibleHeight = 1f;
            tListScrollLE.minHeight = 400f;

            var tListViewportGO = new GameObject("Viewport", typeof(RectTransform));
            tListViewportGO.transform.SetParent(tListScrollGO.transform, false);
            StretchFull((RectTransform)tListViewportGO.transform);
            tListViewportGO.AddComponent<RectMask2D>();

            var tListContentGO = new GameObject("Content", typeof(RectTransform));
            tListContentGO.transform.SetParent(tListViewportGO.transform, false);
            var tListContentRT = (RectTransform)tListContentGO.transform;
            tListContentRT.anchorMin = new Vector2(0f, 1f);
            tListContentRT.anchorMax = new Vector2(1f, 1f);
            tListContentRT.pivot = new Vector2(0.5f, 1f);
            tListContentRT.anchoredPosition = Vector2.zero;
            tListContentRT.sizeDelta = Vector2.zero;

            var tListContentVLG = tListContentGO.AddComponent<VerticalLayoutGroup>();
            tListContentVLG.padding = new RectOffset(8, 8, 8, 8);
            tListContentVLG.spacing = 10f;
            tListContentVLG.childAlignment = TextAnchor.UpperCenter;
            tListContentVLG.childControlWidth = true;
            tListContentVLG.childControlHeight = true;
            tListContentVLG.childForceExpandWidth = true;
            tListContentVLG.childForceExpandHeight = false;

            var tListContentCSF = tListContentGO.AddComponent<ContentSizeFitter>();
            tListContentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            tListScrollRect.viewport = (RectTransform)tListViewportGO.transform;
            tListScrollRect.content  = tListContentRT;

            // Card template (INACTIVE): name text, info text, "Join & Play" button.
            // Controller reads TMP texts in order [0]=name, [1]=info, and the first Button as Join.
            var tCardGO = new GameObject("TournamentCardTemplate", typeof(RectTransform));
            tCardGO.transform.SetParent(tListContentGO.transform, false);
            var tCardBg = tCardGO.AddComponent<Image>();
            tCardBg.color = new Color(1f, 1f, 1f, 0.04f);
            var tCardVLG = tCardGO.AddComponent<VerticalLayoutGroup>();
            tCardVLG.padding = new RectOffset(12, 12, 10, 10);
            tCardVLG.spacing = 8f;
            tCardVLG.childAlignment = TextAnchor.UpperCenter;
            tCardVLG.childControlWidth = true;
            tCardVLG.childControlHeight = true;
            tCardVLG.childForceExpandWidth = true;
            tCardVLG.childForceExpandHeight = false;
            var tCardLE = tCardGO.AddComponent<LayoutElement>();
            tCardLE.minHeight = 170f;
            tCardLE.preferredHeight = 170f;

            CreateText(tCardGO.transform, "NameText", "Tournament",
                28f, true, Color.white, 44f, TextAlignmentOptions.Left);
            CreateText(tCardGO.transform, "InfoText", "0 players",
                22f, false, new Color(0.80f, 0.80f, 0.80f, 1f), 36f, TextAlignmentOptions.Left);
            // Join button on the template; its onClick is wired per-clone at runtime by the controller.
            CreateButton(tCardGO.transform, "JoinButton", "Join & Play",
                new Color(0.16f, 0.70f, 0.40f, 1f), 64f, 26f);

            tCardGO.SetActive(false);   // template — clones made at runtime by the controller

            // 3) Back button (gray) — returns home
            var tListBackButton = CreateButton(tListCardGO.transform, "BackButton", "Back",
                new Color(0.30f, 0.30f, 0.35f, 1f), 70f, 26f);

            tListPanelGO.SetActive(false);   // shown only when "Join Open Tournaments" is tapped

            // ---- Open Tournament: SCORE CARD panel (7th sibling under the Canvas) ----
            var tScorePanelGO = new GameObject("TournamentScorePanel", typeof(RectTransform));
            tScorePanelGO.transform.SetParent(canvasGO.transform, false);

            var tScorePanelRT = (RectTransform)tScorePanelGO.transform;
            tScorePanelRT.anchorMin = Vector2.zero;
            tScorePanelRT.anchorMax = Vector2.one;
            tScorePanelRT.offsetMin = Vector2.zero;
            tScorePanelRT.offsetMax = Vector2.zero;

            var tScoreBackdrop = tScorePanelGO.AddComponent<Image>();
            tScoreBackdrop.color = new Color(0f, 0f, 0f, 0.6f);

            // Centered card (mirrors the other cards).
            var tScoreCardGO = new GameObject("Card", typeof(RectTransform));
            tScoreCardGO.transform.SetParent(tScorePanelGO.transform, false);

            var tScoreCardRT = (RectTransform)tScoreCardGO.transform;
            tScoreCardRT.anchorMin = new Vector2(0.5f, 0.5f);
            tScoreCardRT.anchorMax = new Vector2(0.5f, 0.5f);
            tScoreCardRT.pivot = new Vector2(0.5f, 0.5f);
            tScoreCardRT.sizeDelta = new Vector2(600f, 800f);
            tScoreCardRT.anchoredPosition = Vector2.zero;

            var tScoreCardBg = tScoreCardGO.AddComponent<Image>();
            tScoreCardBg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            var tScoreLayout = tScoreCardGO.AddComponent<VerticalLayoutGroup>();
            tScoreLayout.padding = new RectOffset(40, 40, 40, 40);
            tScoreLayout.spacing = 20f;
            tScoreLayout.childAlignment = TextAnchor.UpperCenter;
            tScoreLayout.childControlWidth = true;
            tScoreLayout.childControlHeight = true;
            tScoreLayout.childForceExpandWidth = true;
            tScoreLayout.childForceExpandHeight = false;

            // 1) Header (score + best + rank line)
            var tScoreHeaderTMP = CreateText(tScoreCardGO.transform, "HeaderText", "Your score: —",
                32f, true, Color.white, 100f);

            // 2) ScrollView (vertical) for the leaderboard rows.
            var tLbScrollGO = new GameObject("ScrollView", typeof(RectTransform));
            tLbScrollGO.transform.SetParent(tScoreCardGO.transform, false);
            var tLbScrollBg = tLbScrollGO.AddComponent<Image>();
            tLbScrollBg.color = new Color(1f, 1f, 1f, 0.05f);
            var tLbScrollRect = tLbScrollGO.AddComponent<ScrollRect>();
            tLbScrollRect.horizontal = false;
            tLbScrollRect.vertical = true;
            tLbScrollRect.movementType = ScrollRect.MovementType.Clamped;
            tLbScrollRect.scrollSensitivity = 20f;
            var tLbScrollLE = tLbScrollGO.AddComponent<LayoutElement>();
            tLbScrollLE.flexibleHeight = 1f;
            tLbScrollLE.minHeight = 360f;

            var tLbViewportGO = new GameObject("Viewport", typeof(RectTransform));
            tLbViewportGO.transform.SetParent(tLbScrollGO.transform, false);
            StretchFull((RectTransform)tLbViewportGO.transform);
            tLbViewportGO.AddComponent<RectMask2D>();

            var tLbContentGO = new GameObject("Content", typeof(RectTransform));
            tLbContentGO.transform.SetParent(tLbViewportGO.transform, false);
            var tLbContentRT = (RectTransform)tLbContentGO.transform;
            tLbContentRT.anchorMin = new Vector2(0f, 1f);
            tLbContentRT.anchorMax = new Vector2(1f, 1f);
            tLbContentRT.pivot = new Vector2(0.5f, 1f);
            tLbContentRT.anchoredPosition = Vector2.zero;
            tLbContentRT.sizeDelta = Vector2.zero;

            var tLbContentVLG = tLbContentGO.AddComponent<VerticalLayoutGroup>();
            tLbContentVLG.padding = new RectOffset(8, 8, 8, 8);
            tLbContentVLG.spacing = 8f;
            tLbContentVLG.childAlignment = TextAnchor.UpperCenter;
            tLbContentVLG.childControlWidth = true;
            tLbContentVLG.childControlHeight = true;
            tLbContentVLG.childForceExpandWidth = true;
            tLbContentVLG.childForceExpandHeight = false;

            var tLbContentCSF = tLbContentGO.AddComponent<ContentSizeFitter>();
            tLbContentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            tLbScrollRect.viewport = (RectTransform)tLbViewportGO.transform;
            tLbScrollRect.content  = tLbContentRT;

            // Row template: 3 TMP columns (rank, name, score). Inactive = template.
            var tLbRowGO = new GameObject("RowTemplate", typeof(RectTransform));
            tLbRowGO.transform.SetParent(tLbContentGO.transform, false);
            var tLbRowBg = tLbRowGO.AddComponent<Image>();
            tLbRowBg.color = new Color(1f, 1f, 1f, 0.04f);
            var tLbRowHLG = tLbRowGO.AddComponent<HorizontalLayoutGroup>();
            tLbRowHLG.padding = new RectOffset(8, 8, 4, 4);
            tLbRowHLG.spacing = 10f;
            tLbRowHLG.childAlignment = TextAnchor.MiddleLeft;
            tLbRowHLG.childControlWidth = true;
            tLbRowHLG.childControlHeight = true;
            tLbRowHLG.childForceExpandWidth = false;
            tLbRowHLG.childForceExpandHeight = true;
            var tLbRowLE = tLbRowGO.AddComponent<LayoutElement>();
            tLbRowLE.minHeight = 56f;
            tLbRowLE.preferredHeight = 56f;

            // Column 1: rank (narrow, centered)
            var tLbRankText = CreateText(tLbRowGO.transform, "RankText", "1.",
                26f, true, Color.white, 56f, TextAlignmentOptions.Center);
            var tLbRankLE = tLbRankText.GetComponent<LayoutElement>();
            tLbRankLE.minWidth = 70f;
            tLbRankLE.preferredWidth = 70f;

            // Column 2: name (flexible, left)
            var tLbNameText = CreateText(tLbRowGO.transform, "NameText", "Player",
                26f, false, Color.white, 56f, TextAlignmentOptions.Left);
            tLbNameText.GetComponent<LayoutElement>().flexibleWidth = 1f;

            // Column 3: score (medium, right)
            var tLbScoreText = CreateText(tLbRowGO.transform, "ScoreText", "0",
                26f, true, new Color(1f, 0.82f, 0.30f, 1f), 56f, TextAlignmentOptions.Right);
            var tLbScoreLE = tLbScoreText.GetComponent<LayoutElement>();
            tLbScoreLE.minWidth = 120f;
            tLbScoreLE.preferredWidth = 140f;

            tLbRowGO.SetActive(false);   // template — clones made at runtime by the controller

            // 3) Refresh (blue) — re-fetch + re-render the standings (card stays open)
            var tRefreshButton = CreateButton(tScoreCardGO.transform, "RefreshButton", "Refresh",
                new Color(0.16f, 0.45f, 0.92f, 1f), 80f, 28f);

            // 4) Play Again (green)
            var tPlayAgainButton = CreateButton(tScoreCardGO.transform, "PlayAgainButton", "Play Again",
                new Color(0.16f, 0.70f, 0.40f, 1f), 90f, 30f);

            // 5) Menu (gray) — returns home
            var tMenuButton = CreateButton(tScoreCardGO.transform, "MenuButton", "Menu",
                new Color(0.30f, 0.30f, 0.35f, 1f), 90f, 30f);

            tScorePanelGO.SetActive(false);   // shown only after an OPEN TOURNAMENT game ends

            // ---- My History panel (parent + two tabs), sibling under the Canvas ----
            var historyPanelGO = new GameObject("HistoryPanel", typeof(RectTransform));
            historyPanelGO.transform.SetParent(canvasGO.transform, false);

            var histPanelRT = (RectTransform)historyPanelGO.transform;
            histPanelRT.anchorMin = Vector2.zero;
            histPanelRT.anchorMax = Vector2.one;
            histPanelRT.offsetMin = Vector2.zero;
            histPanelRT.offsetMax = Vector2.zero;

            var histBackdrop = historyPanelGO.AddComponent<Image>();
            histBackdrop.color = new Color(0f, 0f, 0f, 0.6f);

            // Centered card (mirrors the other cards).
            var histCardGO = new GameObject("Card", typeof(RectTransform));
            histCardGO.transform.SetParent(historyPanelGO.transform, false);

            var histCardRT = (RectTransform)histCardGO.transform;
            histCardRT.anchorMin = new Vector2(0.5f, 0.5f);
            histCardRT.anchorMax = new Vector2(0.5f, 0.5f);
            histCardRT.pivot = new Vector2(0.5f, 0.5f);
            histCardRT.sizeDelta = new Vector2(600f, 800f);
            histCardRT.anchoredPosition = Vector2.zero;

            var histCardBg = histCardGO.AddComponent<Image>();
            histCardBg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);

            var histLayout = histCardGO.AddComponent<VerticalLayoutGroup>();
            histLayout.padding = new RectOffset(40, 40, 40, 40);
            histLayout.spacing = 16f;
            histLayout.childAlignment = TextAnchor.UpperCenter;
            histLayout.childControlWidth = true;
            histLayout.childControlHeight = true;
            histLayout.childForceExpandWidth = true;
            histLayout.childForceExpandHeight = false;

            // 1) Title
            CreateText(histCardGO.transform, "Title", "My History",
                40f, true, Color.white, 70f);

            // 2) Tab row: two buttons side by side.
            var histTabRowGO = new GameObject("TabRow", typeof(RectTransform));
            histTabRowGO.transform.SetParent(histCardGO.transform, false);
            var histTabHLG = histTabRowGO.AddComponent<HorizontalLayoutGroup>();
            histTabHLG.spacing = 12f;
            histTabHLG.childControlWidth = true;
            histTabHLG.childForceExpandWidth = true;
            histTabHLG.childControlHeight = true;
            histTabHLG.childForceExpandHeight = true;
            var histTabRowLE = histTabRowGO.AddComponent<LayoutElement>();
            histTabRowLE.minHeight = 70f;
            histTabRowLE.preferredHeight = 70f;

            var tabRoomsButton = CreateButton(histTabRowGO.transform, "TabRoomsButton", "Private Rooms",
                new Color(0.20f, 0.45f, 0.70f, 1f), 70f, 24f);
            var tabTournamentsButton = CreateButton(histTabRowGO.transform, "TabTournamentsButton", "Open Tournaments",
                new Color(0.20f, 0.45f, 0.70f, 1f), 70f, 24f);

            // 3a) Rooms tab container — holds a vertical ScrollView of room cards.
            var historyRoomsTabGO = new GameObject("RoomsTab", typeof(RectTransform));
            historyRoomsTabGO.transform.SetParent(histCardGO.transform, false);
            var historyRoomsTabLE = historyRoomsTabGO.AddComponent<LayoutElement>();
            historyRoomsTabLE.flexibleHeight = 1f;
            historyRoomsTabLE.minHeight = 420f;

            var hrScrollGO = new GameObject("ScrollView", typeof(RectTransform));
            hrScrollGO.transform.SetParent(historyRoomsTabGO.transform, false);
            StretchFull((RectTransform)hrScrollGO.transform);
            var hrScrollBg = hrScrollGO.AddComponent<Image>();
            hrScrollBg.color = new Color(1f, 1f, 1f, 0.05f);
            var hrScrollRect = hrScrollGO.AddComponent<ScrollRect>();
            hrScrollRect.horizontal = false;
            hrScrollRect.vertical = true;
            hrScrollRect.movementType = ScrollRect.MovementType.Clamped;
            hrScrollRect.scrollSensitivity = 20f;

            var hrViewportGO = new GameObject("Viewport", typeof(RectTransform));
            hrViewportGO.transform.SetParent(hrScrollGO.transform, false);
            StretchFull((RectTransform)hrViewportGO.transform);
            hrViewportGO.AddComponent<RectMask2D>();

            var hrContentGO = new GameObject("Content", typeof(RectTransform));
            hrContentGO.transform.SetParent(hrViewportGO.transform, false);
            var hrContentRT = (RectTransform)hrContentGO.transform;
            hrContentRT.anchorMin = new Vector2(0f, 1f);
            hrContentRT.anchorMax = new Vector2(1f, 1f);
            hrContentRT.pivot = new Vector2(0.5f, 1f);
            hrContentRT.anchoredPosition = Vector2.zero;
            hrContentRT.sizeDelta = Vector2.zero;
            var hrContentVLG = hrContentGO.AddComponent<VerticalLayoutGroup>();
            hrContentVLG.padding = new RectOffset(8, 8, 8, 8);
            hrContentVLG.spacing = 10f;
            hrContentVLG.childAlignment = TextAnchor.UpperCenter;
            hrContentVLG.childControlWidth = true;
            hrContentVLG.childControlHeight = true;
            hrContentVLG.childForceExpandWidth = true;
            hrContentVLG.childForceExpandHeight = false;
            var hrContentCSF = hrContentGO.AddComponent<ContentSizeFitter>();
            hrContentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            hrScrollRect.viewport = (RectTransform)hrViewportGO.transform;
            hrScrollRect.content  = hrContentRT;

            // Room card template (INACTIVE): name text, info text, "View" button.
            // Controller reads TMP texts [0]=name, [1]=info, and the first Button as View.
            var historyRoomCardGO = new GameObject("HistoryRoomCardTemplate", typeof(RectTransform));
            historyRoomCardGO.transform.SetParent(hrContentGO.transform, false);
            var hrCardBg = historyRoomCardGO.AddComponent<Image>();
            hrCardBg.color = new Color(1f, 1f, 1f, 0.04f);
            var hrCardVLG = historyRoomCardGO.AddComponent<VerticalLayoutGroup>();
            hrCardVLG.padding = new RectOffset(12, 12, 10, 10);
            hrCardVLG.spacing = 8f;
            hrCardVLG.childAlignment = TextAnchor.UpperCenter;
            hrCardVLG.childControlWidth = true;
            hrCardVLG.childControlHeight = true;
            hrCardVLG.childForceExpandWidth = true;
            hrCardVLG.childForceExpandHeight = false;
            var hrCardLE = historyRoomCardGO.AddComponent<LayoutElement>();
            hrCardLE.minHeight = 160f;
            hrCardLE.preferredHeight = 160f;
            CreateText(historyRoomCardGO.transform, "NameText", "Room",
                28f, true, Color.white, 44f, TextAlignmentOptions.Left);
            CreateText(historyRoomCardGO.transform, "InfoText", "You: 0   0 players   —",
                20f, false, new Color(0.80f, 0.80f, 0.80f, 1f), 36f, TextAlignmentOptions.Left);
            // View button on the template; its onClick is wired per-clone at runtime by the controller.
            CreateButton(historyRoomCardGO.transform, "ViewButton", "View",
                new Color(0.16f, 0.45f, 0.92f, 1f), 60f, 24f);
            historyRoomCardGO.SetActive(false);   // template — clones made at runtime

            // 3b) Tournaments tab container — a centered placeholder (hidden by default).
            var historyTournamentsTabGO = new GameObject("TournamentsTab", typeof(RectTransform));
            historyTournamentsTabGO.transform.SetParent(histCardGO.transform, false);
            var historyTournamentsTabLE = historyTournamentsTabGO.AddComponent<LayoutElement>();
            historyTournamentsTabLE.flexibleHeight = 1f;
            historyTournamentsTabLE.minHeight = 420f;

            var htPlaceholderGO = new GameObject("Placeholder", typeof(RectTransform));
            htPlaceholderGO.transform.SetParent(historyTournamentsTabGO.transform, false);
            StretchFull((RectTransform)htPlaceholderGO.transform);
            var historyTournamentsPlaceholder = htPlaceholderGO.AddComponent<TextMeshProUGUI>();
            historyTournamentsPlaceholder.text = "No tournament history yet";
            historyTournamentsPlaceholder.fontSize = 28f;
            historyTournamentsPlaceholder.alignment = TextAlignmentOptions.Center;
            historyTournamentsPlaceholder.color = new Color(0.80f, 0.80f, 0.80f, 1f);

            historyTournamentsTabGO.SetActive(false);   // Rooms tab is the default

            // 4) Back button (gray) — closes history -> mode picker. Field is a GameObject (has a Button).
            var historyBackBtn = CreateButton(histCardGO.transform, "HistoryBackButton", "Back",
                new Color(0.30f, 0.30f, 0.35f, 1f), 70f, 26f);

            historyPanelGO.SetActive(false);   // shown only when "My History" is tapped

            // ---- My History: room detail (a room's leaderboard), separate sibling under the Canvas ----
            var historyRoomDetailPanelGO = new GameObject("HistoryRoomDetailPanel", typeof(RectTransform));
            historyRoomDetailPanelGO.transform.SetParent(canvasGO.transform, false);

            var hdPanelRT = (RectTransform)historyRoomDetailPanelGO.transform;
            hdPanelRT.anchorMin = Vector2.zero;
            hdPanelRT.anchorMax = Vector2.one;
            hdPanelRT.offsetMin = Vector2.zero;
            hdPanelRT.offsetMax = Vector2.zero;

            var hdBackdrop = historyRoomDetailPanelGO.AddComponent<Image>();
            hdBackdrop.color = new Color(0f, 0f, 0f, 0.6f);

            var hdCardGO = new GameObject("Card", typeof(RectTransform));
            hdCardGO.transform.SetParent(historyRoomDetailPanelGO.transform, false);
            var hdCardRT = (RectTransform)hdCardGO.transform;
            hdCardRT.anchorMin = new Vector2(0.5f, 0.5f);
            hdCardRT.anchorMax = new Vector2(0.5f, 0.5f);
            hdCardRT.pivot = new Vector2(0.5f, 0.5f);
            hdCardRT.sizeDelta = new Vector2(600f, 800f);
            hdCardRT.anchoredPosition = Vector2.zero;
            var hdCardBg = hdCardGO.AddComponent<Image>();
            hdCardBg.color = new Color(0.10f, 0.12f, 0.16f, 0.95f);
            var hdLayout = hdCardGO.AddComponent<VerticalLayoutGroup>();
            hdLayout.padding = new RectOffset(40, 40, 40, 40);
            hdLayout.spacing = 20f;
            hdLayout.childAlignment = TextAnchor.UpperCenter;
            hdLayout.childControlWidth = true;
            hdLayout.childControlHeight = true;
            hdLayout.childForceExpandWidth = true;
            hdLayout.childForceExpandHeight = false;

            CreateText(hdCardGO.transform, "Title", "Room Leaderboard",
                40f, true, Color.white, 70f);

            var hdScrollGO = new GameObject("ScrollView", typeof(RectTransform));
            hdScrollGO.transform.SetParent(hdCardGO.transform, false);
            var hdScrollBg = hdScrollGO.AddComponent<Image>();
            hdScrollBg.color = new Color(1f, 1f, 1f, 0.05f);
            var hdScrollRect = hdScrollGO.AddComponent<ScrollRect>();
            hdScrollRect.horizontal = false;
            hdScrollRect.vertical = true;
            hdScrollRect.movementType = ScrollRect.MovementType.Clamped;
            hdScrollRect.scrollSensitivity = 20f;
            var hdScrollLE = hdScrollGO.AddComponent<LayoutElement>();
            hdScrollLE.flexibleHeight = 1f;
            hdScrollLE.minHeight = 420f;

            var hdViewportGO = new GameObject("Viewport", typeof(RectTransform));
            hdViewportGO.transform.SetParent(hdScrollGO.transform, false);
            StretchFull((RectTransform)hdViewportGO.transform);
            hdViewportGO.AddComponent<RectMask2D>();

            var hdContentGO = new GameObject("Content", typeof(RectTransform));
            hdContentGO.transform.SetParent(hdViewportGO.transform, false);
            var hdContentRT = (RectTransform)hdContentGO.transform;
            hdContentRT.anchorMin = new Vector2(0f, 1f);
            hdContentRT.anchorMax = new Vector2(1f, 1f);
            hdContentRT.pivot = new Vector2(0.5f, 1f);
            hdContentRT.anchoredPosition = Vector2.zero;
            hdContentRT.sizeDelta = Vector2.zero;
            var hdContentVLG = hdContentGO.AddComponent<VerticalLayoutGroup>();
            hdContentVLG.padding = new RectOffset(8, 8, 8, 8);
            hdContentVLG.spacing = 8f;
            hdContentVLG.childAlignment = TextAnchor.UpperCenter;
            hdContentVLG.childControlWidth = true;
            hdContentVLG.childControlHeight = true;
            hdContentVLG.childForceExpandWidth = true;
            hdContentVLG.childForceExpandHeight = false;
            var hdContentCSF = hdContentGO.AddComponent<ContentSizeFitter>();
            hdContentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            hdScrollRect.viewport = (RectTransform)hdViewportGO.transform;
            hdScrollRect.content  = hdContentRT;

            // Row template: 3 TMP columns (rank, name, score). Inactive = template.
            var hdRowGO = new GameObject("RowTemplate", typeof(RectTransform));
            hdRowGO.transform.SetParent(hdContentGO.transform, false);
            var hdRowBg = hdRowGO.AddComponent<Image>();
            hdRowBg.color = new Color(1f, 1f, 1f, 0.04f);
            var hdRowHLG = hdRowGO.AddComponent<HorizontalLayoutGroup>();
            hdRowHLG.padding = new RectOffset(8, 8, 4, 4);
            hdRowHLG.spacing = 10f;
            hdRowHLG.childAlignment = TextAnchor.MiddleLeft;
            hdRowHLG.childControlWidth = true;
            hdRowHLG.childControlHeight = true;
            hdRowHLG.childForceExpandWidth = false;
            hdRowHLG.childForceExpandHeight = true;
            var hdRowLE = hdRowGO.AddComponent<LayoutElement>();
            hdRowLE.minHeight = 56f;
            hdRowLE.preferredHeight = 56f;

            var hdRankText = CreateText(hdRowGO.transform, "RankText", "1.",
                26f, true, Color.white, 56f, TextAlignmentOptions.Center);
            var hdRankLE = hdRankText.GetComponent<LayoutElement>();
            hdRankLE.minWidth = 70f;
            hdRankLE.preferredWidth = 70f;

            var hdNameText = CreateText(hdRowGO.transform, "NameText", "Player",
                26f, false, Color.white, 56f, TextAlignmentOptions.Left);
            hdNameText.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var hdScoreText = CreateText(hdRowGO.transform, "ScoreText", "0",
                26f, true, new Color(1f, 0.82f, 0.30f, 1f), 56f, TextAlignmentOptions.Right);
            var hdScoreLE = hdScoreText.GetComponent<LayoutElement>();
            hdScoreLE.minWidth = 120f;
            hdScoreLE.preferredWidth = 140f;

            hdRowGO.SetActive(false);   // template — clones made at runtime

            // Back button (gray) — returns to the Rooms list.
            var historyDetailBackButton = CreateButton(hdCardGO.transform, "BackButton", "Back",
                new Color(0.30f, 0.30f, 0.35f, 1f), 70f, 26f);

            historyRoomDetailPanelGO.SetActive(false);   // shown only when a room is viewed from history

            // ---- Attach + auto-wire the runtime controller (no manual inspector dragging) ----
            // Added via Undo.AddComponent so it's part of the same undo group as the rest.
            var controller = Undo.AddComponent<GameBullLobbyController>(root);
            controller.modePickerPanel  = panelGO;
            controller.greetingText     = greetingTMP;
            controller.livesText        = livesTMP;
            controller.soloButton       = soloButton;
            controller.tournamentButton = tournamentButton;
            controller.createRoomPanel      = createRoomPanelGO;
            controller.nameInput            = nameInput;
            controller.expiry30Button       = expiry30Button;
            controller.expiry60Button       = expiry60Button;
            controller.createRoomButton     = createRoomButton;
            controller.createRoomBackButton = createRoomBackButton;
            controller.sharePanel        = sharePanelGO;
            controller.roomNameText      = roomNameText;
            controller.joinUrlText       = joinUrlText;
            controller.copyButton        = copyButton;
            controller.playersText       = playersText;
            controller.playAsHostButton  = playAsHostButton;
            controller.shareBackButton   = shareBackButton;
            controller.joinPanel         = joinPanelGO;
            controller.joinRoomNameText  = joinRoomNameText;
            controller.joinPlayersText   = joinPlayersText;
            controller.nicknameInput     = nicknameInput;
            controller.joinButton        = joinButton;
            controller.leaderboardPanel       = leaderboardPanelGO;
            controller.leaderboardListRoot    = lbContentGO.transform;
            controller.leaderboardRowTemplate = lbRowGO;
            controller.leaderboardMenuButton  = leaderboardMenuButton;
            controller.leaderboardRefreshButton = leaderboardRefreshButton;
            // Open Tournament entry button (mode picker) + the two new panels.
            controller.openTournamentButton      = openTournamentButton;
            controller.tournamentListPanel       = tListPanelGO;
            controller.tournamentListRoot        = tListContentGO.transform;
            controller.tournamentCardTemplate    = tCardGO;
            controller.tournamentListBackButton  = tListBackButton;
            controller.tournamentScorePanel      = tScorePanelGO;
            controller.tournamentScoreHeader     = tScoreHeaderTMP;
            controller.tournamentLbRoot          = tLbContentGO.transform;
            controller.tournamentLbRowTemplate   = tLbRowGO;
            controller.tournamentPlayAgainButton = tPlayAgainButton;
            controller.tournamentMenuButton      = tMenuButton;
            controller.tournamentRefreshButton   = tRefreshButton;
            // My History panel (mode-picker button + parent/tabs + room detail).
            controller.historyMenuButton             = historyMenuButton;
            controller.historyPanel                  = historyPanelGO;
            controller.tabRoomsButton                = tabRoomsButton;
            controller.tabTournamentsButton          = tabTournamentsButton;
            controller.historyBackButton             = historyBackBtn.gameObject;
            controller.historyRoomsTab               = historyRoomsTabGO;
            controller.historyRoomsListRoot          = hrContentGO.transform;
            controller.historyRoomCardTemplate       = historyRoomCardGO;
            controller.historyTournamentsTab         = historyTournamentsTabGO;
            controller.historyTournamentsPlaceholder = historyTournamentsPlaceholder;
            controller.historyRoomDetailPanel        = historyRoomDetailPanelGO;
            controller.historyRoomDetailListRoot     = hdContentGO.transform;
            controller.historyRoomDetailRowTemplate  = hdRowGO;
            controller.historyRoomDetailBackButton   = historyDetailBackButton;
            EditorUtility.SetDirty(controller);

            // UI elements need an EventSystem to be clickable.
            EnsureEventSystem();

            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(root.scene);

            Undo.CollapseUndoOperations(undoGroup);
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null) return;

            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
        }

        // Creates a TextMeshPro label with a fixed-height LayoutElement under 'parent'.
        private static TextMeshProUGUI CreateText(
            Transform parent, string name, string text,
            float fontSize, bool bold, Color color, float height,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = alignment;
            tmp.color = color;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            return tmp;
        }

        // Creates a flat-colored button: Image background + Button + a centered TMP label.
        // Layout only — no onClick is wired up.
        private static Button CreateButton(
            Transform parent, string name, string label,
            Color background, float height, float labelFontSize)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var image = go.AddComponent<Image>();
            image.color = background;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            // Centered label stretched to fill the button.
            var labelGO = new GameObject("Label", typeof(RectTransform));
            labelGO.transform.SetParent(go.transform, false);

            var labelRT = (RectTransform)labelGO.transform;
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = Vector2.zero;
            labelRT.offsetMax = Vector2.zero;

            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = labelFontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return button;
        }

        // Creates a TMP_InputField: visible background + clipped text area + placeholder + editable text.
        private static TMP_InputField CreateInputField(
            Transform parent, string name, string placeholderText, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(1f, 1f, 1f, 0.1f);

            var input = go.AddComponent<TMP_InputField>();
            input.targetGraphic = bg;

            var le = go.AddComponent<LayoutElement>();
            le.minHeight = height;
            le.preferredHeight = height;

            // Text Area (clipped viewport)
            var textAreaGO = new GameObject("Text Area", typeof(RectTransform));
            textAreaGO.transform.SetParent(go.transform, false);
            var textAreaRT = (RectTransform)textAreaGO.transform;
            textAreaRT.anchorMin = Vector2.zero;
            textAreaRT.anchorMax = Vector2.one;
            textAreaRT.offsetMin = new Vector2(12f, 6f);
            textAreaRT.offsetMax = new Vector2(-12f, -6f);
            textAreaGO.AddComponent<RectMask2D>();

            // Placeholder (italic gray)
            var placeholderGO = new GameObject("Placeholder", typeof(RectTransform));
            placeholderGO.transform.SetParent(textAreaGO.transform, false);
            StretchFull((RectTransform)placeholderGO.transform);
            var placeholder = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholder.text = placeholderText;
            placeholder.fontSize = 24f;
            placeholder.fontStyle = FontStyles.Italic;
            placeholder.alignment = TextAlignmentOptions.Left;
            placeholder.color = new Color(0.70f, 0.70f, 0.70f, 0.70f);

            // Editable text
            var textGO = new GameObject("Text", typeof(RectTransform));
            textGO.transform.SetParent(textAreaGO.transform, false);
            StretchFull((RectTransform)textGO.transform);
            var text = textGO.AddComponent<TextMeshProUGUI>();
            text.text = "";
            text.fontSize = 24f;
            text.alignment = TextAlignmentOptions.Left;
            text.color = Color.white;

            input.textViewport = textAreaRT;
            input.textComponent = text;
            input.placeholder = placeholder;

            return input;
        }

        // Stretches a RectTransform to fully fill its parent.
        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
