### Method
`On_NPCKilled`

### Description
Runs when an NPC (animal) has died.

### C# Event
```csharp
public static event KillHandlerDelegate OnNPCKilled;
public delegate void KillHandlerDelegate(DeathEvent de);
```

### Argument(s)
- `DeathEvent DeathEvent` - See [`On_PlayerKilled`](../Player/On_PlayerKilled.md) for its properties.
  `DeathEvent.Victim` will be an `NPC` instance for this hook.

### ⚠️ Safety: check `VictimIsNPC`/`AttackerIsX` and null before use

Don't blindly cast `DeathEvent.Victim` to `NPC` - check `DeathEvent.VictimIsNPC` first. NPCs can die from
another player's weapon, another NPC, an entity/trap, or from fall damage/drowning/decay with **no**
attacker at all (`DeathEvent.Attacker == null`, all `AttackerIsX` flags `false`). See
[`On_PlayerHurt`'s safety section](../Player/On_PlayerHurt.md#️-safety-victimattacker-are-object-always-check-the-type-first)
for the full breakdown of every flag/combination.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnNPCKilled += NPCKilledHandler;
}

public override void DeInitialize()
{
    Hooks.OnNPCKilled -= NPCKilledHandler;
}

public void NPCKilledHandler(DeathEvent de)
{
    if (!de.VictimIsNPC)
        return;

    NPC animal = (NPC) de.Victim;

    // de.Attacker can be null (fall damage, drowning, decay, ...) - always check first!
    string attackerName = "the environment";
    if (de.AttackerIsPlayer)
    {
        attackerName = ((Fougerite.Player) de.Attacker).Name;
    }
    else if (de.AttackerIsNPC)
    {
        attackerName = ((NPC) de.Attacker).Name;
    }
    else if (de.AttackerIsEntity)
    {
        attackerName = ((Entity) de.Attacker).Name;
    }

    Server.GetServer().Broadcast(animal.Name + " was killed by " + attackerName + "!");
}
```

#### Python
```python
def On_NPCKilled(self, DeathEvent):
    if not DeathEvent.VictimIsNPC:
        return

    # DeathEvent.Attacker can be None (fall damage, drowning, decay, ...) - always check first!
    AttackerName = "the environment"
    if DeathEvent.AttackerIsPlayer or DeathEvent.AttackerIsNPC or DeathEvent.AttackerIsEntity:
        AttackerName = DeathEvent.Attacker.Name

    Util.Log(DeathEvent.Victim.Name + " was killed by " + AttackerName + "!")
```

#### JavaScript
```javascript
function On_NPCKilled(DeathEvent)
{
    if (!DeathEvent.VictimIsNPC)
        return;

    // DeathEvent.Attacker can be null (fall damage, drowning, decay, ...) - always check first!
    var AttackerName = "the environment";
    if (DeathEvent.AttackerIsPlayer || DeathEvent.AttackerIsNPC || DeathEvent.AttackerIsEntity)
    {
        AttackerName = DeathEvent.Attacker.Name;
    }

    Util.Log(DeathEvent.Victim.Name + " was killed by " + AttackerName + "!");
}
```

#### Lua
```lua
function On_NPCKilled(DeathEvent)
    if not DeathEvent.VictimIsNPC then
        return
    end

    -- DeathEvent.Attacker can be nil (fall damage, drowning, decay, ...) - always check first!
    local AttackerName = "the environment"
    if DeathEvent.AttackerIsPlayer or DeathEvent.AttackerIsNPC or DeathEvent.AttackerIsEntity then
        AttackerName = DeathEvent.Attacker.Name
    end

    Util.Log(DeathEvent.Victim.Name .. " was killed by " .. AttackerName .. "!")
end
```
