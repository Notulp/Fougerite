### Method
`On_VoiceChat`

### Description
Runs when a player is talking on the in-game microphone (voice chat), so nearby players can hear them.

### C# Event
```csharp
public static event ShowTalkerDelegate OnShowTalker;
public delegate void ShowTalkerDelegate(uLink.NetworkPlayer player, Player p);
```

### Argument(s)
- `uLink.NetworkPlayer NetworkPlayer` - The low-level network player who is talking.
- `Player Player` - The Fougerite player wrapper for the talker.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnShowTalker += TalkerHandler;
}

public override void DeInitialize()
{
    Hooks.OnShowTalker -= TalkerHandler;
}

public void TalkerHandler(uLink.NetworkPlayer netPlayer, Fougerite.Player player)
{
    Logger.Log(player.Name + " is talking.");
}
```

#### Python
```python
def On_VoiceChat(self, NetworkPlayer, Player):
    Server.Log(Player.Name + " is talking.")
```

#### JavaScript
```javascript
function On_VoiceChat(NetworkPlayer, Player)
{
    Server.Log(Player.Name + " is talking.");
}
```

#### Lua
```lua
function On_VoiceChat(NetworkPlayer, Player)
    Server.Log(Player.Name .. " is talking.")
end
```
