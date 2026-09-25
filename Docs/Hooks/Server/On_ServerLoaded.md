### Method
`On_ServerLoaded`

### Description
Runs when the server has finished loading the map and is ready to accept players.

### C# Event
```csharp
public static event ServerLoadedDelegate OnServerLoaded;
public delegate void ServerLoadedDelegate();
```

### Argument(s)
None.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnServerLoaded += ServerLoadedHandler;
}

public override void DeInitialize()
{
    Hooks.OnServerLoaded -= ServerLoadedHandler;
}

public void ServerLoadedHandler()
{
    Logger.Log("Server has finished loading.");
}
```

#### Python
```python
def On_ServerLoaded(self):
    Server.Log("Server has finished loading.")
```

#### JavaScript
```javascript
function On_ServerLoaded()
{
    Server.Log("Server has finished loading.");
}
```

#### Lua
```lua
function On_ServerLoaded()
    Server.Log("Server has finished loading.")
end
```
