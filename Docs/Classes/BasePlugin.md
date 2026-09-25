### Class
`Fougerite.PluginLoaders.BasePlugin`

### Description
`BasePlugin` is the common base class for **every** plugin loaded by Fougerite, no matter the language:
- C# modules (compiled `.dll` plugins) subclass `BasePlugin` directly (or a module-specific subclass of it).
- Python (`PythonPlugin`), JavaScript/Jint (`JintPlugin`) and Lua/MoonSharp (`MoonSharpPlugin`) plugins are
  all `BasePlugin` subclasses internally; the loader compiles/executes your script and wires its `On_Xxx`
  methods into `BasePlugin`'s `BaseOnXxx` handlers (see [`Hooks/README.md`](../Hooks/README.md)).

Because every plugin *is* a `BasePlugin`, all of the members documented below are available to **every**
plugin, in every language, either directly (`this`/`self` in C#) or through the global `Plugin` variable in
script plugins.

### Fields / Properties
- `Author`, `About`, `Version` - Metadata about the plugin, usually filled from `__author__`, `__about__`,
  `__version__` (Python) or similar per-language conventions.
- `Name` - The plugin's name (matches its class name / file name).
- `RootDir` - The `DirectoryInfo` of the folder the plugin lives in.
- `Type` - `PluginType` enum: `Python`, `JS`, `Lua`, `CS`, `Undefined`, etc.
- `State` - `PluginState` enum: `NotLoaded`, `Loaded`, `FailedToLoad`, etc.
- `HasErrors` / `LastError` - Set automatically whenever an invoked method throws.
- `DontReload` - If `true`, `fougerite.reload` skips this plugin.
- `Globals` - List of every global method name the loader discovered in your plugin.
- `CachedGlobals` - Dictionary of global method name -> compiled callable, used internally by `Invoke`.
- `Timers` / `ParallelTimers` - This plugin's currently-running Normal Timers (see [Timers](Timers.md)).
- `WebSockets` - This plugin's currently-open `ScriptWebSocket`s.
- `CommandList` - Commands registered by this plugin (used for cleanup on unload/reload).
- `GlobalData` (`static`) - A `ConcurrentDictionary<string, object>` shared across **all** plugins, useful
  for simple cross-plugin data sharing without using `SendMessage`.

### Key Methods
- `Invoke(string method, params object[] args)` - Calls one of your plugin's global methods by name (this is
  how hooks are dispatched into your script).
- `CreateTimer(...)` / `CreateParallelTimer(...)` - See [Timers](Timers.md).
- `CreateDict()`, `CreateStringDict()`, `CreateDynamicDict()`, `CreateList()`, `CreateStringList()`,
  `CreateConcurrentDict()`, `CreateConcurrentList()`, `CreateReaderWriterLock()` - Helpers to create managed
  .NET collections from script languages that don't have a native equivalent constructor syntax.
- `Log(string path, string text)`, `RotateLog(string logfile, int max = 6)`, `DeleteLog(string path)` -
  Simple per-plugin file logging under the plugin's folder.
- `GetIni(string path)`, `CreateIni(string path = null)`, `IniExists(string path)`, `GetInis(string path)` -
  `.ini` config file helpers.
- `JsonFileExists`, `FromJsonFile`, `ToJsonFile` - Simple JSON file persistence helpers.
- `GetPlugin(string name)` - Fetches another loaded plugin by name (for cross-plugin calls).
- `SendMessage(string targetName, object message)` / `SendMessageAsync(...)` - Send a message/object to
  another plugin's `On_PluginMessage` hook.
- `CreateWebSocket(string socketId, string url)` / `RemoveWebSocket(string socketId)`.
- `GetDate()`, `GetTime()`, `GetTicks()`, `GetTimestamp()` - Small date/time helpers.

### The `BaseOnXxx` methods
For every hook (see [Hooks reference](../Hooks/README.md)), `BasePlugin` defines a virtual `BaseOnXxx`
method (e.g. `BaseOnChat`, `BaseOnPlayerConnected`, `BaseOnCommand`, ...). These are called by
`Fougerite.Hooks` and, by default, look up and `Invoke` the matching `On_Xxx` global method in your
script/plugin. **You should never need to call or override these directly** in a script plugin - just define
`On_Xxx` and it will be picked up automatically. C# modules that don't derive from a script-loader subclass
subscribe to `Fougerite.Hooks` events manually instead (see the examples in each hook's documentation page).

### Script Plugin Global Variables
Every Python/JavaScript/Lua plugin gets the same set of global variables automatically injected into its
scope before your code runs (see `PythonPlugin.Load`, and the equivalent `Load` in `JintPlugin`/
`MoonSharpPlugin`). You can use these directly, without any `import`/`require`:

| Variable | Type | Description |
|---|---|---|
| `Plugin` | `BasePlugin` (your own plugin instance) | `this`/`self`. Gives access to everything documented above: timers, dictionaries, logging, `SendMessage`, etc. |
| `Server` | [`Server`](Server.md) | The running Rust server: player list, chat, bans, broadcasts. |
| `Util` | [`Util`](Util.md) | Misc. helper methods, thread info, System Timers, type lookups. |
| `World` | `World` | Access to entities, resources, structures, supply crates, etc. |
| `Web` | [`Web`](Web.md) | Synchronous/asynchronous HTTP requests. |
| `Loom` | [`Loom`](Loom.md) | Queue code onto the main thread / run code on a background thread. |
| `WinHttpClient` | `WinHttpClient` | Alternative HTTP client implementation. |
| `Data` / `DataStore` | `Data` / `DataStore` | Simple persistent key-value/data storage helpers. |
| `JSON` | `JsonAPI.GetInstance` | JSON (de)serialization helper. |
| `MySQL` | `MySQLConnector.GetInstance` | MySQL database connector factory. |
| `SQLite` | `SQLiteConnector.GetInstance` | SQLite database connector factory. |
| `PermissionSystem` | `PermissionSystem` | Permission groups/permissions management. |
| `PlayerCache` | `PlayerCache` | Cached/offline player lookups (by SteamID, name, etc). |
| `EntityCache` | `EntityCache` | Cached entity lookups. |
| `NPCCache` | `NPCCache` | Cached NPC/animal lookups. |
| `SleeperCache` | `SleeperCache` | Cached sleeper (sleeping bag/bed) lookups. |
| `PluginCollector` | `GlobalPluginCollector` *(obsolete)* | Legacy way to look up other plugins; prefer `Plugin.GetPlugin(name)`. |

> C# module plugins don't get these as free-standing globals; instead you access the equivalent static
> singletons directly, e.g. `Server.GetServer()`, `Util.GetUtil()`, `World.GetWorld()`, `Web.GetInstance()`,
> `Loom.Current` (or the static `Loom.QueueOnMainThread(...)`/`Loom.RunAsync(...)` helpers).

### C# Modules, Single AppDomain, and Reloading
Fougerite runs on a **custom Mono build**. Unlike vanilla .NET/Mono plugin systems that isolate each plugin
in its own `AppDomain`, this custom build loads/unloads C# module assemblies **directly into a single, shared
domain** via native interop calls defined in `Fougerite.Icalls`:

- `Icalls.mono_fg_load_plugin(pluginName, data, dataLen)` - Loads a plugin `Assembly` from a raw memory
  buffer into the current domain.
- `Icalls.mono_fg_unload_plugin(pluginName)` - Unloads a previously-loaded plugin.
- `NativeMono.mono_fg_create_domain()` / `mono_fg_unload_domain()` - Low-level domain creation/teardown,
  implemented in the custom `mono.dll` shipped with the project.

Because everything lives in one domain, C# module plugins can be **hot-reloaded** at runtime (e.g. via the
`fougerite.reload` console command) without restarting the server - the old assembly is unloaded and the new
one is loaded back in, in-place. This is more fragile than true `AppDomain` isolation though:
- Static state (like `BasePlugin.GlobalData`, or any `static` field in your own plugin) is **not** reset
  automatically between reloads unless you clean it up yourself.
- Make sure to unsubscribe from `Fougerite.Hooks` events and kill your timers/web sockets in your plugin's
  `DeInitialize()`/unload logic (see `BasePlugin.KillTimers()`), otherwise reloading will leave "ghost"
  subscribers/timers still referencing the unloaded assembly's types.
- Set `DontReload = true` on your plugin if it should be excluded from `fougerite.reload`.

Script plugins (Python/JS/Lua) don't go through `Icalls` at all - they are recompiled/re-executed by their
respective script engines (IronPython/Jint/MoonSharp) each time they're (re)loaded, so they don't share this
particular caveat, but they still share the same `BasePlugin.GlobalData` static storage as C# modules.
