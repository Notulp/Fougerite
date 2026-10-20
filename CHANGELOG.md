# Changelog

This file collects the release notes that are normally posted on Discord and on the
[GitHub releases page](https://github.com/Notulp/Fougerite/releases). Newest version first.

## v1.9.971

Full Changelog: https://github.com/Notulp/Fougerite/compare/v1.9.97...v1.9.971

A small follow-up release on top of 1.9.97: one gameplay bug fix, a big `IniParser` performance fix, and a
large documentation catch-up. No new hooks or config settings, so updating is drop-in.

- Fixed `GlitchFix` blocking foundations (and pillars/ramps) placed next to a doorway that already has a door
  installed on it, which made it impossible to expand a base and silently refunded/destroyed the piece with
  "You cannot place structures on deployables here (MetalDoor(Clone))".
  `AntiFoundationHide`/`AntiPillarStash`/`AntiRampObject` treat any nearby `DeployableObject` as loot being
  hidden under the new structure, but a `Door` is a `DeployableObject` too and is a completely normal part of
  a base, so `SphereContains` now skips deployables whose name contains "door" instead of reporting them. As
  a side effect the check also got more accurate: it used to stop at the first deployable it found, so a real
  woodbox/stash hidden behind a door was never even reached.
- Fixed the same bug in `GlitchFix`'s older `structurecheck` from the other direction: its door-name match
  applied to Pillar/Foundation/Ramp as well, even though only `SmallStash` placement is actually supposed to
  care about a nearby door (to catch a stash disguised behind one). It now only looks for a nearby door when a
  `SmallStash` is being placed. The woodbox/stash detection of every other type is unchanged.
- Made `IniParser` saving fast. Writing a config used to build the whole file in a single string with `+=`
  per line while rescanning the key list once per section, so saving got quadratically slower as a config
  grew (a ~0.6 MB ini took over 1.5 seconds). `SaveSettings` now groups the keys by section in one pass and
  streams the result through a 64 KB buffered writer, and `IniParser.SectionPair` finally has real
  `Equals`/`GetHashCode` instead of the reflection-based `ValueType` default that hashed the section name
  only and put every key of a section in the same hash bucket. The same 0.6 MB config now saves in ~7 ms
  (over 200x faster), with byte-identical output. Saving is also atomic now (written to a `.tmp` file and
  swapped in), so a crash or a full disk mid-save can no longer leave behind a half-written config.
- Because of that, `Config.Save()`/`IniParser.Save()` no longer spawns a background thread for files bigger
  than 0.6 MB. That old workaround wrote the file from an unsynchronized thread nobody ever joined, which
  could race with the next save or with the server shutting down. Saving is plain synchronous again, and now
  faster than the thread hand-off itself used to cost.
- Added a bunch of documentation that was missing: [`Docs/Patcher.md`](Docs/Patcher.md) explains what
  `Fougerite.Patcher` actually does, why patching always has to start from the clean/deobfuscated DLLs in
  `Fougerite\References\CleanPatchTargetDlls\`, and the skeleton-stub + `OpenField`/`OpenMethod`
  "patch-to-public" workflow used whenever a new hook needs to touch a private member on a game type (either
  because of the `Fougerite.dll`/`Assembly-CSharp.dll` circular build dependency, because the behavior itself
  needs changing, or simply because of Rust Legacy's own legacy code quality).
  [`Docs/AdminAccess.md`](Docs/AdminAccess.md) walks through getting your first admin on a fresh server:
  setting `rcon.password`, logging in with `rcon.login` from the client console, bootstrapping yourself with
  Rust++'s `/setmasteradmin`, and making sure it's actually persisted to disk (`/saveall`) instead of only
  living in memory until the next autosave. The three bundled modules that were previously undocumented now
  have their own pages too: [`Docs/RustPP.md`](Docs/RustPP.md) (the Rust++ plugin's own admin/flag system, full
  command reference, and JSON persistence), [`Docs/PermissionManager.md`](Docs/PermissionManager.md) (the
  `/pem`/`/pemg` commands for `Fougerite.Permissions.PermissionSystem`), and
  [`Docs/GlitchFix.md`](Docs/GlitchFix.md) (every `GlitchFix.cfg` setting and what each anti-glitch check does).
- Closed the remaining gaps in the hooks reference: added
  [`Docs/Hooks/Server/On_PluginShutdown.md`](Docs/Hooks/Server/On_PluginShutdown.md) (fired on a script plugin
  right before its hooks are torn down on unload/reload) and
  [`Docs/Hooks/Server/On_CustomMapLoaded.md`](Docs/Hooks/Server/On_CustomMapLoaded.md) (fired once the level +
  save finish loading, stock map included), and documented the obsolete `On_Console`/`OnConsoleReceived` alias
  inside `On_ConsoleWithCancel.md`. Every event in `PluginLoaderEvents` now has a matching page.
- Finished the 1.9.97 freeze-watchdog documentation that didn't make it into the release itself:
  [`Docs/Hooks/Server/On_ServerFreeze.md`](Docs/Hooks/Server/On_ServerFreeze.md) with examples for every
  engine, and the expanded `freeze.*` convar section in the README (defaults, warnings and recommended
  values).
- Added [`Docs/Permissions.md`](Docs/Permissions.md): a flat reference of every permission string/flag that
  actually exists in the codebase and what it functionally gates - Fougerite core's `RCON`,
  `fougerite.console` and `bansystem.notification` checks, Rust++'s full `Can*`/`RCON`/`Moderator` flag
  whitelist and which command each one maps to, GlitchFix's `glitchfix.bypass`/`glitchfix.reload`, and
  PermissionManager's `pem.admin` gatekeeper, including how granting the bare `RCON` string through
  `PermissionManager` affects both Fougerite core and every Rust++ command at once.
- Expanded `Docs/Permissions.md` after a more thorough scan of every `PlayerHasPermission` call site: documented
  the (obsolete) `Player.Moderator` property, which checks the bare `Moderator` permission through Rust++
  (flag or `PermissionSystem`, same dual check as every Rust++ command) when Rust++ is loaded, and otherwise
  falls back to a third, older mechanism - membership in the `"Moderators"` `DataStore` table - that isn't part
  of either permission system. Also noted that script plugins (JS/Lua/Python) get `PermissionSystem` injected
  directly into their global scope, so they can call `PlayerHasPermission` without any C#-side glue.
- Added [`Docs/Examples/`](Docs/Examples/README.md): ten worked examples, each reimplementing the core
  mechanic of a real community plugin (originally written against Fougerite/Magma, see
  [dretax/Python-Plugins](https://github.com/dretax/Python-Plugins)) in C#, JavaScript, Python and Lua -
  `BambeeProtector` (new-player kill protection), `Warps` (named teleports), `LevelSystem` (gathering XP/level
  progression), `Kits` (config-driven loadouts), `iConomy` (a player-balance economy), `Clans` (role-based
  clans with opt-in friendly fire), `DestroySystem` (hit-to-destroy your own structures with a raid-cooldown
  lock), `Recorder` (recording/respawning a structure as a blueprint), `TDM` (a round-based arena mode, plus a
  write-up of how to finish its never-completed team split with a locked `Zone3D` arena and a RustBuster GUI
  lobby), and `HungerGames` (a battle-royale shrinking safe zone). Linked from `Docs/README.md`.
- Fixed a real bug found while reviewing the above: `Docs/CSScriptPluginTutorial.md` used
  `((dynamic) plugin).Engine` to read a loaded C# plugin's `Module`. The `dynamic` keyword needs
  `Microsoft.CSharp.dll`'s runtime binder, which isn't part of .NET 3.5 (added in .NET 4.0) and isn't
  referenced anywhere in this solution, and the project's old Mono (Mini JIT) build predates DLR/dynamic
  support entirely - so that line would never actually work. Replaced it with a plain `as`-cast against
  `CSPlugin`/`CSScriptPlugin` (both expose the same `public Module Engine;` field), no reflection needed.
- Corrected several inaccurate API calls across `Docs/Examples/*.md` found during the same pass:
  `HurtEvent`/`DeathEvent.Attacker`/`.Victim` are typed `object` in C#, so every example that read
  `he.Attacker.UID`/`.MessageFrom(...)` directly without first checking `AttackerIsPlayer`/`VictimIsPlayer`
  and casting (`BambeeProtector`, `Clans`, `DestroySystem`, `iConomy`, `TDM`) would not have compiled;
  `DataStore.Contains(...)` doesn't exist (it's `ContainsKey`, fixed in `DestroySystem` across all four
  languages); and `Util.CreateVector(...)` doesn't exist - replaced with `new UnityEngine.Vector3(...)` (C#/
  JS), `UnityEngine.Vector3(...)` (Python) and `UnityEngine.Vector3.__new(...)` (Lua), and the fabricated
  `Vector3.Add`/`.Subtract` methods in `Recorder`'s JavaScript/Lua samples were replaced with explicit
  component-wise math.

## v1.9.97

Full Changelog: https://github.com/Notulp/Fougerite/compare/v1.9.96...v1.9.97

- Added a native server freeze watchdog (`FreezeMonitor`). A background thread keeps an eye on a heartbeat
  that the managed side refreshes every frame; if it stops advancing for `freeze.timeout` seconds the server
  is considered frozen and the watchdog starts writing a readable report (native + managed call stacks of the
  main thread) to a `FreezeDumps` folder next to the server executable, optionally follows up with a minidump
  (`freeze.dumpafter`, capped by `freeze.maxdumps`, full memory via `freeze.fulldump`), and can force-kill the
  process after `freeze.killafter` seconds so an external watchdog/orchestrator script can restart it instead
  of it hanging forever. Recovery is reported too, with the total freeze duration. All of this diagnostics
  work runs on its own thread so it never adds load to, or further blocks, the already-frozen main thread.
  See the [FreezeMonitor section](README.md#freezemonitor-watchdog) of the README for the full breakdown and
  the new `freeze.*` settings.
- Added `Hooks.OnServerFreeze` (`On_ServerFreeze` for script plugins), fired for every plugin engine (C#,
  CSScript, Python, JavaScript, Lua) whenever the watchdog detects a freeze, writes a dump, or the server
  recovers. Useful for posting a Discord/RCON alert automatically instead of only noticing a frozen server
  after someone complains. Keep handlers fast and exception-safe: they run on a separate diagnostics thread
  while the main thread may genuinely be frozen. See
  [`Docs/Hooks/Server/On_ServerFreeze.md`](Docs/Hooks/Server/On_ServerFreeze.md) for the full signature and
  examples in every engine.
- Documented and cleaned up `Hooks_Base.cs` and `Fougerite.Tools.FreezeContext` with proper XML summaries, and
  moved the native freeze callback handling (`OnNativeFreeze`) into `Hooks_Base.cs` next to the rest of the
  hook plumbing it belongs with.

## v1.9.96

Full Changelog: https://github.com/Notulp/Fougerite/compare/v1.9.95...v1.9.96

- Added a full C# Script (`CSScript`) plugin engine. It compiles plain `.cs` source files at startup/reload
  instead of requiring a prebuilt `.dll` - drop `Modules\Name\Name.cs` (plus any other `.cs` file in that
  folder, they're all compiled together) and it just works. New `Fougerite.cfg` settings `EnableCSScript` and
  `CSScriptCompiler` (leave empty to auto-detect: MSBuild Roslyn first, then the .NET Framework `csc.exe`,
  then Mono `mcs.exe`). Use `// #require OtherPlugin` in a source file to compile/load-order against another
  CSScript plugin or DLL module, and drop extra `.dll` references in `Modules\Name\References\`. Hot-reloads
  on save through the same file watcher as the script plugins. See `Docs/CSScriptPluginTutorial.md` for a full
  walkthrough and example plugin.
- Added `Hooks.OnPluginLoaded` and `Hooks.OnPluginUnloaded`. They fire for every plugin type (C# modules,
  CSScript, Python, JS, Lua) right after it finished loading/unloading, so a plugin can react to other plugins
  appearing or disappearing instead of only knowing about `OnModulesLoaded`/`OnAllPluginsLoaded`.
- Added console commands `fougerite ban <steamid|ip|name> [reason]` and `fougerite unban <steamid|ip|name>`.
  `ban` accepts a connected player's name, a raw IP, or a SteamID64 (validated with the new
  `Util.IsSteamId64`) and bans by whichever matches; `unban` looks it up the same way and removes the ban.
  Both require admin rights and reply with the result in-game/console.
- Banned players (via `BanPlayerIP`, `BanPlayerID` or `BanPlayerIPandID`) are now disconnected immediately if
  they're currently online, instead of staying connected until their next reconnect attempt. They're shown
  the ban reason and who banned them before being kicked.
- Added `TrustedSteamIDs` (`Save\TrustedSteamIDs.json`). Lets you whitelist specific SteamID64s so they're
  always allowed regardless of what `SteamAuthMode`/the Steam Web API reports about their account, useful for
  `RustOwners`/`SteamPaidAccounts` when a trusted player doesn't own Rust or has a limited account.
- Fixed the native plugin domain leaking when a C# module (DLL) plugin's `Load()` failed partway through -
  it is now explicitly unloaded and removed from `ModuleManager.Modules` instead of leaving a stale entry
  that a later load with the same name would silently overwrite.
- Unified creation/teardown of the shared unmanaged Fougerite Mono plugin registry between C# modules (DLL)
  and the new CSScript engine through a new internal `NativeDomainManager`. This fixes the registry never
  being created when `EnableCSharp` was disabled while only CSScript was enabled (or vice versa), and stops
  one engine's unload from tearing the registry out from under the other engine's still-loaded plugins.
- Fixed a potential crash when reloading/unloading C#, Python, JS or Lua plugins while their internal plugin
  collection was being iterated, by snapshotting to an array first.
- Fixed a misplaced `OnPluginShutdown` hook call in `PluginLoader.RemoveHooks` and cleaned up the surrounding
  redundant block.
- Added lots of documentation for all of the above (`Docs/CSScriptPluginTutorial.md`,
  `Docs/Hooks/Server/On_PluginLoaded.md`, `On_PluginUnloaded.md`, updated `Docs/FougeriteCfg.md`,
  `Docs/Classes/PluginLoaders.md`, `Docs/Classes/BasePlugin.md`).

## v1.9.95

Full Changelog: https://github.com/Notulp/Fougerite/compare/v1.9.94...v1.9.95

- Added the `SteamPaidAccounts` mode to `SteamAuthMode`. It allows Spacewar players whose ticket is verified
  through the Steam Web API and whose Steam account is not limited, meaning it has spent at least 5 USD on
  Steam. This keeps out freshly made alt accounts without requiring Rust ownership. Private profiles are
  fine, but the player must have set up a Steam Community profile once. Requires `SteamWebAPIKey`.
- Fixed genuine Spacewar tickets being rejected by the Steam Web API with "101 Invalid ticket". Fougerite now
  retries for about 5 seconds while Steam registers a fresh ticket. It also cuts off buffer padding some
  clients send after the ticket. No client changes are needed.
- Rewrote the Steam setting comments in `Fougerite.cfg` as a readable table, showing per mode who can join,
  whether an API key is needed and what the Steam profile needs. If your config still shows the old comments,
  delete the four Steam lines once and they will be written again on the next startup.
- The startup log now warns about the profile requirements of `RustOwners` and `SteamPaidAccounts`, so server
  owners aren't surprised by rejected players.
- `AssetBundleLoader` received a simple asset loading API. `LoadAsync<T>(path, callback)` waits for queued
  plugin bundles and the game's own bundles, then loads the asset without stalling a frame. `Load<T>(path)`
  does the same lookup instantly. `LoadBundle(path, version)` queues a bundle without a callback. Plugin
  bundles are searched first, then the game's.
- Added `AssetBundleLoader.LoadAndInjectBundle` and `InjectWhenBundlingLoaded` for plugins that keep calling
  `Facepunch.Bundling.Load` directly. They inject the bundle as soon as the game finished loading.
- Rewrote `InjectIntoFacepunchBundling` against the game's actual Bundling layout. It no longer relies on
  `Util` reflection, ignores duplicate injections and logs exactly what is missing if a game build differs.
- `AssetBundleLoader` documentation now has a code example on every public member.

## v1.9.94

Full Changelog: https://github.com/Notulp/Fougerite/compare/v1.9.93...v1.9.94

- Added lots of AI documentations.
- Updated the patcher exe, because it wasn't uploaded with the previous release.
- Added `Hooks.OnPlayerUnban` and `UnbanEvent`.
- Added `Fougerite.cfg` setting `DisableFacePunchTruthPunish`. It is automatically added on next startup.
- Added `CustomMap` class, `Hooks.OnCustomMapLoaded` and `CustomMapLoadedEvent`. This is still experimental
  code.
- Added `AssetBundleLoader` class for smooth asset loading. It also has an `InjectIntoFacepunchBundling`
  function.
- Updated `SteamAPITools` class, added `SteamTicketValidator` class, `SteamDenyEvent` received new
  properties.
- Added `Fougerite.cfg` settings `SteamAuthMode`, `SteamWebAPIKey`, `SteamWebAPITimeout`,
  `SteamWebAPIFailOpen`. All will be added to your config next startup. The new Steam configuration settings
  give server owners control over how connection tickets are handled when standard Steam authentication
  fails (such as Spacewar 480 tickets or cracked clients). Before this, plugins like AuthAllow were used for
  that, but they are no longer required. `SteamAuthMode` sets the strictness of player access, letting you
  require verified Rust ownership (`RustOwners`), genuine Steam accounts (`SteamAccounts`), basic offline
  ticket validation (`SteamAccountsUnverified`), legacy plugin-handled checks (`Legacy`), or unrestricted
  access (`AllowAll`). To support the API-based modes, `SteamWebAPIKey` stores your Steam developer key,
  `SteamWebAPITimeout` sets how long a connecting player waits for API responses (1 to 45 seconds), and
  `SteamWebAPIFailOpen` determines whether players on Spacewar tickets are allowed or rejected if the Steam
  Web API times out or goes offline.

  Ranking from most safe to least safe (you need a Steam Web API key from
  https://steamcommunity.com/dev/apikey to use the API-based modes):
  - `RustOnly` (Most Safe) - Enforces strict native Steam authentication for Rust (252490). Completely blocks
    all Spacewar, emulated, or cracked clients.
  - `RustOwners` (High Safety) - Allows Spacewar tickets, but uses the Steam Web API to verify the account
    actually owns paid Rust.
  - `SteamAccounts` (Medium Safety) - Verifies via Web API that the Spacewar ticket belongs to a legitimate
    Steam account, but does not require Rust ownership. This is the most recommended setting to use with
    RustBuster.
  - `SteamAccountsUnverified` (Low Safety) - Performs basic offline checks to filter out sloppy emulators,
    bypassable by well-forged tickets spoofing any SteamID.
  - `Legacy` (Very Low Safety) - Performs no ticket verification on its own, delegating access decisions
    entirely to plugins.
  - `AllowAll` (Zero Safety) - Disables all checks and allows anyone to connect, including clients without
    Steam.
