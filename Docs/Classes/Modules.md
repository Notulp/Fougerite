### Class
`Fougerite.Module` / `Fougerite.ModuleContainer` / `Fougerite.ModuleManager`

### Description
These three classes are Fougerite's **original** C# plugin system, predating the current multi-language
[`PluginLoaders`](PluginLoaders.md) subsystem (`BasePlugin`/`PluginLoader`/`CSharpPluginLoader`). They are kept
only for backwards compatibility with a large ecosystem of legacy C# plugins that still derive from `Module`,
and are marked `[Obsolete]` - **new plugins should derive from [`BasePlugin`](BasePlugin.md) instead** and be
loaded through `PluginLoader.GetInstance().Plugins`.

### Module
`public abstract class Module : BasePlugin, IDisposable` - because `Module` inherits from `BasePlugin`, any
old plugin written against this API still gets every modern `BasePlugin` facility (timers, logging,
websockets, etc.) for free, while exposing its own, older-style surface:
- `Name` / `Version` / `Author` / `Description` - plugin metadata (`new virtual`, shadowing `BasePlugin`'s).
- `Enabled` (get/set) - whether the module is active.
- `Order` - load priority (lower loads first; defaults to `uint.MaxValue`, i.e. last).
- `UpdateURL` - an optional URL describing where to fetch plugin updates from (purely informational, Fougerite
  does not auto-update plugins from it).
- `ModuleFolder` - only valid once `Initialize()` has been called; the plugin's own data folder.
- `Initialize()` / `DeInitialize()` - abstract lifecycle methods (same purpose as `BasePlugin`'s).
- `Dispose()` / `Dispose(bool disposing)` - standard `IDisposable` pattern; a finalizer also calls
  `Dispose(false)` as a safety net.

### ModuleContainer
A thin wrapper owning exactly one loaded `Module` instance:
- `Plugin` - the wrapped `Module` instance.
- `Initialized` - whether `Initialize()` has been called on it yet.
- `Dll` - whether the module was loaded from a compiled `.dll` (as opposed to some other hosting mechanism).
- `Initialize()` / `DeInitialize()` - calls through to `Plugin.Initialize()`/`Plugin.DeInitialize()` and
  updates `Initialized`.
- `Dispose()` - calls through to `Plugin.Dispose()`.

### ModuleManager *(obsolete)*
Static class that originally discovered and loaded every `Module`-derived plugin:
- `ModulesFolder` / `PublicFolder` - resolved paths from `Fougerite.cfg`.
- `ApiVersion` - the Fougerite API version at the time this system was written (`1.0.0.0`; not updated anymore
  and not representative of the current Fougerite version).
- `Modules` / `Plugins` *(obsolete)* - the list of loaded `ModuleContainer`s. **Use
  `PluginLoader.GetInstance().Plugins.Values` instead** (see [`PluginLoaders`](PluginLoaders.md)).
- `LoadModules()` / `UnloadModules()` / `ReloadModules()` *(internal)* - scans every subfolder of
  `ModulesFolder` for a `<FolderName>.dll`, skips anything listed in `ignoredmodules.txt` or not listed under
  the `[Modules]` section of `Fougerite.cfg`, loads the assembly and instantiates every public, non-abstract
  `Module` subclass it finds via reflection.

### Example - C# (a legacy-style module, for reference only)
```csharp
public class MyLegacyModule : Module
{
    public override string Name { get { return "MyLegacyModule"; } }
    public override Version Version { get { return new Version(1, 0); } }
    public override string Author { get { return "YourName"; } }

    public override void Initialize()
    {
        Logger.Log(Name + " initialized from " + ModuleFolder);
    }

    public override void DeInitialize()
    {
        Logger.Log(Name + " unloaded.");
    }
}
```

> New plugins should not use this pattern - see [`BasePlugin`](BasePlugin.md) for the current, supported way
> to write a C# (or Python/JS/Lua) plugin.

See also: [`BasePlugin`](BasePlugin.md) · [`PluginLoaders`](PluginLoaders.md)
