### Method
`On_ItemModInstall`

### Description
Runs when a player installs an attachment/mod (scope, silencer, extended mag, etc.) onto a weapon.

### C# Event
```csharp
public static class OnItemMod<T> where T : HeldItemDataBlock
{
    public static event ItemModInstalledEventDelegate<T> OnItemModInstall;
}
public delegate void ItemModInstalledEventDelegate<T>(ItemModInstallEvent<T> e) where T : HeldItemDataBlock;
```

> This hook is generic per weapon datablock type (e.g. `Hooks.OnItemMod<BulletWeaponDataBlock>.OnItemModInstall`).
> Script plugins subscribe to it simply via `On_ItemModInstall`.

### Argument(s)
- `ItemModInstallEvent<T> ItemModInstallEvent`

### Properties/Methods
- `ItemModInstallEvent.Player` - The player installing the mod.
- `ItemModInstallEvent.HeldItem` - The weapon the mod is being installed on.
- `ItemModInstallEvent.ModData` - The `ItemModDataBlock` for the mod being installed.
- `ItemModInstallEvent.ItemRep` - The `ItemRepresentation`.
- `ItemModInstallEvent.Slot` - The mod slot index.
- `ItemModInstallEvent.Cancelled` - Whether the install is cancelled.
- `ItemModInstallEvent.Cancel()` - Cancels the mod install.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnItemMod<BulletWeaponDataBlock>.OnItemModInstall += ModInstallHandler;
}

public void ModInstallHandler(ItemModInstallEvent<BulletWeaponDataBlock> e)
{
    Logger.Log(e.Player.Name + " installed a mod: " + e.ModData.name);
}
```

#### Python
```python
def On_ItemModInstall(self, ModInstallEvent):
    Util.Log(ModInstallEvent.Player.Name + " installed a mod.")
```

#### JavaScript
```javascript
function On_ItemModInstall(ModInstallEvent)
{
    Util.Log(ModInstallEvent.Player.Name + " installed a mod.");
}
```

#### Lua
```lua
function On_ItemModInstall(ModInstallEvent)
    Util.Log(ModInstallEvent.Player.Name .. " installed a mod.")
end
```
