### Method
`On_FlareIgnite` / `On_TorchIgnite`

### Description
Runs when a player ignites a flare (`On_FlareIgnite`) or a basic torch (`On_TorchIgnite`). If you need
de-selection handling too, use [`On_BeltUse`](On_BeltUse.md) and write the logic yourself.

### C# Event
```csharp
public static event FlareIgniteEventDelegate OnFlareIgnite;
public delegate void FlareIgniteEventDelegate(FlareIgniteEvent tie);

public static event BasicTorchIgniteEventDelegate OnBasicTorchIgnite;
public delegate void BasicTorchIgniteEventDelegate(BasicTorchIgniteEvent btie);
```

### Argument(s)
- `FlareIgniteEvent` / `BasicTorchIgniteEvent`

### Properties/Methods
- `.Player` - The player igniting the item.
- `.Item` - The `ITorchItem`/`IBasicTorchItem` being ignited.
- `.Instance` - The item's datablock.
- `.Stream` - The raw `uLink.BitStream`.
- `.ItemRep` - The `ItemRepresentation`.
- `.Info` - The `uLink.NetworkMessageInfo`.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnFlareIgnite += FlareIgniteHandler;
    Hooks.OnBasicTorchIgnite += TorchIgniteHandler;
}

public override void DeInitialize()
{
    Hooks.OnFlareIgnite -= FlareIgniteHandler;
    Hooks.OnBasicTorchIgnite -= TorchIgniteHandler;
}

public void FlareIgniteHandler(FlareIgniteEvent e)
{
    Logger.Log(e.Player.Name + " ignited a flare.");
}

public void TorchIgniteHandler(BasicTorchIgniteEvent e)
{
    Logger.Log(e.Player.Name + " ignited a torch.");
}
```

#### Python
```python
def On_FlareIgnite(self, FlareIgniteEvent):
    Server.Log(FlareIgniteEvent.Player.Name + " ignited a flare.")

def On_TorchIgnite(self, TorchIgniteEvent):
    Server.Log(TorchIgniteEvent.Player.Name + " ignited a torch.")
```

#### JavaScript
```javascript
function On_FlareIgnite(FlareIgniteEvent)
{
    Server.Log(FlareIgniteEvent.Player.Name + " ignited a flare.");
}

function On_TorchIgnite(TorchIgniteEvent)
{
    Server.Log(TorchIgniteEvent.Player.Name + " ignited a torch.");
}
```

#### Lua
```lua
function On_FlareIgnite(FlareIgniteEvent)
    Server.Log(FlareIgniteEvent.Player.Name .. " ignited a flare.")
end

function On_TorchIgnite(TorchIgniteEvent)
    Server.Log(TorchIgniteEvent.Player.Name .. " ignited a torch.")
end
```
