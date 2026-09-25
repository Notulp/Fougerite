### Method
`On_ItemRemoved`

### Description
Runs when an item is removed from an inventory (consumed, dropped, crafted away, etc.).

### C# Event
```csharp
public static event ItemRemovedDelegate OnItemRemoved;
public delegate void ItemRemovedDelegate(InventoryModEvent e);
```

### Argument(s)
- `InventoryModEvent InventoryModEvent` - See [`On_ItemAdded`](On_ItemAdded.md) for its properties.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnItemRemoved += ItemRemovedHandler;
}

public override void DeInitialize()
{
    Hooks.OnItemRemoved -= ItemRemovedHandler;
}

public void ItemRemovedHandler(InventoryModEvent e)
{
    Logger.Log(e.ItemName + " was removed from an inventory.");
}
```

#### Python
```python
def On_ItemRemoved(self, InventoryModEvent):
    Server.Log(InventoryModEvent.ItemName + " was removed from an inventory.")
```

#### JavaScript
```javascript
function On_ItemRemoved(InventoryModEvent)
{
    Server.Log(InventoryModEvent.ItemName + " was removed from an inventory.");
}
```

#### Lua
```lua
function On_ItemRemoved(InventoryModEvent)
    Server.Log(InventoryModEvent.ItemName .. " was removed from an inventory.")
end
```
