### Method
`On_PluginLoaded`

### Description
Runs every time **any** plugin finishes loading - C# Module, C# Script (CSScript), Python, JavaScript or
Lua, in that order as each one comes online. This fires once per plugin, right after it is added to
`PluginLoader.Plugins` and its own hooks have been installed. Useful for admin/ops plugins that want to
track, inspect or react to other plugins being (re)loaded (e.g. logging, permission syncing, dependency
checks) without caring what language the other plugin is written in.

> This is different from [`OnPluginInit`](On_PluginInit.md) (a plugin notifying itself that *it* finished
> initializing) and from [`OnAllPluginsLoaded`](On_AllPluginsLoaded.md) (fires once, after every plugin
> queued at startup is done). `OnPluginLoaded` fires once **per plugin**, including plugins loaded later
> through a `reload`/hot-reload after startup.

### C# Event
```csharp
public static event PluginLoadedDelegate OnPluginLoaded;
public delegate void PluginLoadedDelegate(BasePlugin plugin);
```

### Argument(s)
- `plugin` (`Fougerite.PluginLoaders.BasePlugin`) - the plugin that just loaded.
  - `plugin.Name` - the plugin's name.
  - `plugin.Type` (`PluginType`) - `CSharp`, `CSScript`, `Python`, `JavaScript` or `Lua`.
  - `plugin.Author` / `plugin.Version` / `plugin.About` - plugin metadata.
  - `plugin.State` - always `PluginState.Loaded` here (see [`PluginLoaders`](../../Classes/PluginLoaders.md)).

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPluginLoaded += HandlePluginLoaded;
}

public override void DeInitialize()
{
    Hooks.OnPluginLoaded -= HandlePluginLoaded;
}

public void HandlePluginLoaded(BasePlugin plugin)
{
    Logger.Log($"[MyPlugin] {plugin.Name} ({plugin.Type}) just loaded.");
}
```

#### Python
```python
def On_PluginLoaded(self, plugin):
    Util.Log("%s (%s) just loaded." % (plugin.Name, plugin.Type))
```

#### JavaScript
```javascript
function On_PluginLoaded(plugin)
{
    Util.Log(plugin.Name + " (" + plugin.Type + ") just loaded.");
}
```

#### Lua
```lua
function On_PluginLoaded(plugin)
    Util.Log(plugin.Name .. " (" .. tostring(plugin.Type) .. ") just loaded.")
end
```

See also: [`On_PluginUnloaded`](On_PluginUnloaded.md) · [`On_AllPluginsLoaded`](On_AllPluginsLoaded.md) ·
[`PluginLoaders`](../../Classes/PluginLoaders.md)
