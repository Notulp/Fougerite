### Method
`On_CommandRestriction`

### Description
Runs when a chat/console command is restricted or unrestricted, either globally or for a specific player.
This is a tracking hook: Fougerite doesn't have a central command-registration API, so this notifies plugins
whenever the restriction lists change.

### C# Event
```csharp
public static event CommandRestrictionEventDelegate OnCommandRestriction;
public delegate void CommandRestrictionEventDelegate(CommandRestrictionEvent commandRestrictionEvent);
```

### Argument(s)
- `CommandRestrictionEvent CommandRestrictionEvent`

### Properties/Methods
- `CommandRestrictionEvent.Player` - The player being restricted/unrestricted. `null` if `CommandRestrictionScale` is `Global`.
- `CommandRestrictionEvent.Command` - The command being restricted/unrestricted.
- `CommandRestrictionEvent.IsBeingRestricted` - `true` if it's being restricted, `false` if unrestricted.
- `CommandRestrictionEvent.CommandRestrictionType` - `Command` or `ConsoleCommand`.
- `CommandRestrictionEvent.CommandRestrictionScale` - `Global` or `SpecificPlayer`.
- `CommandRestrictionEvent.Cancelled` - Whether the event was cancelled.
- `CommandRestrictionEvent.Cancel()` - Cancels the restriction change.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnCommandRestriction += RestrictionHandler;
}

public override void DeInitialize()
{
    Hooks.OnCommandRestriction -= RestrictionHandler;
}

public void RestrictionHandler(CommandRestrictionEvent e)
{
    Logger.Log($"Command '{e.Command}' restricted: {e.IsBeingRestricted}");
}
```

#### Python
```python
def On_CommandRestriction(self, RestrictionEvent):
    Server.Log("Command '" + RestrictionEvent.Command + "' restricted: " + str(RestrictionEvent.IsBeingRestricted))
```

#### JavaScript
```javascript
function On_CommandRestriction(RestrictionEvent)
{
    Server.Log("Command '" + RestrictionEvent.Command + "' restricted: " + RestrictionEvent.IsBeingRestricted);
}
```

#### Lua
```lua
function On_CommandRestriction(RestrictionEvent)
    Server.Log("Command '" .. RestrictionEvent.Command .. "' restricted: " .. tostring(RestrictionEvent.IsBeingRestricted))
end
```
