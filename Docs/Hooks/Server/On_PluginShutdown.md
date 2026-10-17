### Method
`On_PluginShutdown`

### Description
Runs on **this specific plugin** right before its hooks are torn down - whenever it is about to be unloaded
or hot-reloaded. Unlike [`OnPluginUnloaded`](On_PluginUnloaded.md) (a global notification broadcast to
*other* plugins), `On_PluginShutdown` is a self-targeted lifecycle callback, the mirror image of
[`OnPluginInit`](On_PluginInit.md): `OnPluginInit` tells a plugin it just finished loading, `On_PluginShutdown`
tells it it's about to go away, so it gets one last chance to stop timers, flush files, close sockets, etc.
before its event subscriptions are removed.

This only fires for script plugins (CSScript/Python/JavaScript/Lua) going through `PluginLoader.RemoveHooks` -
C# `Module`s are not routed through this path, so they should rely on their own `DeInitialize()` override
instead.

### C# Event
Not applicable - there is no `Fougerite.Hooks` event for this. It is invoked directly on the plugin instance
(`BasePlugin.BaseOnPluginShutdown()`) from `PluginLoader.RemoveHooks`, so C# plugins should use
`DeInitialize()` instead of subscribing to anything.

### Argument(s)
None.

### Examples

#### Python
```python
def On_PluginShutdown(self):
    Util.Log("MyPlugin is shutting down, cleaning up.")
```

#### JavaScript
```javascript
function On_PluginShutdown()
{
    Util.Log("MyPlugin is shutting down, cleaning up.");
}
```

#### Lua
```lua
function On_PluginShutdown()
    Util.Log("MyPlugin is shutting down, cleaning up.")
end
```

See also: [`On_PluginInit`](On_PluginInit.md) · [`On_PluginUnloaded`](On_PluginUnloaded.md)
