### Method
`On_ItemPickup`

### Description
Runs when a player picks up an item from the ground.

### C# Event
```csharp
public static event ItemPickupDelegate OnItemPickup;
public delegate void ItemPickupDelegate(ItemPickupEvent itemPickupEvent);
```

### Argument(s)
- `ItemPickupEvent ItemPickupEvent`

### Properties/Methods
- `ItemPickupEvent.Player` - The player picking up the item.
- `ItemPickupEvent.Item` - The `IInventoryItem` being picked up.
- `ItemPickupEvent.Inventory` - The target inventory.
- `ItemPickupEvent.PickupEventType` - Enum describing the pickup type.
- `ItemPickupEvent.Result` - The `Inventory.AddExistingItemResult`.
- `ItemPickupEvent.Cancelled` - Whether the pickup is cancelled.
- `ItemPickupEvent.Cancel()` - Cancels the pickup.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnItemPickup += PickupHandler;
}

public override void DeInitialize()
{
    Hooks.OnItemPickup -= PickupHandler;
}

public void PickupHandler(ItemPickupEvent e)
{
    Logger.Log(e.Player.Name + " picked up " + e.Item.GetDatablock().name);
}
```

#### Python
```python
def On_ItemPickup(self, ItemPickupEvent):
    Server.Log(ItemPickupEvent.Player.Name + " picked up an item.")
```

#### JavaScript
```javascript
function On_ItemPickup(ItemPickupEvent)
{
    Server.Log(ItemPickupEvent.Player.Name + " picked up an item.");
}
```

#### Lua
```lua
function On_ItemPickup(ItemPickupEvent)
    Server.Log(ItemPickupEvent.Player.Name .. " picked up an item.")
end
```
