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
    Fougerite.Player victim = (Fougerite.Player) de.Victim;
    Server.GetServer().Broadcast(victim.Name + " died!");
    de.DropItems = false; // Prevent item drop
}
```

#### Python
```python
def On_PlayerKilled(self, DeathEvent):
    Victim = DeathEvent.Victim
    Server.Broadcast(Victim.Name + " died!")
    DeathEvent.DropItems = False
```

#### JavaScript
```javascript
function On_PlayerKilled(DeathEvent)
{
    var Victim = DeathEvent.Victim;
    Server.Broadcast(Victim.Name + " died!");
    DeathEvent.DropItems = false;
}
```

#### Lua
```lua
function On_PlayerKilled(DeathEvent)
    local Victim = DeathEvent.Victim
    Server.Broadcast(Victim.Name .. " died!")
    DeathEvent.DropItems = false
end
```
