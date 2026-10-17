### Method
`On_CustomMapLoaded`

### Description
Fires once the server's level and its save have finished loading, right before
[`OnServerLoaded`](On_ServerLoaded.md). This fires for the **stock map too**, not just plugin-supplied ones -
check `IsCustomMap` to tell them apart. This is the server half of RustBuster's client-side `CustomMap`
feature, which lets a plugin replace the server's map (given via `-map`) with a scene from its own asset
bundle; see `Fougerite.CustomMap`'s XML doc comments for the full claim/commit workflow
(`CustomMap.GetInstance().Claim(...)`, then `CustomMapHandle.Commit`/`CommitFile`/`Abandon`/`Fail`).

### C# Event
```csharp
public static event CustomMapLoadedDelegate OnCustomMapLoaded;
public delegate void CustomMapLoadedDelegate(CustomMapLoadedEvent ev);
```

### Argument(s)
- `CustomMapLoadedEvent ev`

### Properties/Methods
- `.SceneName` - The scene that was actually loaded.
- `.ServerLevelName` - The level given with `-map`. Differs from `SceneName` when a plugin replaced the map.
- `.Owner` - The name of the plugin that supplied the map, or `null` for the stock map.
- `.Bundle` - The `AssetBundle` the scene came from, or `null` for the stock map. Do not `Unload` it.
- `.IsCustomMap` - `true` if the loaded level came from a plugin rather than the game.

### Examples

#### C#
```csharp
public override void Initialize()
{
    CustomMapHandle map = CustomMap.GetInstance().Claim(Name, 120f);
    if (map == null)
    {
        return; // Another plugin owns the map, or the level is already loaded.
    }

    Hooks.OnCustomMapLoaded += CustomMapLoadedHandler;

    map.CommitFile(Path.Combine(ModuleFolder, "hapis_island.unity3d"), "hapis_island");
}

public override void DeInitialize()
{
    Hooks.OnCustomMapLoaded -= CustomMapLoadedHandler;
}

public void CustomMapLoadedHandler(CustomMapLoadedEvent ev)
{
    if (!ev.IsCustomMap)
    {
        return;
    }

    Logger.Log("Custom map loaded: " + ev.SceneName);
}
```

#### Python
```python
def On_CustomMapLoaded(self, CustomMapLoadedEvent):
    if CustomMapLoadedEvent.IsCustomMap:
        Util.Log("Custom map loaded: " + CustomMapLoadedEvent.SceneName)
```

#### JavaScript
```javascript
function On_CustomMapLoaded(CustomMapLoadedEvent)
{
    if (CustomMapLoadedEvent.IsCustomMap)
    {
        Util.Log("Custom map loaded: " + CustomMapLoadedEvent.SceneName);
    }
}
```

#### Lua
```lua
function On_CustomMapLoaded(CustomMapLoadedEvent)
    if CustomMapLoadedEvent.IsCustomMap then
        Util.Log("Custom map loaded: " .. CustomMapLoadedEvent.SceneName)
    end
end
```

See also: [`On_ServerLoaded`](On_ServerLoaded.md)
