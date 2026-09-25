### Method
`On_ServerShutdown`

### Description
Runs when the server is stopping/shutting down. Use this to save plugin data before the process exits.

### C# Event
```csharp
public static event ServerShutdownDelegate OnServerShutdown;
public delegate void ServerShutdownDelegate();
```

### Argument(s)
None. For C# plugins, use the `DeInitialize()` override for cleanup logic instead of/in addition to this.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnServerShutdown += ShutdownHandler;
}

public void ShutdownHandler()
{
    Logger.Log("Server is shutting down. Saving data...");
}
```

#### Python
```python
def On_ServerShutdown(self):
    Server.Log("Server is shutting down. Saving data...")
```

#### JavaScript
```javascript
function On_ServerShutdown()
{
    Server.Log("Server is shutting down. Saving data...");
}
```

#### Lua
```lua
function On_ServerShutdown()
    Server.Log("Server is shutting down. Saving data...")
end
```
