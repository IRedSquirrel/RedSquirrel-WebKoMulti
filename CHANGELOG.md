# Changelog

## 1.0.0 — 2026-11-10

First public release.

- Multi-account browser client for Knight Online on ardream.net, one WebView2 profile per account.
- Automatic login: launcher START → ID / password (typed text verified in the box) → server → character → in game.
- Manual login mode (the browser opens, you log in yourself).
- Follow the leader by reading the on-screen coordinates; adaptive pace; canvas located inside larger views.
- Input mirroring from the leader: keyboard only (default) or keyboard + mouse.
- Per-account Windows PPTP VPN or SOCKS5 / HTTP CONNECT proxy through a local SOCKS5 bridge, no direct fallback.
- After entering the game: close pop-up windows, mute, close PMs; one-time game options per account.
- Browser data reset for all profiles.
- Local log of actions, decisions and setting changes; accounts and VPN settings encrypted with DPAPI.
- Clear message (with a link to the download page) when the Microsoft Edge WebView2 Runtime is missing.
- "Kes" while the VPN / proxy is still connecting cancels the attempt cleanly (state: off, nothing left open).
- App icon on the main, Oyunlar and Lider windows; the Kayıt tab's "Giriş" filter shows the login engine's lines.
- Offline self-tests (Debug): `follow-selftest`, `login-selftest`, `vpn-selftest`; off-screen UI `render`.
