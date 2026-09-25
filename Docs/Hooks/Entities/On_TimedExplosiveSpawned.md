### Method
`On_TimedExplosiveSpawned`

### Description
Runs when a timed explosive (e.g. C4) is placed in the world.

### C# Event
```csharp
public static event TimedExplosiveEventDelegate OnTimedExplosiveSpawned;
public delegate void TimedExplosiveEventDelegate(TimedExplosiveEvent timedExplosiveEvent);
```

### Argument(s)
- `TimedExplosiveEvent TimedExplosiveEvent`

### Properties/Methods
- `TimedExplosiveEvent.TimedExplosive` - The underlying `TimedExplosive` component.
- `TimedExplosiveEvent.Location` - The `Vector3` spawn position.
- `TimedExplosiveEvent.GameObject` - The associated `GameObject`.
- `TimedExplosiveEvent.Cancelled` - Whether the spawn is cancelled.
- `TimedExplosiveEvent.Cancel()` - Cancels the placement/spawn.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnTimedExplosiveSpawned += ExplosiveHandler;
}

public override void DeInitialize()
{
    Hooks.OnTimedExplosiveSpawned -= ExplosiveHandler;
}

public void ExplosiveHandler(TimedExplosiveEvent e)
{
    Logger.Log("A C4 was placed at " + e.Location);
}
```

#### Python
```python
def On_TimedExplosiveSpawned(self, TimedExplosiveEvent):
    Server.Log("A C4 was placed at " + str(TimedExplosiveEvent.Location))
```

#### JavaScript
```javascript
function On_TimedExplosiveSpawned(TimedExplosiveEvent)
{
    Server.Log("A C4 was placed at " + TimedExplosiveEvent.Location);
}
```

#### Lua
```lua
function On_TimedExplosiveSpawned(TimedExplosiveEvent)
    Server.Log("A C4 was placed at " .. tostring(TimedExplosiveEvent.Location))
end
```
