### Method
`On_SupplySignalExploded`

### Description
Runs when a supply signal grenade explodes at a position (this is what triggers an airdrop to be called).

### C# Event
```csharp
public static event SupplySignalDelegate OnSupplySignalExpode;
public delegate void SupplySignalDelegate(SupplySignalExplosionEvent supplySignalExplosionEvent);
```

### Argument(s)
- `SupplySignalExplosionEvent SupplySignalExplosionEvent`

### Properties/Methods
- `SupplySignalExplosionEvent.SignalGrenade` - The `SignalGrenade` component.
- `SupplySignalExplosionEvent.AirdropPosition` - The position the airdrop will be called to.
- `SupplySignalExplosionEvent.Cancelled` - Whether the explosion/airdrop is cancelled.
- `SupplySignalExplosionEvent.Cancel()` - Cancels the resulting airdrop.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnSupplySignalExpode += SignalHandler;
}

public override void DeInitialize()
{
    Hooks.OnSupplySignalExpode -= SignalHandler;
}

public void SignalHandler(SupplySignalExplosionEvent e)
{
    Server.GetServer().Broadcast("A supply signal exploded at " + e.AirdropPosition);
}
```

#### Python
```python
def On_SupplySignalExploded(self, SupplySignalExplosionEvent):
    Server.Broadcast("A supply signal exploded at " + str(SupplySignalExplosionEvent.AirdropPosition))
```

#### JavaScript
```javascript
function On_SupplySignalExploded(SupplySignalExplosionEvent)
{
    Server.Broadcast("A supply signal exploded at " + SupplySignalExplosionEvent.AirdropPosition);
}
```

#### Lua
```lua
function On_SupplySignalExploded(SupplySignalExplosionEvent)
    Server.Broadcast("A supply signal exploded at " .. tostring(SupplySignalExplosionEvent.AirdropPosition))
end
```
