### Class
`Fougerite.PluginLoaders.*` - `PluginLoader`, `IPlugin`, `IPluginLoader`, `ISingleton`, `Singleton<T>`,
`CountedInstance`, `PluginWatcher`, `PluginLoaderEvents`, `CSScriptPluginLoader`, `CSScriptPlugin`

### Description
This namespace is the machinery behind loading, hot-reloading and unloading plugins written in any of the five
supported plugin types (C# Modules, C# Script/CSScript, Python, JavaScript, Lua). You normally never touch
most of these classes directly from a plugin - `BasePlugin` (see [`BasePlugin`](BasePlugin.md)) is what
plugins actually derive from/script against - but `PluginLoader` itself is useful when a plugin needs to
manage *other* plugins (e.g. an admin/ops plugin that reloads modules on command).

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
- `OnPluginLoaded(BasePlugin plugin)` / `OnPluginUnloaded(BasePlugin plugin)` - called by every language's
  plugin loader right after a plugin finishes loading/unloading; in turn fires
  `Hooks.OnPluginLoaded`/`Hooks.OnPluginUnloaded` (see [`On_PluginLoaded`](../Hooks/Server/On_PluginLoaded.md) /
  [`On_PluginUnloaded`](../Hooks/Server/On_PluginUnloaded.md)) for **every** plugin type, not just one language.

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
The contract each language's loader (`CSharpPluginLoader`, `CSScriptPluginLoader`, `PythonPluginLoader`,
`JavaScriptPluginLoader`, `LuaPluginLoader`) implements, and what `PluginLoader.PluginLoaders` stores per
`PluginType`:
- `GetExtension()` - the file extension for this language (e.g. `.py`, `.cs`).
- `GetSource(string path)` / `GetMainFilePath(string pluginname)` / `GetPluginDirectoryPath(string name)` -
  filesystem resolution helpers.
- `GetPluginNames()` - every plugin name discovered on disk for this language.
- `LoadPlugin(string name)` / `LoadPlugins()` / `ReloadPlugin(string name)` / `ReloadPlugins()` /
  `UnloadPlugin(string name)` / `UnloadPlugins()`.

### CSScriptPluginLoader / CSScriptPlugin
`CSScriptPluginLoader` (a `Singleton<CSScriptPluginLoader>` implementing `IPluginLoader`) is the loader for
**C# Script** plugins - `Modules\Name\Name.cs` plus every other `.cs` file in that folder, compiled on the
server at load time instead of being shipped as a prebuilt `.dll`. Full walkthrough:
[`CSScriptPluginTutorial.md`](../CSScriptPluginTutorial.md). Key members beyond the standard
`IPluginLoader` contract:
- `IsEngineEnabled` - mirrors `EnableCSScript` from `Fougerite.cfg`'s `[Engines]` section.
- `PluginDirectory` / `CacheDirectory` - `Modules\` and `Save\.CSScriptCache\` respectively (compiled
  assemblies are cached by a content hash of sources + references + compiler, so unchanged plugins skip
  recompilation on the next load).
- Compiler auto-detection (`CSScriptCompiler` in `Fougerite.cfg`, or MSBuild Roslyn -> .NET Framework ->
  Mono `mcs`, tried in that order) and the `// #require OtherPlugin` directive (compile-time reference +
  load-order dependency against another CSScript plugin or DLL module by name) are documented in detail in
  the tutorial above.

`CSScriptPlugin` (a `BasePlugin`) is the loaded plugin instance, mirroring `CSPlugin`'s role for DLL
modules:
- `Engine` (`Module`) - the compiled plugin's `Module` instance; cast to your plugin's own type to call its
  public members directly (typically after a `#require` guarantees it's already loaded).
- `CompiledAssembly` / `AssemblyPath` - the in-memory `Assembly` and the `.dll` path under `.CSScriptCache`.
- `Dependencies` - the plugin names this one requires via `#require`, used for load ordering.

### NativeDomainManager
`internal static class NativeDomainManager` is a small helper, shared by `CSharpPluginLoader` and
`CSScriptPluginLoader`, that centralizes the lifecycle of the single unmanaged **Fougerite Mono plugin
registry** (`NativeMono.mono_fg_create_domain()` / `mono_fg_unload_domain()`, see
[`BasePlugin`](BasePlugin.md#c-modules-single-appdomain-and-reloading)). Before it existed, only
`CSharpPluginLoader` called those two native functions directly, which caused two problems:
- If DLL modules (`EnableCSharp`) were disabled while C# script plugins (`EnableCSScript`) were enabled, the
  registry was never created at all.
- `mono_fg_unload_domain()` tears down **every** tracked plugin domain and releases the whole registry, so
  whichever loader's `UnloadPlugins()` ran first would rip the registry out from under the other loader's
  still-loaded plugins.

`NativeDomainManager` fixes both with a lazy, `lock`-protected, reference-counted registry:
- `EnsureCreated(string owner)` - lazily calls `mono_fg_create_domain()` exactly once (thread-safe, idempotent)
  and adds `owner` (e.g. `"CSharp"`/`"CSScript"`) to the set of current users. Both `CSharpPluginLoader` and
  `CSScriptPluginLoader` call this before loading any plugin (`LoadPlugin`/`LoadPlugins`), so whichever engine
  is enabled - one, the other, or both - creates the registry on demand.
- `ReleaseOwner(string owner)` - called at the end of each loader's `UnloadPlugins()`. Removes `owner` from
  the set of users and only calls `mono_fg_unload_domain()` once that set is empty, i.e. once **every** loader
  that ever needed the registry has finished unloading its own plugins.
- `IsCreated` - diagnostic flag, true while the registry is believed to exist.

You normally never call this directly from a plugin; it's an internal implementation detail of the two C#
based loaders, documented here so the domain lifecycle rules above are easy to find.

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
