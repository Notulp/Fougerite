### Method
`OnModulesLoaded` *(C# modules only)*

### Description
Runs when all C# modules have finished loading. There is no equivalent script method for this, since it
concerns the C# module loading lifecycle specifically (before script plugins are loaded).

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
    Logger.Log("All C# modules loaded.");
}
```
