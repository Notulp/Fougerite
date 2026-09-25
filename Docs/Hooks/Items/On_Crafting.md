### Method
`On_Crafting`

### Description
Runs when a player starts crafting an item.

### C# Event
```csharp
public static event CraftingDelegate OnCrafting;
public delegate void CraftingDelegate(CraftingEvent e);
```

### Argument(s)
- `CraftingEvent CraftingEvent`

### Properties/Methods
- `CraftingEvent.Player` - The player crafting.
- `CraftingEvent.BlueprintDataBlock` - The blueprint being crafted.
- `CraftingEvent.ItemName` - The result item's name.
- `CraftingEvent.ResultItem` - The result `ItemDataBlock`.
- `CraftingEvent.ResultItemNumber` - The result item's id.
- `CraftingEvent.Amount` - How many are being crafted.
- `CraftingEvent.Ingredients` - The `BlueprintDataBlock.IngredientEntry[]` required.
- `CraftingEvent.RequireWorkbench` - Whether a workbench is required.
- `CraftingEvent.IsLegit` - Whether the client-reported craft time is legitimate.
- `CraftingEvent.StartTime` - The crafting start timestamp.
- `CraftingEvent.LastWorkBenchTime` - Timestamp of the last time a workbench buff was applied.
- `CraftingEvent.CraftingInventory` - The `CraftingInventory` instance.
- `CraftingEvent.Cancel()` - Cancels the crafting request.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnCrafting += CraftingHandler;
}

public override void DeInitialize()
{
    Hooks.OnCrafting -= CraftingHandler;
}

public void CraftingHandler(CraftingEvent e)
{
    Logger.Log(e.Player.Name + " is crafting " + e.Amount + "x " + e.ItemName);
}
```

#### Python
```python
def On_Crafting(self, CraftingEvent):
    Util.Log(CraftingEvent.Player.Name + " is crafting " + str(CraftingEvent.Amount) + "x " + CraftingEvent.ItemName)
```

#### JavaScript
```javascript
function On_Crafting(CraftingEvent)
{
    Util.Log(CraftingEvent.Player.Name + " is crafting " + CraftingEvent.Amount + "x " + CraftingEvent.ItemName);
}
```

#### Lua
```lua
function On_Crafting(CraftingEvent)
    Util.Log(CraftingEvent.Player.Name .. " is crafting " .. tostring(CraftingEvent.Amount) .. "x " .. CraftingEvent.ItemName)
end
```
