### Method
`On_SleeperSpawned`

### Description
Runs when a sleeping player (bag/bed spawn point, sleeping avatar) gets registered on the server.

### C# Event
```csharp
public static event SleeperSpawnEventDelegate OnSleeperSpawned;
public delegate void SleeperSpawnEventDelegate(Sleeper sleeper);
```

### Argument(s)
- `Sleeper Sleeper` - The `Fougerite.Sleeper` wrapper around the sleeping player's avatar.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnSleeperSpawned += SleeperHandler;
}

public override void DeInitialize()
{
    Hooks.OnSleeperSpawned -= SleeperHandler;
}

public void SleeperHandler(Sleeper sleeper)
{
    Logger.Log("A sleeper spawned: " + sleeper.Name);
}
```

#### Python
```python
def On_SleeperSpawned(self, Sleeper):
    Util.Log("A sleeper spawned: " + Sleeper.Name)
```

#### JavaScript
```javascript
function On_SleeperSpawned(Sleeper)
{
    Util.Log("A sleeper spawned: " + Sleeper.Name);
}
```

#### Lua
```lua
function On_SleeperSpawned(Sleeper)
    Util.Log("A sleeper spawned: " .. Sleeper.Name)
end
```
