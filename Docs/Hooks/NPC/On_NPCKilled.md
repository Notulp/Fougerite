### Method
`On_NPCKilled`

### Description
Runs when an NPC (animal) has died.

### C# Event
```csharp
public static event KillHandlerDelegate OnNPCKilled;
public delegate void KillHandlerDelegate(DeathEvent de);
```

### Argument(s)
- `DeathEvent DeathEvent` - See [`On_PlayerKilled`](../Player/On_PlayerKilled.md) for its properties.
  `DeathEvent.Victim` will be an `NPC` instance for this hook.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnNPCKilled += NPCKilledHandler;
}

public override void DeInitialize()
{
    Hooks.OnNPCKilled -= NPCKilledHandler;
}

public void NPCKilledHandler(DeathEvent de)
{
    NPC animal = (NPC) de.Victim;
    Server.GetServer().Broadcast(animal.Name + " was killed!");
}
```

#### Python
```python
def On_NPCKilled(self, DeathEvent):
    Server.Broadcast(DeathEvent.Victim.Name + " was killed!")
```

#### JavaScript
```javascript
function On_NPCKilled(DeathEvent)
{
    Server.Broadcast(DeathEvent.Victim.Name + " was killed!");
}
```

#### Lua
```lua
function On_NPCKilled(DeathEvent)
    Server.Broadcast(DeathEvent.Victim.Name .. " was killed!")
end
```
