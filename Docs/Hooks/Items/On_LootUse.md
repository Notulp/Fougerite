### Method
`On_LootUse`

### Description
Runs when a player starts looting a container/corpse/deployable (loot start).

### C# Event
```csharp
public static event LootEnterDelegate OnLootUse;
public delegate void LootEnterDelegate(LootStartEvent lootStartEvent);
```

### Argument(s)
- `LootStartEvent LootStartEvent`

### Properties/Methods
- `LootStartEvent.Player` - The player starting to loot.
- `LootStartEvent.Entity` - The entity being looted, if applicable.
- `LootStartEvent.LootableObject` - The `LootableObject` component.
- `LootStartEvent.Useable` - The `Useable` component used to interact.
- `LootStartEvent.IsObject` - Whether the target is a static object (not an `Entity`).
- `LootStartEvent.LootName` - The display name of the loot container.
- `LootStartEvent.RustInventory` - The raw `Inventory`.
- `LootStartEvent.OccupiedText` - Text shown when the loot is occupied by another player.
- `LootStartEvent.IsCancelled` - Whether looting is cancelled.
- `LootStartEvent.Cancel()` - Cancels the loot action.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnLootUse += LootHandler;
}

public override void DeInitialize()
{
    Hooks.OnLootUse -= LootHandler;
}

public void LootHandler(LootStartEvent e)
{
    Logger.Log(e.Player.Name + " is looting " + e.LootName);
}
```

#### Python
```python
def On_LootUse(self, LootStartEvent):
    Server.Log(LootStartEvent.Player.Name + " is looting " + LootStartEvent.LootName)
```

#### JavaScript
```javascript
function On_LootUse(LootStartEvent)
{
    Server.Log(LootStartEvent.Player.Name + " is looting " + LootStartEvent.LootName);
}
```

#### Lua
```lua
function On_LootUse(LootStartEvent)
    Server.Log(LootStartEvent.Player.Name .. " is looting " .. LootStartEvent.LootName)
end
```
