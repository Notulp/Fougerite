### Class
`Fougerite.Entity`

### Description
A generic wrapper around any placeable/lootable world object: structures (walls, foundations, floors),
deployables (furnaces, workbenches, doors...), storage containers, campfires/fire barrels, resource
targets, and supply crates. You get `Entity` instances from hooks (`On_EntityHurt`, `On_EntityDestroyed`,
`On_EntityDeployed`, `On_DoorUse`, ...), from `Player.Structures`/`Deployables`/`Shelters`/`Storage`/`Fires`,
or from `World` (`World.Entities`, `World.StructureComponents()`, `World.DeployableObjects()`,
`World.BasicDoors()`, `World.LootableObjects`, `World.SupplyCrates`).

> `World`'s entity-lookup methods internally use `UnityEngine.Object.FindObjectsOfType`, which is **only
> safe to call from the main thread**. Only call them directly from a Normal Timer/hook, or wrap the call
> with `Loom.QueueOnMainThread` if you're inside a System Timer. See [Timers](Timers.md) for details.

### Properties
- `Name` - The entity's Rust name (e.g. `"Wooden Wall"`, `"Furnace"`).
- `Object` - The underlying Rust game object (`DeployableObject`, `StructureComponent`, etc).
- `Owner` - The [`Player`](Player.md) who owns the entity (if known/online).
- `OwnerName` / `OwnerID` / `UOwnerID` - Owner info even if the owner isn't currently a resolved `Player`.
- `Creator` / `CreatorName` / `CreatorID` / `UCreatorID` - Who originally placed/created it (may differ from
  the current owner after an ownership change).
- `Health` / `MaxHealth` - Current/maximum durability.
- `Location` / `X` / `Y` / `Z` - World position.
- `Rotation` - World orientation.
- `Inventory` - `EntityInv` wrapper, if the entity has an inventory (see `HasInventory`).
- `HasInventory` / `hasInventory` - Whether the entity has an inventory slot.
- `InstanceID` - Unity instance ID, useful as a stable dictionary key.
- `IsDestroyed` - Set to `true` once the entity has been destroyed.
- `ResourceTarget` - Set if this entity is a resource node (tree/ore); see `IsResourceTarget()`.
- `SupplyCrate` - Set if this entity is a supply crate; see `IsSupplyCrate()`.

### Methods
- `ChangeOwner(Player p)` / `ChangeOwner(ulong steamId)` - Transfers ownership.
- `Destroy()` - Destroys the entity (fires `On_EntityDestroyed`).
- `GetLinkedStructs()` - Returns structures physically linked/snapped to this one.
- `GetTakeDamage()` - Returns the underlying `TakeDamage` component.
- `SetDecayEnabled(bool c)` - Enables/disables decay for this entity.
- `UpdateHealth()` - Forces a health/durability refresh.
- `IsResourceTarget()`, `IsBasicDoor()`, `IsLootableObject()`, `IsDeployableObject()`, `IsStorage()`,
  `IsStructure()`, `IsStructureMaster()`, `IsSleeper()`, `IsFireBarrel()`, `IsSupplyCrate()` - Type checks,
  useful since a single `Entity` wrapper is reused for many underlying Rust types.

### Example - C#
```csharp
public void EntityHurtHandler(HurtEvent he)
{
    Entity entity = he.Victim as Entity;
    if (entity != null && entity.IsStructure())
    {
        Logger.Log($"{entity.Name} owned by {entity.OwnerName} took damage, health: {entity.Health}");
    }
}
```

### Example - Python
```python
def On_EntityHurt(self, HurtEvent):
    Entity = HurtEvent.Victim
    if Entity.IsStructure():
        Server.Log(Entity.Name + " owned by " + Entity.OwnerName + " took damage, health: " + str(Entity.Health))
```

See also: [`Player`](Player.md) (owns arrays of `Entity`) · [`Sleeper`](Sleeper.md)
