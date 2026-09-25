### Method
`On_AnimalMovement`

### Description
Runs when an animal's AI movement gets updated (either NavMesh-based or the basic wildlife movement).
This fires continuously while animals move, so avoid heavy logic here.

### C# Event
```csharp
public static event AnimalMovementEventDelegate OnAnimalMovement;
public delegate void AnimalMovementEventDelegate(AnimalMovementEvent ame);
```

### Argument(s)
- `AnimalMovementEvent AnimalMovementEvent`

### Properties/Methods
- `AnimalMovementEvent.NPC` - The NPC/animal moving.
- `AnimalMovementEvent.NavMeshMovement` - The NavMesh movement component, if applicable.
- `AnimalMovementEvent.BasicWildLifeMovement` - The basic wildlife movement component, if applicable.
- `AnimalMovementEvent.SimMillis` - Simulation time in milliseconds.
- `AnimalMovementEvent.Type` - `AnimalMovementType` (which movement system triggered this).

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnAnimalMovement += AnimalMovementHandler;
}

public override void DeInitialize()
{
    Hooks.OnAnimalMovement -= AnimalMovementHandler;
}

public void AnimalMovementHandler(AnimalMovementEvent e)
{
    // Careful: called very often!
}
```

#### Python
```python
def On_AnimalMovement(self, AnimalMovementEvent):
    pass  # Careful: called very often!
```

#### JavaScript
```javascript
function On_AnimalMovement(AnimalMovementEvent)
{
    // Careful: called very often!
}
```

#### Lua
```lua
function On_AnimalMovement(AnimalMovementEvent)
    -- Careful: called very often!
end
```
