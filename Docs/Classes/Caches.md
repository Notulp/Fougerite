### Class
`Fougerite.Caches.EntityCache` / `Fougerite.Caches.NPCCache` / `Fougerite.Caches.SleeperCache` /
`Fougerite.Caches.PlayerCache` / `Fougerite.Caches.CachedPlayer`

### Description
Fougerite maintains a handful of thread-safe singleton caches so plugins (and Fougerite itself) never have to
call Unity's `Object.FindObjectsOfType` to enumerate entities/NPCs/sleepers - that call is not thread-safe and
gets extremely slow on large maps (~0.8s for 80k objects). `EntityCache`, `NPCCache` and `SleeperCache` are
populated automatically by `Hooks` as objects spawn/die and expose read-only snapshot methods that are safe to
call from a [System Timer](Timers.md) or any other background thread.

`PlayerCache` is unrelated to the above: it's a small JSON-backed history store (`Save/CachedPlayers.json`) of
every player that has ever connected, so you can still look up a player's last known name/IP/aliases even if
they are not currently online (e.g. to resolve the owner name of an entity after a server restart).

### EntityCache / NPCCache / SleeperCache
All three classes share the exact same shape (only the Sleeper name shown below differs per class):
- `GetInstance()` - static method returning the singleton.
- `GetEntities()` / `GetNPCs()` / `GetSleepers()` - returns a **shallow copy** `List<T>` snapshot of everything
  currently tracked. Safe to call and iterate from any thread.
- `GetEntityByInstanceId(int instanceId)` - returns the matching `Entity`/`NPC`/`Sleeper`, or `null` if it
  isn't cached.

> Adding/removing entries (`Add`/`Remove`/`Contains`/`GrabOrAllocate`) is `internal` - it's driven by `Hooks`
> when objects are instantiated/destroyed, plugins only ever read from these caches.

### PlayerCache
- `GetPlayerCache()` - static method returning the singleton.
- `CachedPlayers` - the underlying `ConcurrentDictionary<ulong, CachedPlayer>` (SteamID -> `CachedPlayer`).
- `GetPlayerBySteamId(ulong steamId)` / `GetPlayerBySteamId(string steamIdStr)` - exact lookup by SteamID.
- `GetPlayerByName(string name)` / `GetPlayerByAlias(string alias)` / `GetPlayerByIP(string ip)` - first match,
  case-insensitive.
- `GetPlayersByName(string name)` / `GetPlayersByAlias(string alias)` / `GetPlayersByIP(string ip)` - **all**
  matches, case-insensitive. Useful to spot ban evaders/alt accounts sharing a name, alias or IP.
- `GetPlayersByNameContains(string namePart)` / `GetPlayersByAliasContains(string aliasPart)` - partial,
  case-insensitive search.
- `SaveToDisk()` - persists the current dictionary to `Save/CachedPlayers.json` immediately (normally handled
  automatically by Fougerite on save/shutdown).

### CachedPlayer
Plain data object stored per SteamID inside `PlayerCache.CachedPlayers`:
- `Name` - the player's current/last known name.
- `Aliases` - `List<string>` of every name this SteamID has ever used.
- `IPAddresses` - `List<string>` of every IP this SteamID has connected from.
- `LastLogin` / `LastLogout` - nullable UTC `DateTime` of the last connect/disconnect.

### Example - C# (iterating entities from a background timer)
```csharp
public override void Initialize()
{
    Plugin.CreateParallelTimer("ScanEntities", 5000, null).Start();
}

public void ScanEntitiesCallback(ATimedEvent e)
{
    // Safe off the main thread: this is a snapshot copy, no Unity calls happen here.
    List<Entity> entities = EntityCache.GetInstance().GetEntities();
    Logger.Log($"Tracking {entities.Count} entities.");
}
```

### Example - C# (resolving an offline owner's name)
```csharp
public void OnEntityDestroyed(Entity entity)
{
    CachedPlayer owner = PlayerCache.GetPlayerCache().GetPlayerBySteamId(entity.UOwnerID);
    string ownerName = owner != null ? owner.Name : "Unknown";
    Logger.Log($"{entity.Name} belonging to {ownerName} was destroyed.");
}
```

### Example - Python (finding alt accounts by IP)
```python
def On_PlayerConnected(self, Player):
    matches = PlayerCache.GetPlayerCache().GetPlayersByIP(Player.IP)
    if len(matches) > 1:
        Util.Log(Player.Name + " shares an IP with " + str(len(matches) - 1) + " other known account(s).")
```

See also: [`Util`](Util.md) · [`Timers`](Timers.md) · [`Entity`](Entity.md) · [`Sleeper`](Sleeper.md)
