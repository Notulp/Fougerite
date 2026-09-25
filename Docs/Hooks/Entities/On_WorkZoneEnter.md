### Method
`On_WorkZoneEnter`

### Description
Runs while a player is standing inside a workbench's work zone trigger. This event runs continuously while
the player remains inside the trigger, so be careful when using it in script plugins.

### C# Event
```csharp
public static event WorkZoneEnterEventDelegate OnWorkZoneEnter;
public delegate void WorkZoneEnterEventDelegate(WorkZoneEnterEvent wze);
```

### Argument(s)
- `WorkZoneEnterEvent WorkZoneEnterEvent`

### Properties/Methods
- `WorkZoneEnterEvent.Entity` - The workbench entity.
- `WorkZoneEnterEvent.Instance` - The `WorkZone` component.
- `WorkZoneEnterEvent.Collider` - The trigger `Collider`.
- `WorkZoneEnterEvent.CraftingInventory` - The player's crafting inventory.
- `WorkZoneEnterEvent.Player` - The player standing in the zone.
- `WorkZoneEnterEvent.Cancelled` - Whether the effect is cancelled.
- `WorkZoneEnterEvent.Cancel()` - Cancels the workbench-crafting-speed effect for this tick.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnWorkZoneEnter += WorkZoneHandler;
}

public override void DeInitialize()
{
    Hooks.OnWorkZoneEnter -= WorkZoneHandler;
}

public void WorkZoneHandler(WorkZoneEnterEvent e)
{
    // Careful: called continuously!
}
```

#### Python
```python
def On_WorkZoneEnter(self, WorkZoneEnterEvent):
    pass  # Careful: called continuously!
```

#### JavaScript
```javascript
function On_WorkZoneEnter(WorkZoneEnterEvent)
{
    // Careful: called continuously!
}
```

#### Lua
```lua
function On_WorkZoneEnter(WorkZoneEnterEvent)
    -- Careful: called continuously!
end
```
