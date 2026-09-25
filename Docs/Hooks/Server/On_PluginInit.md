### Method
`On_PluginInit`

### Description
Runs when this specific plugin has finished loading/initializing. This is the recommended place to set up
timers, load configs, or register commands for script plugins.

### C# Event
```csharp
public static event PluginInitHandlerDelegate OnPluginInit;
public delegate void PluginInitHandlerDelegate();
```

### Argument(s)
None. For C# plugins, use the `Initialize()` override instead of subscribing to this event.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Logger.Log("MyPlugin initialized.");
}
```

#### Python
```python
def On_PluginInit(self):
    Util.Log("MyPlugin initialized.")
```

#### JavaScript
```javascript
function On_PluginInit()
{
    Util.Log("MyPlugin initialized.");
}
```

#### Lua
```lua
function On_PluginInit()
    Util.Log("MyPlugin initialized.")
end
```
