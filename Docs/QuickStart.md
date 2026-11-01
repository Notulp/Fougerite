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

The latest premade server already ships with `AutoUpdate-Fougerite.ps1` in the server root, so just run it
to make sure you're on the newest Fougerite version:

1. If you're using the premade download above, `AutoUpdate-Fougerite.ps1` is already in the server root -
   skip straight to step 2. If you started from the **original Rust Legacy server files** instead (no
   script present), download [`AutoUpdate-Fougerite.ps1`](../AutoUpdate-Fougerite.ps1) from this repository
   and place it directly in the server root (next to `rust_server.exe`).
2. Run it - either double-click it, right-click it and choose **"Run with PowerShell"**, or run
   `powershell -File .\AutoUpdate-Fougerite.ps1` from a terminal opened in that folder - and follow the
   prompts.
3. When asked how to handle `Save\*.cfg`/`*.ini` files, choose **`[P]` Prompt me** (recommended) - it shows
   you a line-by-line diff of anything that would change before overwriting, instead of silently resetting
   `Fougerite.cfg` - or any plugin's own default config/ini file under `Save\` - back to defaults.

Full details and what each prompt means: [`Scripts.md`](Scripts.md). Run this same script again whenever a
new Fougerite release comes out.

### 3. Open your server's ports

Forward/allow these ports on your router and firewall before anyone can connect:

| Port | Protocol | Purpose |
|---|---|---|
| game port | UDP | Main game port - used by every client to actually connect and play. Passed to `rust_server.exe` as `-port <number>` in the `.bat` file (the shipped example `.bat` uses `29015`, **not** `28015` - check your own `.bat` file for the real value). |
| game port + 1 | TCP + UDP | Query (UDP, server browser queries) + RCON (TCP, remote console - `rcon.login`, see step 4). Both protocols need to be open. This offset is the common convention but can be overridden independently in `server.cfg`/the `.bat` file, so double-check yours. |
| game port + 2 | TCP | RustBuster client-side integration port - only needed if you want players using the [RustBuster](../README.md#server-browser-visibility-fougerite-vs-rustbuster) client to get its extra features. Unlike the query/RCON port, this one is **always** the game port + 2, it isn't separately configurable. |

For example, if your `.bat` file uses `-port 28015` (Rust Legacy's traditional default), that's `28016` for
query/RCON and `28017` for RustBuster. If it uses `29015` (the port in the shipped example `.bat`), that's
`29016` and `29017` instead - open whichever ports match **your actual** game port, not these examples.

If you're not using RustBuster at all, its port (game port + 2) can be left closed - everything else still
works over the game port and the query/RCON port.

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

### 5. Adjust `Fougerite.cfg` so the right players can actually join

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
