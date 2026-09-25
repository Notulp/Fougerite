### Method
`On_Logger`

### Description
Runs whenever an internal Fougerite log event is triggered (info/warning/error/debug messages).

### C# Event
```csharp
public static event LoggerDelegate OnLogger;
public delegate void LoggerDelegate(LoggerEvent loggerEvent);
```

### Argument(s)
- `LoggerEvent LoggerEvent`

### Properties/Methods
- `LoggerEvent.Type` - The `LoggerEventType` (Info/Warning/Error/Debug).
- `LoggerEvent.Message` - The logged message text.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnLogger += LoggerHandler;
}

public override void DeInitialize()
{
    Hooks.OnLogger -= LoggerHandler;
}

public void LoggerHandler(LoggerEvent e)
{
    Console.WriteLine("[" + e.Type + "] " + e.Message);
}
```

#### Python
```python
def On_Logger(self, LoggerEvent):
    print("[" + str(LoggerEvent.Type) + "] " + LoggerEvent.Message)
```

#### JavaScript
```javascript
function On_Logger(LoggerEvent)
{
    print("[" + LoggerEvent.Type + "] " + LoggerEvent.Message);
}
```

#### Lua
```lua
function On_Logger(LoggerEvent)
    print("[" .. tostring(LoggerEvent.Type) .. "] " .. LoggerEvent.Message)
end
```
