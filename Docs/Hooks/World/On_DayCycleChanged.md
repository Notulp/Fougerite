### Method
`On_DayCycleChanged`

### Description
Runs when the day/night cycle changes (day -> night or night -> day).

### C# Event
```csharp
public static event DayCycleChangeEventDelegate OnDayCycleChanged;
public delegate void DayCycleChangeEventDelegate(DayCycleChangeEvent dcche);
```

### Argument(s)
- `DayCycleChangeEvent DayCycleChangeEvent`

### Properties/Methods
- `DayCycleChangeEvent.EnvironmentControlCenter` - The `EnvironmentControlCenter` instance.
- `DayCycleChangeEvent.IsNight` - `true` if it just became night, `false` if day.
- `DayCycleChangeEvent.PreviousDayCycle` - The `PreviousDayCycle` enum value before the change.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnDayCycleChanged += DayCycleHandler;
}

public override void DeInitialize()
{
    Hooks.OnDayCycleChanged -= DayCycleHandler;
}

public void DayCycleHandler(DayCycleChangeEvent e)
{
    Server.GetServer().Broadcast(e.IsNight ? "Night has fallen!" : "The sun has risen!");
}
```

#### Python
```python
def On_DayCycleChanged(self, DayCycleChangeEvent):
    Message = "Night has fallen!" if DayCycleChangeEvent.IsNight else "The sun has risen!"
    Server.Broadcast(Message)
```

#### JavaScript
```javascript
function On_DayCycleChanged(DayCycleChangeEvent)
{
    var Message = DayCycleChangeEvent.IsNight ? "Night has fallen!" : "The sun has risen!";
    Server.Broadcast(Message);
}
```

#### Lua
```lua
function On_DayCycleChanged(DayCycleChangeEvent)
    local Message = DayCycleChangeEvent.IsNight and "Night has fallen!" or "The sun has risen!"
    Server.Broadcast(Message)
end
```
