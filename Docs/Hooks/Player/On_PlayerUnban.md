### Method
`On_PlayerUnban`

### Description
Runs when a player/name/IP/ID is being unbanned.

### C# Event
```csharp
public static event UnbanEventDelegate OnPlayerUnban;
public delegate void UnbanEventDelegate(UnbanEvent unbanEvent);
```

### Argument(s)
- `UnbanEvent UnbanEvent`

### Properties/Methods
- `UnbanEvent.UnbanType` - The type of unban being issued (Name/IP/ID).
- `UnbanEvent.UnbanSender` - The `Player` who issued the unban (if any).
- `UnbanEvent.IP` / `UnbanEvent.ID` / `UnbanEvent.Name` - Identifying info of the unbanned entity.
- `UnbanEvent.UnbannerName` - Name of the admin/plugin that issued the unban.
- `UnbanEvent.Cancelled` - Whether the unban was cancelled.
- `UnbanEvent.Cancel()` - Cancels the unban.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerUnban += UnbanHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerUnban -= UnbanHandler;
}

public void UnbanHandler(UnbanEvent e)
{
    Server.GetServer().Broadcast(e.Name + " was unbanned.");
}
```

#### Python
```python
def On_PlayerUnban(self, UnbanEvent):
    Server.Broadcast(UnbanEvent.Name + " was unbanned.")
```

#### JavaScript
```javascript
function On_PlayerUnban(UnbanEvent)
{
    Server.Broadcast(UnbanEvent.Name + " was unbanned.");
}
```

#### Lua
```lua
function On_PlayerUnban(UnbanEvent)
    Server.Broadcast(UnbanEvent.Name .. " was unbanned.")
end
```
