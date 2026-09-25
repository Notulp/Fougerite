### Method
`On_SteamDeny`

### Description
Runs when a connecting player gets kicked/denied by Steam (VAC/Steam validation failure), before Fougerite's
own approval logic runs.

### C# Event
```csharp
public static event SteamDenyDelegate OnSteamDeny;
public delegate void SteamDenyDelegate(SteamDenyEvent sde);
```

### Argument(s)
- `SteamDenyEvent SteamDenyEvent`

### Properties/Methods
- `SteamDenyEvent.NetUser` - The `NetUser` instance related to the connection.
- `SteamDenyEvent.Reason` - The deny reason string.
- `SteamDenyEvent.ErrorNumber` - The `NetError` associated with the deny.
- `SteamDenyEvent.ForceAllow` - Set to `true` to override the deny and let the player in anyway.
- `SteamDenyEvent.IsValidSteamUser` - Whether Steam considers the connecting user valid.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnSteamDeny += SteamDenyHandler;
}

public override void DeInitialize()
{
    Hooks.OnSteamDeny -= SteamDenyHandler;
}

public void SteamDenyHandler(SteamDenyEvent e)
{
    Logger.Log("Steam denied a connection: " + e.Reason);
}
```

#### Python
```python
def On_SteamDeny(self, SteamDenyEvent):
    Server.Log("Steam denied a connection: " + SteamDenyEvent.Reason)
```

#### JavaScript
```javascript
function On_SteamDeny(SteamDenyEvent)
{
    Server.Log("Steam denied a connection: " + SteamDenyEvent.Reason);
}
```

#### Lua
```lua
function On_SteamDeny(SteamDenyEvent)
    Server.Log("Steam denied a connection: " .. SteamDenyEvent.Reason)
end
```
