# Fougerite Documentation

This folder contains developer documentation for writing Fougerite plugins in **C#**, **Python** (IronPython),
**JavaScript** (Jint) and **Lua** (MoonSharp).

Fougerite plugins react to game events by either:
- **C#**: subscribing to a `Fougerite.Hooks` event (e.g. `Hooks.OnChat += MyHandler;`), or
- **Script plugins (Python/JS/Lua)**: defining a method with the matching name (e.g. `On_Chat`), which the
  plugin loader automatically discovers and calls when the corresponding hook fires.

### Contents

- [`CSharpPluginTutorial.md`](CSharpPluginTutorial.md) - **Start here if you're new to C# modules.**
  A beginner-friendly, step-by-step walkthrough: setting up a Visual Studio (or JetBrains Rider) project,
  targeting .NET 3.5, writing your first `Module`, subscribing to hooks, and building/installing the
  compiled plugin on the server.
- [`Installation.md`](Installation.md) - Where to physically place each type of plugin (C#/Python/JS/Lua)
  on the server, how the `[Modules]` section of `Fougerite.cfg` maps folders to modules, and how to
  relocate the `Modules`/`Save` folders with `FougeriteDirectory.cfg`.
- [`FougeriteCfg.md`](FougeriteCfg.md) - Full reference for every section/key of `Fougerite.cfg`
  (`[Fougerite]`, `[Modules]`, `[Engines]`, `[Logging]`).
- [`Scripts.md`](Scripts.md) - The `AutoUpdate-Fougerite.ps1` and `Collect-Logs.ps1` maintenance scripts:
  what they do and how to use them.
- [`Hooks/`](Hooks/README.md) - Full reference of every hook/event exposed by Fougerite, one file per hook,
  grouped by category (Player, NPC, Entities, Items, Combat, World, Server).
- [`Classes/`](Classes/README.md) - Reference and usage examples for important helper classes:
  - [`BasePlugin`](Classes/BasePlugin.md) - The base class of every plugin, plus the full table of script
    global variables (`Plugin`, `Server`, `Util`, `World`, `Web`, `Loom`, ...) and how C# module reloading
    works on the custom single-domain Mono build.
  - [`Player`](Classes/Player.md) / [`Server`](Classes/Server.md) / [`Entity`](Classes/Entity.md) /
    [`Sleeper`](Classes/Sleeper.md) - The core game object wrappers.
  - [`Util`](Classes/Util.md) - Thread-info helpers, world/entity lookups, reflection/hash helpers, and the
    System Timer API.
  - [`Web`](Classes/Web.md) - Making synchronous/asynchronous HTTP requests from a plugin.
  - [`Loom`](Classes/Loom.md) - Running code on the main thread / running heavy work on background threads.
  - [`Timers`](Classes/Timers.md) - **Normal Timer vs System Timer**: which thread each runs on, and when
    to use which (`Plugin.CreateTimer`/`CreateParallelTimer` vs `Util.CreateSystemTimer`/
    `CreateParallelSystemTimer`).
  - [`Caches`](Classes/Caches.md) - Thread-safe `EntityCache`/`NPCCache`/`SleeperCache` snapshots, and the
    persistent `PlayerCache` (name/alias/IP history) for offline SteamID lookups.
  - [`PluginLoaders`](Classes/PluginLoaders.md) - How plugins are loaded/hot-reloaded/unloaded, and
    managing other plugins from your own.
  - [`SteamAuth`](Classes/SteamAuth.md) - The extended Steam authentication pipeline used to admit/reject
    non-Rust Steam tickets (e.g. RustBuster/Spacewar).
  - [`PluginMessaging`](Classes/PluginMessaging.md) - Inter-Plugin Communication between plugins.
  - [`Inventory`](Classes/Inventory.md) - Chest/stash contents, player backpacks/belt/armor, and the weapon
    mod API (`FInventory`/`EntityInv`/`EntityItem`/`PlayerInv`/`PlayerItem`).
  - [`World`](Classes/World.md) - Spawning, airdrops, terrain, zones (`World`/`Zone3D`), wildlife (`NPC`),
    and the advanced `CustomMap` API.
  - [`Data`](Classes/Data.md) - The persistent `DataStore` key/value store and the `Data` string/number
    helper singleton.
  - [`Modules`](Classes/Modules.md) - Fougerite's original, now-obsolete C# plugin system
    (`Module`/`ModuleContainer`/`ModuleManager`).
  - [`Config`](Classes/Config.md) - Reading/writing `Fougerite.cfg`, and `Bootstrap`'s runtime settings.
  - [`Networking`](Classes/Networking.md) - `WinHttpClient`, `ScriptWebSocket`, and the
    `MySQLConnector`/`SQLiteConnector` database helpers.
  - [`Utilities`](Classes/Utilities.md) - `ChatString`, `Flood`, `JsonAPI`, `ReflectionExtensions`,
    `SuperFastHashUInt16Hack`, `Stopper`, `CoroutineHost`, `Icalls`, `ItemsBlocks`, `AssetBundleLoader`, and
    the legacy `RustPPExtension`/`GlobalPluginCollector`.
  - [`ServerSystems`](Classes/ServerSystems.md) - `ServerSaveHandler` (map save scheduling) and
    `WaterSystemServer` (oxygen-based swimming/drowning, dry-region exclusions).

### How hooks map to script methods

Every hook has:
1. A C# event on `Fougerite.Hooks` (e.g. `Hooks.OnChat`).
2. A constant script method name defined in `Fougerite.PluginLoaders.PluginLoaderEvents` (e.g. `On_Chat`).

For C# plugins (modules), you subscribe/unsubscribe manually in `Initialize()`/`DeInitialize()`.
For Python/JS/Lua plugins, you simply declare a method/function with the exact name (e.g. `On_Chat`) and the
correct amount of arguments, and the loader will hook it up automatically.

### General plugin skeleton

**C#**
```csharp
public class MyPlugin : Fougerite.PluginLoaders.BasePlugin // or your module's plugin base
{
    public override void Initialize()
    {
        Fougerite.Hooks.OnChat += ChatHandler;
    }

    public override void DeInitialize()
    {
        Fougerite.Hooks.OnChat -= ChatHandler;
    }

    public void ChatHandler(Fougerite.Player player, ref Fougerite.ChatString chatString)
    {
        // ...
    }
}
```

**Python**
```python
class MyPlugin:
    def On_PluginInit(self):
        pass

    def On_Chat(self, Player, ChatEvent):
        pass
```

**JavaScript**
```javascript
function On_PluginInit() {
}

function On_Chat(Player, ChatEvent) {
}
```

**Lua**
```lua
function On_PluginInit()
end

function On_Chat(Player, ChatEvent)
end
```

See [`Hooks/README.md`](Hooks/README.md) for the full list of hooks.
