### Method
`On_EntityHurt`

### Description
Runs when a structure/deployable entity (wall, door, deployable, etc.) takes damage.

### C# Event
```csharp
public static event EntityHurtDelegate OnEntityHurt;
public delegate void EntityHurtDelegate(HurtEvent he);
```

### Argument(s)
- `HurtEvent HurtEvent` - See [`On_PlayerHurt`](../Player/On_PlayerHurt.md) for its properties.
  `HurtEvent.Victim` will be an `Entity` instance for this hook.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnEntityHurt += EntityHurtHandler;
}

public override void DeInitialize()
{
    Hooks.OnEntityHurt -= EntityHurtHandler;
}

public void EntityHurtHandler(HurtEvent he)
{
    Entity entity = (Entity) he.Victim;
    Logger.Log(entity.Name + " took " + he.DamageAmount + " damage.");
}
```

#### Python
```python
def On_EntityHurt(self, HurtEvent):
    Server.Log(HurtEvent.Victim.Name + " took " + str(HurtEvent.DamageAmount) + " damage.")
```

#### JavaScript
```javascript
function On_EntityHurt(HurtEvent)
{
    Server.Log(HurtEvent.Victim.Name + " took " + HurtEvent.DamageAmount + " damage.");
}
```

#### Lua
```lua
function On_EntityHurt(HurtEvent)
    Server.Log(HurtEvent.Victim.Name .. " took " .. tostring(HurtEvent.DamageAmount) .. " damage.")
end
```
