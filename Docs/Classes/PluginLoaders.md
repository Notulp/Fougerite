### Class
`Fougerite.PluginLoaders.*` - `PluginLoader`, `IPlugin`, `IPluginLoader`, `ISingleton`, `Singleton<T>`,
`CountedInstance`, `PluginWatcher`, `PluginLoaderEvents`

### Description
This namespace is the machinery behind loading, hot-reloading and unloading plugins written in any of the four
supported languages (C#, Python, JavaScript, Lua). You normally never touch most of these classes directly from
a plugin - `BasePlugin` (see [`BasePlugin`](BasePlugin.md)) is what plugins actually derive from/script against
- but `PluginLoader` itself is useful when a plugin needs to manage *other* plugins (e.g. an admin/ops plugin
that reloads modules on command).

### PluginLoader
A `Singleton<PluginLoader>` that owns every loaded plugin and dispatches hooks to them.
- `PluginLoader.GetInstance()` - static accessor for the singleton.
- `Plugins` - `Dictionary<string, BasePlugin>` of every currently loaded plugin, keyed by plugin name.
- `PluginLoaders` - `Dictionary<PluginType, IPluginLoader>`, one entry per scripting language, registered by
  each language-specific loader (`CSharpPluginLoader`, `PythonPluginLoader`, `JavaScriptPluginLoader`,
  `LuaPluginLoader`) on startup.
- `AllPluginsLoaded` - `true` once every plugin that was queued at startup has finished loading (fires
  `Hooks.OnAllPluginsLoaded` / `On_AllPluginsLoaded` the moment it flips).
- `ModulesFolder` / `PublicFolder` - the resolved paths from `Fougerite.cfg` (`[Modules]` section).
- `LoadPlugin(string name, PluginType type)` / `LoadPlugin(string name, bool callInit)` - load a single plugin,
  either by explicit type or by searching every registered loader for a matching plugin name.
- `LoadPlugins()` / `UnloadPlugins()` / `ReloadPlugins()` - bulk operations across every registered loader.
- `UnloadPlugin(string name)` / `ReloadPlugin(string name)` / `ReloadPlugin(BasePlugin plugin)` - operate on a
  single already-loaded plugin by name or instance.
- `InstallHooks(BasePlugin plugin)` / `RemoveHooks(BasePlugin plugin)` - wires up every `Fougerite.Hooks` event
  the plugin declared a matching `On_X`/`BaseOnX` handler for (driven by the `HookNames` list, which mirrors
  every constant in `PluginLoaderEvents`). Called automatically on load/unload - you only need these if you're
  building custom plugin-loading logic.

> **Intensive events**: A handful of very high-frequency hooks (`On_PlayerMove`, `On_Shoot`, `On_ShotgunShoot`,
> `On_BowShoot`, `On_AnimalMovement`, `On_ServerTick`, `On_HeatZoneEnter`, `On_WorkZoneEnter`,
> `On_AudibleSound`) are only wired up for C# plugins by default. Script plugins (Python/JS/Lua) can only use
> them if `EnableScriptPluginsIntensiveEvents` is enabled in `Fougerite.cfg`, since an interpreted handler
> running on every tick/shot/move can meaningfully hurt performance on a busy server.

### IPlugin / PluginState / PluginType
`IPlugin` is the minimal contract every loaded plugin instance (`BasePlugin` subclass) implements:
- `Load(string code = "")` - (re)loads/compiles the plugin.
- `Invoke(string method, params object[] args)` - calls a named method/function on the plugin (used to call
  `On_X` script methods).
- `FormatException(Exception ex)` - language-appropriate exception formatting for logging.

`PluginState` (`sbyte` enum): `FailedToLoad` (-1), `NotLoaded` (0), `Loaded` (1).

`PluginType` (enum): `Undefined`, `Python`, `JavaScript`, `CSharp`, `CSScript`, `Lua`.

### IPluginLoader
The contract each language's loader (`CSharpPluginLoader`, `PythonPluginLoader`, `JavaScriptPluginLoader`,
`LuaPluginLoader`) implements, and what `PluginLoader.PluginLoaders` stores per `PluginType`:
- `GetExtension()` - the file extension for this language (e.g. `.py`).
- `GetSource(string path)` / `GetMainFilePath(string pluginname)` / `GetPluginDirectoryPath(string name)` -
  filesystem resolution helpers.
- `GetPluginNames()` - every plugin name discovered on disk for this language.
- `LoadPlugin(string name)` / `LoadPlugins()` / `ReloadPlugin(string name)` / `ReloadPlugins()` /
  `UnloadPlugin(string name)` / `UnloadPlugins()`.

### ISingleton / Singleton\<T\> / CountedInstance
- `ISingleton` - `Initialize()` + `CheckDependencies()` (return `false` to disable the singleton, e.g. when a
  language's dependency/engine isn't available - it logs a warning and no plugins of that type load).
- `Singleton<T>` - generic lazy-singleton base (`GetInstance()`), used by `PluginLoader`, `PluginWatcher` and
  the per-language loaders. Calls `CheckDependencies()`/`Initialize()` once, the first time `GetInstance()` is
  touched.
- `CountedInstance` - base class that tracks how many instances of each derived type have been
  created/finalized (useful for leak-hunting). `CountedInstance.InstanceReportText()` dumps a per-type
  created/destroyed/currently-alive report.

### PluginWatcher
A `Singleton<PluginWatcher>` wrapping one `FileSystemWatcher` per plugin language, so dropping/editing a
plugin file on disk hot-reloads it automatically without a server restart.
- `AddWatcher(PluginType type, string filter, string path)` - registers a watcher for a language/extension
  pair (called once per loader on startup).
- `Watchers` - the list of active `PluginTypeWatcher` instances.
- A changed/created file automatically triggers `PluginLoader.ReloadPlugin`/`LoadPlugin` on the main thread
  (via `Loom.QueueOnMainThread`), so plugins can safely touch Unity/`World` state on load.

### PluginLoaderEvents
A static class of ~90 `public const string` fields (`OnChat = "On_Chat"`, `OnServerInit = "On_ServerInit"`,
...) - the canonical script method names the loader looks for on a plugin's globals, one per `Fougerite.Hooks`
event. Useful if you need the exact string for a hook when reflecting over a plugin's declared methods.

### Example - C# (reloading another plugin by name, e.g. from a chat command)
```csharp
public void ReloadModule(string pluginName)
{
    if (PluginLoader.GetInstance().Plugins.ContainsKey(pluginName))
    {
        PluginLoader.GetInstance().ReloadPlugin(pluginName);
        Logger.Log($"Reloaded {pluginName}.");
    }
    else
    {
        Logger.Log($"No plugin named {pluginName} is loaded.");
    }
}
```

### Example - C# (listing every loaded plugin and its language)
```csharp
foreach (BasePlugin plugin in PluginLoader.GetInstance().Plugins.Values)
{
    Logger.Log($"{plugin.Name} v{plugin.Version} ({plugin.Type}) - {plugin.State}");
}
```

See also: [`BasePlugin`](BasePlugin.md) · [`Timers`](Timers.md) · [`PluginMessaging`](PluginMessaging.md)
