# Rust++ (RustPP)

**Rust++** (`RustPP\`, bundled module name `RustPP`) is Fougerite's own permission/moderation plugin, originally
created by EquiFox & xEnt (see the README footer) and still shipped by default. It provides a self-contained
admin system (independent from [`PermissionManager`](PermissionManager.md)/`Fougerite.Permissions`), chat
commands for moderation, and small quality-of-life features (MOTD, friends, door sharing, player cache).

It's a normal `Fougerite.Module` (see [`Classes/Modules.md`](Classes/Modules.md)) - `RustPPModule.cs` is the
module entry point that wires Fougerite hooks to `Core`/`ChatCommand` dispatch, while `Core.cs` owns the shared
static state (config, lists, registered commands) and `Helper.cs` handles save/load.

## Enabling / configuring

Everything is controlled from `RustPP\Configs\Rust++.cfg` (an `IniParser` file, see
[`Classes/Config.md`](Classes/Config.md)), deployed next to the module:

- `[Settings] rust++_enabled` - master on/off switch, checked by `Core.IsEnabled()`.
- `[Settings] motd` / `motd1`, `motd2`, ... - optional join message-of-the-day lines (`Core.motd`).
- `[Settings] default_admin_flags` - the flag string a brand-new `Administrator` gets when created without an
  explicit preset (see `Administrator(ulong, string)`).
- `[Commands]` section - each command below is individually toggled here; `ChatCommand.Enabled` reads
  `isCommandOn(name)` before ever dispatching the command.

## The admin/permission flag system

RustPP has its own lightweight flag-based admin model, separate from the full group/permission system in
[`PermissionManager.md`](PermissionManager.md):

- `RustPP.Permissions.Administrator` wraps a SteamID64 + display name + a `List<string>` of flags, picked from
  the fixed `Administrator.PermissionsFlags` whitelist (`CanKick`, `CanBan`, `CanTeleport`, `RCON`, ...).
  `Administrator.AdminList` is the full in-memory roster, checked via `IsAdmin`/`GetAdmin`.
- Every admin-restricted `ChatCommand` sets its own `AdminFlags` string (e.g. `BanCommand.AdminFlags = "CanBan"`)
  in `Core.InitializeCommands()`. `ChatCommand.CallCommand` then gates execution
  (`RustPP\Commands\ChatCommand.cs`):
  - If `AdminFlags == "RCON"`, the command only runs for a player whose connection is authenticated as a native
    RCON/console admin (`arg.argUser.admin`) **or** who holds the `RCON` flag - see
    [`AdminAccess.md`](AdminAccess.md) for how a player actually gets `argUser.admin == true`.
  - Otherwise it runs if the player is a registered `Administrator` with that specific flag (or holds the
    equivalent permission through [`Fougerite.Permissions.PermissionSystem`](PermissionManager.md), which
    `PlayerHasPermission` also consults).
  - Anyone else gets a rejection message instead of silently no-oping.

## Commands

All commands are chat commands (type them in the in-game chat), registered in `Core.InitializeCommands()` and
individually toggleable under `[Commands]` in `Rust++.cfg`. Non-exhaustive highlights (see `RustPP\Commands\`
for the full set and exact required flag):

| Command | Required flag | Purpose |
|---|---|---|
| `/setmasteradmin` | `RCON` | Grants the calling player **every** admin flag, see [`AdminAccess.md`](AdminAccess.md). |
| `/addadmin` | `CanAddAdmin` | Registers a player as an `Administrator` with a given flag set. |
| `/unadmin` | `CanDeleteAdmin` | Removes a player's `Administrator` entry entirely. |
| `/addflag` / `/unflag` | `CanAddFlags` / `CanUnflag` | Grants/revokes individual flags on an existing admin. |
| `/getflags` | `CanGetFlags` | Lists an admin's current flags. |
| `/ban`, `/unban`, `/kick`, `/kill`, `/mute`, `/unmute` | matching `Can*` flag | Standard moderation actions. |
| `/god`, `/instako`, `/instakoall`, `/give`, `/i`, `/loadout`, `/tphere`, `/tpto` | matching `Can*` flag | Admin utility/cheat commands. |
| `/saveall` | `CanSaveAll` | Forces an immediate server save (`World.ServerSaveHandler.ManualBackGroundSave()`), which in turn flushes RustPP's own JSON state through `Helper.CreateSaves()` - see persistence below. |
| `/reload` | `CanReload` | Reloads RustPP's runtime state. |
| `/shutdown` | `CanShutdown` | Shuts the server down. |
| `/about`, `/help`, `/motd`, `/players`, `/ping`, `/rules` | none | Public, unrestricted informational commands. |
| `/addfriend`, `/unfriend`, `/friends` | none | Player friends list (used by the share/anti-raid features). |
| `/share`, `/unshare`, `/sharelist` | none | Door sharing between players/friends. |
| `/pm`, `/r` | none | Private messages. |
| `/addwl` | `CanWhiteList` | Adds a player to the whitelist. |

The `MasterAdminCommand` preset (`/setmasteradmin`) is worth calling out explicitly since it's the easiest way
to bootstrap your first admin on a fresh server - see [`AdminAccess.md`](AdminAccess.md) for the full walkthrough
including how to actually authenticate as RCON in the first place.

## Persistence

RustPP keeps its state as plain JSON files next to the module (`RustPP\` module folder at runtime), written by
`Helper.SaveJson`/loaded by `Helper.LoadWithMigration` (which also auto-migrates older `.xml`/`.rpp` save
formats into JSON the first time it successfully loads them, then deletes the legacy files):

- `admins.json` - `Administrator.AdminList` (the whole admin roster + flags).
- `doorsSave.json` - shared doors (`ShareCommand`).
- `friendsSave.json` - friends lists (`FriendsCommand`).
- `userCache.json` - SteamID → last known name cache.
- `whitelist.json` / `mutelist.json` / `bans.json` - the three `PList`-based lists.

These are **not** written immediately on every change (e.g. `/setmasteradmin` only updates the in-memory
`Administrator.AdminList`) - `Helper.CreateSaves()` is what actually serializes everything to disk, and it's
only called from:

- `RustPPModule.OnServerSaved` - whenever the server performs its own native save (periodic autosave or a
  manual `save`/`/saveall`).
- `RustPP.TimedEvents` - a periodic internal timer independent of the server's save interval.
- The legacy `Fougerite.RustPPExtension.CreateSaves()` path.

In other words: any change made through RustPP commands will eventually persist on its own, but if you need it
saved **immediately** (e.g. right after granting yourself master admin), trigger a save explicitly with
`/saveall` or the server's native `save` console command rather than assuming it's already on disk.
