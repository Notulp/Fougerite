### Method
`On_ServerFreeze`

### Description
Runs whenever the native server freeze watchdog ([`FreezeMonitor`](../../../README.md#freezemonitor-watchdog))
reports a state change: the main thread was detected as frozen, a diagnostic dump was written, or the server
recovered from a freeze. See the main [README](../../../README.md#freezemonitor-watchdog) for how the
watchdog itself works (heartbeat timeout, stack sampling, minidumps, auto-kill).

**Runs on a separate thread (`FreezeDiagWorker`), not the main thread.** Do not perform `UnityEngine`
operations from this handler, and keep it fast/exception-safe since the server may genuinely be frozen while
It fires. This applies to every plugin engine (C#, Python, JavaScript, Lua) alike.

### C# Event
```csharp
public static event ServerFreezeDelegate OnServerFreeze;
public delegate void ServerFreezeDelegate(Hooks.ServerFreezeType type, int frozenSeconds, string info);
```

### Argument(s)
- `Hooks.ServerFreezeType type` - The reported state: `Detected`, `Dumped`, or `Recovered`.
- `int frozenSeconds` - How many seconds the server has been frozen for.
- `string info` - Additional diagnostic text about the freeze (e.g. the last known context), may be empty.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnServerFreeze += ServerFreezeHandler;
}

public override void DeInitialize()
{
    Hooks.OnServerFreeze -= ServerFreezeHandler;
}

public void ServerFreezeHandler(Hooks.ServerFreezeType type, int frozenSeconds, string info)
{
    // Careful: this may run while the main thread is frozen, keep it minimal.
    Logger.Log($"Server freeze {type}: {frozenSeconds}s - {info}");
}
```

#### Python
```python
def On_ServerFreeze(self, type, frozenSeconds, info):
    # Careful: this may run while the main thread is frozen, keep it minimal.
    print("Server freeze " + str(type) + ": " + str(frozenSeconds) + "s - " + info)
```

#### JavaScript
```javascript
function On_ServerFreeze(type, frozenSeconds, info)
{
    // Careful: this may run while the main thread is frozen, keep it minimal.
    print("Server freeze " + type + ": " + frozenSeconds + "s - " + info);
}
```

#### Lua
```lua
function On_ServerFreeze(type, frozenSeconds, info)
    -- Careful: this may run while the main thread is frozen, keep it minimal.
    print("Server freeze " .. tostring(type) .. ": " .. frozenSeconds .. "s - " .. info)
end
```
