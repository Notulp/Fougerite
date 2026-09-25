### Method
`On_ArmorEquip` / `On_ArmorUnEquip`

### Description
Runs when a player equips or unequips a piece of armor.

### C# Event
```csharp
public static event ArmorEquippedEventDelegate OnArmorEquip;
public static event ArmorEquippedEventDelegate OnArmorUnEquip;
public delegate void ArmorEquippedEventDelegate(ArmorEquipEvent ae);
```

### Argument(s)
- `ArmorEquipEvent ArmorEquipEvent`

### Properties/Methods
- `ArmorEquipEvent.Player` - The player equipping/unequipping.
- `ArmorEquipEvent.Item` - The `IEquipmentItem` armor piece.
- `ArmorEquipEvent.ArmorBlock` - The `ArmorDataBlock`.
- `ArmorEquipEvent.ChangeType` - The `ArmorChangeType` (Equip/UnEquip).

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnArmorEquip += ArmorEquipHandler;
    Hooks.OnArmorUnEquip += ArmorUnEquipHandler;
}

public override void DeInitialize()
{
    Hooks.OnArmorEquip -= ArmorEquipHandler;
    Hooks.OnArmorUnEquip -= ArmorUnEquipHandler;
}

public void ArmorEquipHandler(ArmorEquipEvent e)
{
    Logger.Log(e.Player.Name + " equipped armor.");
}

public void ArmorUnEquipHandler(ArmorEquipEvent e)
{
    Logger.Log(e.Player.Name + " unequipped armor.");
}
```

#### Python
```python
def On_ArmorEquip(self, ArmorEquipEvent):
    Server.Log(ArmorEquipEvent.Player.Name + " equipped armor.")

def On_ArmorUnEquip(self, ArmorEquipEvent):
    Server.Log(ArmorEquipEvent.Player.Name + " unequipped armor.")
```

#### JavaScript
```javascript
function On_ArmorEquip(ArmorEquipEvent)
{
    Server.Log(ArmorEquipEvent.Player.Name + " equipped armor.");
}

function On_ArmorUnEquip(ArmorEquipEvent)
{
    Server.Log(ArmorEquipEvent.Player.Name + " unequipped armor.");
}
```

#### Lua
```lua
function On_ArmorEquip(ArmorEquipEvent)
    Server.Log(ArmorEquipEvent.Player.Name .. " equipped armor.")
end

function On_ArmorUnEquip(ArmorEquipEvent)
    Server.Log(ArmorEquipEvent.Player.Name .. " unequipped armor.")
end
```
