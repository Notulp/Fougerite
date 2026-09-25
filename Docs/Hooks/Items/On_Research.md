### Method
`On_Research`

### Description
Runs when a player researches an item.

### C# Event
```csharp
public static event ResearchDelegate OnResearch;
public delegate void ResearchDelegate(ResearchEvent re);
```

### Argument(s)
- `ResearchEvent ResearchEvent`

### Properties/Methods
- `ResearchEvent.Player` - The player researching.
- `ResearchEvent.Item` - The `IInventoryItem` being researched.
- `ResearchEvent.ItemDataBlock` - The item's datablock.
- `ResearchEvent.ItemName` - The item's name.
- `ResearchEvent.Cancelled` - Whether the research is cancelled.
- `ResearchEvent.Cancel()` - Cancels the research.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnResearch += ResearchHandler;
}

public override void DeInitialize()
{
    Hooks.OnResearch -= ResearchHandler;
}

public void ResearchHandler(ResearchEvent e)
{
    Logger.Log(e.Player.Name + " is researching " + e.ItemName);
}
```

#### Python
```python
def On_Research(self, ResearchEvent):
    Server.Log(ResearchEvent.Player.Name + " is researching " + ResearchEvent.ItemName)
```

#### JavaScript
```javascript
function On_Research(ResearchEvent)
{
    Server.Log(ResearchEvent.Player.Name + " is researching " + ResearchEvent.ItemName);
}
```

#### Lua
```lua
function On_Research(ResearchEvent)
    Server.Log(ResearchEvent.Player.Name .. " is researching " .. ResearchEvent.ItemName)
end
```
