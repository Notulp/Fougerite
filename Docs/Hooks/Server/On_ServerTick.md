### Method
`On_ServerTick`

### Description
Runs on every single server tick. Use with extreme caution: it can be called multiple times per second and
may cause performance issues if used improperly. It only runs when the server is fully initialized and not
shutting down.

### C# Event
```csharp
public static event ServerTickDelegate OnServerTick;
public delegate void ServerTickDelegate();
```

### Argument(s)
None.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnServerTick += TickHandler;
}

public override void DeInitialize()
{
    Hooks.OnServerTick -= TickHandler;
}

public void TickHandler()
{
    // Extremely hot path - keep this as light as possible!
}
```

#### Python
```python
def On_ServerTick(self):
    pass  # Extremely hot path - keep this as light as possible!
```

#### JavaScript
```javascript
function On_ServerTick()
{
    // Extremely hot path - keep this as light as possible!
}
```

#### Lua
```lua
function On_ServerTick()
    -- Extremely hot path - keep this as light as possible!
end
```
