### Method
`On_ConsumableUse`

### Description
Runs when a player eats/drinks a consumable item.

### C# Event
```csharp
public static event ConsumableUseEventDelegate OnConsumableUse;
public delegate void ConsumableUseEventDelegate(ConsumableUseEvent e);
```

### Argument(s)
- `ConsumableUseEvent ConsumableUseEvent`

### Properties/Methods
- `ConsumableUseEvent.Player` - The player consuming the item.
- `ConsumableUseEvent.Item` - The `IConsumableItem` being consumed.
- `ConsumableUseEvent.DataBlock` - The `ConsumableDataBlock`.
- `ConsumableUseEvent.Calories` - Calories added. Can be modified.
- `ConsumableUseEvent.Water` - Water added. Can be modified.
- `ConsumableUseEvent.AntiRads` - Anti-radiation added. Can be modified.
- `ConsumableUseEvent.HealthToHeal` - Health healed. Can be modified.
- `ConsumableUseEvent.PoisonAmount` - Poison added. Can be modified.
- `ConsumableUseEvent.AmountToConsume` - How many items are consumed.
- `ConsumableUseEvent.Cancelled` - Whether the use is cancelled.
- `ConsumableUseEvent.Cancel()` - Cancels the consumption.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnConsumableUse += ConsumableHandler;
}

public override void DeInitialize()
{
    Hooks.OnConsumableUse -= ConsumableHandler;
}

public void ConsumableHandler(ConsumableUseEvent e)
{
    e.Calories *= 2; // Double the calories given
}
```

#### Python
```python
def On_ConsumableUse(self, ConsumableUseEvent):
    ConsumableUseEvent.Calories = ConsumableUseEvent.Calories * 2
```

#### JavaScript
```javascript
function On_ConsumableUse(ConsumableUseEvent)
{
    ConsumableUseEvent.Calories = ConsumableUseEvent.Calories * 2;
}
```

#### Lua
```lua
function On_ConsumableUse(ConsumableUseEvent)
    ConsumableUseEvent.Calories = ConsumableUseEvent.Calories * 2
end
```
