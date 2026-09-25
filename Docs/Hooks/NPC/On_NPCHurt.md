### Method
`On_NPCHurt`

### Description
Runs when an NPC (animal/bear/wolf/zombie etc.) gets damaged.

### C# Event
```csharp
public static event HurtHandlerDelegate OnNPCHurt;
public delegate void HurtHandlerDelegate(HurtEvent he);
```

### Argument(s)
- `HurtEvent HurtEvent`

### Properties/Methods
See [`On_PlayerHurt`](../Player/On_PlayerHurt.md) for the full list of `HurtEvent` properties.
`HurtEvent.Victim` will be an `NPC` instance for this hook.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnNPCHurt += NPCHurt;
}

public override void DeInitialize()
{
    Hooks.OnNPCHurt -= NPCHurt;
}

public void NPCHurt(HurtEvent he)
{
    NPC animal = (NPC) he.Victim;
    string animalName = animal.Name;
    Server.GetServer().Broadcast(animalName + " got hurt!");
}
```

#### Python
```python
def On_NPCHurt(self, HurtEvent):
    Server.Broadcast(HurtEvent.Victim.Name + " got hurt!")
```

#### JavaScript
```javascript
function On_NPCHurt(HurtEvent) {
    Server.Broadcast(HurtEvent.Victim.Name + " got hurt!");
}
```

#### Lua
```lua
function On_NPCHurt(HurtEvent)
    Server.Broadcast(HurtEvent.Victim.Name .. " got hurt!")
end
```
