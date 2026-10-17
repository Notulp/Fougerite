# Permissions reference

There is no single permission system in Fougerite - there are **two independent ones** plus a couple of
hardcoded engine checks, and plugins are free to use whichever fits (or both, like Rust++ does). This page is a
flat reference of every permission string/flag that actually exists in the codebase today, where it's checked,
and what it functionally gates. See [`PermissionManager.md`](PermissionManager.md) and
[`RustPP.md`](RustPP.md#the-adminpermission-flag-system) for the full write-up of each system; this page only
lists the concrete strings.

## The two systems, in one sentence each

- **`Fougerite.Permissions.PermissionSystem`** - a generic, plugin-agnostic group/permission engine built into
  Fougerite itself (`Save\GroupPermissions.json` / `Save\PlayerPermissions.json`, managed in-game via
  [`PermissionManager`](PermissionManager.md)'s `/pem`/`/pemg` commands). Any plugin can define its own
  permission strings and check them with `PermissionSystem.GetPermissionSystem().PlayerHasPermission(...)`;
  there is no central registry, so the strings below are simply whatever each module happens to check.
- **`RustPP.Permissions.Administrator`** - Rust++'s own, completely separate admin/flag model (SteamID + a
  `List<string>` of flags from a fixed whitelist, persisted to `RustPP\admins.json`). It predates
  `PermissionSystem` and intentionally keeps its own roster rather than depending on it.

Rust++ actually straddles both: every admin-restricted `ChatCommand` accepts the command if the caller is a
registered `Administrator` holding the matching flag **or** holds the equivalent permission string through
`PermissionSystem` (same bare flag name, e.g. `"CanBan"`) - see `ChatCommand.CallCommand`. So granting
`"CanBan"` to a player through `/pem addperm <id> CanBan` works exactly like adding them as an `Administrator`
with the `CanBan` flag, without needing a Rust++ admin entry at all.

## Fougerite core (engine-level, not module-specific)

These are checked directly in `Fougerite.dll` itself, outside of any module:

| Permission string | Checked in | Functionally gates |
|---|---|---|
| `RCON` | `Hooks.PlayerConnect` (`Fougerite\Fougerite\Hooks.cs`) | On every connect, if the player holds `RCON` through `PermissionSystem`, Fougerite forces `netUser.admin = true` for them - i.e. holding this permission makes you a native RCON admin automatically, the same as if `rcon.login` had been used. This is also how `/setmasteradmin` (which grants the Rust++ `RCON` flag) ends up flipping native admin on reconnect. See [`AdminAccess.md`](AdminAccess.md). |
| `RCON` | `Hooks.ConsoleReceived` (`Fougerite\Fougerite\Hooks.cs`) | Decides `adminRights` for a console/chat command for logging purposes (`a.argUser.admin \|\| PlayerHasPermission(..., "RCON")`). |
| `fougerite.console` | `Util.ConsoleLog(str, adminOnly: true)` (`Fougerite\Fougerite\Util.cs`) | Lets a non-native-admin still receive admin-only console broadcast messages sent by a plugin through `Util.ConsoleLog`. |
| `bansystem.notification` | `Server.BanPlayer` (`Fougerite\Fougerite\Server.cs`) | Besides `Admin`/`Moderator`, anyone holding this permission also gets the "player X was banned by Y" chat notification when a ban isn't announced server-wide. |
| `Moderator` | `Player.Moderator` (`Fougerite\Fougerite\Player.cs`, `[Obsolete]`) | When Rust++ is loaded and the player is a registered `Administrator`, this reads the bare `Moderator` permission (Rust++ flag **or** `PermissionSystem` string, same dual check as every Rust++ command) through `GetRustPPAPI().HasPermission(UID, "Moderator")`. If Rust++ isn't loaded, or the player isn't a registered `Administrator`, it falls back to a third, older mechanism entirely: membership in the `"Moderators"` `DataStore` table (`DataStore.GetInstance().ContainsKey("Moderators", SteamID)`), which has nothing to do with either permission system and is only ever populated by code that writes to that table directly. |

Note `RCON` here is literally the same bare string as Rust++'s own `RCON` flag (see below) - granting it to a
player through `PermissionManager`'s `/pem addperm <id> RCON` affects **both** systems simultaneously: it makes
Fougerite treat them as a native admin on connect, and it satisfies every Rust++ command whose required flag is
`RCON`. The same sharing applies to `Moderator` (see `Player.Moderator` above), minus the native-admin side
effect.

Script plugins (JavaScript/Jint, Lua/MoonSharp, IronPython) also get the `PermissionSystem` singleton injected
directly into their global scope at load time (`JavaScriptPlugin.cs`/`LuaPlugin.cs`/`PythonPlugin.cs`), so a
script can call `PermissionSystem.PlayerHasPermission(player, "my.permission")` without any C#-side plumbing.

## Rust++ (`RustPP.Permissions.Administrator.PermissionsFlags`)

Defined as the fixed whitelist in `RustPP\Permissions\Administrator.cs`; an `Administrator`'s `Flags` list may
only ever contain names from this set (anything else is silently dropped by `GetProperName`/`AddFlagsToList`).
Each is wired to one or more commands in `Core.InitializeCommands()`:

| Flag | Used by | Functionally gates |
|---|---|---|
| `RCON` | `/setmasteradmin`, and as a universal override everywhere else (see below) | Full native-admin-equivalent access. `ChatCommand.CallCommand` treats `AdminFlags == "RCON"` specially: it only checks `arg.argUser.admin` (true native RCON) or the `RCON` permission - not `Administrator.IsAdmin`. It also acts as a blanket bypass: most other admin commands (`/addflag`, `/unflag`, `/ban`, `/kick`) additionally allow the action if the caller holds `RCON`, even without the command's own specific flag. |
| `CanBan` | `/ban` | Banning a player. |
| `CanUnban` | `/unban` | Removing a ban. |
| `CanKick` | `/kick` | Kicking a player. |
| `CanKill` | `/kill` | Killing a player's character. |
| `CanMute` | `/mute` | Muting a player's chat. |
| `CanUnmute` | `/unmute` | Unmuting a player (a muted admin can also unmute themselves if they hold `CanAddFlags` or `RCON`, see `MuteCommand`). |
| `CanWhiteList` | `/addwl` | Adding a player to the whitelist. |
| `CanTeleport` | `/tphere`, `/tpto` | Teleporting players. |
| `CanLoadout` | `/loadout` | Giving a predefined item loadout. |
| `CanAnnounce` | announcement command(s) | Server-wide announcements. |
| `CanSpawnItem` | `/i` | Spawning an item on the ground/in-hand. |
| `CanGiveItem` | `/give` | Giving an item directly to a player's inventory (`GiveItemCommand`). |
| `CanReload` | `/reload` | Reloading Rust++'s runtime state/config. |
| `CanSaveAll` | `/saveall` | Forcing an immediate server save (which also flushes Rust++'s own JSON state). |
| `CanAddAdmin` | `/addadmin` | Registering a new `Administrator`. |
| `CanDeleteAdmin` | `/unadmin` | Removing an `Administrator` entirely. |
| `CanGetFlags` | `/getflags` | Listing another admin's current flags. |
| `CanAddFlags` | `/addflag` | Granting additional flags to an existing admin. |
| `CanUnflag` | `/unflag` | Revoking flags from an existing admin. |
| `CanInstaKO` | `/instako` | Toggling one-hit-kill for yourself. |
| `CanInstaKOAll` | `/instakoall` | Toggling one-hit-kill server-wide. |
| `CanGodMode` | `/god` | Toggling invulnerability. |
| `CanShutdown` | `/shutdown` | Shutting the server down. |
| `Moderator` | reserved/general-purpose | A lighter-weight role flag; not tied to one specific command but available for custom gating. Read back via the (obsolete) `Player.Moderator` property, which also consults `bansystem.notification`'s check in `Server.BanPlayer` - see the `Moderator` row in the Fougerite-core table above for the full lookup chain, including its legacy `DataStore` fallback. |

`MasterAdminCommand.MasterAdminPreset` (`/setmasteradmin`) grants a fixed superset of the above in one shot:
`"CanKick|CanBan|CanUnban|CanTeleport|CanLoadout|CanAnnounce|CanSpawnItem|CanGiveItem|CanReload|CanSaveAll|
CanAddAdmin|CanDeleteAdmin|CanGetFlags|CanAddFlags|CanUnflag|CanInstaKO|CanGodMode|RCON|Moderator|CanShutdown|
CanInstaKOAll|CanMute|CanUnmute|CanWhiteList|CanInstaKOAll"` (see [`AdminAccess.md`](AdminAccess.md) for how to
actually run it). `[Settings] default_admin_flags` in `Rust++.cfg` controls what a brand-new `Administrator`
created via `new Administrator(userID, name)` starts with (no flags) rather than a full preset.

## GlitchFix

Defined as `const string` fields directly in `GlitchFix\GlitchFixModule.cs`, namespaced `glitchfix.*` (the
pattern worth following for any new module-local permission: `<module>.<feature>`, lowercase, checked only
through `PermissionSystem` since GlitchFix has no permission system of its own):

| Permission string | Config toggle | Functionally gates |
|---|---|---|
| `glitchfix.bypass` | `[Settings] PermissionBypass` | When `PermissionBypass` is enabled, a player holding this permission is exempt from every anti-glitch check (`HasBypass`), the same as `AdminBypass` exempting native admins. |
| `glitchfix.reload` | always checked | Lets a non-admin run `/glitchfix reload` (and see the `/glitchfix` info/version message) without being a native admin. |

## PermissionManager

A single gatekeeper permission, `pem.admin`, checked throughout `PermissionManager\PermissionManager.cs`:

| Permission string | Functionally gates |
|---|---|
| `pem.admin` | Required (unless you're a native admin) to use **any** `/pem`/`/pemg` sub-command at all - it's the permission that lets you manage everyone else's permissions. Several destructive sub-commands (`/pem delperm`, `/pem delplayer`) additionally re-check `pem.admin` on the **target**, so a non-RCON `pem.admin` holder can't delete/demote another `pem.admin` holder - only a native RCON admin can. |

Since `pem.admin` is itself just a `PermissionSystem` permission string, the very first grant has to come from
either native admin or manually editing `Save\PlayerPermissions.json`/`GroupPermissions.json` (or being in the
`Default` group if you've added `pem.admin` to it) - see [`AdminAccess.md`](AdminAccess.md) for the general
bootstrapping problem and [`PermissionManager.md`](PermissionManager.md#gatekeeping-pemadmin) for the detail.

## Adding your own

If you're writing a new module and don't need a full standalone admin roster like Rust++'s, the GlitchFix
pattern is the one to copy: a `private const string FeaturePermission = "mymodule.feature";` checked via
`PermissionSystem.GetPermissionSystem().PlayerHasPermission(player, FeaturePermission)`, usually `||`-ed with a
native-admin bypass (`player.Admin`). There's no registry to update - the permission simply exists the moment
your code checks for it, and server owners grant it with `/pem addperm <id> mymodule.feature`.
