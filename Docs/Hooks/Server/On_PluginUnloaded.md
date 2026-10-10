### Method
`On_PluginUnloaded`

### Description
Runs every time **any** plugin is unloaded - C# Module, C# Script (CSScript), Python, JavaScript or Lua.
This includes plugins unloaded as the first half of a reload (`UnloadPlugin` followed by `LoadPlugin`), not
only a permanent removal. Fires after the plugin's own `DeInitialize()`/unload logic has already run and its
hooks have been removed, right before it's taken out of `PluginLoader.Plugins`.

### C# Event
```csharp
public static event PluginUnloadedDelegate OnPluginUnloaded;
public delegate void PluginUnloadedDelegate(BasePlugin plugin);
```

### Argument(s)
- `plugin` (`Fougerite.PluginLoaders.BasePlugin`) - the plugin that just unloaded.
  - `plugin.Name` - the plugin's name.
  - `plugin.Type` (`PluginType`) - `CSharp`, `CSScript`, `Python`, `JavaScript` or `Lua`.
  - `plugin.Author` / `plugin.Version` / `plugin.About` - plugin metadata (still valid, read it before you
    drop any reference to the plugin).

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPluginUnloaded += HandlePluginUnloaded;
}

public override void DeInitialize()
{
    Hooks.OnPluginUnloaded -= HandlePluginUnloaded;
}

public void HandlePluginUnloaded(BasePlugin plugin)
{
    Logger.Log($"[MyPlugin] {plugin.Name} ({plugin.Type}) just unloaded.");
}
```

#### Python
```python
def On_PluginUnloaded(self, plugin):
    Util.Log("%s (%s) just unloaded." % (plugin.Name, plugin.Type))
```

#### JavaScript
```javascript
function On_PluginUnloaded(plugin)
{
    Util.Log(plugin.Name + " (" + plugin.Type + ") just unloaded.");
}
```

#### Lua
```lua
function On_PluginUnloaded(plugin)
    Util.Log(plugin.Name .. " (" .. tostring(plugin.Type) .. ") just unloaded.")
end
```

See also: [`On_PluginLoaded`](On_PluginLoaded.md) · [`PluginLoaders`](../../Classes/PluginLoaders.md)
