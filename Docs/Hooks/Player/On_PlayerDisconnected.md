### Method
`On_PlayerDisconnected`

### Description
Runs when a player disconnects from the server.

### C# Event
```csharp
public static event DisconnectionHandlerDelegate OnPlayerDisconnected;
public delegate void DisconnectionHandlerDelegate(Player player);
```

### Argument(s)
- `Player Player` - The player who disconnected.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerDisconnected += DisconnectHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerDisconnected -= DisconnectHandler;
}

public void DisconnectHandler(Fougerite.Player player)
{
    Server.GetServer().Broadcast(player.Name + " has left the server!");
}
```

#### Python
```python
def On_PlayerDisconnected(self, Player):
    Server.Broadcast(Player.Name + " has left the server!")
```

#### JavaScript
```javascript
function On_PlayerDisconnected(Player)
{
    Server.Broadcast(Player.Name + " has left the server!");
}
```

#### Lua
```lua
function On_PlayerDisconnected(Player)
    Server.Broadcast(Player.Name .. " has left the server!")
end
```
