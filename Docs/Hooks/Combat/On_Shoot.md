### Method
`On_Shoot`

### Description
Runs when a player fires a bullet weapon.

### C# Event
```csharp
public static event ShootEventDelegate OnShoot;
public delegate void ShootEventDelegate(ShootEvent shootEvent);
```

### Argument(s)
- `ShootEvent ShootEvent`

### Properties/Methods
- `ShootEvent.Player` - The player shooting.
- `ShootEvent.BulletWeaponDataBlock` - The weapon's datablock.
- `ShootEvent.IBulletWeaponItem` - The weapon item instance.
- `ShootEvent.HitNetworkObject` - Whether the shot hit a networked object (player/entity).
- `ShootEvent.HitBodyPart` - Whether it hit a specific body part.
- `ShootEvent.IsHeadShot` - Whether it was a headshot.
- `ShootEvent.Bodypart` - The `BodyPart` hit.
- `ShootEvent.Part` - The `IDRemoteBodyPart` hit.
- `ShootEvent.EndPos` / `Offset` - Trajectory info.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnShoot += ShootHandler;
}

public override void DeInitialize()
{
    Hooks.OnShoot -= ShootHandler;
}

public void ShootHandler(ShootEvent e)
{
    if (e.IsHeadShot)
    {
        Server.GetServer().Broadcast(e.Player.Name + " got a headshot!");
    }
}
```

#### Python
```python
def On_Shoot(self, ShootEvent):
    if ShootEvent.IsHeadShot:
        Server.Broadcast(ShootEvent.Player.Name + " got a headshot!")
```

#### JavaScript
```javascript
function On_Shoot(ShootEvent)
{
    if (ShootEvent.IsHeadShot)
    {
        Server.Broadcast(ShootEvent.Player.Name + " got a headshot!");
    }
}
```

#### Lua
```lua
function On_Shoot(ShootEvent)
    if ShootEvent.IsHeadShot then
        Server.Broadcast(ShootEvent.Player.Name .. " got a headshot!")
    end
end
```
