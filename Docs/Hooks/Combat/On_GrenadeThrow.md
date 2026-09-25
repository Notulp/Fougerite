### Method
`On_GrenadeThrow`

### Description
Runs when a player throws a grenade.

### C# Event
```csharp
public static event GrenadeThrowEventDelegate OnGrenadeThrow;
public delegate void GrenadeThrowEventDelegate(GrenadeThrowEvent grenadeThrowEvent);
```

### Argument(s)
- `GrenadeThrowEvent GrenadeThrowEvent`

### Properties/Methods
- `GrenadeThrowEvent.Player` - The player throwing.
- `GrenadeThrowEvent.HandGrenadeDataBlock` - The grenade's datablock.
- `GrenadeThrowEvent.IHandGrenadeItem` - The grenade item instance.
- `GrenadeThrowEvent.GameObject` - The spawned grenade `GameObject`.
- `GrenadeThrowEvent.ItemRepresentation` / `NetworkMessageInfo` - Raw network info.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnGrenadeThrow += GrenadeThrowHandler;
}

public override void DeInitialize()
{
    Hooks.OnGrenadeThrow -= GrenadeThrowHandler;
}

public void GrenadeThrowHandler(GrenadeThrowEvent e)
{
    Logger.Log(e.Player.Name + " threw a grenade.");
}
```

#### Python
```python
def On_GrenadeThrow(self, GrenadeThrowEvent):
    Server.Log(GrenadeThrowEvent.Player.Name + " threw a grenade.")
```

#### JavaScript
```javascript
function On_GrenadeThrow(GrenadeThrowEvent)
{
    Server.Log(GrenadeThrowEvent.Player.Name + " threw a grenade.");
}
```

#### Lua
```lua
function On_GrenadeThrow(GrenadeThrowEvent)
    Server.Log(GrenadeThrowEvent.Player.Name .. " threw a grenade.")
end
```
