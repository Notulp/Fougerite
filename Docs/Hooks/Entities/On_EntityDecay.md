### Method
`On_EntityDecay`

### Description
Runs when an entity is damaged by the default Rust decay system.

### C# Event
```csharp
public static event EntityDecayDelegate OnEntityDecay;
public delegate void EntityDecayDelegate(DecayEvent de);
```

### Argument(s)
- `DecayEvent DecayEvent`

### Properties/Methods
- `DecayEvent.Entity` - The entity decaying.
- `DecayEvent.DamageAmount` - The decay damage amount. Can be changed (e.g. set to `0` to stop decay).

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnEntityDecay += DecayHandler;
}

public override void DeInitialize()
{
    Hooks.OnEntityDecay -= DecayHandler;
}

public void DecayHandler(DecayEvent de)
{
    de.DamageAmount = 0f; // Disable decay
}
```

#### Python
```python
def On_EntityDecay(self, DecayEvent):
    DecayEvent.DamageAmount = 0.0
```

#### JavaScript
```javascript
function On_EntityDecay(DecayEvent)
{
    DecayEvent.DamageAmount = 0.0;
}
```

#### Lua
```lua
function On_EntityDecay(DecayEvent)
    DecayEvent.DamageAmount = 0.0
end
```
