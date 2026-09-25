### Method
`On_RepairBench`

### Description
Runs when a player repairs an item at a repair bench.

### C# Event
```csharp
public static event RepairBenchEventDelegate OnRepairBench;
public delegate void RepairBenchEventDelegate(Fougerite.Events.RepairEvent repairEvent);
```

### Argument(s)
- `RepairEvent RepairEvent`

### Properties/Methods
- `RepairEvent.Player` - The player repairing.
- `RepairEvent.Inv` - The `Inventory` containing the item(s) being repaired.
- `RepairEvent.RepairBench` - The `RepairBench` instance.
- `RepairEvent.Cancelled` - Whether the repair is cancelled.
- `RepairEvent.Cancel()` - Cancels the repair.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnRepairBench += RepairHandler;
}

public override void DeInitialize()
{
    Hooks.OnRepairBench -= RepairHandler;
}

public void RepairHandler(RepairEvent e)
{
    Logger.Log(e.Player.Name + " is repairing an item.");
}
```

#### Python
```python
def On_RepairBench(self, RepairEvent):
    Server.Log(RepairEvent.Player.Name + " is repairing an item.")
```

#### JavaScript
```javascript
function On_RepairBench(RepairEvent)
{
    Server.Log(RepairEvent.Player.Name + " is repairing an item.");
}
```

#### Lua
```lua
function On_RepairBench(RepairEvent)
    Server.Log(RepairEvent.Player.Name .. " is repairing an item.")
end
```
