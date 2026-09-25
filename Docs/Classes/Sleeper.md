### Class
`Fougerite.Sleeper`

### Description
Wraps a sleeping bag / bed and its associated sleeping avatar (the "sleeping body" a player leaves behind
when they disconnect or die while under the effects of sleep). You receive a `Sleeper` from
`On_SleeperSpawned`, `Player.Sleeper`, or `Server.GetServer().Sleepers` / `SleeperCache` (script global).

### Properties
- `Name` - The sleeper's Rust name (e.g. `"Sleeping Bag"`, `"Bed"`).
- `Object` - The underlying `DeployableObject`.
- `SleepingAvatar` - The underlying `SleepingAvatar` component (the sleeping player's body).
- `Health` - Current health/durability.
- `Location` / `X` / `Y` / `Z` - World position.
- `OwnerName` / `OwnerID` / `UOwnerID` - The player who owns this bag/bed.
- `SteamID` / `UID` - The owning player's SteamID.
- `InstanceID` - Unity instance ID.
- `IsDestroyed` - Set to `true` once destroyed.

### Methods
- `UpdateHealth()` - Forces a health/durability refresh.
- `Destroy()` - Destroys the sleeper (removes the bag/bed and body).

### Example - C#
```csharp
public override void Initialize()
{
    Hooks.OnSleeperSpawned += SleeperSpawnedHandler;
}

public void SleeperSpawnedHandler(Sleeper sleeper)
{
    Logger.Log($"Sleeper spawned for {sleeper.OwnerName} at {sleeper.Location}");
}
```

### Example - Python
```python
def On_SleeperSpawned(self, Sleeper):
    Util.Log("Sleeper spawned for " + Sleeper.OwnerName + " at " + str(Sleeper.Location))
```

See also: [`Player.Sleeper`](Player.md) · [`Server.Sleepers`](Server.md)
