### Method
`On_GenericSpawnLoad`

### Description
Runs when a generic resource spawner (`GenericSpawner`) has loaded.

### C# Event
```csharp
public static event GenericSpawnerLoadDelegate OnGenericSpawnerLoad;
public delegate void GenericSpawnerLoadDelegate(GenericSpawner genericSpawner);
```

### Argument(s)
- `GenericSpawner GenericSpawner` - The spawner that loaded.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnGenericSpawnerLoad += SpawnerLoadHandler;
}

public override void DeInitialize()
{
    Hooks.OnGenericSpawnerLoad -= SpawnerLoadHandler;
}

public void SpawnerLoadHandler(GenericSpawner gs)
{
    Logger.Log("A generic spawner loaded.");
}
```

#### Python
```python
def On_GenericSpawnLoad(self, GenericSpawner):
    Util.Log("A generic spawner loaded.")
```

#### JavaScript
```javascript
function On_GenericSpawnLoad(GenericSpawner)
{
    Util.Log("A generic spawner loaded.");
}
```

#### Lua
```lua
function On_GenericSpawnLoad(GenericSpawner)
    Util.Log("A generic spawner loaded.")
end
```
