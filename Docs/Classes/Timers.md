### Class
`TimedEvent` (created via `Plugin.CreateTimer` / `Plugin.CreateParallelTimer`) and
`SystemTimerEvent` (created via `Util.CreateSystemTimer` / `Util.CreateParallelSystemTimer`)

### Description
Fougerite plugins can schedule repeated/delayed logic using timers. There are **two different timer
implementations** and it is important to understand the difference, since using the wrong one for the
wrong job can crash the server or silently stop working after a long uptime.

| | Normal Timer (`TimedEvent`) | System Timer (`SystemTimerEvent`) |
|---|---|---|
| Created via | `Plugin.CreateTimer` / `Plugin.CreateParallelTimer` | `Util.CreateSystemTimer` / `Util.CreateParallelSystemTimer` |
| Underlying implementation | A `UnityEngine.GameObject` + `MonoBehaviour` running a `Coroutine` (`WaitForSeconds`) | A plain `System.Timers.Timer` |
| Which thread does it fire on? | **The Unity main thread** (same thread as the game loop) | **A .NET ThreadPool thread** (NOT the main thread) |
| Safe to call UnityEngine/World stuff directly? | Yes | **No** - see warning below |
| Recommended usage | Any logic that touches `World`, `Entity`, `Player`, or any other UnityEngine-backed object/method | Purely managed/background logic: HTTP calls, file I/O, database queries, heavy math, etc. |
| Caveats | None special | Long-running `System.Timers.Timer` instances are known to silently stop firing after the server has been running for a very long time. **Kill and recreate System Timers periodically instead of leaving them running forever.** |

#### Why does the thread matter?
Certain Rust/Fougerite APIs internally call `UnityEngine.Object.FindObjectsOfType` (any of the Unity `Find...`
methods), for example:
- `World.Entities`, `World.SupplyCrates`, `World.LootableObjects`
- `World.StructureComponents()`, `World.DeployableObjects()`, `World.BasicDoors()`
- `Player.Disconnect()` (Fougerite automatically hops to the main thread for you here, so you don't need to
  handle it)

Calling these from any thread other than the main Unity thread will cause the infamous
**"Oops! Crashed"** popup. This is a UnityEngine limitation, not a Rust or Fougerite bug.

- A **Normal Timer** (`CreateTimer`/`CreateParallelTimer`) already runs on the main thread, so you can use
  the above APIs directly inside its callback.
- A **System Timer** (`CreateSystemTimer`/`CreateParallelSystemTimer`) runs on a background thread. If you
  need to touch UnityEngine/World/Entity/Player APIs from inside a System Timer callback, you **must** wrap
  that code with [`Loom.QueueOnMainThread`](Loom.md) to safely jump back to the main thread first.

`Util.GetUtil().MainThreadID` and `Util.GetUtil().CurrentWorkingThreadID` let you check at runtime whether
you're currently on the main thread (see the `Loom` docs for a full example).

> Both `CreateTimer` and `CreateParallelTimer` internally call `Util.ThreadTimerCheck`, which logs a warning
> if you create them from a thread other than the main one, since a normal Timer's `GameObject`/`Coroutine`
> must be created on the main thread.

### Normal vs Parallel (naming)
The "Parallel" in `CreateParallelTimer`/`CreateParallelSystemTimer` does **not** mean "runs on another
thread" - both the normal and parallel variants of each timer type share the exact same execution model.
The only difference is that `CreateTimer`/`CreateSystemTimer` enforce a single, uniquely-named timer per
plugin (calling it again with the same name returns the existing timer), while `CreateParallelTimer`/
`CreateParallelSystemTimer` always create a brand-new timer instance, so multiple timers can share the same
`name` and run "in parallel" with each other (handy for e.g. one timer per player).

### Methods (Normal Timer, main thread)
- `Plugin.CreateTimer(string name, int timeoutDelay, bool autoReset = false, int maxElapsedCount = 0)`
- `Plugin.CreateTimer(string name, int timeoutDelay, Action<TimedEvent> callback, bool autoReset = false, int maxElapsedCount = 0)`
- `Plugin.CreateTimer(string name, int timeoutDelay, Dictionary<string, object> args, bool autoReset = false, int maxElapsedCount = 0)`
- `Plugin.CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args, bool autoReset = false)`
- `Plugin.CreateParallelTimer(string name, int timeoutDelay, Dictionary<string, object> args, Action<TimedEvent> callback, bool autoReset = false, int maxElapsedCount = 0)`
- `Plugin.GetTimer(string name)` / `Plugin.GetParallelTimer(string name)`
- `Plugin.KillTimer(string name)` / `Plugin.KillParallelTimer(string name)` / `Plugin.KillTimers()`

### `TimedEvent` Properties/Methods
- `TimedEvent.Name` - The timer's name.
- `TimedEvent.Interval` - The timer's interval, in milliseconds.
- `TimedEvent.Args` - The `Dictionary<string, object>` passed when creating the timer.
- `TimedEvent.AutoReset` - Whether the timer repeats automatically.
- `TimedEvent.MaxElapsedCount` - Max number of fires before it kills itself (0 = infinite).
- `TimedEvent.Start()` - Starts the timer.
- `TimedEvent.Stop()` - Stops the timer (can be restarted).
- `TimedEvent.Kill()` - Stops and disposes the timer permanently.
- `TimedEvent.OnKilled` - `Action<string>` event fired when the timer is killed.

### Methods (System Timer, background thread)
- `Util.CreateSystemTimer(string name, int timeoutDelay, Action<SystemTimerEvent> callback, bool autoReset = false, string pluginName = "", int maxElapsedCount = 0)`
- `Util.CreateParallelSystemTimer(string name, int timeoutDelay, Dictionary<string, object> args, Action<SystemTimerEvent> callback, bool autoReset = false, string pluginName = "", int maxElapsedCount = 0)`
- `Util.GetSystemTimer(string name)` / `Util.GetParallelSystemTimer(string name)`
- `Util.KillSystemTimer(string name)` / `Util.KillParallelSystemTimer(string name)`

### `SystemTimerEvent` Properties/Methods
- `SystemTimerEvent.Name` / `.PluginName` / `.Interval` / `.Args` / `.AutoReset` / `.MaxElapsedCount`
- `SystemTimerEvent.Start()` / `.Stop()` / `.Kill()`
- `SystemTimerEvent.OnFire` - `SystemTimerFireDelegate` event, fired **on a background thread**.
- `SystemTimerEvent.OnKilled` - `Action<string>` event fired when the timer is killed.

### Examples

#### C# - Normal Timer (main thread)
```csharp
public override void Initialize()
{
    // Fires every 10 seconds, on the main thread - safe to touch World/Entity/Player directly here.
    CreateTimer("Heartbeat", 10000, HeartbeatCallback, true);
}

public void HeartbeatCallback(TimedEvent te)
{
    Logger.Log("Heartbeat! Entities: " + World.GetWorld().Entities.Count());
}
```

#### C# - System Timer (background thread)
```csharp
public void Call()
{
    // Guard against accidentally touching UnityEngine off the main thread.
    if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
    {
        Loom.QueueOnMainThread(() => { Call(); });
        return;
    }

    Console.WriteLine("Hello from the main thread");
}

public override void Initialize()
{
    Util.GetUtil().CreateSystemTimer("BackgroundWork", 5000, te =>
    {
        // This runs on a ThreadPool thread - DO NOT touch UnityEngine/World/Entity/Player here directly.
        Call(); // Call() hops back to the main thread by itself before touching UnityEngine.
    }, true, Name);
}
```

#### Python
```python
def On_PluginInit(self):
    ConnectionData = Plugin.CreateDict()
    Plugin.CreateParallelTimer("Connect", 10000, ConnectionData).Start()

def ConnectCallback(self, ATimedEvent):
    ATimedEvent.Kill()
    Server.Log("Timer 'Connect' fired.")
    Server.Log("MainThreadID: " + str(Util.MainThreadID) + " CurrentWorkingThreadID: "
                + str(Util.CurrentWorkingThreadID))

    # Using Loom to call at the main thread:
    Loom.QueueOnMainThread(lambda:
        Server.Log("Now running on the main thread!")
    )
```

#### JavaScript
```javascript
function On_PluginInit()
{
    var ConnectionData = Plugin.CreateDict();
    Plugin.CreateParallelTimer("Connect", 10000, ConnectionData).Start();
}

function ConnectCallback(ATimedEvent)
{
    ATimedEvent.Kill();
    Server.Log("Timer 'Connect' fired.");
}
```

#### Lua
```lua
function On_PluginInit()
    local ConnectionData = Plugin.CreateDict()
    Plugin.CreateParallelTimer("Connect", 10000, ConnectionData):Start()
end

function ConnectCallback(ATimedEvent)
    ATimedEvent:Kill()
    Server.Log("Timer 'Connect' fired.")
end
```

See also: [`Loom` class](Loom.md) - for hopping back to the main thread from a System Timer or any other
background thread.
