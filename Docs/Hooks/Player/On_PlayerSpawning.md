### Method
`On_PlayerSpawning`

### Description
Runs right before a player spawns. Allows overriding the spawn location.

### C# Event
```csharp
public static event PlayerSpawnHandlerDelegate OnPlayerSpawning;
public delegate void PlayerSpawnHandlerDelegate(Player player, SpawnEvent se);
```

### Argument(s)
- `Player Player` - The player about to spawn.
- `SpawnEvent SpawnEvent`

### Properties/Methods
- `SpawnEvent.Location` - The `Vector3` spawn position. Can be changed to override the spawn point.
- `SpawnEvent.X` / `SpawnEvent.Y` / `SpawnEvent.Z` - Shortcut accessors for the location coordinates.
- `SpawnEvent.CampUsed` - Whether the player used a sleeping bag/bed (camp) to spawn.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerSpawning += SpawningHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerSpawning -= SpawningHandler;
}

public void SpawningHandler(Fougerite.Player player, SpawnEvent se)
{
    se.Location = new UnityEngine.Vector3(0, 100, 0);
}
```

#### Python
```python
import UnityEngine

def On_PlayerSpawning(self, Player, SpawnEvent):
    SpawnEvent.Location = UnityEngine.Vector3(0, 100, 0)
```

#### JavaScript
```javascript
function On_PlayerSpawning(Player, SpawnEvent)
{
    SpawnEvent.Location = new UnityEngine.Vector3(0, 100, 0);
}
```

#### Lua
```lua
function On_PlayerSpawning(Player, SpawnEvent)
    SpawnEvent.Location = UnityEngine.Vector3(0, 100, 0)
end
```
