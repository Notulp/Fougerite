### Method
`On_PlayerKilled`

### Description
Runs when a player has died.

### C# Event
```csharp
public static event KillHandlerDelegate OnPlayerKilled;
public delegate void KillHandlerDelegate(DeathEvent de);
```

### Argument(s)
- `DeathEvent DeathEvent` - Inherits all of [`HurtEvent`](On_PlayerHurt.md)'s properties (`Victim`, `Attacker`,
  `DamageAmount`, `WeaponName`, etc.) plus:

### Properties/Methods
- `DeathEvent.DropItems` - Whether the victim's inventory should be dropped on death. Set to `false` to
  prevent item drops.

### ⚠️ Safety: `DeathEvent` inherits `HurtEvent`'s null/type pitfalls

Since `DeathEvent` **is** a `HurtEvent` (it just adds `DropItems`), the exact same rules apply: always
check `DeathEvent.VictimIsPlayer` before casting `Victim`, and always check the `AttackerIsX` flags (or
handle `Attacker == null`) before touching `Attacker` - a player can die to fall damage, drowning,
starvation/bleeding (`AttackerIsMetabolism`), an NPC, another player, or with **no** attacker at all. See
[`On_PlayerHurt`'s safety section](On_PlayerHurt.md#️-safety-victimattacker-are-object-always-check-the-type-first)
for the full breakdown.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerKilled += KilledHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerKilled -= KilledHandler;
}

public void KilledHandler(DeathEvent de)
{
    if (!de.VictimIsPlayer)
        return;

    Fougerite.Player victim = (Fougerite.Player) de.Victim;

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
    else if (de.AttackerIsMetabolism)
    {
        attackerName = "hunger/thirst/poison/radiation";
    }

    Server.GetServer().Broadcast(victim.Name + " died to " + attackerName + "!");
    de.DropItems = false; // Prevent item drop
}
```

#### Python
```python
def On_PlayerKilled(self, DeathEvent):
    if not DeathEvent.VictimIsPlayer:
        return

    Victim = DeathEvent.Victim

    # DeathEvent.Attacker can be None (fall damage, drowning, decay, ...) - always check first!
    AttackerName = "the environment"
    if DeathEvent.AttackerIsPlayer or DeathEvent.AttackerIsNPC or DeathEvent.AttackerIsEntity:
        AttackerName = DeathEvent.Attacker.Name
    elif DeathEvent.AttackerIsMetabolism:
        AttackerName = "hunger/thirst/poison/radiation"

    Util.Log(Victim.Name + " died to " + AttackerName + "!")
    DeathEvent.DropItems = False
```

#### JavaScript
```javascript
function On_PlayerKilled(DeathEvent)
{
    if (!DeathEvent.VictimIsPlayer)
        return;

    var Victim = DeathEvent.Victim;

    // DeathEvent.Attacker can be null (fall damage, drowning, decay, ...) - always check first!
    var AttackerName = "the environment";
    if (DeathEvent.AttackerIsPlayer || DeathEvent.AttackerIsNPC || DeathEvent.AttackerIsEntity)
    {
        AttackerName = DeathEvent.Attacker.Name;
    }
    else if (DeathEvent.AttackerIsMetabolism)
    {
        AttackerName = "hunger/thirst/poison/radiation";
    }

    Util.Log(Victim.Name + " died to " + AttackerName + "!");
    DeathEvent.DropItems = false;
}
```

#### Lua
```lua
function On_PlayerKilled(DeathEvent)
    if not DeathEvent.VictimIsPlayer then
        return
    end

    local Victim = DeathEvent.Victim

    -- DeathEvent.Attacker can be nil (fall damage, drowning, decay, ...) - always check first!
    local AttackerName = "the environment"
    if DeathEvent.AttackerIsPlayer or DeathEvent.AttackerIsNPC or DeathEvent.AttackerIsEntity then
        AttackerName = DeathEvent.Attacker.Name
    elseif DeathEvent.AttackerIsMetabolism then
        AttackerName = "hunger/thirst/poison/radiation"
    end

    Util.Log(Victim.Name .. " died to " .. AttackerName .. "!")
    DeathEvent.DropItems = false
end
```
