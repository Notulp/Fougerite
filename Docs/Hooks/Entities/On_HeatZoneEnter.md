### Method
`On_HeatZoneEnter`

### Description
Runs while a player is standing inside a heat source's trigger zone (campfire, furnace, etc.). This event
runs continuously while the player remains inside the trigger, so be careful when using it in script
plugins.

### C# Event
```csharp
public static event HeatZoneEnterEventDelegate OnHeatZoneEnter;
public delegate void HeatZoneEnterEventDelegate(HeatZoneEnterEvent hze);
```

### Argument(s)
- `HeatZoneEnterEvent HeatZoneEnterEvent`

### Properties/Methods
- `HeatZoneEnterEvent.Entity` - The heat source entity.
- `HeatZoneEnterEvent.Instance` - The `HeatZone` component.
- `HeatZoneEnterEvent.Collider` - The trigger `Collider`.
- `HeatZoneEnterEvent.Metabolism` - The player's `Metabolism`.
- `HeatZoneEnterEvent.Player` - The player standing in the zone.
- `HeatZoneEnterEvent.Cancelled` - Whether the effect is cancelled.
- `HeatZoneEnterEvent.Cancel()` - Cancels the heat effect for this tick.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnHeatZoneEnter += HeatZoneHandler;
}

public override void DeInitialize()
{
    Hooks.OnHeatZoneEnter -= HeatZoneHandler;
}

public void HeatZoneHandler(HeatZoneEnterEvent e)
{
    // Careful: called continuously!
}
```

#### Python
```python
def On_HeatZoneEnter(self, HeatZoneEnterEvent):
    pass  # Careful: called continuously!
```

#### JavaScript
```javascript
function On_HeatZoneEnter(HeatZoneEnterEvent)
{
    // Careful: called continuously!
}
```

#### Lua
```lua
function On_HeatZoneEnter(HeatZoneEnterEvent)
    -- Careful: called continuously!
end
```
