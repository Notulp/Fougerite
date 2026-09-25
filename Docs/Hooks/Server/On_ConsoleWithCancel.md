### Method
`On_ConsoleWithCancel`

### Description
Runs when a console command is received (either from the server console or an in-game F1 console), allowing
the command to be cancelled.

### C# Event
```csharp
public static event ConsoleHandlerWithCancelDelegate OnConsoleReceivedWithCancel;
public delegate void ConsoleHandlerWithCancelDelegate(ref ConsoleSystem.Arg arg, bool external, ConsoleEvent ce);
```

### Argument(s)
- `ConsoleSystem.Arg Arg` - The raw console argument (passed by `ref` in C#).
- `bool External` - Whether the command came from an external source (e.g. RCON).
- `ConsoleEvent ConsoleEvent`

### Properties/Methods
- `ConsoleEvent.Cancelled` - Whether the command is cancelled.
- `ConsoleEvent.Cancel()` - Cancels the console command from executing.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnConsoleReceivedWithCancel += ConsoleHandler;
}

public override void DeInitialize()
{
    Hooks.OnConsoleReceivedWithCancel -= ConsoleHandler;
}

public void ConsoleHandler(ref ConsoleSystem.Arg arg, bool external, ConsoleEvent ce)
{
    Logger.Log("Console command: " + arg.ArgsStr);
}
```

#### Python
```python
def On_ConsoleWithCancel(self, Arg, External, ConsoleEvent):
    Util.Log("Console command received.")
```

#### JavaScript
```javascript
function On_ConsoleWithCancel(Arg, External, ConsoleEvent)
{
    Util.Log("Console command received.");
}
```

#### Lua
```lua
function On_ConsoleWithCancel(Arg, External, ConsoleEvent)
    Util.Log("Console command received.")
end
```
