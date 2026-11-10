# Contributing / Katkı

**TR:** Proje MIT lisanslıdır — dilediğiniz gibi kopyalayın, değiştirin, dağıtın. **Bir rica (şart değil):** geliştirdiğiniz
bir şey varsa lütfen [github.com/IRedSquirrel](https://github.com/IRedSquirrel) altındaki proje deposuna pull request olarak
gönderin, herkes yararlansın.

**EN:** The project is MIT-licensed — copy, change and redistribute it as you like. **A friendly request (not a condition):**
if you improve it, please send the improvement back as a pull request to the project's repository under
[github.com/IRedSquirrel](https://github.com/IRedSquirrel), so everyone benefits.

## How to open a pull request

1. Fork the repository on GitHub.
2. Create a branch: `git checkout -b feature/short-name`.
3. Make the change, build, run the self-tests (below).
4. Push to your fork and open a Pull Request against the IRedSquirrel repository. Describe what changed and how you
   tested it (which self-tests, whether you tried it in the game).

## Rules

- **The build stays clean** — no new errors or warnings:
  `MSBuild RedSquirrel.WebKoMulti.sln -restore -p:Configuration=Debug -p:Platform=x64 /v:minimal`
- **Self-tests pass** (Debug build): `follow-selftest`, `login-selftest`, `vpn-selftest` must end with `SONUC: GECTI n/n`.
  If you change a reader or a decision, add a check that fails without your change.
- **New features are logged** through `Services/Logging/BotLog`: what the bot does, every decision it makes, and every
  setting the user changes ("X açıldı", "eşik 40 → 55").
- **No credentials in code, logs, frames or commits.** Passwords are never logged; an account ID only as its length.
  Never commit `Settings/`, `Sessions/`, `Logs/`, `*.dat`, or real game screenshots that show other players.
- New `.vb` files must be added to `RedSquirrel.WebKoMulti.vbproj` (explicit file list).
- Code identifiers in English; text the user sees (UI, log) in Turkish.
- Read [AGENTS.md](AGENTS.md) for the architecture and the rules learned the hard way.
