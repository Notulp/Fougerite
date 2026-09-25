### Method
`On_WaterDamage`

### Description
Runs when a player takes damage from being submerged in water for too long (drowning).

### C# Event
```csharp
public static event WaterDamageDelegate OnWaterDamage;
public delegate void WaterDamageDelegate(WaterDamageEvent e);
```

### Argument(s)
- `WaterDamageEvent WaterDamageEvent`

### Properties/Methods
- `WaterDamageEvent.Player` - The player taking water damage.
- `WaterDamageEvent.Depth` - How deep the player is submerged.
- `WaterDamageEvent.Oxygen` - The player's remaining oxygen.
- `WaterDamageEvent.SubmergedSeconds` - How long the player has been submerged.
- `WaterDamageEvent.SwimFlagSet` - Whether the swim flag is set.
- `WaterDamageEvent.DamageAmount` - The amount of damage to apply. Can be modified.
- `WaterDamageEvent.Cancelled` - Whether the damage is cancelled.
- `WaterDamageEvent.Cancel()` - Cancels the damage.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnWaterDamage += WaterDamageHandler;
}

public override void DeInitialize()
{
    Hooks.OnWaterDamage -= WaterDamageHandler;
}

public void WaterDamageHandler(WaterDamageEvent e)
{
    e.Cancel(); // No more drowning
}
```

#### Python
```python
def On_WaterDamage(self, WaterDamageEvent):
    WaterDamageEvent.Cancel()
```

#### JavaScript
```javascript
function On_WaterDamage(WaterDamageEvent)
{
    WaterDamageEvent.Cancel();
}
```

#### Lua
```lua
function On_WaterDamage(WaterDamageEvent)
    WaterDamageEvent.Cancel()
end
```
