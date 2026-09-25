### Method
`On_BlueprintUse`

### Description
Runs when a player uses/learns a blueprint item.

### C# Event
```csharp
public static event BlueprintUseHandlerDelegate OnBlueprintUse;
public delegate void BlueprintUseHandlerDelegate(Player player, BPUseEvent ae);
```

### Argument(s)
- `Player Player` - The player using the blueprint.
- `BPUseEvent BPUseEvent`

### Properties/Methods
- `BPUseEvent.DataBlock` - The `BlueprintDataBlock` being learned.
- `BPUseEvent.Item` - The blueprint `IBlueprintItem` being consumed.
- `BPUseEvent.ItemName` - The resulting item's name.
- `BPUseEvent.Cancel` - Set to `true` to cancel the blueprint use.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnBlueprintUse += BlueprintHandler;
}

public override void DeInitialize()
{
    Hooks.OnBlueprintUse -= BlueprintHandler;
}

public void BlueprintHandler(Fougerite.Player player, BPUseEvent ae)
{
    Logger.Log(player.Name + " learned blueprint: " + ae.ItemName);
}
```

#### Python
```python
def On_BlueprintUse(self, Player, BPUseEvent):
    Util.Log(Player.Name + " learned blueprint: " + BPUseEvent.ItemName)
```

#### JavaScript
```javascript
function On_BlueprintUse(Player, BPUseEvent)
{
    Util.Log(Player.Name + " learned blueprint: " + BPUseEvent.ItemName);
}
```

#### Lua
```lua
function On_BlueprintUse(Player, BPUseEvent)
    Util.Log(Player.Name .. " learned blueprint: " .. BPUseEvent.ItemName)
end
```
