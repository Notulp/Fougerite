### Class
`Fougerite.Loom`

### Description
Usage of some `UnityEngine` methods causes the Rust server to throw an "Oops! Crashed" window. This is not a
Rust nor a Fougerite bug, but a UnityEngine version bug: it happens when specific UnityEngine methods are
called outside of the main thread (e.g. from a new thread or a timer callback).

Currently known methods that cause it:
- `UnityEngine.Object.FindObjectsOfType` (any Unity `Find...` methods)

Currently known Fougerite/Rust methods that call them internally:
- `Player.Disconnect()` - `Loom` is automatically used internally here, you don't need to handle this.
- `World.Entities`
- `World.SupplyCrates`
- `World.LootableObjects`
- `World.StructureComponents()`
- `World.DeployableObjects()`
- `World.BasicDoors()`

### How to avoid the crash popup

Use the `Loom` class to call code from the main thread. Crash popups can happen when specific code calling
`UnityEngine.Object.FindObj...` runs on a background thread.

### Methods
- `Loom.QueueOnMainThread(Action action)` - Queues an action to run on the main thread as soon as possible.
- `Loom.QueueOnMainThread(Action action, float time)` - Queues an action to run on the main thread after a
  delay (in seconds).
- `Loom.ExecuteInBiggerStackThread(Action action)` - Runs an action on a background thread with a bigger
  stack size, useful for CPU-heavy work (e.g. big loops/recursion) without blocking the main thread.
- `Loom.RunAsync(Action a)` - Runs an action on a plain background `Thread` and returns the `Thread`.
- `Loom.maxThreads` - The maximum amount of worker threads Loom will use.
- `Loom.AmountOfThreads` - The current amount of active worker threads.

### Examples

#### C# - Calling code from the main thread
```csharp
public void Call()
{
    if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
    {
        Loom.QueueOnMainThread(() =>
        {
            Call();
        });
        return;
    }

    Console.WriteLine("Hello");
}
```

#### Python - Calling code from the main thread
```python
def On_PluginInit(self):
    ConnectionData = Plugin.CreateDict()
    Plugin.CreateParallelTimer("Connect", 10000, ConnectionData).Start()

def ConnectCallback(self, ATimedEvent):
    ATimedEvent.Kill()
    Plugin.Log("Testing", "MainThreadID: " + str(Util.MainThreadID) + " CurrentWorkingThreadID: "
               + str(Util.CurrentWorkingThreadID))

    # Using Loom to call at the main thread:
    Loom.QueueOnMainThread(lambda:
        Plugin.Log("Testing", "MainThreadID: " + str(Util.MainThreadID) + " CurrentWorkingThreadID: "
                   + str(Util.CurrentWorkingThreadID))
    )
```

#### JavaScript - Calling code from the main thread
```javascript
function On_PluginInit()
{
    var ConnectionData = Plugin.CreateDict();
    Plugin.CreateParallelTimer("Connect", 10000, ConnectionData).Start();
}

function ConnectCallback(ATimedEvent)
{
    ATimedEvent.Kill();

    Loom.QueueOnMainThread(function()
    {
        Plugin.Log("Testing", "MainThreadID: " + Util.MainThreadID + " CurrentWorkingThreadID: " + Util.CurrentWorkingThreadID);
    });
}
```

#### Lua - Calling code from the main thread
```lua
function On_PluginInit()
    local ConnectionData = Plugin.CreateDict()
    Plugin.CreateParallelTimer("Connect", 10000, ConnectionData):Start()
end

function ConnectCallback(ATimedEvent)
    ATimedEvent:Kill()

    Loom.QueueOnMainThread(function()
        Plugin.Log("Testing", "MainThreadID: " .. tostring(Util.MainThreadID) .. " CurrentWorkingThreadID: " .. tostring(Util.CurrentWorkingThreadID))
    end)
end
```

### Simple threading for heavy CPU work

Use `Loom.ExecuteInBiggerStackThread` (Python/JS/Lua) or the `Threading` class (C#) to run CPU-heavy code
without blocking the main server thread.

#### Python
```python
def HardStuffThatUsesCPU(self, Number):
    for i in xrange(0, Number):
        math.sqrt(i)

def On_PluginInit(self):
    Loom.ExecuteInBiggerStackThread(lambda:
        self.HardStuffThatUsesCPU(1000000)
    )
```

#### JavaScript
```javascript
function HardStuffThatUsesCPU(Number)
{
    for (var i = 0; i < Number; i++)
    {
        Math.sqrt(i);
    }
}

function On_PluginInit()
{
    Loom.ExecuteInBiggerStackThread(function()
    {
        HardStuffThatUsesCPU(1000000);
    });
}
```

#### Lua
```lua
function HardStuffThatUsesCPU(Number)
    for i = 0, Number do
        math.sqrt(i)
    end
end

function On_PluginInit()
    Loom.ExecuteInBiggerStackThread(function()
        HardStuffThatUsesCPU(1000000)
    end)
end
```
