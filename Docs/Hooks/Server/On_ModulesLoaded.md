### Method
`OnModulesLoaded` *(C# plugins only)*

### Description
Runs once every enabled C# based plugin engine has finished its startup load, that is the DLL `Module`s
(`EnableCSharp`) and the [CSScript](../../CSScriptPluginTutorial.md) plugins (`EnableCSScript`), in whichever
combination is actually enabled:
- Both engines enabled - DLL modules load first, then CSScript plugins, then this fires once.
- Only one of the two engines enabled - fires once that engine finished loading.
- Neither engine enabled - never fires.

There is no equivalent script method for this, since it concerns the C# plugin loading lifecycle
specifically (before Python/JS/Lua plugins necessarily finish loading). It's a good place for a C# plugin to
scan [`PluginLoader.GetInstance().Plugins`](../../Classes/PluginLoaders.md) for every other already-loaded C#
plugin and grab its API - see [`CSScriptPluginTutorial.md`](../../CSScriptPluginTutorial.md#6-onmodulesloaded-waiting-for-every-c-plugin-then-scanning-them)
for a full example.

### C# Event
```csharp
public static event ModulesLoadedDelegate OnModulesLoaded;
public delegate void ModulesLoadedDelegate();
```

### Argument(s)
None.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnModulesLoaded += ModulesLoadedHandler;
}

public override void DeInitialize()
{
    Hooks.OnModulesLoaded -= ModulesLoadedHandler;
}

public void ModulesLoadedHandler()
{
    Logger.Log("All C# plugins loaded.");

    // Every C# module/CSScript plugin that finished its startup load is available here.
    foreach (BasePlugin plugin in PluginLoader.GetInstance().Plugins.Values)
    {
        if (plugin.Type == PluginType.CSharp || plugin.Type == PluginType.CSScript)
        {
            Logger.Log("Found C# plugin: " + plugin.Name);
        }
    }
}
```
