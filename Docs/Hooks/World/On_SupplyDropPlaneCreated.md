### Method
`On_SupplyDropPlaneCreated`

### Description
Runs when the supply drop plane spawns to deliver an airdrop.

### C# Event
```csharp
public static event SupplyDropPlaneCreatedDelegate OnSupplyDropPlaneCreated;
public delegate void SupplyDropPlaneCreatedDelegate(SupplyDropPlane plane);
```

### Argument(s)
- `SupplyDropPlane Plane` - The plane that spawned.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnSupplyDropPlaneCreated += PlaneHandler;
}

public override void DeInitialize()
{
    Hooks.OnSupplyDropPlaneCreated -= PlaneHandler;
}

public void PlaneHandler(SupplyDropPlane plane)
{
    Server.GetServer().Broadcast("The supply plane has arrived!");
}
```

#### Python
```python
def On_SupplyDropPlaneCreated(self, Plane):
    Server.Broadcast("The supply plane has arrived!")
```

#### JavaScript
```javascript
function On_SupplyDropPlaneCreated(Plane)
{
    Server.Broadcast("The supply plane has arrived!");
}
```

#### Lua
```lua
function On_SupplyDropPlaneCreated(Plane)
    Server.Broadcast("The supply plane has arrived!")
end
```
