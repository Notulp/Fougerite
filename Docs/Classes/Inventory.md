### Class
`Fougerite.FInventory` / `Fougerite.EntityInv` / `Fougerite.EntityItem` / `Fougerite.PlayerInv` / `Fougerite.PlayerItem`

### Description
These five classes are Fougerite's wrappers around the raw Rust `Inventory`/`IInventoryItem` engine objects, and
cover every place a plugin deals with items: a world container's contents (`EntityInv`, via
[`Entity.Inventory`](Entity.md)), a player's backpack/belt/armor (`PlayerInv`, via [`Player.Inventory`](Player.md)),
a single slot inside either of those (`EntityItem`/`PlayerItem`), and the generic inventory wrapper used inside
`On_ItemAdded`/`On_ItemRemoved`/`On_ItemMove` hooks (`FInventory`, see
[`On_ItemMove`](../Hooks/Items/On_ItemMove.md)). All of them operate on item *names* (the `ItemDataBlock`
name, e.g. `"Wood"`, `"rifle.ak"`) resolved via `DatablockDictionary` under the hood - you never need to touch
`ItemDataBlock` yourself for basic add/remove/check operations.

### FInventory
Generic wrapper around a raw `Inventory` component, independent of whether it belongs to an `Entity` or a
`Player`. This is what you receive directly in item-related hooks (`FInventory` parameters), since at that point
Fougerite doesn't need to know/care which kind of container it is.
- `AddItem(string name)` / `AddItem(string name, int amount)` - adds an item by name.
- `AddItemTo(string name, int slot)` / `AddItemTo(string name, int slot, int amount)` - adds into a specific slot.
- `RemoveItem(string name, int amount = 1)` / `RemoveItem(int slot, int amount = 1)` - removes by name or slot.
- `HasItem(string name, int amount = 1)` - checks for at least `amount` uses/stacks of an item.
- `MoveItem(int s1, int s2)` - moves the contents of slot `s1` into empty slot `s2`.
- `ClearAll()` - empties every slot.
- `Items` - `EntityItem[]` snapshot of every slot (length == `SlotCount`).
- `FreeSlots` / `SlotCount` - free/total slot counts.

### EntityInv
Same shape as `FInventory` (`AddItem`/`AddItemTo`/`RemoveItem`/`MoveItem`/`HasItem`/`ClearAll`/`Items`/
`FreeSlots`/`SlotCount`), plus:
- `Entity` - the owning [`Entity`](Entity.md) (chest, stash, furnace, ...).
- `InternalInventory` - the raw Rust `Inventory` component, for anything not exposed here.

> For a Large Wooden Box the valid slot range is `1-35`, Medium is `1-11`, a Stash is `1-3` (slot `0` is the
> container itself in some containers) - see `AddItemTo`'s parameter docs in the source for the exact ranges
> per container type you're targeting.

### EntityItem
A single slot of an `EntityInv`/`FInventory` (world container):
- `IsEmpty()` - whether the slot has no item.
- `Name` (get/set) - the `ItemDataBlock` name, or `"Empty slot"` when empty.
- `Quantity` (get/set) / `UsesLeft` (get/set) - stack size / remaining uses (ammo count, condition uses, etc.).
  `Quantity` always reads as `1` for non-stackable items (see `Util.UStackable`).
- `Slot` - the current slot index.
- `Inventory` / `RInventoryItem` - the raw Rust `Inventory`/`IInventoryItem`, for anything not exposed here.
- `Drop()` - removes the item and spawns it in the world as a loot bag (`ItemPickup`), returning it.
- `IsEntityInv` / `EntityInv` - whether/which wrapper (`EntityInv` or `FInventory`) owns this slot.
- Weapon mod API (firearms/shotguns only, no-ops otherwise):
  - `TotalModSlots` (get/set) - total mod slots on the weapon; setting it rebuilds the item's network state.
  - `UsedModSlots` / `FreeModSlots` - how many mod slots are occupied/free.
  - `AddMod(string modName, bool removeFromInv = true)` - installs a mod into the first free slot, optionally
    consuming one from the same inventory.
  - `RemoveMod(int slot, bool giveBack = true)` - uninstalls the mod in `slot` (0-4), optionally returning it
    to the inventory.
  - `ClearMods(bool giveBack = true)` - removes every mod.
  - `GetMods()` - `List<string>` of mod names per slot (`"Empty slot"` for unused ones).

### PlayerInv
A player's full inventory ([`Player.Inventory`](Player.md)), split into three logical groups:
- `Items` - `PlayerItem[30]`, the main backpack grid.
- `BarItems` - `PlayerItem[6]`, the hotbar/belt.
- `ArmorItems` - `PlayerItem[4]`, the armor/wearable slots.
- `ActiveItem` - the `PlayerItem` currently held in-hand, or `null`.
- `AddItem(string name)` / `AddItem(string name, int amount)` - adds via the game's native `give` console
  command path (so it also updates crafting/quick-give UI correctly).
- `AddItem(PlayerItem item, int i = 1)` - adds `i` more of an existing `PlayerItem`'s type.
- `AddItemTo(string name, int slot)` / `AddItemTo(string name, int slot, int amount)` - adds into a specific
  slot (automatically targets belt/armor `Inventory.Slot.Kind` based on the slot index).
- `HasItem(string name)` / `HasItem(string name, int number)` - searches `Items`, `BarItems` and `ArmorItems`.
- `RemoveItem(PlayerItem pi)` / `RemoveItem(int slot)` / `RemoveItem(int slot, int number)` /
  `RemoveItem(string name, int number = 1)` / `RemoveItemAll(string name)` - removal across all three groups.
- `MoveItem(int s1, int s2)` - moves slot `s1` into empty slot `s2`.
- `DropItem(PlayerItem pi)` / `DropItem(int slot)` / `DropAll()` - drops one or every item on the ground.
- `Clear()` / `ClearItems()` / `ClearArmor()` / `ClearBar()` / `ClearAll()` - bulk-removal helpers for each
  group (or the whole inventory).
- `FreeSlots` - free slots in the main backpack grid (`Items`, excluding bar/armor).
- `InternalInventory` / `PlayerInventory` - the raw Rust `Inventory` and, when applicable, the more specific
  `PlayerInventory` component.

### PlayerItem
Same shape as `EntityItem` (`IsEmpty`/`Name`/`Quantity`/`UsesLeft`/`Slot`/`Drop`/mod API, etc.), plus:
- `Consume(int qty)` - reduces `UsesLeft` by `qty` without fully removing the item (used internally by
  `PlayerInv.RemoveItem`'s partial-stack logic, also usable directly).
- `TryCombine(PlayerItem pi)` - attempts to combine this item with `pi` (e.g. merging/crafting-like stacking
  rules the engine defines for that item type).
- `TryStack(PlayerItem pi)` - attempts to merge `pi`'s uses into this stack (same item type), consuming `pi`.

### Example - C# (restocking a Large Wooden Box)
```csharp
public void RestockBox(Entity box)
{
    EntityInv inv = box.Inventory;
    if (inv.FreeSlots > 0 && !inv.HasItem("Wood", 1000))
    {
        inv.AddItem("Wood", 1000);
    }
}
```

### Example - C# (checking/removing a crafting cost from a player)
```csharp
public bool TryPay(Player player, string item, int amount)
{
    PlayerInv inv = player.Inventory;
    if (!inv.HasItem(item, amount))
    {
        return false;
    }

    inv.RemoveItem(item, amount);
    return true;
}
```

### Example - C# (logging weapon mods whenever an item is moved)
```csharp
public void ItemMoveHandler(ItemMoveEvent e)
{
    EntityItem item = e.ToInventory.Items[e.ToSlot];
    if (!item.IsEmpty() && item.GetMods().Count > 0)
    {
        Logger.Log($"{item.Name} moved by {e.Player.Name} with mods: {string.Join(", ", item.GetMods())}");
    }
}
```

### Example - Python (giving a kit on connect)
```python
def On_PlayerSpawn(self, Player, SpawnPoint):
    Player.Inventory.AddItem("Wood", 500)
    Player.Inventory.AddItem("rock", 1)
    Util.Log(Player.Name + " received a starter kit.")
```

See also: [`Entity`](Entity.md) · [`Player`](Player.md) · [`Util`](Util.md) ·
[`On_ItemMoved`](../Hooks/Entity/On_ItemMoved.md)
