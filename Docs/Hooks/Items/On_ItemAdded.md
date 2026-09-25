### Method
`On_ItemAdded`

### Description
Runs when an item is added to an inventory (crafting result, loot, admin give, etc.).

### C# Event
```csharp
public static event ItemAddedDelegate OnItemAdded;
public delegate void ItemAddedDelegate(InventoryModEvent e);
```

### Argument(s)
- `InventoryModEvent InventoryModEvent`

### Properties/Methods
- `InventoryModEvent.Player` - The owner of the inventory (if a player inventory).
- `InventoryModEvent.InventoryItem` - The item being added.
- `InventoryModEvent.Item` - The `EntityItem` wrapper (name, amount, etc.).
- `InventoryModEvent.ItemName` - The item's name.
- `InventoryModEvent.Slot` - The target slot index.
- `InventoryModEvent.Inventory` - The target `Inventory`.
- `InventoryModEvent.FInventory` - The Fougerite `FInventory` wrapper.
- `InventoryModEvent.Type` - The reason/type of the modification.
- `InventoryModEvent.IsInternalMove` - Whether this is part of an internal move (not a "real" addition).
- `InventoryModEvent.Cancelled` - Whether the addition is cancelled.
- `InventoryModEvent.Cancel()` - Cancels the item from being added.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnItemAdded += ItemAddedHandler;
}

public override void DeInitialize()
{
    Hooks.OnItemAdded -= ItemAddedHandler;
}

public void ItemAddedHandler(InventoryModEvent e)
{
    Logger.Log(e.ItemName + " was added to an inventory.");
}
```

#### Python
```python
def On_ItemAdded(self, InventoryModEvent):
    Server.Log(InventoryModEvent.ItemName + " was added to an inventory.")
```

#### JavaScript
```javascript
function On_ItemAdded(InventoryModEvent)
{
    Server.Log(InventoryModEvent.ItemName + " was added to an inventory.");
}
```

#### Lua
```lua
function On_ItemAdded(InventoryModEvent)
    Server.Log(InventoryModEvent.ItemName .. " was added to an inventory.")
end
```
