### Method
`On_PlayerGathering`

### Description
Runs when a player gathers resources from a resource node (tree, ore, etc.) or by hitting an animal/NPC.

### C# Event
```csharp
public static event PlayerGatheringHandlerDelegate OnPlayerGathering;
public delegate void PlayerGatheringHandlerDelegate(Player player, GatherEvent ge);
```

### Argument(s)
- `Player Player` - The player who is gathering.
- `GatherEvent GatherEvent`

### Properties/Methods
- `GatherEvent.Item` - The item name being gathered.
- `GatherEvent.Quantity` - The amount being given. Can be modified to change the gather amount.
- `GatherEvent.AmountLeft` - The amount left in the resource node.
- `GatherEvent.PercentFull` - How full the target inventory is (percentage).
- `GatherEvent.Type` - The type of the gather source.
- `GatherEvent.Override` - Set to `true` together with a custom `Quantity`/`Item` to override the default result.
- `GatherEvent.ResourceTarget` - The `ResourceTarget` object (tree/ore node), if any.
- `GatherEvent.ResourceTargetType` - Enum describing the resource target type.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerGathering += GatherHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerGathering -= GatherHandler;
}

public void GatherHandler(Fougerite.Player player, GatherEvent ge)
{
    // Double all gather yields
    ge.Quantity *= 2;
    ge.Override = true;
}
```

#### Python
```python
def On_PlayerGathering(self, Player, GatherEvent):
    # Double all gather yields
    GatherEvent.Quantity = GatherEvent.Quantity * 2
    GatherEvent.Override = True
```

#### JavaScript
```javascript
function On_PlayerGathering(Player, GatherEvent)
{
    // Double all gather yields
    GatherEvent.Quantity = GatherEvent.Quantity * 2;
    GatherEvent.Override = true;
}
```

#### Lua
```lua
function On_PlayerGathering(Player, GatherEvent)
    -- Double all gather yields
    GatherEvent.Quantity = GatherEvent.Quantity * 2
    GatherEvent.Override = true
end
```
