### Method
`On_ServerInit`

### Description
Runs when the server started loading (very early in the boot sequence).

### C# Event
```csharp
public static event ServerInitDelegate OnServerInit;
public delegate void ServerInitDelegate();
```

### Argument(s)
None.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnServerInit += ServerInitHandler;
}

public override void DeInitialize()
{
    Hooks.OnServerInit -= ServerInitHandler;
}

public void ServerInitHandler()
{
    Logger.Log("Server started loading.");
}
```

#### Python
```python
def On_ServerInit(self):
    Server.Log("Server started loading.")
```

#### JavaScript
```javascript
function On_ServerInit()
{
    Server.Log("Server started loading.");
}
```

#### Lua
```lua
function On_ServerInit()
    Server.Log("Server started loading.")
end
```
