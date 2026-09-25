### Method
`On_MedikitUse`

### Description
Runs when a player uses a medkit or bandage.

### C# Event
```csharp
public static event MedikitUseEventDelegate OnMedikitUse;
public delegate void MedikitUseEventDelegate(MedikitUseEvent e);
```

### Argument(s)
- `MedikitUseEvent MedikitUseEvent`

### Properties/Methods
- `MedikitUseEvent.Player` - The player using the medkit.
- `MedikitUseEvent.Item` - The `IBasicHealthKit` being used.
- `MedikitUseEvent.DataBlock` - The `BasicHealthKitDataBlock`.
- `MedikitUseEvent.HealthAddMin` / `HealthAddMax` - The health heal range. Can be modified.
- `MedikitUseEvent.StopsBleeding` - Whether it stops bleeding.
- `MedikitUseEvent.AmountToConsume` - How many are consumed.
- `MedikitUseEvent.Cancelled` - Whether the use is cancelled.
- `MedikitUseEvent.Cancel()` - Cancels the medkit use.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnMedikitUse += MedikitHandler;
}

public override void DeInitialize()
{
    Hooks.OnMedikitUse -= MedikitHandler;
}

public void MedikitHandler(MedikitUseEvent e)
{
    e.HealthAddMax *= 2; // Double max heal
}
```

#### Python
```python
def On_MedikitUse(self, MedikitUseEvent):
    MedikitUseEvent.HealthAddMax = MedikitUseEvent.HealthAddMax * 2
```

#### JavaScript
```javascript
function On_MedikitUse(MedikitUseEvent)
{
    MedikitUseEvent.HealthAddMax = MedikitUseEvent.HealthAddMax * 2;
}
```

#### Lua
```lua
function On_MedikitUse(MedikitUseEvent)
    MedikitUseEvent.HealthAddMax = MedikitUseEvent.HealthAddMax * 2
end
```
