### Class
`Fougerite.ServerSaveHandler` / `Fougerite.WaterSystemServer`

### Description
Two unrelated but important server-side systems Fougerite replaces/manages on top of vanilla Rust Legacy.
`ServerSaveHandler` runs the actual map save (background-threaded so a huge map doesn't freeze the server
while saving) and exposes the scheduling knobs (`World.GetWorld().ServerSaveHandler`). `WaterSystemServer`
replaces vanilla's instant-kill-on-touching-water "drowning" with a real oxygen budget, and lets plugins mark
sealed/underwater structures as dry so players inside them don't suffocate.

### ServerSaveHandler
Accessible via `World.GetWorld().ServerSaveHandler`. Uses a `BackgroundWorker` internally so saving tens of
thousands of objects doesn't lag the main thread.
- `ManualSave()` - saves synchronously-ish without the `BackgroundWorker` (blocks less code paths, still
  avoid calling this very frequently on a large map).
- `ManualBackGroundSave()` - saves via the `BackgroundWorker`, same path the autosave timer uses.
- `ServerSavePath` - the current save file path.
- `ServerIsSaving` - whether a save is currently in progress (check this before triggering another one).
- `ServerSaveTime` (get/set) - autosave interval, in minutes. **Never set this to `0`.**
- `SaveCopies` (get/set) - how many rotating `.sav` backups to keep (keep at `5`+).
- `LastSaveTime` / `NextServerSaveTime` - timestamps of the last/next scheduled autosave.
- `StopServerOnSaveFail` (get/set) - whether an exception during save should stop the server (fail safe vs.
  fail loud).
- `CrucialSavePoint` (get/set) - minutes before an autosave during which a manual save request is skipped
  (prevents two saves colliding); `0` disables this guard. Must be smaller than `ServerSaveTime`.

### WaterSystemServer
A static class - no instance needed, just call the methods/fields directly.
- `AllowSwimming` - master switch; `false` restores vanilla's instant-death-on-touching-water behaviour.
- `OxygenSeconds` / `OxygenRefillMultiplier` / `DrownDamagePerMinute` / `SubmergedDepth` / `HeadHeight` /
  `UseEyesOrigin` / `SubmergeTriggerOffset` / `SilenceFootstepsInWater` / `StateTimeout` /
  `SyncOxygenToClient` / `OxygenSyncInterval` - tunable config fields for the oxygen simulation (how long a
  player can hold their breath, how fast air refills, drowning damage, etc.).
- `GetOxygen(Character character)` - remaining air, `0`-`1` (`1` = full, untracked characters read as full;
  `-1` for an invalid/destroyed character).
- `ResetOxygen(Character character)` - drops a character's tracked oxygen state (e.g. call on respawn so a
  player doesn't spawn already out of breath).
- `Clear()` - drops every tracked character's oxygen state.
- `TrackedCount` - how many characters currently have oxygen state.
- `WaterLineHeight` - the level's global water height, or `float.MinValue` if the level has no water.
- `DepthBelowWater(Vector3 point)` - how far below the waterline a point is (`<= 0` means dry); always `<= 0`
  for a point inside a registered dry region, regardless of actual depth.
- `IsInWater(Character character)` / `IsPointExcluded(Vector3 point)` - convenience checks.
- Dry-region exclusion API (lets plugins mark a volume as "never drowns", e.g. the interior of a sealed
  underwater base):
  - `AddExclusion(string owner, string key, Bounds bounds)` - registers/replaces a dry region. `owner` should
    be your plugin's name (so you can bulk-remove your own regions later); `key` is a stable identity (e.g. a
    structure ID) so re-registering a grown structure is just calling this again.
  - `RemoveExclusion(string key)` - removes one region by key.
  - `RemoveExclusionsFor(string owner)` - removes every region registered by a given plugin (call this from
    `DeInitialize()` to clean up after yourself).
  - `ClearExclusions()` - removes every region, from every plugin.
  - `ExclusionCount` - how many dry regions are currently registered.
  - `ExclusionCellSize` - spatial bucket size (metres) used internally for fast lookups; only tune this if
    you register thousands of regions and profiling shows it matters.

### Example - C# (triggering a manual save from a chat command)
```csharp
public void OnChat(Player player, ref ChatString message)
{
    if (message.OriginalMessage == "/forcesave")
    {
        if (ServerSaveHandler.ServerIsSaving)
        {
            player.Message("A save is already in progress.");
            return;
        }

        World.GetWorld().ServerSaveHandler.ManualBackGroundSave();
        player.Message("Save started.");
    }
}
```

### Example - C# (keeping a sealed underwater base dry)
```csharp
public void MarkBaseDry(Entity foundation)
{
    Bounds bounds = new Bounds(foundation.Location, new Vector3(20f, 10f, 20f));
    WaterSystemServer.AddExclusion(Name, foundation.InstanceID.ToString(), bounds);
}

public override void DeInitialize()
{
    WaterSystemServer.RemoveExclusionsFor(Name);
}
```

See also: [`World`](World.md) · [`Entity`](Entity.md) · [`Data`](Data.md)
