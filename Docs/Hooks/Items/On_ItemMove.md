### Method
`On_ItemMove`

### Description
Runs when an item is moved between inventory slots (drag & drop, shift-click, etc.), even across
different inventories (e.g. from a player's inventory to a container).

### C# Event
```csharp
public static event ItemMoveEventDelegate OnItemMove;
public delegate void ItemMoveEventDelegate(ItemMoveEvent itemMoveEvent);
```

### Argument(s)
- `ItemMoveEvent ItemMoveEvent`

### Properties/Methods
- `ItemMoveEvent.Player` - The player moving the item.
- `ItemMoveEvent.FromInventory` / `ItemMoveEvent.ToInventory` - Source/destination inventories.
- `ItemMoveEvent.FromSlot` / `ItemMoveEvent.ToSlot` - Source/destination slot indexes.
- `ItemMoveEvent.SlotOperation` - The raw `Inventory.SlotOperationsInfo`.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnItemMove += ItemMoveHandler;
}

public override void DeInitialize()
{
    Hooks.OnItemMove -= ItemMoveHandler;
}

public void ItemMoveHandler(ItemMoveEvent e)
{
    Logger.Log(e.Player.Name + " moved an item from slot " + e.FromSlot + " to " + e.ToSlot);
}
```

#### Python
```python
def On_ItemMove(self, ItemMoveEvent):
    Util.Log(ItemMoveEvent.Player.Name + " moved an item.")
```

#### JavaScript
```javascript
function On_ItemMove(ItemMoveEvent)
{
    Util.Log(ItemMoveEvent.Player.Name + " moved an item.");
}
```

#### Lua
```lua
function On_ItemMove(ItemMoveEvent)
    Util.Log(ItemMoveEvent.Player.Name .. " moved an item.")
end
```
