### Method
`On_PlayerSpawned`

### Description
Runs right after a player has spawned into the world.

### C# Event
```csharp
public static event PlayerSpawnHandlerDelegate OnPlayerSpawned;
public delegate void PlayerSpawnHandlerDelegate(Player player, SpawnEvent se);
```

### Argument(s)
- `Player Player` - The player who spawned.
- `SpawnEvent SpawnEvent` - See [`On_PlayerSpawning`](On_PlayerSpawning.md) for its properties.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerSpawned += SpawnedHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerSpawned -= SpawnedHandler;
}

public void SpawnedHandler(Fougerite.Player player, SpawnEvent se)
{
    player.Notice("", "Welcome, " + player.Name + "!", 4f);
}
```

#### Python
```python
def On_PlayerSpawned(self, Player, SpawnEvent):
    Player.Message("Welcome, " + Player.Name + "!")
```

#### JavaScript
```javascript
function On_PlayerSpawned(Player, SpawnEvent)
{
    Player.Message("Welcome, " + Player.Name + "!");
}
```

#### Lua
```lua
function On_PlayerSpawned(Player, SpawnEvent)
    Player.Message("Welcome, " .. Player.Name .. "!")
end
```
