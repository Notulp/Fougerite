### Method
`On_CraftingComplete`

### Description
Runs when a queued crafting operation completes and the resulting item is given to the player.

### C# Event
```csharp
public static event CraftingCompleteDelegate OnCraftComplete;
public delegate void CraftingCompleteDelegate(CraftCompleteEvent e);
```

### Argument(s)
- `CraftCompleteEvent CraftCompleteEvent`

### Properties/Methods
- `CraftCompleteEvent.Player` - The player who finished crafting.
- `CraftCompleteEvent.CraftingInventory` - The `CraftingInventory` instance.
- `CraftCompleteEvent.EventType` - The `CraftCompleteEventType` (e.g. normal completion).

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnCraftComplete += CraftCompleteHandler;
}

public override void DeInitialize()
{
    Hooks.OnCraftComplete -= CraftCompleteHandler;
}

public void CraftCompleteHandler(CraftCompleteEvent e)
{
    Logger.Log(e.Player.Name + " finished crafting.");
}
```

#### Python
```python
def On_CraftingComplete(self, CraftCompleteEvent):
    Util.Log(CraftCompleteEvent.Player.Name + " finished crafting.")
```

#### JavaScript
```javascript
function On_CraftingComplete(CraftCompleteEvent)
{
    Util.Log(CraftCompleteEvent.Player.Name + " finished crafting.");
}
```

#### Lua
```lua
function On_CraftingComplete(CraftCompleteEvent)
    Util.Log(CraftCompleteEvent.Player.Name .. " finished crafting.")
end
```
