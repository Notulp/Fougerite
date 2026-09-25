### Method
`On_Command`

### Description
Runs when a player (or the console) executes a chat command that starts with `/`.

### C# Event
```csharp
public static event CommandHandlerDelegate OnCommand;
public delegate void CommandHandlerDelegate(Player player, string cmd, string[] args);
```

### Argument(s)
- `Player Player` - The player who executed the command. May be `null` for server console commands.
- `string Command` - The command name (without the leading `/`).
- `string[] Args` - The arguments passed after the command name.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnCommand += CommandHandler;
}

public override void DeInitialize()
{
    Hooks.OnCommand -= CommandHandler;
}

public void CommandHandler(Fougerite.Player player, string cmd, string[] args)
{
    if (cmd == "home")
    {
        player.Notice("", "Teleporting you home!", 4f);
    }
}
```

#### Python
```python
def On_Command(self, Player, Command, Args):
    if Command == "home":
        Player.Message("Teleporting you home!")
```

#### JavaScript
```javascript
function On_Command(Player, Command, Args)
{
    if (Command === "home")
    {
        Player.Message("Teleporting you home!");
    }
}
```

#### Lua
```lua
function On_Command(Player, Command, Args)
    if Command == "home" then
        Player.Message("Teleporting you home!")
    end
end
```
