### Method
`On_BowShoot`

### Description
Runs when a player fires a bow.

### C# Event
```csharp
public static event BowShootEventDelegate OnBowShoot;
public delegate void BowShootEventDelegate(BowShootEvent bowshootEvent);
```

### Argument(s)
- `BowShootEvent BowShootEvent`

### Properties/Methods
- `BowShootEvent.Player` - The player shooting.
- `BowShootEvent.BowWeaponDataBlock` - The bow's datablock.
- `BowShootEvent.IBowWeaponItem` - The bow item instance.
- `BowShootEvent.ItemRepresentation` / `NetworkMessageInfo` - Raw network info.
- `BowShootEvent.RemoveArrow()` - Removes an arrow from the player's inventory.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnBowShoot += BowShootHandler;
}

public override void DeInitialize()
{
    Hooks.OnBowShoot -= BowShootHandler;
}

public void BowShootHandler(BowShootEvent e)
{
    Logger.Log(e.Player.Name + " shot a bow.");
}
```

#### Python
```python
def On_BowShoot(self, BowShootEvent):
    Server.Log(BowShootEvent.Player.Name + " shot a bow.")
```

#### JavaScript
```javascript
function On_BowShoot(BowShootEvent)
{
    Server.Log(BowShootEvent.Player.Name + " shot a bow.");
}
```

#### Lua
```lua
function On_BowShoot(BowShootEvent)
    Server.Log(BowShootEvent.Player.Name .. " shot a bow.")
end
```
