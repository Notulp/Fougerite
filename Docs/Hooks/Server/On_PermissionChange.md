### Method
`On_PermissionChange`

### Description
Runs when a permission system action is about to be performed (group created, permission granted, etc.).

### C# Event
```csharp
public static event PermissionEventDelegate OnPermissionChange;
public delegate void PermissionEventDelegate(PermissionEvent e);
```

### Argument(s)
- `PermissionEvent PermissionEvent`

### Properties/Methods
- `PermissionEvent.ActionType` - The `PermissionActionType` being performed.
- `PermissionEvent.SteamId` - The target player's SteamID, if applicable.
- `PermissionEvent.GroupName` - The target permission group name.
- `PermissionEvent.Permission` - The permission string.
- `PermissionEvent.NickName` - The target player's nickname, if known.
- `PermissionEvent.Cancelled` - Whether the action is cancelled.
- `PermissionEvent.Cancel()` - Cancels the permission action.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPermissionChange += PermissionHandler;
}

public override void DeInitialize()
{
    Hooks.OnPermissionChange -= PermissionHandler;
}

public void PermissionHandler(PermissionEvent e)
{
    Logger.Log("Permission change: " + e.ActionType + " on group " + e.GroupName);
}
```

#### Python
```python
def On_PermissionChange(self, PermissionEvent):
    Server.Log("Permission change: " + str(PermissionEvent.ActionType) + " on group " + PermissionEvent.GroupName)
```

#### JavaScript
```javascript
function On_PermissionChange(PermissionEvent)
{
    Server.Log("Permission change: " + PermissionEvent.ActionType + " on group " + PermissionEvent.GroupName);
}
```

#### Lua
```lua
function On_PermissionChange(PermissionEvent)
    Server.Log("Permission change: " .. tostring(PermissionEvent.ActionType) .. " on group " .. PermissionEvent.GroupName)
end
```
