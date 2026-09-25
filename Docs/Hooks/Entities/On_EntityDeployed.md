### Method
`On_EntityDeployed`

### Description
Runs when an entity (deployable) is placed on the ground by a player.

> Note: `OnEntityDeployed` (with just `Player` and `Entity`) is obsolete. Use `OnEntityDeployedWithPlacer`
> internally in C# (still exposed to scripts as `On_EntityDeployed`) which additionally exposes the actual
> placer, useful when a deployable is placed via a proxy (e.g. an admin tool).

### C# Event
```csharp
[Obsolete("Use OnEntityDeployedWithPlacer", false)]
public static event EntityDeployedDelegate OnEntityDeployed;
public delegate void EntityDeployedDelegate(Player player, Entity e);

public static event EntityDeployedWithPlacerDelegate OnEntityDeployedWithPlacer;
public delegate void EntityDeployedWithPlacerDelegate(Player player, Entity e, Player actualplacer);
```

### Argument(s)
- `Player Player` - The owner of the entity.
- `Entity Entity` - The entity that was placed.
- `Player ActualPlacer` *(WithPlacer variant only)* - The player who physically placed the entity.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnEntityDeployedWithPlacer += DeployedHandler;
}

public override void DeInitialize()
{
    Hooks.OnEntityDeployedWithPlacer -= DeployedHandler;
}

public void DeployedHandler(Fougerite.Player player, Entity e, Fougerite.Player actualPlacer)
{
    Logger.Log(player.Name + " placed a " + e.Name);
}
```

#### Python
```python
def On_EntityDeployed(self, Player, Entity):
    Server.Log(Player.Name + " placed a " + Entity.Name)
```

#### JavaScript
```javascript
function On_EntityDeployed(Player, Entity)
{
    Server.Log(Player.Name + " placed a " + Entity.Name);
}
```

#### Lua
```lua
function On_EntityDeployed(Player, Entity)
    Server.Log(Player.Name .. " placed a " .. Entity.Name)
end
```
