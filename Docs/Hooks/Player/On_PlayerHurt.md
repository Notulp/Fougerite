### Method
`On_PlayerHurt`

### Description
Runs when a player takes damage from any source.

### C# Event
```csharp
public static event HurtHandlerDelegate OnPlayerHurt;
public delegate void HurtHandlerDelegate(HurtEvent he);
```

### Argument(s)
- `HurtEvent HurtEvent`

### Properties/Methods
- `HurtEvent.Victim` - The object taking damage (cast to `Player`).
- `HurtEvent.Attacker` - The object dealing damage (`Player`, `Entity`, `NPC`, or `null`).
- `HurtEvent.DamageAmount` - The amount of damage dealt. Can be changed.
- `HurtEvent.DamageType` - The type of damage (e.g. Bullet, Bleeding, Fall, ...).
- `HurtEvent.WeaponName` - The name of the weapon used, if any.
- `HurtEvent.WeaponData` - The `WeaponImpact` data.
- `HurtEvent.Cancelled` - Whether the damage is cancelled.
- `HurtEvent.Cancel()` (via `Cancelled = true`) - Cancels/negates the damage.
- `HurtEvent.LifeStatus` - The resulting life status (Alive/Dead).
- `HurtEvent.AttackerIsPlayer` / `AttackerIsEntity` / `AttackerIsNPC` / `AttackerIsMetabolism` - Type helpers.
- `HurtEvent.VictimIsPlayer` / `VictimIsEntity` / `VictimIsNPC` - Type helpers.
- `HurtEvent.Sleeper` / `VictimIsSleeper` - Whether the victim is a sleeping player.

### ⚠️ Safety: `Victim`/`Attacker` are `object`, always check the type first

`HurtEvent` (and `DeathEvent`, which inherits it) is shared by **every** damage-related hook
(`On_PlayerHurt`, `On_NPCHurt`, `On_EntityHurt`, `On_PlayerKilled`, `On_NPCKilled`, ...). Both `Victim` and
`Attacker` are declared as plain `object`, because depending on *what* got hurt and *what* caused the
damage, they can actually be a `Fougerite.Player`, an `Entity`, a `Sleeper`, an `NPC`, **or `null`**.

Blindly casting or accessing a member on them (like the naive examples used to do, e.g.
`(Fougerite.Player) he.Victim` inside `On_NPCHurt`, or `he.Attacker.Name` on fall damage) will throw an
`InvalidCastException`/`NullReferenceException` and crash your handler. Always check the matching flag (or
`null`) **before** touching the object:

| Situation | What `Attacker` is | Flag(s) to check |
|---|---|---|
| Shot/melee'd by another player | `Fougerite.Player` | `AttackerIsPlayer` |
| Hit by an NPC/animal | `NPC` | `AttackerIsNPC` |
| Damaged by a Spike Wall / Supply Crate / other entity | `Entity` | `AttackerIsEntity` |
| Hunger/Radiation/Poison/Bleeding (self-damage) | the `Player` itself | `AttackerIsMetabolism` |
| Fall damage, decay, environment/other unhandled cases | **`null`** | all of the above `false` |

| Situation | What `Victim` is | Flag(s) to check |
|---|---|---|
| A player got hurt | `Fougerite.Player` (or `Sleeper` if asleep) | `VictimIsPlayer`, and `VictimIsSleeper` |
| An NPC/animal got hurt | `NPC` | `VictimIsNPC` |
| A structure/deployable got hurt | `Entity` | `VictimIsEntity` |

The same logic applies to `HurtEvent.WeaponData` (can be `null` when the attacker used something that
isn't a `WeaponImpact`, e.g. explosives or claws) - null-check it before dereferencing.

Since `On_PlayerHurt`/`On_NPCHurt`/`On_EntityHurt` all fire from the **same** underlying event, don't
assume the victim's actual type from *which hook fired* alone if you copy code between handlers - always
check the flags, they are what your other plugins/updates rely on to stay crash-free.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerHurt += HurtHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerHurt -= HurtHandler;
}

public void HurtHandler(HurtEvent he)
{
    // he.Victim is guaranteed to be a Player/Sleeper for OnPlayerHurt, but always check anyway,
    // it costs nothing and protects you if this method is ever reused for another hook.
    if (!he.VictimIsPlayer)
        return;

    Fougerite.Player victim = (Fougerite.Player) he.Victim;

    // he.Attacker can legitimately be null (fall damage, decay, environment, ...)!
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
    else if (he.AttackerIsMetabolism)
    {
        attackerName = "hunger/thirst/poison/radiation";
    }

    Server.GetServer().Broadcast(victim.Name + " took " + he.DamageAmount + " damage from " + attackerName + "!");
}
```

#### Python
```python
def On_PlayerHurt(self, HurtEvent):
    if not HurtEvent.VictimIsPlayer:
        return

    Victim = HurtEvent.Victim

    # HurtEvent.Attacker can legitimately be None (fall damage, decay, environment, ...)!
    AttackerName = "the environment"
    if HurtEvent.AttackerIsPlayer or HurtEvent.AttackerIsNPC or HurtEvent.AttackerIsEntity:
        AttackerName = HurtEvent.Attacker.Name
    elif HurtEvent.AttackerIsMetabolism:
        AttackerName = "hunger/thirst/poison/radiation"

    Util.Log(Victim.Name + " took " + str(HurtEvent.DamageAmount) + " damage from " + AttackerName + "!")
```

#### JavaScript
```javascript
function On_PlayerHurt(HurtEvent)
{
    if (!HurtEvent.VictimIsPlayer)
        return;

    var Victim = HurtEvent.Victim;

    // HurtEvent.Attacker can legitimately be null (fall damage, decay, environment, ...)!
    var AttackerName = "the environment";
    if (HurtEvent.AttackerIsPlayer || HurtEvent.AttackerIsNPC || HurtEvent.AttackerIsEntity)
    {
        AttackerName = HurtEvent.Attacker.Name;
    }
    else if (HurtEvent.AttackerIsMetabolism)
    {
        AttackerName = "hunger/thirst/poison/radiation";
    }

    Util.Log(Victim.Name + " took " + HurtEvent.DamageAmount + " damage from " + AttackerName + "!");
}
```

#### Lua
```lua
function On_PlayerHurt(HurtEvent)
    if not HurtEvent.VictimIsPlayer then
        return
    end

    local Victim = HurtEvent.Victim

    -- HurtEvent.Attacker can legitimately be nil (fall damage, decay, environment, ...)!
    local AttackerName = "the environment"
    if HurtEvent.AttackerIsPlayer or HurtEvent.AttackerIsNPC or HurtEvent.AttackerIsEntity then
        AttackerName = HurtEvent.Attacker.Name
    elseif HurtEvent.AttackerIsMetabolism then
        AttackerName = "hunger/thirst/poison/radiation"
    end

    Util.Log(Victim.Name .. " took " .. tostring(HurtEvent.DamageAmount) .. " damage from " .. AttackerName .. "!")
end
```
