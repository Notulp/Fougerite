### Method
`On_ResourceSpawn`

### Description
Runs when a resource node (tree, ore rock, etc.) spawns in the world.

### C# Event
```csharp
public static event ResourceSpawnDelegate OnResourceSpawned;
public delegate void ResourceSpawnDelegate(ResourceTarget t);
```

### Argument(s)
- `ResourceTarget ResourceTarget` - The spawned resource node.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnResourceSpawned += ResourceSpawnedHandler;
}

public override void DeInitialize()
{
    Hooks.OnResourceSpawned -= ResourceSpawnedHandler;
}

public void ResourceSpawnedHandler(ResourceTarget target)
{
    Logger.Log("A resource node spawned.");
}
```

#### Python
```python
def On_ResourceSpawn(self, ResourceTarget):
    Util.Log("A resource node spawned.")
```

#### JavaScript
```javascript
function On_ResourceSpawn(ResourceTarget)
{
    Util.Log("A resource node spawned.");
}
```

#### Lua
```lua
function On_ResourceSpawn(ResourceTarget)
    Util.Log("A resource node spawned.")
end
```
