### Method
`On_PlayerTeleport`

### Description
Runs when a player is teleported using the Fougerite `Player.Teleport()` API (not raw position sets).

### C# Event
```csharp
public static event TeleportDelegate OnPlayerTeleport;
public delegate void TeleportDelegate(Player player, Vector3 from, Vector3 dest);
```

### Argument(s)
- `Player Player` - The player being teleported.
- `Vector3 From` - The origin position.
- `Vector3 Dest` - The destination position.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerTeleport += TeleportHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerTeleport -= TeleportHandler;
}

public void TeleportHandler(Fougerite.Player player, Vector3 from, Vector3 dest)
{
    Logger.Log(player.Name + " teleported from " + from + " to " + dest);
}
```

#### Python
```python
def On_PlayerTeleport(self, Player, From, Dest):
    Util.Log(Player.Name + " teleported from " + str(From) + " to " + str(Dest))
```

#### JavaScript
```javascript
function On_PlayerTeleport(Player, From, Dest)
{
    Util.Log(Player.Name + " teleported from " + From + " to " + Dest);
}
```

#### Lua
```lua
function On_PlayerTeleport(Player, From, Dest)
    Util.Log(Player.Name .. " teleported from " .. tostring(From) .. " to " .. tostring(Dest))
end
```
