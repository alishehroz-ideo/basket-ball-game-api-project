# GameBull + WebGL Integration Guide (Replication Playbook)

> **Purpose:** This is the master reference for everything we did on the **Coupra BasketBall** game
> (`Basket Ball Dunk`, Unity `6000.4.3f1`). Use it to reproduce the exact same setup on the other
> games. It covers four pillars:
>
> 1. **GameBull SDK** — the complete backend/lobby integration.
> 2. **WebGL resolution / responsive scaling** — game fits any container, no scrollbars, no distortion.
> 3. **Ideofuzion WebGL template** — the custom, rebuild-proof template that carries the bridge + loading UI.
> 4. **Loading screen & loading bar** — hero background, game icon, rounded custom bar, bug fixes, Safari fix.
>
> Anything marked **🔁 PER-GAME** changes for every title. Everything else is copy-paste identical.

---

## Table of Contents

1. [Prerequisites](#0--prerequisites)
2. [Part A0 — Strip Existing Platform & Monetization SDKs (Coupra / Skillz / PlayFab)](#part-a0--strip-existing-platform--monetization-sdks-do-this-first)
3. [Part A — GameBull SDK (Complete)](#part-a--gamebull-sdk-complete)
4. [Part B — WebGL Resolution & Responsive Scaling](#part-b--webgl-resolution--responsive-scaling)
5. [Part C — The Ideofuzion WebGL Template](#part-c--the-ideofuzion-webgl-template)
6. [Part D — Loading Screen & Loading Bar Changes](#part-d--loading-screen--loading-bar-changes)
7. [Part E — Step-by-Step Replication Checklist](#part-e--step-by-step-replication-checklist-for-a-new-game)
8. [Part F — Deploy the Build to GitHub Pages (the URL for the GameBull admin panel)](#part-f--deploy-the-build-to-github-pages-the-url-for-the-gamebull-admin-panel)
9. [Reference — Exact Values, Colors & File Locations](#reference--exact-values-colors--file-locations)

---

## 0 — Prerequisites

| Item | Detail |
|---|---|
| **Unity version** | Must match `ProjectSettings/ProjectVersion.txt` exactly (this project: `6000.4.3f1`). Minor mismatches cause errors. |
| **WebGL Build Support module** | Unity Hub → Installs → gear icon next to the version → **Add modules** → check **WebGL Build Support** (~500 MB). Without it the WebGL platform is missing from Build Settings. |
| **Newtonsoft JSON** | GameBull's `GameBullApi.cs` needs `com.unity.nuget.newtonsoft-json`. Install via Package Manager if you get `CS0246: 'Newtonsoft' could not be found`. |
| **TextMeshPro** | The GameBull lobby UI uses TMP. Import TMP Essentials if prompted. |
| **Local serving** | You cannot open `index.html` from `file://` — browsers block WebGL. Serve it: `python -m http.server 8000` from the build folder, then `http://localhost:8000`. |

> **Stripping the old platform + monetization SDKs is a REQUIRED first step** before adding GameBull.
> Every game ships wired to a host platform (**Coupra** here; **Skillz / SkillziOS** in the other titles)
> and often a **PlayFab** monetization package. See **[Part A0](#part-a0--strip-existing-platform--monetization-sdks-do-this-first)** for exactly what to remove and how.

---

## Part A0 — Strip Existing Platform & Monetization SDKs (do this first)

Every game we port ships wired to a **host platform** and usually a **monetization SDK**. Before adding
GameBull, strip these so the build is a clean, standalone, game-only WebGL. Three things to hunt for:
the **host platform** (Coupra / Skillz), the **PlayFab monetization package**, and any leftover backend
scripts.

### A0.1 The host platform integration — Coupra (here) / Skillz (other games)

Each game was built for a platform that gates play behind auth and pushes scores to its own backend.
It is **not** an Asset-Store plugin — it's woven into the game's own scripts, so it must be stripped by hand.

| Game family | Platform baked in | Tell-tale signs |
|---|---|---|
| Coupra BasketBall (this reference build) | **Coupra** | `Assets/ServerAPIs/`, hosts `api-php.coupra.app` + `api-nodejs.coupra.app`, `token` URL param, Japanese leaderboard (位 rank / 点 points) |
| The other GameBull titles (Canyon Crash, BrainPuzzle, Fruit-Candy-Monsters, Monsters-Galaxy, LapsFuse, Cube-and-Hexa…) | **Skillz / SkillziOS** | folder & version names like `*SkillziOS*` / `*Skillz*`, a `Skillz` SDK folder/DLL, `SkillzDelegate` / `SkillzCrossPlatform` / match-launch calls |

**Removal approach** (worked example = Coupra; the full pre-removal map is saved in the
`coupra-integration.md` memory):

1. **Find the data hub** — the singleton that reads the URL/token and holds all platform data
   (Coupra → `DataFromReact.cs`; Skillz → the `SkillzDelegate` / match-start controller).
2. **Rewrite the hub to a static-defaults stub** — no URL parsing, no auth, no API calls, keeping only
   the fields the game actually reads:
   ```csharp
   public class DataFromReact : MonoBehaviour {
       public static string gameType = "free";
       public static int    difficulty = 1;
       public static string server_userName = "Player";
       // …safe defaults only
   }
   ```
3. **Neutralize the backend scripts.** Do **not** just delete the files if scenes still have those
   components attached — Unity throws "Script is missing" / `CS0101` / `CS0111`. Replace each with an
   **empty stub class**, same class name + method signatures:
   ```csharp
   public class ServerScorePost       : UnityEngine.MonoBehaviour { }
   public class MenuLeaderBoardPaid   : UnityEngine.MonoBehaviour { public void StartRun() { } }
   public class UpdatePointsAndTokens : UnityEngine.MonoBehaviour { public void RequestUpdatePointCoupons(int p,int c){} public void GiveFreeCoupon(){} }
   ```
   (If nothing in any scene references them, you may hard-delete instead — that's what the Downloads copy did.)
4. **Remove the backend hosts** and every coroutine that calls them. Verify: `grep -ri coupra` /
   `grep -ri skillz` over `Assets/**/*.cs` must return **nothing**.

**Coupra files removed/stubbed in this reference build (all in `Assets/ServerAPIs/`):**

| File | Role | Action taken |
|---|---|---|
| `DataFromReact.cs` | central hub: token/URL parse, auth, all static data | rewrote to 44-line static-defaults stub |
| `MenuLeaderBoardPaid.cs` | menu leaderboard fetch | empty stub (`StartRun()` kept) |
| `LeaderBoardPaid.cs` | in-game leaderboard | empty stub |
| `LeaderboardData.cs` | leaderboard DTO | empty stub |
| `ServerScorePost.cs` | POST final score to Coupra | empty stub |
| `UpdatePointsAndTokens.cs` | wallet (CP-token/points) update | empty stub |
| `UpdateRankingBoard.cs` | ranking POST | empty stub |
| `SaveImage.cs` / `SaveImageWebGL.cs` | screenshot + app-login redirect | empty stubs |

**Backend hosts removed:** `https://api-php.coupra.app/api` (Laravel — auth/profile) ·
`https://api-nodejs.coupra.app` (Node — coupons / leaderboard / scoring).

### A0.2 PlayFab + monetization package — `Assets/MyPlayfab/`

These games also bundle a **PlayFab-backed monetization / crypto-reward package**. In the BasketBall
build it's `Assets/MyPlayfab/` and contains:
- `PlayFab.prefab`, `Leaderboard_Canvas.prefab`, `BTN_Withdrawal.prefab`, `MoneyBankAnimation.prefab`,
  spin-wheel / wheel-of-luck assets, bitcoin/coin art, a `ZBD` (Zebedee) folder.
- `_Packages/6in1 Monetization Mini Games/` — **Lucky Wheel, Treasure Hunt, PickerWheel** mini-games,
  each with its own `GameController` / `UIProfileController` referencing PlayFab.

**Remove it for a game-only build:**
1. Confirm the core gameplay scenes don't use it (search scenes for `MyPlayfab` prefabs / `PlayFab`
   components). It's normally a separate monetization layer, not the actual game.
2. Delete the `Assets/MyPlayfab/` folder **and its `.meta`**. If any script elsewhere references a
   PlayFab type, remove or stub that reference the same way as the platform scripts.
3. This also shrinks the WebGL build (core `build.data` + `build.wasm` are already ~62 MB — dropping
   unused monetization code/assets helps load time).

> ⚠️ PlayFab often ships as **source, not a DLL**, so deleting the folder can surface compile references.
> Recompile after deleting and fix by removing the calling code or stubbing the missing type.

### A0.3 What to KEEP (don't remove)

- `Assets/Plugins/DownloadFile.jslib` — a **generic** WebGL file-download helper, not platform-specific.
  Keep if you want screenshot/download; otherwise optional.
- Standard gameplay/UI libraries: DOTween (`Demigiant`), TextMeshPro, EZ Camera Shake, Layer Lab,
  Toy_Box_UI, SimpleFX, BubbleShooterKit — keep.
- **Add** (for GameBull): **Newtonsoft JSON** and GameBull's own `Assets/GameBull/Plugins/GameBullClipboard.jslib`.

### A0.4 Removal checklist (per game)

- [ ] Identify the host platform (**Coupra** *or* **Skillz / SkillziOS**) and its data hub script.
- [ ] Stub the data hub to static defaults; empty-stub (or delete) its backend scripts.
- [ ] Remove all platform backend hosts/URLs; `grep -ri coupra` / `grep -ri skillz` returns nothing.
- [ ] Delete `Assets/MyPlayfab/` (**PlayFab** + 6in1 monetization) if the core game doesn't use it.
- [ ] Recompile; fix any missing-script / missing-type errors with stubs.
- [ ] Build once and confirm the game runs standalone (no auth, straight to gameplay) before wiring GameBull.

---

## Part A — GameBull SDK (Complete)

### A.1 What GameBull is

GameBull is a play-for-prizes platform. A player opens the game **embedded in an iframe** on the
GameBull site, with all session info passed in the **URL query string**. The game reads those params,
calls the GameBull API for the player's context (branding, lives, tournament), runs, and **submits the
score back**. Three play modes are supported:

| Mode | Internal enum | Backend mode | How it starts |
|---|---|---|---|
| **Solo** | `PlayMode.Solo` | `HEAD_TO_HEAD_1V1` | Player taps Solo |
| **Private Room (4-player)** | `PlayMode.Room` | `TOURNAMENT_ROOM` | Host creates a room → shares a join link; friends join via `roomId` in URL |
| **Open Tournament** | `PlayMode.Tournament` | `OPEN_TOURNAMENT` | Player joins a listed tournament, starts a session |

### A.2 File-by-file architecture

The SDK lives in `Assets/GameBull/`. It is a clean layered design — **the game never touches URLs or
`UnityWebRequest` directly**, it only calls `GameBullApi` methods (or the lobby controller).

```
Assets/GameBull/
├── GameBullBoot.cs            ← Layer 0: reads URL query params at startup (auto-runs)
├── GameBullClient.cs          ← Layer 1: the ONLY code that hits the network (GetJson / PostJson)
├── GameBullEndpoints.cs       ← Layer 2: every GameBull URL in one place
├── GameBullApi.cs             ← Layer 3: typed methods + response DTOs (uses Newtonsoft)
├── GameBullLobbyController.cs ← Layer 4: MonoBehaviour — the whole lobby UI + game flow + score routing
├── GameBullApiTester.cs       ← Dev-only OnGUI tester ("Get Context")
├── GameBullBootDebugLabel.cs  ← Dev-only OnGUI label showing parsed boot params
├── Editor/
│   └── GameBullSetupWindow.cs ← Editor tool: menu "GameBull → Setup Lobby" generates the lobby canvas
└── Plugins/
    └── GameBullClipboard.jslib ← WebGL clipboard copy (share link). MUST be under a "Plugins" folder.
```

**Layer 0 — `GameBullBoot.cs`**
Runs automatically via `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` (no GameObject needed). Reads
`Application.absoluteURL`, parses the query string, and exposes static props the rest of the SDK reads:

| Property | URL param | Meaning |
|---|---|---|
| `TenantSlug` | `tenantSlug` | The partner/tenant |
| `SessionToken` | `sessionToken` | Bearer auth token |
| `Seed` | `seed` | Deterministic RNG seed |
| `Mode` | `mode` | Play mode |
| `GameId` | `gameId` | This game's id |
| `ReturnUrl` | `returnUrl` | Where to send the player after the game |
| `RoomId` | `roomId` | Present → this player is a **joiner** of a private room |
| `PlayerId` | `playerId` | This player's id inside a room |
| `OpenTournamentId` | `openTournamentId` | Present → open-tournament game |

**Layer 1 — `GameBullClient.cs`**
Two async methods, `GetJson(url, bearerToken?)` and `PostJson(url, jsonBody, bearerToken?)`, both wrap
`UnityWebRequest` and turn Unity's coroutine send into an awaitable `Task`. Auth goes in the
`Authorization: Bearer <token>` header **or** in the JSON body depending on the endpoint (see below).

**Layer 2 — `GameBullEndpoints.cs`**
Single source of truth for URLs. **Base URL:** `https://api-gamebull.hyperfunded.pro` (swap to
`http://localhost:3000` for local backend testing). Key routes:

| Method | Route |
|---|---|
| Context (branding/lives/tournament) | `GET /play/games/{gameId}/context?tenantSlug={slug}[&roomId=]` |
| Solo score | `POST /play/scores` |
| Room score | `POST /play/rooms/{roomId}/scores` |
| Create room | `POST /play/rooms` |
| Join room | `POST /play/rooms/{roomId}/join` |
| Room leaderboard | `GET /play/rooms/{roomId}/leaderboard` |
| Tournament list | `GET /tenants/{slug}/tournaments` |
| Tournament join / session / score / leaderboard | `POST/GET /play/tournaments/{id}/...` |
| My room history | `GET /me/rooms` |

**Layer 3 — `GameBullApi.cs`**
Static async methods returning typed DTOs (`GameContext`, `ScoreResult`, `CreateRoomResult`,
`RoomLeaderboard`, `OpenTournament`, etc.). Important quirks documented in the code:
- **Token placement differs per endpoint.** Solo & tournament scores put `sessionToken` **in the body**;
  room scores use `playerId` in the body; create/join room pass the token as the **Bearer header**.
- **Score field naming differs.** Solo/tournament use `value`; room uses `score`. `ScoreResult` exposes
  `effectiveValue` to paper over that.

**Layer 4 — `GameBullLobbyController.cs`** (the big one, ~1180 lines)
A `DontDestroyOnLoad` singleton (`Instance`) that lives on the `GameBullLobby` root. It:
- Reads `/context` (`LoadContext()`), applies **branding** (splash logo, primary color tint, background).
- Renders the mode picker, create-room, share, join, leaderboard, open-tournament, and my-history panels
  (all fields wired in the Inspector under `[Header(...)]` groups).
- Remembers the chosen mode + room/tournament identity across the scene load (home → main → home).
- **Routes the score** on game-over via `SubmitScore(score, playTimeMs)`, picking the right API call
  for the current mode.
- Handles the **joiner flow**: if `roomId` is in the URL, shows the join panel once (`_joinPanelShownOnce`).
- Copies the share link to the browser clipboard via the `.jslib`.
- Prints its build number on `Awake()`: `[GB-VERSION] Build v<BUILD_VERSION>` — **`BUILD_VERSION` is a
  `const int` you bump on every SDK edit** so you can confirm the deployed build in the browser console.

### A.3 Partner branding / custom assets

`/context` returns `customization` with `colors.primary`, `colors.secondary`, `logoUrl`, and an
`assets[]` array of `{ key, url, mimeType, bytes }`. The lobby downloads these into static sprites and
applies them:

| Asset key | Field | Applied to | Fallback |
|---|---|---|---|
| `"ball"` | `PartnerBallSprite` | the ball's `SpriteRenderer` (all current + future balls) | original sprite |
| `"background"` | `PartnerBackgroundSprite` | gameplay BG (`backgroundObjectName`, a UI `Image` or `SpriteRenderer`), `.color = white` | tint BG with `colors.primary` |
| `colors.primary` | — | `primaryColorTargets[]` GameObjects (card Images) | — |
| `logoUrl` | — | `splashLogoImage` on the splash scene | — |

Both image assets download **asynchronously**; the apply code **polls up to ~5 s** for the sprite before
falling back, so it works even if the gameplay scene loads before the download finishes.

### A.4 Inspector fields that are 🔁 PER-GAME

These `GameBullLobbyController` fields under `[Header("Scene control")]` must match each game's scenes/objects:

```csharp
public string lobbySceneName        = "home";          // scene where the lobby shows
public string gameplaySceneName     = "main";          // scene loaded when a mode starts
public string splashSceneName       = "AdController";   // splash scene (logo swap)
public string backgroundObjectName  = "background";     // the gameplay BG object to skin
public string splashLogoObjectName  = "logo";           // the splash logo Image to swap
```

Also wire (per game): `splashLogoImage`, `primaryColorTargets[]`, and all the panel/button/text
references in each `[Header]` group.

### A.5 How to add GameBull to a new game (summary)

1. **Import** the GameBull `.unitypackage` (scripts + `Plugins/GameBullClipboard.jslib` + Editor tool).
2. Ensure **Newtonsoft JSON** and **TMP** are present. Resolve any leftover compile errors from the old
   platform (stub deleted scripts, rename any duplicate `Bridge`/`OnSceneLoaded` collisions).
3. Menu **`GameBull → Setup Lobby` → "Create Lobby Canvas"** — generates `GameBullLobby > Canvas >
   ModePickerPanel` (ScreenSpaceOverlay, `sortingOrder = 100`, ScaleWithScreenSize) + an EventSystem.
   The whole creation is one Undo group.
4. Build out / wire the remaining panels and drag references into the controller's Inspector fields.
5. Set the **scene-control** fields (A.4) to this game's scene and object names.
6. **Route the score:** in the game's `GameManager` at game-over, add:
   ```csharp
   if (GameBull.GameBullLobbyController.Instance != null)
       GameBull.GameBullLobbyController.Instance.SubmitScore(Score, 30000); // score, playTimeMs
   // For Room/Tournament modes, hide the game's own end UI (GameBull shows the report card):
   var gb = GameBull.GameBullLobbyController.Instance;
   if (gb != null && (gb.IsRoomGame || gb.IsTournamentGame)) { /* hide GameOverPanel etc. */ }
   ```
7. **Apply the partner ball sprite** on the gameplay scene: poll `GameBullLobbyController.PartnerBallSprite`
   (up to ~5 s) and assign it to the ball's `SpriteRenderer`.
8. **Bump `BUILD_VERSION`** so you can verify the deployed build in the browser console.

---

## Part B — WebGL Resolution & Responsive Scaling

### B.1 The problem

The default Unity WebGL page renders the canvas at a **fixed pixel size**, so inside an iframe/container
it overflows and shows scrollbars, or gets stretched/distorted. We need the game to **scale to fit its
container, keep aspect ratio (letterbox, not stretch), and never produce scrollbars** — at any size,
including inside an iframe.

### B.2 Project resolution settings (🔁 PER-GAME aspect)

`ProjectSettings/ProjectSettings.asset` — this is a **portrait** game:

```
defaultScreenWidth:     1920      # standalone
defaultScreenHeight:    1080
defaultScreenWidthWeb:  1080      # ← WebGL render resolution (portrait)
defaultScreenHeightWeb: 1920      # ← aspect = 1080 / 1920 = 0.5625
webGLMemorySize:        32
webGLLinkerTarget:      1         # WebAssembly
webGLPowerPreference:   2         # high-performance GPU
webGLTemplate:          PROJECT:IdeofuzionBridge   # ← our custom template (Part C)
```

> 🔁 **PER-GAME:** set `defaultScreenWidthWeb` / `defaultScreenHeightWeb` to the game's real aspect.
> The template picks this up automatically via `{{{ WIDTH }}} / {{{ HEIGHT }}}`.

### B.3 The scaling solution (in `index.html`)

Three ingredients make it responsive:

1. **Full-viewport, no-scroll shell** — CSS:
   ```css
   html, body { margin:0; padding:0; width:100%; height:100%; overflow:hidden; }
   #unity-container { position:fixed; inset:0; }      /* fills the viewport */
   #unity-canvas    { display:block; position:absolute; }  /* NO fixed px size; JS positions it */
   ```
2. **JS letterbox** — `resizeCanvas()` computes the largest rectangle of the game's aspect ratio that
   fits, and centers it. Runs on load **and** on every `resize`, **and again after Unity finishes loading**
   (so it's correct once the loading bar disappears):
   ```javascript
   var GAME_ASPECT = {{{ WIDTH }}} / {{{ HEIGHT }}};   // e.g. 1080 / 1920
   function resizeCanvas() {
     var canvas = document.getElementById('unity-canvas');
     var availW = window.innerWidth, availH = window.innerHeight, w, h;
     if (availW / availH > GAME_ASPECT) { h = availH; w = Math.round(h * GAME_ASPECT); }
     else                               { w = availW; h = Math.round(w / GAME_ASPECT); }
     canvas.style.width  = w + 'px';
     canvas.style.height = h + 'px';
     canvas.style.left   = Math.round((availW - w) / 2) + 'px';
     canvas.style.top    = Math.round((availH - h) / 2) + 'px';
   }
   window.addEventListener('resize', resizeCanvas);
   resizeCanvas();
   ```
3. **Render matches display** — in the `createUnityInstance` config:
   ```javascript
   matchWebGLToCanvasSize: true,   // Unity's render buffer tracks the CSS canvas size
   ```

### B.4 Footer removed

The default template's footer (Unity logo + fullscreen button + build title, inside `#unity-footer`) was
**deleted** so nothing shows below the game. Three coordinated changes:
- Delete the `<div id="unity-footer">…</div>` markup.
- Delete the `#unity-footer` CSS rule.
- Simplify `resizeCanvas()` to use the **full** `window.innerHeight` (previously it subtracted
  `footer.offsetHeight`).
- Remove the leftover JS line that set `.onclick` on the now-removed `#unity-fullscreen-button`
  (otherwise `getElementById(...)` returns `null` and setting `.onclick` throws, breaking the rest of the
  `createUnityInstance().then()` callback).

> This build is an HTML/JS-only change to the **template** — no C# rebuild is needed to re-tune scaling,
> but the template is what gets baked into every future Unity build.

---

## Part C — The Ideofuzion WebGL Template

### C.1 Why a custom template

Unity **regenerates `TemplateData/style.css` and the built `index.html` on every build**. If you edit the
build output directly, the next build wipes it. So all customization lives in a **custom template** that
Unity copies from on each build. Ours is `IdeofuzionBridge`. It also carries the **JS↔Unity bridge** so
the page can talk to the game.

### C.2 Location & selection

```
Assets/WebGLTemplates/IdeofuzionBridge/
├── index.html                 ← the page (letterbox + loading UI + bridge)
├── thumbnail.png              ← shown in Player Settings template picker
└── TemplateData/
    ├── style.css              ← Unity's default stylesheet (we OVERRIDE it inline in index.html)
    ├── game-icon.jpg          🔁 PER-GAME  loading logo (copied from the game's icon)
    ├── BasketBall_Hero_BG.jpg 🔁 PER-GAME  loading-screen hero background
    ├── favicon.ico
    ├── progress-bar-*.png, unity-logo-*.png, fullscreen-button.png, … (default assets, unused/overridden)
```

**Select it:** Player Settings → Resolution and Presentation → **WebGL Template → IdeofuzionBridge**.
In `ProjectSettings.asset` this shows as `webGLTemplate: PROJECT:IdeofuzionBridge`.

> **Golden rule:** because `style.css` is regenerated every build, **all CSS overrides go in the inline
> `<style>` block inside `index.html`**, using `!important` where they must beat `style.css`
> (e.g. the progress-bar background images). Never rely on editing `TemplateData/style.css`.

### C.3 Template variables (Mustache-style `{{{ … }}}`)

Unity substitutes these at build time — keep them in the template so it works for any game:

| Variable | Becomes |
|---|---|
| `{{{ PRODUCT_NAME }}}` | Product name (page `<title>`) |
| `{{{ WIDTH }}}` / `{{{ HEIGHT }}}` | Default web resolution → used for `GAME_ASPECT` |
| `{{{ LOADER_FILENAME }}}` / `{{{ DATA_FILENAME }}}` / `{{{ FRAMEWORK_FILENAME }}}` / `{{{ CODE_FILENAME }}}` | Build file names |
| `{{{ COMPANY_NAME }}}` / `{{{ PRODUCT_VERSION }}}` | From Player Settings |
| `{{{ SPLASH_SCREEN_STYLE.toLowerCase() }}}` | `dark`/`light` (default logo/bar variant — we override these) |

### C.4 The page ↔ game bridge

After `createUnityInstance(...).then(unityInstance => { … })` resolves, the template:
1. **Announces readiness** to the parent page: `window.parent.postMessage({ type: 'game:ready' }, '*');`
2. **Listens** for a `campaign:init` message and forwards a runtime **ball image** into Unity:
   ```javascript
   window.addEventListener('message', function (event) {
     var msg = event.data;
     if (!msg || msg.type !== 'campaign:init') return;
     var cfg = msg.config || {};
     if (cfg.ballImage) unityInstance.SendMessage('Bridge', 'SetBallImage', cfg.ballImage);
   });
   ```
3. **Self-test:** `?ballTest=1` draws a hot-pink circle on a canvas and pushes it as the ball image —
   lets you verify the swap with no external image / no CORS. (Confirmed working at
   `http://localhost:8000/?ballTest=1`.)

**Game side — `Assets/Script/Bridge.cs`:** a `MonoBehaviour` auto-created at startup via
`[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]` + `DontDestroyOnLoad`, named exactly **`Bridge`** (so
`SendMessage('Bridge', …)` finds it). `SetBallImage(string dataUrl)`:
- Strips a `data:image/...;base64,` prefix if present, `Convert.FromBase64String` → `Texture2D.LoadImage`
  → `Sprite.Create`.
- Stores it statically and re-applies on every `sceneLoaded` (so balls spawned later also get it).
- Preserves sprite scale using PPU math: `ppu = newTexWidth * origPPU / origSpriteRectWidth`.
- Null-safe: no image sent → the ball keeps its original sprite.

> **Naming caution:** some games already have a `Bridge` class. A duplicate causes `CS0101`/`CS0111`.
> Reuse the existing one or namespace/rename to avoid the collision.

---

## Part D — Loading Screen & Loading Bar Changes

> This is the `Loading screen: bg image, bar fix, icon, Safari hide` commit, done entirely in
> `Assets/WebGLTemplates/IdeofuzionBridge/index.html` (inline `<style>` + the loader script).

### D.0 Two separate loading layers — don't confuse them

1. **HTML loading screen** — our grey/dark page with the hero BG, game icon, rounded bar + percentage.
   Shown **while the build downloads, before Unity starts**. ← *All the work below is on this layer.*
2. **Unity engine splash** (Player Settings → Splash Image) — the "Made with Unity" logo that renders
   **inside the canvas after Unity boots**. None of the loading-screen work below touches this.

   **To remove the Unity engine splash:** Player Settings → Splash Image → uncheck **"Show Splash Screen"**
   / **"Show Unity Logo."** ⚠️ This only works on **Unity Plus / Pro / Enterprise**. On **Unity Personal
   (free)** the splash is **forced at build time and cannot be removed** — which is why it's usually left in.

### D.1 Background: black → light → dark + hero image

Evolution of the loading background:
- Started `#231F20` (near-black) on `html/body`, `#unity-container`, `#unity-canvas`.
- Briefly `#F2F2F2` (light).
- **Final:** `#0e0e12` letterbox fill **plus a hero background image** on `#unity-container`:
  ```css
  html, body    { background:#0e0e12; }
  #unity-container {
    position:fixed; inset:0;
    background-color:#0e0e12;                                  /* letterbox bars */
    background-image:url('TemplateData/BasketBall_Hero_BG.jpg'); /* 🔁 PER-GAME image */
    background-size:contain; background-position:center; background-repeat:no-repeat;
  }
  @media (max-aspect-ratio: 1/1) {          /* portrait/mobile: fill screen, crop overflow */
    #unity-container { background-size:cover; }
  }
  #unity-canvas { display:block; position:absolute; background:transparent; }  /* see below */
  ```

**Critical gotchas learned here:**
- **Canvas must be `transparent`, not a solid color.** `#unity-canvas` sits *above* `#unity-container`
  in the stacking order; an opaque canvas background completely hides the container's hero image during
  loading. `transparent` lets the hero show through. Note the hero does **not** disappear once the game
  starts — Unity only fills the **centered, letterboxed play area** of the canvas; the container's hero
  keeps showing in the letterbox side-bars during gameplay too (see next point).
- **⚠️ The hero image is PERMANENT — it fills the letterbox side-bars during BOTH loading AND gameplay.**
  The transparent, letterboxed canvas only covers the centered play area, so whatever is on
  `#unity-container` is visible in the bars the whole time the game is on screen. Therefore the hero must
  be a **wide, full-bleed, brand-neutral background** — **never square or portrait game art**, which gets
  split to both sides and looks oversized/broken (on one ported game, square game art literally peeked out
  of both side-bars behind the running game). 🔁 **If a game has NO dedicated wide hero, omit
  `background-image` entirely and keep `#unity-container` a solid `#0e0e12`** (and drop the `contain`/`cover`
  rules with it). A clean solid bar always beats stretched/duplicated game art.
- **`contain` vs `cover`.** A landscape hero image with `contain` shrinks to a thin band on a narrow
  portrait screen — nearly invisible on mobile. The `@media (max-aspect-ratio: 1/1) { cover }` query fixes
  that while keeping `contain` on desktop. (Only relevant when a hero image is actually used.)
- **File extension & casing matter.** GitHub Pages is case-sensitive; a `.png` reference to a `.jpg` file
  404s silently. Match the real filename exactly (`BasketBall_Hero_BG.jpg`).

### D.2 Game icon instead of "Made with Unity"

The default loading logo is `#unity-logo` (a CSS `background-image` div, **not** an `<img>`), whose image
comes from `style.css` (`unity-logo-dark/light.png`). We repurpose that div to show the **game's own icon**:
- Copied the game's large icon (`Assets/Images/basket_Icon.jpg`, ~500px) into
  `TemplateData/game-icon.jpg` — the `favicon.ico` was too small (16–48px) for the 154–180px box.
- Overrode `#unity-logo` **inline in `index.html`** (beats `style.css`, survives rebuilds):
  ```css
  #unity-logo {
    width:180px; height:180px;                 /* was 154; final 180 */
    margin:0 auto 12px;                         /* centered, 12px gap above the bar */
    background:url('TemplateData/game-icon.jpg') center / contain no-repeat;
  }
  ```
- HTML: `<div id="unity-logo"></div>` is the **first child** of `#unity-loading-bar`, above the track.

> The icon was removed and re-added a couple of times during iteration — the final state **keeps** it at
> 180×180. 🔁 PER-GAME: swap `game-icon.jpg`.

### D.3 Rounded custom progress bar + live percentage

Replaced Unity's PNG-image bar with a pure-CSS rounded bar and added a live `%` readout:

```css
#unity-progress-bar-empty {           /* track */
  width:300px; height:18px;
  background:#d9d9d9 !important; background-image:none !important;  /* kill the PNG */
  border-radius:9px; overflow:hidden; margin:12px auto 0;
}
#unity-progress-bar-full {            /* fill */
  width:0%; height:100%;
  background:#1C3A5B !important; background-image:none !important;
  margin-top:0 !important;            /* ← BAR FIX, see below */
  border-radius:9px; transition:width 0.1s ease;
}
#unity-progress-percent {             /* the % text */
  text-align:center; color:#F2F2F2;   /* light — readable on dark bg (was #231F20, invisible) */
  font-size:14px; font-family:arial, sans-serif; margin-top:6px;
}
```
```html
<div id="unity-loading-bar">
  <div id="unity-logo"></div>
  <div id="unity-progress-bar-empty"><div id="unity-progress-bar-full"></div></div>
  <div id="unity-progress-percent">0%</div>   <!-- new -->
</div>
```

### D.4 Loading bar bug fixes

**Fix 1 — fill was half-height (thin centered strip).**
`style.css` sets `margin-top:10px` on `#unity-progress-bar-full`. Un-overridden, it pushed the 18px fill
down inside its 18px track, leaving ~8px visible. Fix: `margin-top:0 !important` in the inline style.

**Fix 2 — bar/percent desync + stuck at 90%.**
Unity's `onProgress` callback **plateaus at 0.9 and never fires 1.0**, and the fill width and the `%`
text were being driven separately. Fixes:
- **One source of truth** — a single `setLoadProgress(p)` sets *both* the fill width and the `%` text
  from `Math.round(p*100)`:
  ```javascript
  function setLoadProgress(p) {
    document.querySelector("#unity-progress-bar-full").style.width  = Math.round(p*100) + "%";
    document.querySelector("#unity-progress-percent").innerText     = Math.round(p*100) + "%";
  }
  createUnityInstance(canvas, config, setLoadProgress).then((unityInstance) => {
    setLoadProgress(1);   // ← force 100% before hiding, so it never sticks at Unity's 0.9 plateau
    document.querySelector("#unity-loading-bar").style.display = "none";
    …
  });
  ```
- Removed any fake/`setInterval` progress animators (there were none in the final file — the desync was a
  stale earlier build).

### D.5 Safari force-hide of the icon

**Symptom:** on **Safari**, after load, the game icon (`#unity-logo`) stayed visible during gameplay,
even though the parent `#unity-loading-bar` was set to `display:none` (which cascades fine on Chrome).
Safari has a background-image paint-after-hide quirk.

**Fix:** after hiding the loading bar, **explicitly** force-hide the logo with multiple properties:

```javascript
setLoadProgress(1);
document.querySelector("#unity-loading-bar").style.display = "none";
var logo = document.querySelector("#unity-logo");
if (logo) { logo.style.display = "none"; logo.style.visibility = "hidden"; logo.style.opacity = "0"; }
```

This is the final state in the shipped template.

---

## Part E — Step-by-Step Replication Checklist (for a new game)

Do these in order for each new game:

- [ ] **1. Unity + module.** Match `ProjectVersion.txt`; install **WebGL Build Support**.
- [ ] **2. Deps.** Ensure **Newtonsoft JSON** + **TMP**. Resolve old-platform compile errors (stub deleted
      scripts; fix any duplicate `Bridge`).
- [ ] **3. Strip old platform + monetization SDKs** (see [Part A0](#part-a0--strip-existing-platform--monetization-sdks-do-this-first)):
      remove the host platform (**Coupra** or **Skillz/SkillziOS**) — stub its data hub + backend scripts;
      delete **`Assets/MyPlayfab/`** (PlayFab + 6in1 monetization). `grep -ri` for the platform name returns nothing.
- [ ] **4. Import GameBull SDK** (`Assets/GameBull/…` incl. `Plugins/…jslib` and `Editor/…`).
- [ ] **5. `GameBull → Setup Lobby → Create Lobby Canvas`;** wire all Inspector panel/button/text fields.
- [ ] **6. Set 🔁 scene-control fields** (`lobbySceneName`, `gameplaySceneName`, `splashSceneName`,
      `backgroundObjectName`, `splashLogoObjectName`) to this game's names.
- [ ] **7. Route score** in `GameManager` on game-over → `Instance.SubmitScore(score, playTimeMs)`; hide the
      game's own end UI for Room/Tournament modes.
- [ ] **8. Apply partner ball sprite** (poll `PartnerBallSprite`) and confirm partner background/logo/colors.
- [ ] **9. Copy the `IdeofuzionBridge` template** into `Assets/WebGLTemplates/`; add **`Bridge.cs`** (named
      exactly `Bridge`).
- [ ] **10. Set 🔁 web resolution** (`defaultScreenWidthWeb/HeightWeb`) to the game's aspect.
- [ ] **11. Player Settings → WebGL Template → `IdeofuzionBridge`.**
- [ ] **12. Replace 🔁 `TemplateData/game-icon.jpg` and 🔁 `TemplateData/<Game>_Hero_BG.jpg`;** update the
      `background-image` filename (exact case + extension) in `index.html`. The hero must be a **wide,
      full-bleed, brand-neutral** image (it shows in the letterbox bars during gameplay too, not just
      loading) — **or, if no dedicated wide hero exists for this game, omit the hero image and leave
      `#unity-container: #0e0e12`.**
- [ ] **13. Set `<title>` / product name;** confirm the letterbox, footer-removed, rounded bar, %, force-100,
      Safari hide are all present in the template `index.html`.
- [ ] **14. Bump `BUILD_VERSION`;** build; serve locally; verify console shows `[GB-VERSION] Build v…`,
      the loading screen, `?ballTest=1` swap, and score submission.
- [ ] **15. Deploy the `build/` folder to GitHub Pages** and register the Pages URL in the GameBull admin
      panel (see [Part F](#part-f--deploy-the-build-to-github-pages-the-url-for-the-gamebull-admin-panel)).

---

## Part F — Deploy the Build to GitHub Pages (the URL for the GameBull admin panel)

GameBull doesn't host the game — **you host the WebGL build yourself and give GameBull a URL.** We host
the build as a static site on **GitHub Pages**, and that Pages URL is what goes into the GameBull admin
panel as the game's URL. GameBull then loads it in an iframe and appends the session params that
`GameBullBoot` reads (Part A).

### F.1 The model — the `build/` folder is its own git repo

- The Unity project's `.gitignore` **ignores `build/`**, so the build is **not** part of the main project
  repo. Instead, `build/` is its **own separate git repo** pushed to a **public** GitHub repository.
- GitHub Pages serves that repo's `index.html` at `https://<user>.github.io/<repo>/`.
- Files are served **uncompressed** (no `.gz`/`.br`), so plain Pages works with no special server config.
  Paths are **case-sensitive** — asset filenames in `index.html` must match exactly.

**This reference build (`Basket Ball Dunk`):**

| Thing | Value |
|---|---|
| Deploy repo | `https://github.com/alishehroz-ideo/basket-ball.git` (branch `main`) |
| Live Pages URL (→ admin panel) | `https://alishehroz-ideo.github.io/basket-ball/` |

### F.2 One-time setup (per game)

1. Build the game in Unity into the game's `build/` folder.
2. Create a **new public repo** on GitHub, e.g. `alishehroz-ideo/<game-slug>`. 🔁 PER-GAME slug.
3. Turn the `build/` folder into a repo and push it:
   ```bash
   cd "<project>/build"
   git init
   git branch -M main
   git remote add origin https://github.com/alishehroz-ideo/<game-slug>.git
   git add .
   git commit -m "Initial build"
   git push -u origin main
   ```
4. On GitHub: **repo → Settings → Pages → Build and deployment → Source: "Deploy from a branch" →
   Branch: `main` / `/ (root)` → Save.** Wait ~1–2 min for the first deploy.
5. The game is now live at `https://alishehroz-ideo.github.io/<game-slug>/`. Open it and confirm the
   loading screen + game load (hard-refresh / incognito to avoid cache).

### F.3 Register the URL in the GameBull admin panel

Paste the **Pages URL** (`https://<user>.github.io/<game-slug>/`) into the game's URL field in the GameBull
admin panel. GameBull launches it with the session query params appended, which `GameBullBoot` parses:

```
https://<user>.github.io/<game-slug>/?tenantSlug=<slug>&gameId=<GAMEID>&seed=<seed>&sessionToken=<token>&returnUrl=<url>
# room games also add &roomId=…&playerId=…  ·  open tournaments add &openTournamentId=…
```

Values used for this build (🔁 PER-GAME `gameId` + URL): `tenantSlug=ideo-test`, `gameId=BASKET_BALL`,
API `https://api-gamebull.hyperfunded.pro`.

### F.4 Updating after every rebuild

1. Rebuild in Unity into the **same** `build/` folder (overwrites `Build/` + regenerates `index.html` from
   the template).
2. Push:
   ```bash
   cd "<project>/build"
   git add .
   git commit -m "New build"
   git push
   ```
3. Wait ~1–2 min for Pages to redeploy, then hard-refresh / incognito. The admin-panel URL never changes —
   only the content behind it updates.

### F.5 ⚠️ Security & size (the repo is PUBLIC)

- **Do not commit secrets.** ⚠️ In this build, auth artifacts leaked into the public repo: **`c.txt` and
  `h.txt`** (curl cookie files holding a GameBull `gb_user` session cookie), and the tester writes a
  transient `session.json`. There is **no `.gitignore`** in the build repo. **Add one** and untrack the
  leaked files:
  ```bash
  cd "<project>/build"
  printf 'c.txt\nh.txt\nsession.json\ncurl\n*.bat\n' > .gitignore
  git rm --cached c.txt h.txt 2>/dev/null
  git commit -m "Stop tracking local auth/cookie files"
  git push
  ```
  (Rotate the token if it was ever a real one, not just a guest test token.)
- **File-size limit:** GitHub blocks any single file **> 100 MB**. Here `build.wasm` ≈ 42 MB and
  `build.data` ≈ 33 MB — fine. If a heavier game exceeds 100 MB, split assets, enable **Git LFS**, or use
  Unity build compression — but Brotli/Gzip needs response headers Pages doesn't set, so prefer keeping
  files **uncompressed and under 100 MB**.

### F.6 Optional — local test batch files (`build/`)

For fast local testing we kept two Windows batch files in the build folder (they simulate what GameBull
does — mint a guest token and open the deployed URL with session params). 🔁 PER-GAME: `BUILDDIR`,
`GAMEID`, `GAMEURL`, `TENANT`.

- **`deploy-and-play.bat`** — full cycle: `git add/commit/push` → wait 150 s for Pages → mint guest token
  (`POST /tenants/<tenant>/guests` then `POST /play/sessions`) → `start chrome --incognito "<GAMEURL>?…"`.
- **`play.bat`** — quick re-test: mint token → open incognito, **no push/wait** (build already deployed).

> These are convenience/testing tools only — the actual player launch is done by the GameBull platform via
> the admin-panel URL. (In `for /f` the piped PowerShell must keep its `^|` escaping.) Per F.5, keep
> `*.bat` and `session.json` out of the public repo.

---

## Reference — Exact Values, Colors & File Locations

**Colors / sizes used on the loading screen:**

| Thing | Value |
|---|---|
| Letterbox / page background | `#0e0e12` |
| Progress track | `#d9d9d9`, `height:18px`, `border-radius:9px`, `width:300px` |
| Progress fill | `#1C3A5B`, `height:100%`, `margin-top:0 !important`, `transition:width 0.1s ease` |
| Percent text | `#F2F2F2`, `14px arial` |
| Game icon box | `180×180px`, `margin:0 auto 12px`, `contain` |
| Portrait BG switch | `@media (max-aspect-ratio: 1/1) { background-size: cover }` |

**Per-game values on this project (`Basket Ball Dunk`):**

| Setting | Value |
|---|---|
| Web resolution | `1080 × 1920` (portrait, aspect `0.5625`) |
| Company / product / version | `ASN Games` / `Basket Ball Dunk` / `2` |
| WebGL template | `PROJECT:IdeofuzionBridge` |
| Hero BG file | `TemplateData/BasketBall_Hero_BG.jpg` |
| Icon file | `TemplateData/game-icon.jpg` (from `Assets/Images/basket_Icon.jpg`) |
| GameBull `tenantSlug` / `gameId` | `ideo-test` / `BASKET_BALL` |
| Deploy repo | `github.com/alishehroz-ideo/basket-ball` (branch `main`) |
| Live URL (→ GameBull admin panel) | `https://alishehroz-ideo.github.io/basket-ball/` |

**Key file locations:**

| File | Purpose |
|---|---|
| `Assets/GameBull/` | GameBull SDK (all layers + Editor + Plugins) |
| `Assets/Script/Bridge.cs` | JS→Unity ball-image bridge |
| `Assets/WebGLTemplates/IdeofuzionBridge/index.html` | Custom template — letterbox, loading UI, bridge |
| `Assets/WebGLTemplates/IdeofuzionBridge/TemplateData/` | Template assets (icon, hero BG, style.css…) |
| `ProjectSettings/ProjectSettings.asset` | Web resolution, `webGLTemplate`, memory/linker/power |
| `build/` | The deployed WebGL output (Build/, TemplateData/, index.html) — **its own git repo → GitHub Pages** (Part F) |
| `build/deploy-and-play.bat` / `play.bat` | Local test scripts (push + mint token + open) — keep out of the public repo |

**GameBull backend:** base URL `https://api-gamebull.hyperfunded.pro` (in `GameBullEndpoints.cs`).
**Hosting:** `build/` is pushed to a public GitHub repo and served via GitHub Pages; the Pages URL is the
value registered in the GameBull admin panel (Part F).

---

*Generated from the project source and the full Claude session history of the Coupra BasketBall build.
When you finish porting a game, update the "Per-game values" table with that game's numbers.*
