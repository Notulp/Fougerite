### Method
`On_BloodDraw`

### Description
Runs when a player uses a Blood Draw Kit.

### C# Event
```csharp
public static event BloodDrawUseEventDelegate OnBloodDraw;
public delegate void BloodDrawUseEventDelegate(BloodDrawEvent be);
```

### Argument(s)
- `BloodDrawEvent BloodDrawEvent`

### Properties/Methods
- `BloodDrawEvent.Player` - The player using the kit.
- `BloodDrawEvent.Item` - The `IBloodDrawItem` being used.
- `BloodDrawEvent.ItemAmount` - The amount of items being used.
- `BloodDrawEvent.BloodToTake` - The amount of blood to take. Can be modified.
- `BloodDrawEvent.Cancelled` - Whether the action is cancelled.
- `BloodDrawEvent.Cancel()` - Cancels the blood draw.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnBloodDraw += BloodDrawHandler;
}

public override void DeInitialize()
{
    Hooks.OnBloodDraw -= BloodDrawHandler;
}

public void BloodDrawHandler(BloodDrawEvent e)
{
    Logger.Log(e.Player.Name + " drew blood: " + e.BloodToTake);
}
```

#### Python
```python
def On_BloodDraw(self, BloodDrawEvent):
    Server.Log(BloodDrawEvent.Player.Name + " drew blood: " + str(BloodDrawEvent.BloodToTake))
```

#### JavaScript
```javascript
function On_BloodDraw(BloodDrawEvent)
{
    Server.Log(BloodDrawEvent.Player.Name + " drew blood: " + BloodDrawEvent.BloodToTake);
}
```

#### Lua
```lua
function On_BloodDraw(BloodDrawEvent)
    Server.Log(BloodDrawEvent.Player.Name .. " drew blood: " .. tostring(BloodDrawEvent.BloodToTake))
end
```
