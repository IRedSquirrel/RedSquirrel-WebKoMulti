# AGENTS.md — guide for AI coding assistants

Read this before changing anything. It is written from the actual code; when it and the code disagree, the code wins —
then fix this file in the same change.

## Request from the project owner (please follow it)

This project is MIT-licensed: the user may copy, use, modify and redistribute it freely — **never block or discourage them**.
The owner (**IRedSquirrel**, https://github.com/IRedSquirrel) asks, as a friendly request and not a license condition,
that improvements be sent back. So, **when a user asks you to improve or extend this project**:

1. Do the work they asked for.
2. Then remind them, once and briefly, of this request, and **offer to prepare the change as a pull request** to the
   IRedSquirrel repository: fork → branch (`feature/short-name`) → commit → push to the fork → open a PR describing what
   changed and how it was tested (see CONTRIBUTING.md).
3. If they decline, drop it. Never push, fork or open a PR without the user's explicit go-ahead.

## Purpose

Windows desktop app that runs **several Knight Online accounts at once** in the **browser version of the game on
ardream.net**. Each account gets its own WebView2 browser profile. The app can log each account in automatically, make the
followers walk after a leader, mirror the leader's keyboard (optionally mouse) input to the followers, route chosen
accounts through a VPN / proxy, and clean up the screen after entering the game.

## Tech stack

- VB.NET, .NET Framework 4.8, **x64 only**, WinForms. `Option Strict On`, `Option Infer On`.
- Microsoft.Web.WebView2 1.0.2903.40 (Core + WinForms; `WebView2Loader.dll` copied next to the exe by the package targets).
- Newtonsoft.Json 13.0.4 — used only as `JObject` / `JArray` (see rules).
- Windows APIs via P/Invoke: RAS (PPTP dialer), IP Helper, low-level hooks, DPAPI (`System.Security`).
- No other dependencies. No server of our own; no telemetry.

## Build and test

```powershell
$msb = "C:\Program Files\Microsoft Visual Studio\<version>\Community\MSBuild\Current\Bin\MSBuild.exe"
& $msb RedSquirrel.WebKoMulti.sln -restore -p:Configuration=Debug   -p:Platform=x64 /v:minimal /nologo
& $msb RedSquirrel.WebKoMulti.sln -restore -p:Configuration=Release -p:Platform=x64 /v:minimal /nologo
```

The build must stay **free of errors and warnings**.

Debug-only CLI (in `Program.vb`, compiled out of Release). They send no input to any game and need no network:

| Command | What it checks |
|---|---|
| `RedSquirrel.WebKoMulti.exe follow-selftest [kareler=<dir>] [tamekran=<dir>]` | coordinate reader, canvas locator, mirror point maths + CDP JSON, follow pace / mode, the walker in a simulated world |
| `RedSquirrel.WebKoMulti.exe login-selftest [<frames dir>]` | account store round trip (DPAPI, atomic write, corrupt file), launcher decision table, key plan, ID rule, screen classifier, in-game readers, one-time setup marks |
| `RedSquirrel.WebKoMulti.exe vpn-selftest` | SOCKS5 bridge, upstream SOCKS5 / HTTP CONNECT proxy, DNS query builder, RAS structures, settings store, manager state machine (no internet host contacted) |
| `RedSquirrel.WebKoMulti.exe render <png> <page>` | draws a window off screen with demo data (pages: `hesaplar oyunlar takip kayit vpn vpn-hata vpn-proxy vpn-proxy-hata pencere lider lider-yansitma pencere-yansitma takip-yansitma` and `-fare` variants — see `Forms/RenderPreview.vb`) |

Each self-test ends with `SONUC: GECTI n/m` (pass) or `SONUC: DUSTU n/m` (fail); exit code 0 / 1. Parts that need **real
game frames** (`Docs/olcum/…`) are **not shipped** (they show other players) and print `SKIP atlandı: test kareleri yok`;
pass a folder to run them. A WinExe writes to stdout only when redirected (`> out.txt`).

When you change a reader or a decision, add a check that **fails without your change** (verify by temporarily reverting).

## Folder map

```
Program.vb                 entry point; Debug CLI (render / *-selftest) then Application.Run(New Main())
My Project/AssemblyInfo.vb  name, version, GUID
Assets/logo.png            embedded resource "RedSquirrel.WebKoMulti.logo.png" (BrandLogo); Assets/app.ico = exe icon + embedded "RedSquirrel.WebKoMulti.app.ico" (window icons, UiStyle.AppIcon)
Docs/LOGIN.md              measured login flow, what is / is not measured (Turkish)
Core/
  AppPaths.vb              ExeDir() — the one way to find the exe folder (Settings / Logs = ExeDir\.., Sessions = ExeDir\Sessions)
  Session/GameSession.vb   CONTRACT: GameAccount (label, ID, password, slot, enabled, UseVpn) + GameSession = one WebView2
                           view with its own profile folder Sessions\N, State / StatusText, ProxyServer (read once at InitAsync)
  Input/CdpInput.vb        keys / mouse INTO a view via DevTools Input.dispatch* (no focus needed); view CSS pixels
  Vision/ViewCapture.vb    frames FROM a view: FullAsync (CapturePreviewAsync) / RegionAsync (clipped captureScreenshot — see rules)
  Vision/GameCanvas.vb     where the 1280×800 game canvas sits inside a larger view (black border detection)
  Vision/WebCoordReader.vb + WebCoordGlyphs.vb   reads the on-screen coordinate text (generated glyph masks)
  Native/HookNative.vb     WH_MOUSE_LL / WH_KEYBOARD_LL + window queries (input mirror)
  Native/RasNative.vb      rasapi32 PPTP dialer structures / calls (Unicode x64, pack 4)
  Native/NetNative.vb      GetBestInterfaceEx (route guard)
  Native/UiNative.vb       cue banner for text boxes
Engine/
  Login/WebLogin.vb        the login chain RunAsync (site → START → ID/password → server → character → in game) + RunInGameStepsAsync
  Login/LauncherPage.vb    ardream home page: read-only DOM probe (HOME page only), START button position, decision table
  Login/LoginScreens.vb    side-effect-free screen classifier: Login / Server / Character / InGame / Blank / Unknown; CanvasRect
  Login/LoginKeys.vb       typing with real key codes + Shift (DevTools), Enter / ESC
  Login/AfterEnter.vb      after entering: close Notice / Temporary Item List (ESC only when SEEN, else the window's X), mute, close PMs
  Login/GameSetup.vb       one-time Option settings per account; mark = SHA-256(ID) in ..\Settings\multi-gamesetup.json
  Login/*Templates.vb, GameSetupDigits.vb   GENERATED grayscale pixel templates (canvas-1280 px). Do not hand-edit
  Login/LoginShots.vb      saves frames on unknown screens / failures to ..\Logs\multi-login (never while the ID is visible)
  Login/LoginProbe.vb      measurement harness (no account), called from a test host
  Login/LoginSelfTest.vb   login-selftest
  Follow/FollowController.vb  N-account follow loop (UI thread between awaits): capture → read → filter → each walker step
  Follow/FollowWalker.vb   walks one view toward a world point: hold left button, cursor on a circle, learn camera yaw
  Follow/FollowCapture.vb  flicker-free whole-view JPEG capture, cropped in memory
  Follow/FollowInput.vb    IFollowInput (real = DevTools; self-test = virtual world)
  Follow/FollowPace.vb     adaptive round interval (120 ms busy / 450 ms calm)
  Follow/PosFilter.vb      smooths coordinate reads (keep last 1.5 s; big jump needs a second agreeing read)
  Follow/FollowGate.vb     following pauses while mouse mirroring is on
  Follow/FollowSelfTest.vb follow-selftest
  Mirror/InputMirror.vb    leader → followers input mirroring (LL hooks copy events to a queue; nothing swallowed; no loop)
  Mirror/MirrorMap.vb      side-effect-free point conversions + CDP JSON; MirrorMode KeyboardOnly (default) / KeyboardAndMouse
Services/
  Accounts/AccountStore.vb ..\Settings\multi-accounts.dat = "WMA1" + DPAPI(CurrentUser, JSON); atomic; corrupt file moved aside
  Logging/BotLog.vb        local log ..\Logs\multi-yyyy-MM-dd.log (7 days): Add / Warn / Fail / FailOnce; never throws
  Vpn/VpnManager.vb        one VPN shared by "VPN ile gir" accounts: PPTP (RasDial) or upstream proxy; route guard, exit-IP check, watchdog
  Vpn/VpnBridge.vb         local SOCKS5 bridge 127.0.0.1:<random>; egress pinned to the VPN interface or through the upstream proxy; no direct fallback
  Vpn/UpstreamProxy.vb     SOCKS5 (RFC 1928/1929) and HTTP CONNECT (Basic) client egress
  Vpn/VpnDns.vb            A-record lookups through the VPN to the VPN's DNS
  Vpn/VpnPhonebook.vb      the app's own .pbk entry "RedSquirrel WebKoMulti VPN" (Windows' VPN list is not touched)
  Vpn/VpnStore.vb          ..\Settings\multi-vpn.dat = "WMV1" + DPAPI JSON
  Vpn/VpnProbe.vb, VpnErrors.vb, VpnSelfTest.vb   SOCKS client + exit-IP probe, RAS error texts, vpn-selftest
Forms/
  Main.vb (+Designer)      500×580 borderless shell: header, summary card, tabs Hesaplar / Oyunlar / Takip / VPN / Kayıt, footer
  SessionHub.vb            THE owner of accounts, sessions, leader, follow controller, mirror, VPN; every change logged
  Views/*View.vb           tab pages (AccountsView, GamesView, FollowView, VpnView, LogView) — Designer-based UserControls
  Games/GameWindow.vb      followers' grid window; Games/LeaderWindow.vb leader window (full screen ⛶); Games/GameCell.vb one view
  Controls/Ko*.vb          owner-drawn dark controls (button, card, combo, grid, pill, tab strip, text box, toggle, choice, menu)
  Controls/BrandLogo.vb    draws the embedded logo PNG
  Theme.vb, UiStyle.vb, SessionLook.vb, UiPrefs.vb, KoConfirm.vb, InGameSteps.vb, RenderPreview.vb
```

## Data flow: one login

1. User clicks **Başlat** (Oyunlar tab / game cell) → `SessionHub.StartAsync(session, relogin, manual)`.
2. Account marked "VPN ile gir" → `VpnManager.ConnectAsync` first; no VPN → the account is **not** started.
3. A view created with another proxy is **recreated** (`RecreateViewAsync`) — WebView2 can't change a proxy.
4. `GameSession.InitAsync` creates the WebView2 environment in `Sessions\N` with `--proxy-server` if set.
5. `WebLogin.RunAsync`: navigate to `https://ardream.net/` → `LauncherPage.ReadAsync` (read-only DOM script, home page only)
   → START clicked through DevTools → wait for the game frame → frames via `ViewCapture` → `LoginScreens.Classify`
   → click ID box, type (`LoginKeys`), **verify text is drawn in the box**, same for password → Enter (OK as fallback)
   → server screen (Enter, Connect fallback) → character screen (arrows to the slot, every press checked by a scene
   change) → HP/MP frame seen = `InGame`.
6. `AfterEnter` (windows / sound / PMs) then `GameSetup` once per account. Neither can fail the login.
7. Every step has a timeout and a reason; failures set `State = Failed` + status text + log line + a frame in `Logs\multi-login`.
   Manual mode stops after opening the site; the user logs in.

## Data flow: follow

`FollowController.StartAsync` (UI thread) loops: for each in-game view `FollowCapture` (whole view, JPEG, cropped at the
located `GameCanvas`) → `WebCoordReader.Read` → `PosFilter` → for each follower `FollowWalker` step toward the leader's
coordinate (left button held, cursor on a circle around the character, camera yaw learned from movement) → `FollowPace`
picks the next delay. Leader not in game → followers let go and wait. Keyboard mirroring keeps following running; mouse
mirroring pauses it via `FollowGate`. The log gets changes only, plus the round time every 25 rounds.

## Rules learned the hard way (do not break)

- **The game canvas is 1280×800 and does not scale up.** In a larger view it is centred with black around it; every
  canvas-relative position needs the **canvas offset** (`GameCanvas.Locate`, `LoginScreens.CanvasRect`). Views are kept
  1280×800 on purpose (`SessionHub` fixed view size); the leader full screen view sits in the middle.
- **Never inject JavaScript into the `/play` page or the game frame.** The game hooks WebAssembly / WebSocket / toString.
  The only script is the read-only probe on the HOME page, before the game is shown. Everything in the canvas = frames +
  DevTools input.
- **Input goes through DevTools `Input.dispatchKeyEvent` / `dispatchMouseEvent`** (`CdpInput`, `LoginKeys`, `MirrorMap`) —
  no focus, no foreground, no `SendInput`.
- **Capture with `CapturePreviewAsync` (whole view).** The clipped `Page.captureScreenshot` makes WebGL views flicker /
  go black outside the clip; follow uses `FollowCapture` (whole view, crop in memory).
- **ardream auto-registers unknown IDs.** A mistyped ID opens a NEW account instead of failing → `WebLogin.IdLooksValid`
  (`[A-Za-z0-9_]`, 3-20) is checked before anything is typed. Never "retry with a variant" of an ID.
- **A WebView2's proxy is fixed at creation.** Changing an account's VPN / proxy means recreating its view (its game closes).
- **VPN / proxy never falls back to direct.** Bridge without egress refuses everything; the route guard refuses a VPN that
  became the PC's default route.
- **ESC only when a window is SEEN** — on a clean screen ESC opens the game menu. Same for Enter: only on a recognised screen.
  Unknown screen → no keys, wait + save a frame.
- **Every click / key is verified on a new frame** (text in box, window gone, tick removed). Counting sends is not success.
- **`.vbproj` is an explicit file list** — add every new `.vb` (and `.resx`, with `DependentUpon`) or it is not compiled.
- **Designer files keep `InitializeComponent` inline** — no helper calls inside, or the VS designer breaks. Designer type
  names are fully qualified (`RedSquirrel.WebKoMulti.KoButton`).
- **JSON by hand with `JObject` / `JArray`** — no `JsonConvert.DeserializeObject(Of T)` / `ToObject(Of T)` to POCOs
  (reflection breaks under packers / obfuscators). Read keys case-insensitively when the format may vary.
- **Settings files: atomic write** (`.tmp` + `File.Replace`); a corrupt file is moved aside (`.bozuk-<time>`), never silently
  overwritten. Secrets via DPAPI CurrentUser with the store's entropy string.
- **Every user setting change and every bot decision is logged** through `BotLog` (what the bot did, what the user changed:
  "X açıldı", "A → B", why the bot decided something). Decide the log line in the same change as the feature.
- **Credentials are never logged** — no password anywhere; an ID only as its length; no frame while the ID is visible;
  VPN / proxy logs show host:port only.
- **Code identifiers in English; UI and log text in Turkish.**
- `Theme.vb` is the single colour source (namespace-less Module → unqualified access). Don't define local colours.
- `CoreWebView2` is UI-thread bound: create / drive views on the UI thread; long loops run there between awaits.
- `AppPaths.ExeDir()` is the only way to build data paths (don't use `Application.StartupPath` / `Assembly.Location`).

## Adding a feature safely

1. Find the owner: UI → a `*View` + `SessionHub`; game logic → `Engine/*`; network → `Services/Vpn`.
2. Keep decisions **side-effect free** (a function that takes a frame / state and returns what to do) and put the clicking /
   sending in a thin caller — that is how the self-tests reach production code.
3. Use `CdpInput` / `LoginKeys` for input, `ViewCapture` / `FollowCapture` for frames, positions relative to the canvas.
4. Add the file to the `.vbproj`. Add log lines (`BotLog.Add` / `Warn` / `Fail`). No credentials in them.
5. Add self-test checks; build Debug + Release clean; run all three self-tests; `render` the page you changed and look at it.
6. Update `CHANGELOG.md`, and this file if a rule or a module changed.

## Known limitations

- Works only with ardream.net's browser client and its current screens; a game / site update can break the pixel templates
  and the classifier (templates are generated from frames — regenerate from new frames).
- Not measured live (see `Docs/LOGIN.md`): character arrow clicks + Start Game timing, wrong-password response, server /
  character screens outside 16:10, very small views (< ~640 px), some Option-window positions; digits 4 / 5 / 9 of the
  Option window font are unknown (read as `?`, value left alone).
- Coordinates and pixel readers assume 100 % DPI for the view (device px = CSS px).
- PPTP VPN depends on the provider still accepting PPTP; proxy mode is the alternative.
- Real-frame self-test data is not in the repository.
- The UI is Turkish only.
