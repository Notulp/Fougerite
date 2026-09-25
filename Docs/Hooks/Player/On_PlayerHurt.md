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
    Fougerite.Player victim = (Fougerite.Player) he.Victim;
    Server.GetServer().Broadcast(victim.Name + " took " + he.DamageAmount + " damage!");
}
```

#### Python
```python
def On_PlayerHurt(self, HurtEvent):
    Victim = HurtEvent.Victim
    Server.Broadcast(Victim.Name + " took " + str(HurtEvent.DamageAmount) + " damage!")
```

#### JavaScript
```javascript
function On_PlayerHurt(HurtEvent)
{
    var Victim = HurtEvent.Victim;
    Server.Broadcast(Victim.Name + " took " + HurtEvent.DamageAmount + " damage!");
}
```

#### Lua
```lua
function On_PlayerHurt(HurtEvent)
    local Victim = HurtEvent.Victim
    Server.Broadcast(Victim.Name .. " took " .. tostring(HurtEvent.DamageAmount) .. " damage!")
end
```
