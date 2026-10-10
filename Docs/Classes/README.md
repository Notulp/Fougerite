# Classes Reference

This section documents important helper classes available to Fougerite plugins, beyond the event hooks.

- [`BasePlugin`](BasePlugin.md) - The base class every plugin (C#, Python, JS, Lua) is built on: fields,
  helper methods, the script global variables (`Plugin`, `Server`, `Util`, `World`, ...), and how the
  custom Mono build's single-AppDomain hot-reload works (see `Icalls`).
- [`Player`](Player.md) - The connected/known Rust client wrapper: properties, messaging, teleporting,
  metabolism, command restriction.
- [`Server`](Server.md) - The running server singleton: player list, chat broadcasts, bans, command
  restriction.
- [`Entity`](Entity.md) - Structures, deployables, storage, fire barrels, resource nodes and supply crates.
- [`Sleeper`](Sleeper.md) - Sleeping bags/beds and their sleeping avatars.
- [`Util`](Util.md) - Thread-info helpers, the System Timer API, world/entity lookups, reflection/hash
  helpers.
- [`Web`](Web.md) - Making synchronous and asynchronous HTTP requests from a plugin.
- [`Loom`](Loom.md) - Avoiding "Oops! Crashed" popups by running code on the main thread, and running heavy
  CPU-bound work on background threads.
- [`Timers`](Timers.md) - Normal Timers vs System Timers (`Plugin.CreateTimer`/`CreateParallelTimer` vs
  `Util.CreateSystemTimer`/`CreateParallelSystemTimer`): what thread each runs on, and when to use which.
- [`Caches`](Caches.md) - Thread-safe `EntityCache`/`NPCCache`/`SleeperCache` snapshots, and the persistent
  `PlayerCache` (name/alias/IP history) for offline SteamID lookups.
- [`PluginLoaders`](PluginLoaders.md) - How plugins are loaded/hot-reloaded/unloaded (`PluginLoader`,
  `IPluginLoader`, `PluginWatcher`), and managing other plugins from your own.
- [`SteamAuth`](SteamAuth.md) - The extended Steam authentication pipeline (`SteamAuthMode`,
  `SteamAPITools`, `SteamTicketValidator`, `SteamUserRegistry`) used to admit/reject non-Rust Steam tickets
  (e.g. RustBuster/Spacewar).
- [`PluginMessaging`](PluginMessaging.md) - Inter-Plugin Communication: sending/receiving messages between
  plugins synchronously or asynchronously.
- [`Inventory`](Inventory.md) - `FInventory`/`EntityInv`/`EntityItem`/`PlayerInv`/`PlayerItem`: chest/stash
  contents, player backpacks/belt/armor, and the weapon mod API.
- [`World`](World.md) - The `World` singleton (spawning, airdrops, terrain, zones), `Zone3D`, `NPC` (wildlife),
  and the advanced `CustomMap` API for replacing the server's map at startup.
- [`Prefabs`](Prefabs.md) - A reference list of known prefab names usable with `World.SpawnEntity`/`Spawn`/
  `SpawnAtPlayer` (structures, deployables, resources, animals, loot crates).
- [`Data`](Data.md) - The `DataStore` persistent key/value store (the recommended way to save plugin data
  across restarts) and the older `Data` string/number helper singleton.
- [`Modules`](Modules.md) - `Module`/`ModuleContainer`/`ModuleManager`, Fougerite's original (now obsolete)
  C# plugin system, kept for legacy plugin compatibility.
- [`Config`](Config.md) - Reading/writing `Fougerite.cfg` (`Config.GetValue`/`AddDefault`), and the runtime
  settings exposed as `Bootstrap`'s static fields.
- [`Networking`](Networking.md) - `WinHttpClient` (alternative HTTP client), `ScriptWebSocket` (WebSocket
  client), and `MySQLConnector`/`SQLiteConnector` (simple database access).
- [`Utilities`](Utilities.md) - Smaller single-purpose helpers: `ChatString`, `Flood`, `JsonAPI`,
  `ReflectionExtensions`, `SuperFastHashUInt16Hack`, `Stopper`, `CoroutineHost`, `Icalls`, `ItemsBlocks`,
  `AssetBundleLoader`, and the legacy `RustPPExtension`/`GlobalPluginCollector`.
- [`ServerSystems`](ServerSystems.md) - `ServerSaveHandler` (map save scheduling) and `WaterSystemServer`
  (the oxygen-based swimming/drowning replacement and dry-region exclusions).
