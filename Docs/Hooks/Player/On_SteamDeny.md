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
- `SteamDenyEvent.ClientConnection` - The `ClientConnection` created for this connection.
- `SteamDenyEvent.Reason` - The deny reason string.
- `SteamDenyEvent.ErrorNumber` - The `NetError` associated with the deny.
- `SteamDenyEvent.ForceAllow` - Get/set whether the player is admitted despite the Steam rejection. In
  `SteamAuthMode.Legacy` it starts `false` (set `true` to let the player in, like the old AuthAllow plugin).
  In every other mode it starts as `PolicyAllowed` - a plugin may set it to `false` to tighten the verdict, but
  setting it to `true` never admits a player the configured mode already rejected.
- `SteamDenyEvent.IsValidSteamUser` - Whether Steam Web API confirmed the ticket belongs to this account
  (always `false` in `SteamAuthMode.SteamAccountsUnverified`/`Legacy`, where It only means the ticket contains
  a recognised AppID, which proves nothing on its own).
- `SteamDenyEvent.Mode` - The `SteamAuthMode` (see [`SteamAuth`](../../Classes/SteamAuth.md)) in effect for this
  connection. From safest to least safe: `RustOnly` (Most Safe) -> `RustOwners` (High Safety) ->
  `SteamPaidAccounts` (High Safety) -> `SteamAccounts` (Medium Safety, recommended for RustBuster) ->
  `SteamAccountsUnverified` (Low Safety) -> `Legacy` (Very Low Safety) -> `AllowAll` (Zero Safety).
- `SteamDenyEvent.Ticket` - The parsed `SteamTicketInfo`, or `null` in `Legacy` mode/for malformed tickets.
- `SteamDenyEvent.TicketAppId` - The AppID the ticket claims (0 when unknown).
- `SteamDenyEvent.WebValidation` - The `SteamWebValidation` result, or `null` when no Web API call was made.
- `SteamDenyEvent.PolicyAllowed` - The verdict of the configured `SteamAuthMode` (always `false` in `Legacy`).
- `SteamDenyEvent.PolicyReason` - A human-readable description of why the mode admitted/rejected the player.

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
    Util.Log("Steam denied a connection: " + SteamDenyEvent.Reason)
```

#### JavaScript
```javascript
function On_SteamDeny(SteamDenyEvent)
{
    Util.Log("Steam denied a connection: " + SteamDenyEvent.Reason);
}
```

#### Lua
```lua
function On_SteamDeny(SteamDenyEvent)
    Util.Log("Steam denied a connection: " .. SteamDenyEvent.Reason)
end
```
