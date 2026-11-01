### Guide
Quick Start - getting a Fougerite server running from scratch

### Description
This page is for server owners, not plugin developers: a short, linear checklist to get a working
Fougerite server up, reachable by players, and administrable, as fast as possible. Every step links to the
full reference page for extra detail - this page intentionally keeps each step to a couple of lines so you
don't have to read the entire `Docs/` folder before starting a server.

If you're here to write plugins instead, start at [`Docs/README.md`](README.md).

### 1. Get a premade server

Rather than hand-assembling a clean Rust Legacy dedicated server yourself, grab a ready-to-run copy:

- [OriginalRustServerFilesFougerite1.9.971_Premade.zip](https://github.com/Notulp/Fougerite/releases/download/v1.9.971/OriginalRustServerFilesFougerite1.9.971_Premade.zip)

Extract it anywhere on your machine - this folder (the one containing `rust_server.exe`) is your **server
root** for every step below.

### 2. Update it to the latest Fougerite with `AutoUpdate-Fougerite.ps1`

The premade download may not be on the latest Fougerite version, so update it right away:

1. Download [`AutoUpdate-Fougerite.ps1`](../AutoUpdate-Fougerite.ps1) from this repository and place it
   directly in the server root (next to `rust_server.exe`).
2. Run it (double-click, or `powershell -File .\AutoUpdate-Fougerite.ps1` from a terminal opened in that
   folder) and follow the prompts.
3. When asked how to handle `Save\*.cfg`/`*.ini` files, choose **`[P]` Prompt me** (recommended) - it shows
   you a line-by-line diff of anything that would change before overwriting, instead of silently resetting
   `Fougerite.cfg` to defaults.

Full details and what each prompt means: [`Scripts.md`](Scripts.md). Run this same script again whenever a
new Fougerite release comes out.

### 3. Open your server's ports

Forward/allow these ports on your router and firewall before anyone can connect:

| Port | Protocol | Purpose |
|---|---|---|
| `28015` | UDP | Main game port - Rust Legacy's default, used by every client to actually connect and play. |
| `28016` | UDP | Query + RCON port - server browser queries and remote console (`rcon.login`, see step 4). |
| `28017` | UDP | RustBuster client-side integration port - only needed if you want players using the [RustBuster](../README.md#server-browser-visibility-fougerite-vs-rustbuster) client to get its extra features. |

If you're not using RustBuster at all, `28017` can be left closed - everything else still works over
`28015`/`28016`.

### 4. Set your RCON password and become admin

1. Open `cfg\server.cfg` in the server root and add:
   ```
   rcon.password YourStrongPasswordHere
   ```
2. Start the server once (this is also what generates `Save\Fougerite.cfg`, needed for step 5).
3. Connect with the game client, open the console (`F1`), and log in:
   ```
   rcon.login YourStrongPasswordHere
   ```
4. In chat, run `/setmasteradmin` to make yourself a full Rust++ admin, then `/saveall` to persist it to
   disk immediately.

Full walkthrough and troubleshooting: [`AdminAccess.md`](AdminAccess.md).

### 5. Fix `Fougerite.cfg` so the right players can actually join

This is the single most common reason a server looks "broken" to new players: `Fougerite.cfg` (in
`Save\Fougerite.cfg`) ships with settings that may be too strict (or too loose) for what you want. Open it
and check these two keys under `[Fougerite]`:

- **`SteamAuthMode`** - decides who is allowed to connect. The default (`Legacy`) performs no ticket
  verification on its own. If you want to also allow Spacewar/RustBuster clients while still keeping out
  obvious fakes/alts, `SteamAccounts` is the most commonly recommended value.
- **`SteamWebAPIKey`** - required by `RustOwners`/`SteamPaidAccounts`/`SteamAccounts`. Get a free key from
  <https://steamcommunity.com/dev/apikey> and paste it here, otherwise those modes will fail to verify
  anyone and can lock legitimate players out.

Full list of every `SteamAuthMode` value (with a safety-level table) and every other `Fougerite.cfg` key:
[`FougeriteCfg.md`](FougeriteCfg.md#fougerite-section).

> [!IMPORTANT]
> If you ever update and choose to overwrite `Save\Fougerite.cfg` back to defaults, re-check these two
> settings again afterward - `AutoUpdate-Fougerite.ps1` will remind you if this happens.

### 6. Install your first plugin

Once the server runs and you can log in as admin, you can start adding plugins (C#, CSScript, Python,
JavaScript or Lua). Where to drop each type of file and how to enable it in `Fougerite.cfg` is fully covered
in [`Installation.md`](Installation.md) - follow that guide to install, enable and reload your first plugin.

### Related documentation

- [`Scripts.md`](Scripts.md) - everything `AutoUpdate-Fougerite.ps1`/`Collect-Logs.ps1` do.
- [`AdminAccess.md`](AdminAccess.md) - RCON login, `/setmasteradmin`, persistence.
- [`FougeriteCfg.md`](FougeriteCfg.md) - full `Fougerite.cfg` reference.
- [`Installation.md`](Installation.md) - installing C#/CSScript/Python/JS/Lua plugins.
- [`README.md`](README.md) - full documentation index, mainly aimed at plugin developers.
