### Method
`On_AirdropCrateDropped`

### Description
Runs when the supply crate is dropped from the supply plane.

### C# Event
```csharp
public static event AirdropCrateDroppedDelegate OnAirdropCrateDropped;
public delegate void AirdropCrateDroppedDelegate(SupplyDropPlane plane, Entity supplyCrate);
```

### Argument(s)
- `SupplyDropPlane Plane` - The plane that dropped the crate.
- `Entity SupplyCrate` - The dropped supply crate entity.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnAirdropCrateDropped += CrateDroppedHandler;
}

public override void DeInitialize()
{
    Hooks.OnAirdropCrateDropped -= CrateDroppedHandler;
}

public void CrateDroppedHandler(SupplyDropPlane plane, Entity crate)
{
    Server.GetServer().Broadcast("A supply crate has been dropped!");
}
```

#### Python
```python
def On_AirdropCrateDropped(self, Plane, SupplyCrate):
    Server.Broadcast("A supply crate has been dropped!")
```

#### JavaScript
```javascript
function On_AirdropCrateDropped(Plane, SupplyCrate)
{
    Server.Broadcast("A supply crate has been dropped!");
}
```

#### Lua
```lua
function On_AirdropCrateDropped(Plane, SupplyCrate)
    Server.Broadcast("A supply crate has been dropped!")
end
```
