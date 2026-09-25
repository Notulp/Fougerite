### Class
`Fougerite.Util`

### Description
A grab-bag singleton of helper methods: math/vector helpers, world raycasts/entity lookups, reflection
helpers, hashing, and both timer implementations. Accessible via `Util.GetUtil()` in C#, or the global
`Util` variable in script plugins.

### Threading Properties
- `MainThreadID` - The `ManagedThreadId` of Fougerite's main (Unity) thread.
- `CurrentWorkingThreadID` - The `ManagedThreadId` of whatever thread is currently executing.
- `MainThread` / `CurrentWorkingThread` - The actual `Thread` objects, if you need more than just the ID.
- `TimeInMillis` / `TimeEpoch` - Server time helpers.

Compare `MainThreadID` with `CurrentWorkingThreadID` to know whether your code is running on the main
thread - this matters a lot for [timers](Timers.md) and any UnityEngine/`World` access:
```python
if Util.CurrentWorkingThreadID != Util.MainThreadID:
    Loom.QueueOnMainThread(lambda: DoUnityStuff())
else:
    DoUnityStuff()
```

### Timers
`Util` hosts the **System Timer** API (background-thread timers). See [Timers](Timers.md) for the full
comparison against the Normal Timer API found on `BasePlugin`/`Plugin`.
- `CreateSystemTimer(...)` / `CreateParallelSystemTimer(...)`
- `GetSystemTimer(name)` / `GetParallelSystemTimer(name)`
- `KillSystemTimer(name)` / `KillParallelSystemTimer(name)`
- `KillTimers()` - Kills **every** timer of every kind (Normal + Parallel + System + Parallel System)
  tracked on this `Util` instance.

### World / Entity Lookup Helpers
> These internally perform raycasts/physics overlap checks (and, indirectly, sometimes Unity object lookups)
> - only call them from the main thread (a hook or a Normal Timer), or via `Loom.QueueOnMainThread` if you're
> inside a System Timer/other background thread.

- `FindEntityAt(Vector3 pos, float dist = 1f)` / `FindClosestEntity(...)` / `FindLootableAt(...)` /
  `FindDoorAt(...)` / `FindDeployableAt(...)` / `FindStructureAt(...)` / `FindChestAt(...)`.
- `FindEntitiesAround(Vector3 pos, float dist = 100f)`, `FindDeployablesAround`, `FindDoorsAround`,
  `FindStructuresAround`, `FindLootablesAround`, `FindEntitysAroundFast`, `FindObjectsAroundFast`,
  `FindClosestObject`.
- `GetEntityatCoords(...)`, `GetDooratCoords(...)`.
- `GetLookObject(Player player, int layerMask = -1)` / `GetLookRay(Player player)` /
  `GetEyesRay(Player player)` / `GetLineObject(...)` - Raycast helpers, e.g. "what is this player looking
  at".
- `GetVectorsDistance(Vector3 v1, Vector3 v2)` / `GetVector2sDistance(...)`.
- `Infront(Player p, float length)` - Point in front of a player.
- `RotateX/Y/Z(Quaternion q, float angle)`.

### Reflection / Interop Helpers
- `CreateInstance(string typeName, params object[] args)` / `CreateArrayInstance(string typeName, int size)`.
- `GetStaticField` / `SetStaticField`, `InvokeStatic(className, method, args)`.
- `GetInstanceField` / `SetInstanceField`, `GetInstanceProperty` / `SetInstanceProperty`,
  `CallInstanceMethod(...)` - Useful from script languages that need to touch a type that isn't exposed
  through a friendly Fougerite wrapper.
- `TryFindType(string typeName, out Type t)` / `TryFindReturnType(string typeName)`.

### Misc Helpers
- `Log(string str)` - Shortcut for `Fougerite.Logger.Log`.
- `ConsoleLog(string str, bool adminOnly = false)` - Sends a raw console message to players.
- `ConvertNameToData(string name)` / `BlueprintOfItem(ItemDataBlock item)`.
- `SplitInParts` / `SplitInPartsLs` / `GetQuotedArgs` / `Regex` / `ContainsString`.
- `SuperFastHash`, `SHA1Hash`, `SHA256Hash`, `MD5Hash` (byte[]/string overloads).
- `IsSteamUser(ulong steamId)` / `GetSteamUserType(ulong steamId)`.
- `GetLastSaveFile()`.

### Example - C#
```csharp
public void Call()
{
    if (Util.GetUtil().CurrentWorkingThreadID != Util.GetUtil().MainThreadID)
    {
        Loom.QueueOnMainThread(() => { Call(); });
        return;
    }

    Entity closest = Util.GetUtil().FindClosestEntity(player.Location, 5f);
}
```

### Example - Python
```python
def On_PluginInit(self):
    ConnectionData = Plugin.CreateDict()
    Plugin.CreateParallelTimer("Connect", 10000, ConnectionData).Start()

def ConnectCallback(self, ATimedEvent):
    ATimedEvent.Kill()
    Server.Log("MainThreadID: " + str(Util.MainThreadID) + " CurrentWorkingThreadID: "
                + str(Util.CurrentWorkingThreadID))
```

See also: [`Timers`](Timers.md) · [`Loom`](Loom.md) · [`Entity`](Entity.md)
