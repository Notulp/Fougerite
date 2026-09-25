### Method
`On_FireBarrelToggle`

### Description
Runs when a fire barrel is turned on or off.

### C# Event
```csharp
public static event FireBarrelToggleEventDelegate OnFireBarrelToggle;
public delegate void FireBarrelToggleEventDelegate(FireBarrelToggleEvent fbte);
```

### Argument(s)
- `FireBarrelToggleEvent FireBarrelToggleEvent`

### Properties/Methods
- `FireBarrelToggleEvent.FireBarrel` - The `FireBarrel` component.
- `FireBarrelToggleEvent.Entity` - The fire barrel entity.
- `FireBarrelToggleEvent.On` - `true` if being turned on, `false` if off.
- `FireBarrelToggleEvent.CookDuration` - How long the barrel will cook/burn for.
- `FireBarrelToggleEvent.LastUser` - The last player who interacted with it.
- `FireBarrelToggleEvent.Cancelled` - Whether the toggle is cancelled.
- `FireBarrelToggleEvent.Cancel()` - Cancels the toggle.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnFireBarrelToggle += FireBarrelHandler;
}

public override void DeInitialize()
{
    Hooks.OnFireBarrelToggle -= FireBarrelHandler;
}

public void FireBarrelHandler(FireBarrelToggleEvent e)
{
    Logger.Log("Fire barrel toggled: " + e.On);
}
```

#### Python
```python
def On_FireBarrelToggle(self, FireBarrelToggleEvent):
    Util.Log("Fire barrel toggled: " + str(FireBarrelToggleEvent.On))
```

#### JavaScript
```javascript
function On_FireBarrelToggle(FireBarrelToggleEvent)
{
    Util.Log("Fire barrel toggled: " + FireBarrelToggleEvent.On);
}
```

#### Lua
```lua
function On_FireBarrelToggle(FireBarrelToggleEvent)
    Util.Log("Fire barrel toggled: " .. tostring(FireBarrelToggleEvent.On))
end
```
