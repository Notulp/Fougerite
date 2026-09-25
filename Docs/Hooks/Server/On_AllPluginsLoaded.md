### Method
`On_AllPluginsLoaded`

### Description
Runs once, when all plugins have been successfully loaded for the first time (after server startup).

### C# Event
```csharp
public static event AllPluginsLoadedDelegate OnAllPluginsLoaded;
public delegate void AllPluginsLoadedDelegate();
```

### Argument(s)
None.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnAllPluginsLoaded += AllLoadedHandler;
}

public override void DeInitialize()
{
    Hooks.OnAllPluginsLoaded -= AllLoadedHandler;
}

public void AllLoadedHandler()
{
    Logger.Log("All plugins have loaded.");
}
```

#### Python
```python
def On_AllPluginsLoaded(self):
    Server.Log("All plugins have loaded.")
```

#### JavaScript
```javascript
function On_AllPluginsLoaded()
{
    Server.Log("All plugins have loaded.");
}
```

#### Lua
```lua
function On_AllPluginsLoaded()
    Server.Log("All plugins have loaded.")
end
```
