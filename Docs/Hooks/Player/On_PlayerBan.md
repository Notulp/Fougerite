### Method
`On_PlayerBan`

### Description
Runs when a player is being banned (by name/IP/ID or by an admin).

### C# Event
```csharp
public static event BanEventDelegate OnPlayerBan;
public delegate void BanEventDelegate(BanEvent banEvent);
```

### Argument(s)
- `BanEvent BanEvent`

### Properties/Methods
- `BanEvent.BanType` - The type of ban being issued.
- `BanEvent.BannedUser` - The `Player` being banned (if online).
- `BanEvent.BanSender` - The `Player` who issued the ban (if any).
- `BanEvent.IP` / `BanEvent.ID` / `BanEvent.Name` - Identifying info of the banned entity.
- `BanEvent.Reason` - The ban reason.
- `BanEvent.BannerName` - Name of the admin/plugin that issued the ban.
- `BanEvent.Cancelled` - Whether the ban was cancelled.
- `BanEvent.Cancel()` - Cancels the ban.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerBan += BanHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerBan -= BanHandler;
}

public void BanHandler(BanEvent e)
{
    Server.GetServer().Broadcast(e.Name + " was banned: " + e.Reason);
}
```

#### Python
```python
def On_PlayerBan(self, BanEvent):
    Server.Broadcast(BanEvent.Name + " was banned: " + BanEvent.Reason)
```

#### JavaScript
```javascript
function On_PlayerBan(BanEvent)
{
    Server.Broadcast(BanEvent.Name + " was banned: " + BanEvent.Reason);
}
```

#### Lua
```lua
function On_PlayerBan(BanEvent)
    Server.Broadcast(BanEvent.Name .. " was banned: " .. BanEvent.Reason)
end
```
