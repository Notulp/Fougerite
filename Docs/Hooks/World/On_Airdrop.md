### Method
`On_Airdrop`

### Description
Runs when an airdrop is called in (either via the in-game supply signal or an admin command).

### C# Event
```csharp
public static event AirdropDelegate OnAirdropCalled;
public delegate void AirdropDelegate(Vector3 v);
```

### Argument(s)
- `Vector3 Position` - The position the airdrop is called to.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnAirdropCalled += AirdropHandler;
}

public override void DeInitialize()
{
    Hooks.OnAirdropCalled -= AirdropHandler;
}

public void AirdropHandler(Vector3 position)
{
    Server.GetServer().Broadcast("An airdrop was called at " + position);
}
```

#### Python
```python
def On_Airdrop(self, Position):
    Server.Broadcast("An airdrop was called at " + str(Position))
```

#### JavaScript
```javascript
function On_Airdrop(Position)
{
    Server.Broadcast("An airdrop was called at " + Position);
}
```

#### Lua
```lua
function On_Airdrop(Position)
    Server.Broadcast("An airdrop was called at " .. tostring(Position))
end
```
