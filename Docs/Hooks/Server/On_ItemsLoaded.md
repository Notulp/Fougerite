### Method
`On_ItemsLoaded`

### Description
Runs when the item datablocks have been loaded by the server.

### C# Event
```csharp
public static event ItemsDatablocksLoaded OnItemsLoaded;
public delegate void ItemsDatablocksLoaded(ItemsBlocks items);
```

### Argument(s)
- `ItemsBlocks Items` - Wrapper exposing all loaded item datablocks.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnItemsLoaded += ItemsLoadedHandler;
}

public override void DeInitialize()
{
    Hooks.OnItemsLoaded -= ItemsLoadedHandler;
}

public void ItemsLoadedHandler(ItemsBlocks items)
{
    Logger.Log("Item datablocks have been loaded.");
}
```

#### Python
```python
def On_ItemsLoaded(self, Items):
    Util.Log("Item datablocks have been loaded.")
```

#### JavaScript
```javascript
function On_ItemsLoaded(Items)
{
    Util.Log("Item datablocks have been loaded.");
}
```

#### Lua
```lua
function On_ItemsLoaded(Items)
    Util.Log("Item datablocks have been loaded.")
end
```
