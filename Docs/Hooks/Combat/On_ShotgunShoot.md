### Method
`On_ShotgunShoot`

### Description
Runs when a player fires a shotgun (once per pellet-batch event).

### C# Event
```csharp
public static event ShotgunShootEventDelegate OnShotgunShoot;
public delegate void ShotgunShootEventDelegate(ShotgunShootEvent shootEvent);
```

### Argument(s)
- `ShotgunShootEvent ShotgunShootEvent` - Has all of [`On_Shoot`](On_Shoot.md)'s properties, plus:

### Properties/Methods
- `ShotgunShootEvent.Pellets` - The number of pellets fired.
- `ShotgunShootEvent.SetPellets(int pellets)` - Changes the pellet count.
- `ShotgunShootEvent.ShotgunDataBlock` - The shotgun's datablock.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnShotgunShoot += ShotgunHandler;
}

public override void DeInitialize()
{
    Hooks.OnShotgunShoot -= ShotgunHandler;
}

public void ShotgunHandler(ShotgunShootEvent e)
{
    e.SetPellets(e.Pellets * 2); // Double pellet count
}
```

#### Python
```python
def On_ShotgunShoot(self, ShotgunShootEvent):
    ShotgunShootEvent.SetPellets(ShotgunShootEvent.Pellets * 2)
```

#### JavaScript
```javascript
function On_ShotgunShoot(ShotgunShootEvent)
{
    ShotgunShootEvent.SetPellets(ShotgunShootEvent.Pellets * 2);
}
```

#### Lua
```lua
function On_ShotgunShoot(ShotgunShootEvent)
    ShotgunShootEvent.SetPellets(ShotgunShootEvent.Pellets * 2)
end
```
