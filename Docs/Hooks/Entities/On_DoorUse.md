### Method
`On_DoorUse`

### Description
Runs when a player opens or closes a door.

### C# Event
```csharp
public static event DoorOpenHandlerDelegate OnDoorUse;
public delegate void DoorOpenHandlerDelegate(Player player, DoorEvent de);
```

### Argument(s)
- `Player Player` - The player using the door.
- `DoorEvent DoorEvent`

### Properties/Methods
- `DoorEvent.Entity` - The door entity.
- `DoorEvent.BasicDoor` - The underlying `BasicDoor` component.
- `DoorEvent.Open` - `true` if the door is being opened, `false` if closed.
- `DoorEvent.State` - The `BasicDoor.State` value.
- `DoorEvent.Cancelled` - Whether the action is cancelled.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnDoorUse += DoorHandler;
}

public override void DeInitialize()
{
    Hooks.OnDoorUse -= DoorHandler;
}

public void DoorHandler(Fougerite.Player player, DoorEvent de)
{
    Logger.Log(player.Name + (de.Open ? " opened" : " closed") + " a door.");
}
```

#### Python
```python
def On_DoorUse(self, Player, DoorEvent):
    Action = "opened" if DoorEvent.Open else "closed"
    Util.Log(Player.Name + " " + Action + " a door.")
```

#### JavaScript
```javascript
function On_DoorUse(Player, DoorEvent)
{
    var Action = DoorEvent.Open ? "opened" : "closed";
    Util.Log(Player.Name + " " + Action + " a door.");
}
```

#### Lua
```lua
function On_DoorUse(Player, DoorEvent)
    local Action = DoorEvent.Open and "opened" or "closed"
    Util.Log(Player.Name .. " " .. Action .. " a door.")
end
```
