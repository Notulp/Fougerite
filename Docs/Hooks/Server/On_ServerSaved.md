### Method
`On_ServerSaved`

### Description
Runs when the server finishes saving the world/entities.

### C# Event
```csharp
public static event ServerSavedDelegate OnServerSaved;
public delegate void ServerSavedDelegate(int Amount, double Seconds);
```

### Argument(s)
- `int Amount` - The amount of objects saved.
- `double Seconds` - How long the save took, in seconds.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnServerSaved += SavedHandler;
}

public override void DeInitialize()
{
    Hooks.OnServerSaved -= SavedHandler;
}

public void SavedHandler(int amount, double seconds)
{
    Logger.Log("Server saved " + amount + " objects in " + seconds + " seconds.");
}
```

#### Python
```python
def On_ServerSaved(self, Amount, Seconds):
    Util.Log("Server saved " + str(Amount) + " objects in " + str(Seconds) + " seconds.")
```

#### JavaScript
```javascript
function On_ServerSaved(Amount, Seconds)
{
    Util.Log("Server saved " + Amount + " objects in " + Seconds + " seconds.");
}
```

#### Lua
```lua
function On_ServerSaved(Amount, Seconds)
    Util.Log("Server saved " .. tostring(Amount) .. " objects in " .. tostring(Seconds) .. " seconds.")
end
```
