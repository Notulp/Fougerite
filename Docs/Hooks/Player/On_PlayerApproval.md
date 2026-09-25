### Method
`On_PlayerApproval`

### Description
Runs when a connecting player is being approved by the server, before they are allowed to spawn in.
Allows denying or force-accepting a connection.

### C# Event
```csharp
public static event PlayerApprovalDelegate OnPlayerApproval;
public delegate void PlayerApprovalDelegate(PlayerApprovalEvent e);
```

### Argument(s)
- `PlayerApprovalEvent PlayerApprovalEvent`

### Properties/Methods
- `PlayerApprovalEvent.SteamID` - The connecting player's SteamID.
- `PlayerApprovalEvent.Name` - The connecting player's name.
- `PlayerApprovalEvent.IP` - The connecting player's IP.
- `PlayerApprovalEvent.AboutToDeny` - Whether the connection is about to be denied.
- `PlayerApprovalEvent.ForceAccept` - Set to `true` to force-accept the connection.
- `PlayerApprovalEvent.ServerHasPlayer` - Whether the server already has this player cached.
- `PlayerApprovalEvent.DenyReason` - The reason that will be used if denied.
- `PlayerApprovalEvent.Deny(uLink.NetworkConnectionError reason)` - Denies the connection with a reason.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerApproval += ApprovalHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerApproval -= ApprovalHandler;
}

public void ApprovalHandler(PlayerApprovalEvent e)
{
    if (IsBanned(e.SteamID))
    {
        e.Deny(uLink.NetworkConnectionError.ConnectionBanned);
    }
}
```

#### Python
```python
def On_PlayerApproval(self, ApprovalEvent):
    if IsBanned(ApprovalEvent.SteamID):
        ApprovalEvent.Deny(0)
```

#### JavaScript
```javascript
function On_PlayerApproval(ApprovalEvent)
{
    if (IsBanned(ApprovalEvent.SteamID))
    {
        ApprovalEvent.Deny(0);
    }
}
```

#### Lua
```lua
function On_PlayerApproval(ApprovalEvent)
    if IsBanned(ApprovalEvent.SteamID) then
        ApprovalEvent.Deny(0)
    end
end
```
