### Method
`On_FlareThrow`

### Description
Runs when a player throws a flare.

### C# Event
```csharp
public static event FlareThrowEventDelegate OnFlareThrow;
public delegate void FlareThrowEventDelegate(FlareThrowEvent fe);
```

### Argument(s)
- `FlareThrowEvent FlareThrowEvent`

### Properties/Methods
- `FlareThrowEvent.Player` - The player throwing the flare.
- `FlareThrowEvent.Item` - The `ITorchItem` (flare) being thrown.
- `FlareThrowEvent.Instance` - The `TorchItemDataBlock`.
- `FlareThrowEvent.Origin` / `Forward` - Throw origin and direction.
- `FlareThrowEvent.Cancelled` - Whether the throw is cancelled.
- `FlareThrowEvent.Cancel()` - Cancels the throw.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnFlareThrow += FlareThrowHandler;
}

public override void DeInitialize()
{
    Hooks.OnFlareThrow -= FlareThrowHandler;
}

public void FlareThrowHandler(FlareThrowEvent e)
{
    Logger.Log(e.Player.Name + " threw a flare.");
}
```

#### Python
```python
def On_FlareThrow(self, FlareThrowEvent):
    Server.Log(FlareThrowEvent.Player.Name + " threw a flare.")
```

#### JavaScript
```javascript
function On_FlareThrow(FlareThrowEvent)
{
    Server.Log(FlareThrowEvent.Player.Name + " threw a flare.");
}
```

#### Lua
```lua
function On_FlareThrow(FlareThrowEvent)
    Server.Log(FlareThrowEvent.Player.Name .. " threw a flare.")
end
```
