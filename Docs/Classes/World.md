### Class
`Fougerite.World` / `Fougerite.Zone3D` / `Fougerite.NPC` / `Fougerite.CustomMap` / `Fougerite.CustomMapHandle` /
`Fougerite.CustomMapState`

### Description
`World` is the main singleton for everything that isn't a specific `Player`/`Entity`/`Sleeper`: spawning
objects/airdrops, terrain queries, and the Zone API (`Zone3D`) used to mark off areas of the map (safe zones,
raid-blocked zones, event arenas, etc.). `NPC` is the wrapper around wildlife (bears, wolves, boars, chickens,
...), analogous to `Entity`/`Sleeper` but for `Character`-based AI. `CustomMap`/`CustomMapHandle` is a
specialized, rarely-needed API that lets exactly one plugin replace the server's map with a custom scene from
its own Unity asset bundle at startup (used together with RustBuster's client-side map streaming).

### World
`World.GetWorld()` returns the singleton. Key members:
- Entity/NPC/Sleeper lookups (all cache-backed, **safe to call from any thread/timer**):
  - `Entities` - every tracked `Entity` ([`EntityCache`](Caches.md)).
  - `Doors` / `Resources` / `LootableObjects` / `SupplyCrates` - filtered subsets of `Entities`.
  - `Animals` - every `NPC` ([`NPCCache`](Caches.md)).
  - `Sleepers` - every `Sleeper` ([`SleeperCache`](Caches.md)).
  - `RangedEntities` *(obsolete)* - the old `Object.FindObjectsOfType` based lookup; avoid, it isn't
    thread-safe and can take hundreds of milliseconds on a busy map. Prefer `Entities` or
    `Util.FindEntitysAroundFast`/`Util.FindClosestEntity`.
- Spawning:
  - `SpawnEntity(string prefab, Vector3 location, Quaternion rotation = default, int rep = 1)` (+ overloads
    taking `float x, y, z`) - spawns `rep` copies of `prefab` and returns the (first) resulting `Entity`.
  - `Spawn(...)` - the older, untyped overloads returning `object` instead of `Entity`.
  - `SpawnAtPlayer(string prefab, Player p, int rep = 1)` - spawns at a player's location.
  - See [`Prefabs`](Prefabs.md) for a reference list of known `prefab` names (structures, deployables,
    resources, animals, loot crates).
- Airdrops:
  - `Airdrop()` / `Airdrop(int rep)` - calls a supply drop at a random location.
  - `AirdropAtOriginal(float x, float y, float z, int rep = 1)` / `AirdropAtOriginal(Player p, int rep = 1)` /
    `AirdropAtOriginal(Vector3 target, int rep = 1)` - calls a supply drop at a specific location.
  - `AirdropAt(...)` *(obsolete)* - use `AirdropAtOriginal` instead.
- Terrain:
  - `GetGround(float x, float z)` / `GetGround(Vector3 target)` - ground height at a column.
  - `GetTerrainHeight(...)` / `GetTerrainSteepness(...)` / `GetGroundDist(...)`.
  - `GetAllTreeInstances()` - every `TreeInstance` on the terrain.
- Structures:
  - `CreateSM(Player p, float x = ..., y = ..., z = ..., Quaternion rot = ...)` - creates a new
    `StructureMaster` (foundation) owned by `p`.
  - `BasicDoors(...)` / `DeployableObjects(...)` / `StructureComponents(...)` / `StructureMasters(...)` -
    cached `IEnumerable<Entity>` snapshots, refreshed on demand with `forceupdate: true`.
  - `ManuallySpawnSleeper(ulong steamID)` - forces a sleeping bag/bed owner's sleeper to spawn.
- Zones (see `Zone3D` below):
  - `CreateZone(string name)` - creates and registers a new `Zone3D`.
  - `Get(string name)` / `GetZones()` / `ContainsZone(string name)` - lookups.
  - `RemoveZone(string name)` / `RenameZone(string oldName, string newName)` / `ClearAllZones()`.
  - `GlobalContains(Entity e)` / `GlobalContains(Player p)` - the first zone containing the given
    entity/player, or `null`.
  - `GetZonesAt(Vector3 location)` / `FindClosestZone(Vector3 pos)`.
  - `GetPlayersInZone(string name)` / `GetEntitiesInZone(string name)` / `GetNPCsInZone(string name)` /
    `GetSleepersInZone(string name)`.
  - `IsNPCInZone(NPC npc, string zoneName)` / `IsSleeperInZone(Sleeper sleeper, string zoneName)`.
- `ServerSaveHandler` - the `ServerSaveHandler` instance (manages save/load scheduling).

### Zone3D
A 3D volumetric area: a 2D polygon footprint (`Points`, a `List<Vector2>` of X/Z corners) extruded between
`MinY`/`MaxY`. Internally pre-checks an axis-aligned bounding box before the more expensive point-in-polygon
(ray-casting) test, so `Contains` stays cheap even with many zones.
- `Mark(float x, float y)` / `Mark(Vector2 v)` - appends a corner to the polygon (`y` here is the Z coordinate).
- `Contains(Vector3 v)` / `Contains(Entity en)` / `Contains(Player p)` - point/entity/player-in-zone test.
- `Protected` (get/set) - purely a flag for *your* plugin to read in its own hurt/build hooks; Fougerite does
  not enforce it itself.
- `PVP` (get/set) - same idea, a flag your own hurt hooks are expected to check.
- `ShowMarkers()` / `HideMarkers()` - spawns/despawns metal pillar entities at each corner, for visually
  debugging a zone's shape in-game.
- `Entities` - convenience shortcut to `World.GetWorld().Entities` (every entity in the world, not just this
  zone - use `World.GetEntitiesInZone(name)` to filter).

### NPC
Wraps a wildlife `Character` (`HostileWildlifeAI`/`BasicWildLifeAI`):
- `Name` / `Location` / `X` / `Y` / `Z` / `InstanceID` / `IsAlive` / `Health` (get/set).
- `Character` - the underlying Rust `Character` component.
- `HostileWildlifeAI` / `BasicWildLifeAI` - the AI component (the latter is present on every NPC).
- `Kill()` - instantly kills the NPC.
- `Damage(float dmg)` - deals damage.
- Supports `==`/`!=`/`Equals`/`GetHashCode` (compares by name + instance ID).

### CustomMap / CustomMapHandle / CustomMapState
A niche, advanced API: lets exactly one plugin replace the `-map` level with a custom scene loaded from its own
Unity asset bundle (used together with RustBuster's client-side streamed-scene downloads so clients load the
same custom map). `CustomMap.GetInstance().Claim(name, timeoutSeconds)` (called from `Initialize()`) returns a
`CustomMapHandle` - the **first** plugin to claim wins, every later `Claim` call returns `null`. The handle's
owner must then call exactly one of `Commit(bundle, sceneName)`/`CommitFile(path, sceneName)`,
`Abandon(reason)` or `Fail(reason)` within the timeout, or the server shuts down. `CustomMapState` tracks the
lifecycle: `None` -> `Pending` -> (`Committed` -> `Loading` -> `Loaded`) or `Abandoned`/`Failed`.

### Example - C# (spawning a crate above every player every 10 minutes)
```csharp
public override void Initialize()
{
    Util.GetUtil().CreateSystemTimer("CrateDrop", 600000, null).Start();
}

public void CrateDropCallback(ATimedEvent e)
{
    foreach (Player player in Server.GetServer().GetAllPlayers())
    {
        World.GetWorld().AirdropAtOriginal(player, 1);
    }
}
```

### Example - C# (creating a PvE-safe zone around spawn)
```csharp
public override void Initialize()
{
    Zone3D zone = World.GetWorld().CreateZone("SafeZone");
    zone.Mark(-50f, -50f);
    zone.Mark(50f, -50f);
    zone.Mark(50f, 50f);
    zone.Mark(-50f, 50f);
    zone.MinY = -100f;
    zone.MaxY = 100f;
    zone.PVP = false;
}

public void OnHurt(HurtEvent e)
{
    if (e.VictimIsPlayer && e.AttackerIsPlayer)
    {
        Zone3D zone = World.GetWorld().GlobalContains((Player)e.Victim);
        if (zone != null && !zone.PVP)
        {
            e.DamageAmount = 0f;
        }
    }
}
```

### Example - C# (counting wildlife in a zone every minute)
```csharp
public void AnimalScanCallback(ATimedEvent e)
{
    List<NPC> animals = World.GetWorld().GetNPCsInZone("SafeZone");
    Logger.Log($"{animals.Count} animals are roaming inside SafeZone.");
}
```

See also: [`Entity`](Entity.md) · [`Sleeper`](Sleeper.md) · [`Caches`](Caches.md) · [`Util`](Util.md) ·
[`Prefabs`](Prefabs.md)
