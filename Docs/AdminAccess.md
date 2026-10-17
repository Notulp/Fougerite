# Getting Admin Access (RCON login + `/setmasteradmin`)

Every admin-only feature in Fougerite ultimately traces back to a single native flag: `argUser.admin` /
`Fougerite.Player.Admin`, which the server sets **per-connection** when a client authenticates over Rust
Legacy's built-in RCON protocol. This page explains how to get that flag from a cold start (no admins
registered yet), and how to turn it into a permanent [Rust++](RustPP.md) admin account.

## 1. Set (or find) your RCON password

The server authenticates RCON logins against the `rcon.password` convar, set in the server root's
`cfg/server.cfg` (or passed on the command line). If you don't already have one:

```
rcon.password YourStrongPasswordHere
```

Add that line to `server.cfg` and restart the server (or set it live from the server console, which takes
effect immediately for new logins).

## 2. Log in from the client console

In the Rust Legacy client, open the developer console (`F1` by default) while connected to your server, and run:

```
rcon.login YourStrongPasswordHere
```

If the password matches, the server marks that client's connection as an authenticated admin for the rest of
the session - this is exactly the `argUser.admin` flag that `ChatCommand.CallCommand` checks for any command
whose `AdminFlags` is `"RCON"` (see [`RustPP.md`](RustPP.md#the-adminpermission-flag-system)), and that
`Fougerite.Player.Admin` exposes to every plugin (e.g. `GlitchFix`'s `AdminBypass`, `PermissionManager`'s
`pem.admin` checks).

> [!NOTE]
> `rcon.login` only authenticates the **current connection**. It does not persist anything by itself and does
> not make you a registered admin in any plugin's database - it just flips the native `admin` flag on your
> session, which is the gate RustPP's `RCON`-flagged commands (like `/setmasteradmin`) check.

## 3. Grant yourself a permanent admin account

While authenticated via `rcon.login`, use Rust++'s bootstrap command in the in-game chat:

```
/setmasteradmin
```

`MasterAdminCommand` (`RustPP\Commands\MasterAdminCommand.cs`) registers you as an `Administrator` with every
available flag (`CanKick|CanBan|CanUnban|CanTeleport|... |RCON`, see `MasterAdminPreset`) - i.e. a full Rust++
admin, not just a one-session RCON login. From this point on you no longer need to `rcon.login` every time:
RustPP's own `RCON`-gated commands will also accept you because you now hold the `RCON` flag directly (see
`ChatCommand.CallCommand`'s `haspermission || Administrator.IsAdmin(...)` check), on top of the native
`argUser.admin` flag still working for anything server-side that checks it directly (e.g. `Player.Admin`).

If a second account somehow runs `/setmasteradmin` again, RustPP won't downgrade or duplicate anything - it
just replies that you're already an administrator (`Administrator.IsAdmin` check in `MasterAdminCommand`).

## 4. Make sure it's actually saved to disk

`/setmasteradmin` only updates the **in-memory** `Administrator.AdminList` - see
[`RustPP.md#persistence`](RustPP.md#persistence) for the full breakdown. It gets written to `admins.json`
automatically the next time the server autosaves or RustPP's periodic save timer runs, but if you want it
persisted **immediately** (e.g. right before restarting the server), force a save explicitly:

```
/saveall
```

(You already have `CanSaveAll` from the master admin preset.) Alternatively, the server's own native `save`
console command also triggers `RustPPModule.OnServerSaved`, which flushes RustPP's JSON state the same way.
Either way, confirm `admins.json` next to the RustPP module folder contains your SteamID64 before relying on it
surviving a restart.

## Permission/flag systems this unlocks

Becoming an admin this way is enough for:

- Every `RCON`/`Can*`-flagged [Rust++ command](RustPP.md#commands).
- Any plugin that checks `Fougerite.Player.Admin` directly, e.g. [`GlitchFix`](GlitchFix.md)'s `AdminBypass`
  setting, or [`PermissionManager`](PermissionManager.md)'s `pem.admin`-gated `/pem`/`/pemg` commands (which
  accept either `player.Admin` or the dedicated `pem.admin` permission).

It does **not** automatically register you in `Fougerite.Permissions.PermissionSystem` (the group/permission
system `PermissionManager` manages) - that's a fully separate system with its own storage. If a plugin checks
permissions through `PermissionSystem` specifically instead of `Player.Admin`, see
[`PermissionManager.md`](PermissionManager.md) for how to grant yourself access there too (being a native RCON
admin is enough to bypass `PermissionManager`'s own `pem.admin` requirement when running its commands).
