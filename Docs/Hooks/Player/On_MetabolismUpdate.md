### Method
`On_MetabolismUpdate`

### Description
Runs when a player's metabolism (calories, water, radiation, temperature, etc.) updates. This is called
roughly every 3 seconds for each player, so be careful with heavy logic in script plugins.

### C# Event
```csharp
public static event MetabolismUpdateDelegate OnMetabolismUpdate;
public delegate void MetabolismUpdateDelegate(MetabolismEvent e);
```

### Argument(s)
- `MetabolismEvent MetabolismEvent`

### Properties/Methods
- `MetabolismEvent.Player` - The player whose metabolism updated.
- `MetabolismEvent.Metabolism` - The underlying `Metabolism` object (calories/water/health/etc).
- `MetabolismEvent.Delta` - The time delta since the last update.
- `MetabolismEvent.Cancelled` - Whether the update is cancelled.
- `MetabolismEvent.Cancel()` - Cancels the metabolism update for this tick.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnMetabolismUpdate += MetabolismHandler;
}

public override void DeInitialize()
{
    Hooks.OnMetabolismUpdate -= MetabolismHandler;
}

public void MetabolismHandler(MetabolismEvent e)
{
    // Prevent hunger/thirst from decreasing
    e.Cancel();
}
```

#### Python
```python
def On_MetabolismUpdate(self, MetabolismEvent):
    MetabolismEvent.Cancel()
```

#### JavaScript
```javascript
function On_MetabolismUpdate(MetabolismEvent)
{
    MetabolismEvent.Cancel();
}
```

#### Lua
```lua
function On_MetabolismUpdate(MetabolismEvent)
    MetabolismEvent.Cancel()
end
```
