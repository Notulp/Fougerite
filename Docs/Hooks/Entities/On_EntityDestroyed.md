### Method
`On_EntityDestroyed`

### Description
Runs when an entity (structure/deployable) is destroyed, whether by damage or decay.

### C# Event
```csharp
public static event EntityDestroyedDelegate OnEntityDestroyed;
public delegate void EntityDestroyedDelegate(DestroyEvent de);
```

### Argument(s)
- `DestroyEvent DestroyEvent`

### Properties/Methods
- `DestroyEvent.Entity` - The entity being destroyed.
- `DestroyEvent.Attacker` - The attacker responsible, if any.
- `DestroyEvent.DamageAmount` - The final damage amount that destroyed it.
- `DestroyEvent.DamageType` - The type of damage.
- `DestroyEvent.WeaponName` - The weapon used, if any.
- `DestroyEvent.WeaponData` - The `WeaponImpact` data.
- `DestroyEvent.IsDecay` - Whether the entity was destroyed by decay.
- `DestroyEvent.DamageEvent` - The underlying `DamageEvent`.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnEntityDestroyed += DestroyedHandler;
}

public override void DeInitialize()
{
    Hooks.OnEntityDestroyed -= DestroyedHandler;
}

public void DestroyedHandler(DestroyEvent de)
{
    Logger.Log(de.Entity.Name + " was destroyed. Decay: " + de.IsDecay);
}
```

#### Python
```python
def On_EntityDestroyed(self, DestroyEvent):
    Server.Log(DestroyEvent.Entity.Name + " was destroyed. Decay: " + str(DestroyEvent.IsDecay))
```

#### JavaScript
```javascript
function On_EntityDestroyed(DestroyEvent)
{
    Server.Log(DestroyEvent.Entity.Name + " was destroyed. Decay: " + DestroyEvent.IsDecay);
}
```

#### Lua
```lua
function On_EntityDestroyed(DestroyEvent)
    Server.Log(DestroyEvent.Entity.Name .. " was destroyed. Decay: " .. tostring(DestroyEvent.IsDecay))
end
```
