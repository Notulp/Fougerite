### Method
`On_BeltUse`

### Description
Runs when a player selects/uses a belt (hotbar) slot.

### C# Event
```csharp
public static event BeltUseDelegate OnBeltUse;
public delegate void BeltUseDelegate(BeltUseEvent beltUseEvent);
```

### Argument(s)
- `BeltUseEvent BeltUseEvent`

### Properties/Methods
- `BeltUseEvent.Player` - The player using the belt slot.
- `BeltUseEvent.SelectedBelt` - The selected belt slot index.
- `BeltUseEvent.InventoryHolder` - The `InventoryHolder` component.
- `BeltUseEvent.Bypassed` - Whether the cooldown was bypassed.
- `BeltUseEvent.Cancelled` - Whether the belt use is cancelled.
- `BeltUseEvent.Cancel()` - Cancels the belt selection.
- `BeltUseEvent.BypassBeltCooldown()` - Bypasses the belt-switch cooldown for this action.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnBeltUse += BeltHandler;
}

public override void DeInitialize()
{
    Hooks.OnBeltUse -= BeltHandler;
}

public void BeltHandler(BeltUseEvent e)
{
    Logger.Log(e.Player.Name + " selected belt slot " + e.SelectedBelt);
}
```

#### Python
```python
def On_BeltUse(self, BeltUseEvent):
    Util.Log(BeltUseEvent.Player.Name + " selected belt slot " + str(BeltUseEvent.SelectedBelt))
```

#### JavaScript
```javascript
function On_BeltUse(BeltUseEvent)
{
    Util.Log(BeltUseEvent.Player.Name + " selected belt slot " + BeltUseEvent.SelectedBelt);
}
```

#### Lua
```lua
function On_BeltUse(BeltUseEvent)
    Util.Log(BeltUseEvent.Player.Name .. " selected belt slot " .. tostring(BeltUseEvent.SelectedBelt))
end
```
