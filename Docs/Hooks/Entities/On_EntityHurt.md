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

### ⚠️ Safety: check `VictimIsEntity`/`AttackerIsX` and null before use

Don't blindly cast `HurtEvent.Victim` to `Entity` - check `HurtEvent.VictimIsEntity` first. Structures and
deployables are very often damaged by **decay** or **explosives**, in which case `HurtEvent.Attacker` can
be `null` (all `AttackerIsX` flags `false`) or another `Entity` (a Spike Wall/Supply Crate,
`AttackerIsEntity`), rather than the `Fougerite.Player`/`NPC` you might expect. Check `HurtEvent.IsDecay`
too if you specifically need to tell decay damage apart. See
[`On_PlayerHurt`'s safety section](../Player/On_PlayerHurt.md#️-safety-victimattacker-are-object-always-check-the-type-first)
for the full breakdown of every flag/combination.

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
    if (!he.VictimIsEntity)
        return;

    Entity entity = (Entity) he.Victim;

    // he.Attacker can be null (decay, environment, ...) - always check first!
    string attackerName = he.IsDecay ? "decay" : "the environment";
    if (he.AttackerIsPlayer)
    {
        attackerName = ((Fougerite.Player) he.Attacker).Name;
    }
    else if (he.AttackerIsNPC)
    {
        attackerName = ((NPC) he.Attacker).Name;
    }
    else if (he.AttackerIsEntity)
    {
        attackerName = ((Entity) he.Attacker).Name;
    }

    Logger.Log(entity.Name + " took " + he.DamageAmount + " damage from " + attackerName + ".");
}
```

#### Python
```python
def On_EntityHurt(self, HurtEvent):
    if not HurtEvent.VictimIsEntity:
        return

    # HurtEvent.Attacker can be None (decay, environment, ...) - always check first!
    AttackerName = "decay" if HurtEvent.IsDecay else "the environment"
    if HurtEvent.AttackerIsPlayer or HurtEvent.AttackerIsNPC or HurtEvent.AttackerIsEntity:
        AttackerName = HurtEvent.Attacker.Name

    Util.Log(HurtEvent.Victim.Name + " took " + str(HurtEvent.DamageAmount) + " damage from " + AttackerName + ".")
```

#### JavaScript
```javascript
function On_EntityHurt(HurtEvent)
{
    if (!HurtEvent.VictimIsEntity)
        return;

    // HurtEvent.Attacker can be null (decay, environment, ...) - always check first!
    var AttackerName = HurtEvent.IsDecay ? "decay" : "the environment";
    if (HurtEvent.AttackerIsPlayer || HurtEvent.AttackerIsNPC || HurtEvent.AttackerIsEntity)
    {
        AttackerName = HurtEvent.Attacker.Name;
    }

    Util.Log(HurtEvent.Victim.Name + " took " + HurtEvent.DamageAmount + " damage from " + AttackerName + ".");
}
```

#### Lua
```lua
function On_EntityHurt(HurtEvent)
    if not HurtEvent.VictimIsEntity then
        return
    end

    -- HurtEvent.Attacker can be nil (decay, environment, ...) - always check first!
    local AttackerName = HurtEvent.IsDecay and "decay" or "the environment"
    if HurtEvent.AttackerIsPlayer or HurtEvent.AttackerIsNPC or HurtEvent.AttackerIsEntity then
        AttackerName = HurtEvent.Attacker.Name
    end

    Util.Log(HurtEvent.Victim.Name .. " took " .. tostring(HurtEvent.DamageAmount) .. " damage from " .. AttackerName .. ".")
end
```
