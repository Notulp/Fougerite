# PermissionManager

**PermissionManager** (`PermissionManager\PermissionManager.cs`) is a thin chat-command front-end for
`Fougerite.Permissions.PermissionSystem` - the core group/permission engine built into Fougerite itself (see
`Fougerite\Fougerite\Permissions\PermissionSystem.cs`). The module itself holds almost no state; every command
just forwards to the already-running `PermissionSystem` singleton (`PermissionSystem.GetPermissionSystem()`).

This is a different, independent system from [Rust++'s own admin/flag model](RustPP.md#the-adminpermission-flag-system) -
plugins can use either (or both): some check `PermissionSystem.PlayerHasPermission`, others check RustPP's
`Administrator`/`Can*` flags, and some (like [`GlitchFix`](GlitchFix.md)) check Fougerite's native
`Player.Admin` directly.

## Storage

`PermissionSystem` persists to two JSON files under the server's `Save\` folder, created with sample content the
first time the server starts if they don't already exist:

- `Save\GroupPermissions.json` - the list of `PermissionGroup` (name, nickname, permission list).
- `Save\PlayerPermissions.json` - the list of `PermissionPlayer` (SteamID64, permission list, group membership).

`PermissionSystem.SaveToDisk()` writes these back out (with a backup-and-restore fallback if serialization
fails mid-write), and `ReloadPermissions()` reloads them from disk - both of which `/pem reload` triggers.

## Gatekeeping (`pem.admin`)

Both top-level commands require either native admin or the `pem.admin` permission:

```csharp
if (!player.Admin && !_permissionSystem.PlayerHasPermission(player, "pem.admin")) return;
```

So the very first thing you need before using PermissionManager at all is either
[RCON/native admin access](AdminAccess.md), or someone who already has it granting you `pem.admin` through
`/pem addperm <you> pem.admin`. Several sub-commands additionally re-check `pem.admin` on the **target** player
(e.g. you can't `/pem delperm` or `/pem delplayer` someone who themselves holds `pem.admin` unless you're a
native RCON admin) - this stops a non-RCON `pem.admin` holder from deleting/demoting another `pem.admin` holder.

## Player commands - `/pem`

Run `/pem` with no arguments in-game for the live list; summarized here:

| Sub-command | Usage | Effect |
|---|---|---|
| `reload` | `/pem reload` | Reloads both JSON files from disk. |
| `newplayer` | `/pem newplayer <name>` | Creates a `PermissionPlayer` for an **online** player by name. |
| `newoffplayer` | `/pem newoffplayer <steamid>` | Creates a `PermissionPlayer` by SteamID64 (works offline). |
| `delplayer` | `/pem delplayer <name>` | Removes an online player's `PermissionPlayer` entry. |
| `delofflplayer` | `/pem delofflplayer <steamid>` | Removes a `PermissionPlayer` entry by SteamID64. |
| `addperm` | `/pem addperm <steamid\|name> <permission>` | Grants a single permission string to a player. |
| `delperm` | `/pem delperm <steamid\|name> <permission>` | Revokes a single permission string from a player. |
| `listperms` | `/pem listperms <steamid\|name>` | Lists a player's directly-held permissions. |
| `addtogroup` | `/pem addtogroup <steamid\|name> <group>` | Adds the player to a permission group. |
| `delfromgroup` | `/pem delfromgroup <steamid\|name> <group>` | Removes the player from a permission group. |

A `PermissionPlayer` must exist (via `newplayer`/`newoffplayer`) before any of `addperm`/`delperm`/`listperms`/
`addtogroup`/`delfromgroup` will work on them.

## Group commands - `/pemg`

| Sub-command | Usage | Effect |
|---|---|---|
| `createg` | `/pemg createg <group>` | Creates a new permission group (spaces become `_`). |
| `delg` | `/pemg delg <group>` | Deletes a group (fails on the default group). |
| `listpermsg` | `/pemg listpermsg <group>` | Lists a group's permissions. |
| `addpermg` | `/pemg addpermg <group> <permission>` | Grants a permission to every member of the group. |
| `delpermg` | `/pemg delpermg <group> <permission>` | Revokes a permission from the group. |
| `changenickg` | `/pemg changenickg <group> <nickname>` | Changes the group's display nickname. |
| `changenameg` | `/pemg changenameg <group> <newname>` | Renames the group itself (spaces become `_`). |
| `membersg` | `/pemg membersg <group>` | Lists member SteamID64s of a group. |

## Permission string format

`PermissionSystem.Matches` supports wildcard suffixes - a permission ending in `*` matches anything with that
prefix (e.g. `myplugin.*` grants every `myplugin.xxx` permission), which is how `PlayerHasPermission` resolves
whether a specific requested permission string is covered by a broader grant on the player or one of their
groups.
