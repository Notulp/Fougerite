### Method
`On_TablesLoaded`

### Description
Runs when loot tables have been loaded by the server. Allows inspecting/modifying loot spawn lists.

### C# Event
```csharp
public static event LootTablesLoaded OnTablesLoaded;
public delegate void LootTablesLoaded(Dictionary<string, LootSpawnList> lists);
```

### Argument(s)
- `Dictionary<string, LootSpawnList> Lists` - All loaded loot spawn lists, keyed by name.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnTablesLoaded += TablesLoadedHandler;
}

public override void DeInitialize()
{
    Hooks.OnTablesLoaded -= TablesLoadedHandler;
}

public void TablesLoadedHandler(Dictionary<string, LootSpawnList> lists)
{
    Logger.Log("Loaded " + lists.Count + " loot tables.");
}
```

#### Python
```python
def On_TablesLoaded(self, Lists):
    Server.Log("Loaded " + str(Lists.Count) + " loot tables.")
```

#### JavaScript
```javascript
function On_TablesLoaded(Lists)
{
    Server.Log("Loaded " + Lists.Count + " loot tables.");
}
```

#### Lua
```lua
function On_TablesLoaded(Lists)
    Server.Log("Loaded " .. tostring(Lists.Count) .. " loot tables.")
end
```
