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
    **Only use this for prefabs that resolve to one of `Entity`'s supported components**
    (`StructureComponent`/`LootableObject`/`SupplyCrate`/`ResourceTarget`/`DeployableObject`) - structures,
    deployables, resource nodes, loot crates. Animal prefabs (`Character`-based) aren't one of those, so
    `SpawnEntity(";stag_prefab", ...)` won't give you a usable NPC wrapper.
    **Ownership note:** `SpawnEntity` does **not** assign a real owner by itself.
    - Deployables (`;deploy_...` prefabs) are spawned with `ownerID`/`creatorID` left at `0` (unowned). This
      still works - the entity spawns and functions fine - but it's always recommended to assign a real
      owner right after spawning (see the example below), since a lot of gameplay/plugin logic (door/box
      auth, raid/ownership logs, "who placed this" checks) is owner-based.
    - Structures (`;struct_...` prefabs) are spawned as a bare `StructureComponent`, **not** attached to any
      `StructureMaster`. This also still works on its own (no crash/error for normal use), but decay,
      weight-linking between pieces, and proper save/load behavior all rely on the component belonging to a
      `StructureMaster` - see `CreateSM(...)` below and `StructureMaster.AddStructureComponent(...)`. If
      you're spawning structures (walls, foundations, etc.), it's recommended to create/find a
      `StructureMaster` and attach every spawned component to it.
  - `SpawnAnimal(string prefab, Vector3 location, Quaternion rotation = default, int rep = 1)` (+ overload
    taking just `Vector3 location, int rep = 1`) - spawns `rep` copies of an animal/NPC prefab (e.g.
    `:stag_prefab`, `:wolf_prefab`, `:bear_prefab`, `:chicken_prefab`, see [`Prefabs`](Prefabs.md#animals-npc)) and
    returns the (first) resulting [`NPC`](#npc) instead of `Entity`. Internally this does exactly what the
    game itself does when wildlife naturally spawns (registers the `Character` into [`NPCCache`](Caches.md),
    same as the `AddWildlifeInstance` hook), so the returned `NPC` immediately shows up in `World.Animals`/
    `GetNPCsInZone` too.
  - `SpawnAnimalAtPlayer(string prefab, Player p, int rep = 1)` - shortcut for `SpawnAnimal` at a player's
    location/rotation.
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

### Example - C# (spawning a stag at the player with a command)
A minimal `/spawnstag` command demonstrating `SpawnAnimal`/`SpawnAnimalAtPlayer`. Note it returns an `NPC`,
not an `Entity` - don't use `SpawnEntity` for animal prefabs, since `Character`-based animals aren't one of
the component types `Entity` wraps:
```csharp
public override void Initialize()
{
    Hooks.OnCommand += HandleCommand;
}

public override void DeInitialize()
{
    Hooks.OnCommand -= HandleCommand;
}

public void HandleCommand(Fougerite.Player player, string cmd, string[] args)
{
    if (cmd != "spawnstag")
    {
        return;
    }

    NPC npc = World.GetWorld().SpawnAnimalAtPlayer(";stag_prefab", player);
    if (npc != null)
    {
        player.Message($"Spawned a stag ({npc.Name}, InstanceID {npc.InstanceID}) at your location!");
    }
    else
    {
        player.Message("Failed to spawn the stag.");
    }
}
```
Same idea in Python (see [`PythonPluginTutorial.md`](../PythonPluginTutorial.md)):
```python
def On_Command(self, Player, cmd, args):
    if cmd == "spawnstag":
        npc = World.SpawnAnimalAtPlayer(";stag_prefab", Player)
        if npc is not None:
            Player.Message("Spawned a stag (" + npc.Name + ")!")
        else:
            Player.Message("Failed to spawn the stag.")
```

### Example - C# (spawning deployables/structures with a proper owner)
As noted above, `SpawnEntity` does not assign a real owner by itself. Deployables spawn unowned
(`ownerID`/`creatorID == 0`), and structures spawn without a `StructureMaster`. Both will still work
unowned/masterless, but it's always recommended to assign a real owner - most gameplay/plugin logic (door/box
auth, raid logs, "who placed this") is owner-based, and structures need a `StructureMaster` for decay/saving
to behave correctly:
```csharp
public void SpawnOwnedBoxCommand(Fougerite.Player player)
{
    // Deployables: SpawnEntity leaves ownerID/creatorID at 0. Entity.ChangeOwner(Player) fixes that up
    // for you (it internally calls DeployableObject.SetupCreator + CacheCreator/CreatorSet).
    Entity box = World.GetWorld().SpawnEntity(";deploy_wood_box", player.Location, player.PlayerClient.transform.rotation);
    if (box != null)
    {
        box.ChangeOwner(player);
        player.Message("Spawned a wood box, owned by you.");
    }
}

public void SpawnOwnedWallCommand(Fougerite.Player player)
{
    // Structures: SpawnEntity returns a bare StructureComponent with no StructureMaster. Create (or find a
    // nearby) one first and attach the component to it, THEN assign the owner - Entity.ChangeOwner only
    // works on structures that already have a master (it walks GetLinkedStructs()/IsStructure()).
    StructureMaster master = World.GetWorld().CreateSM(player);
    Entity wall = World.GetWorld().SpawnEntity(";struct_wood_wall", player.Location, player.PlayerClient.transform.rotation);
    if (wall != null && wall.GetObject(out StructureComponent comp))
    {
        master.AddStructureComponent(comp);
        wall.ChangeOwner(player);
        player.Message("Spawned a wood wall, attached to a new StructureMaster and owned by you.");
    }
}
```

See also: [`Entity`](Entity.md) · [`Sleeper`](Sleeper.md) · [`Caches`](Caches.md) · [`Util`](Util.md) ·
[`Prefabs`](Prefabs.md)
