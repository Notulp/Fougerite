### Method
`On_CraftingCancel`

### Description
Runs when a player cancels a queued crafting operation.

### C# Event
```csharp
public static event CraftingCancelDelegate OnCraftCancel;
public delegate void CraftingCancelDelegate(CraftCancelEvent e);
```

### Argument(s)
- `CraftCancelEvent CraftCancelEvent`

### Properties/Methods
- `CraftCancelEvent.Player` - The player cancelling.
- `CraftCancelEvent.CraftingInventory` - The `CraftingInventory` instance.
- `CraftCancelEvent.Cancelled` - Whether cancelling itself was cancelled (i.e. crafting continues).
- `CraftCancelEvent.Cancel()` - Prevents the crafting cancellation (the item keeps crafting).

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnCraftCancel += CraftCancelHandler;
}

public override void DeInitialize()
{
    Hooks.OnCraftCancel -= CraftCancelHandler;
}

public void CraftCancelHandler(CraftCancelEvent e)
{
    Logger.Log(e.Player.Name + " cancelled a crafting operation.");
}
```

#### Python
```python
def On_CraftingCancel(self, CraftCancelEvent):
    Util.Log(CraftCancelEvent.Player.Name + " cancelled a crafting operation.")
```

#### JavaScript
```javascript
function On_CraftingCancel(CraftCancelEvent)
{
    Util.Log(CraftCancelEvent.Player.Name + " cancelled a crafting operation.");
}
```

#### Lua
```lua
function On_CraftingCancel(CraftCancelEvent)
    Util.Log(CraftCancelEvent.Player.Name .. " cancelled a crafting operation.")
end
```
