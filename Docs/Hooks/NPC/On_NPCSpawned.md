### Method
`On_NPCSpawned`

### Description
Runs when an NPC (animal) is spawned into the world.

### C# Event
```csharp
public static event NPCSpawnedEventDelegate OnNPCSpawned;
public delegate void NPCSpawnedEventDelegate(NPC npc);
```

### Argument(s)
- `NPC NPC` - The NPC that spawned.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnNPCSpawned += NPCSpawnedHandler;
}

public override void DeInitialize()
{
    Hooks.OnNPCSpawned -= NPCSpawnedHandler;
}

public void NPCSpawnedHandler(NPC npc)
{
    Logger.Log("NPC spawned: " + npc.Name);
}
```

#### Python
```python
def On_NPCSpawned(self, NPC):
    Util.Log("NPC spawned: " + NPC.Name)
```

#### JavaScript
```javascript
function On_NPCSpawned(NPC)
{
    Util.Log("NPC spawned: " + NPC.Name);
}
```

#### Lua
```lua
function On_NPCSpawned(NPC)
    Util.Log("NPC spawned: " .. NPC.Name)
end
```
