### Method
`On_PlayerConnected`

### Description
Runs when a player has finished connecting to the server.

### C# Event
```csharp
public static event ConnectionHandlerDelegate OnPlayerConnected;
public delegate void ConnectionHandlerDelegate(Player player);
```

### Argument(s)
- `Player Player` - The player who connected.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerConnected += ConnectHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerConnected -= ConnectHandler;
}

public void ConnectHandler(Fougerite.Player player)
{
    Server.GetServer().Broadcast(player.Name + " has joined the server!");
}
```

#### Python
```python
def On_PlayerConnected(self, Player):
    Server.Broadcast(Player.Name + " has joined the server!")
```

#### JavaScript
```javascript
function On_PlayerConnected(Player)
{
    Server.Broadcast(Player.Name + " has joined the server!");
}
```

#### Lua
```lua
function On_PlayerConnected(Player)
    Server.Broadcast(Player.Name .. " has joined the server!")
end
```
