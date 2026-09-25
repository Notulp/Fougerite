### Method
`On_NPCHurt`

### Description
Runs when an NPC (animal/bear/wolf/zombie etc.) gets damaged.

### C# Event
```csharp
public static event HurtHandlerDelegate OnNPCHurt;
public delegate void HurtHandlerDelegate(HurtEvent he);
```

### Argument(s)
- `HurtEvent HurtEvent`

### Properties/Methods
See [`On_PlayerHurt`](../Player/On_PlayerHurt.md) for the full list of `HurtEvent` properties.
`HurtEvent.Victim` will be an `NPC` instance for this hook.

### ⚠️ Safety: check `VictimIsNPC`/`AttackerIsX` and null before use

Even though this hook is named "`On_NPCHurt`", don't blindly cast `HurtEvent.Victim` to `NPC` - always
check `HurtEvent.VictimIsNPC` first (defensive coding also protects you if you reuse this handler for
`On_PlayerHurt`/`On_EntityHurt` later). `HurtEvent.Attacker` is even riskier here: an NPC can die to
another player's weapon (`AttackerIsPlayer`), to another NPC (`AttackerIsNPC`), to a trap/entity
(`AttackerIsEntity`), **or to nothing at all** (fall damage, drowning, decay, ...), in which case
`Attacker` is `null` and all `AttackerIsX` flags are `false`. See
[`On_PlayerHurt`'s safety section](../Player/On_PlayerHurt.md#️-safety-victimattacker-are-object-always-check-the-type-first)
for the full breakdown of every flag/combination.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnNPCHurt += NPCHurt;
}

public override void DeInitialize()
{
    Hooks.OnNPCHurt -= NPCHurt;
}

public void NPCHurt(HurtEvent he)
{
    if (!he.VictimIsNPC)
        return;

    NPC animal = (NPC) he.Victim;

    // he.Attacker can be null (fall damage, drowning, decay, ...) - always check first!
    string attackerName = "the environment";
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

    Server.GetServer().Broadcast(animal.Name + " got hurt by " + attackerName + "!");
}
```

#### Python
```python
def On_NPCHurt(self, HurtEvent):
    if not HurtEvent.VictimIsNPC:
        return

    # HurtEvent.Attacker can be None (fall damage, drowning, decay, ...) - always check first!
    AttackerName = "the environment"
    if HurtEvent.AttackerIsPlayer or HurtEvent.AttackerIsNPC or HurtEvent.AttackerIsEntity:
        AttackerName = HurtEvent.Attacker.Name

    Util.Log(HurtEvent.Victim.Name + " got hurt by " + AttackerName + "!")
```

#### JavaScript
```javascript
function On_NPCHurt(HurtEvent) {
    if (!HurtEvent.VictimIsNPC)
        return;

    // HurtEvent.Attacker can be null (fall damage, drowning, decay, ...) - always check first!
    var AttackerName = "the environment";
    if (HurtEvent.AttackerIsPlayer || HurtEvent.AttackerIsNPC || HurtEvent.AttackerIsEntity) {
        AttackerName = HurtEvent.Attacker.Name;
    }

    Util.Log(HurtEvent.Victim.Name + " got hurt by " + AttackerName + "!");
}
```

#### Lua
```lua
function On_NPCHurt(HurtEvent)
    if not HurtEvent.VictimIsNPC then
        return
    end

    -- HurtEvent.Attacker can be nil (fall damage, drowning, decay, ...) - always check first!
    local AttackerName = "the environment"
    if HurtEvent.AttackerIsPlayer or HurtEvent.AttackerIsNPC or HurtEvent.AttackerIsEntity then
        AttackerName = HurtEvent.Attacker.Name
    end

    Util.Log(HurtEvent.Victim.Name .. " got hurt by " .. AttackerName .. "!")
end
```
