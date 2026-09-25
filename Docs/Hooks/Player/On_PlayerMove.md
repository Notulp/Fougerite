### Method
`On_PlayerMove`

### Description
Runs whenever a player's `HumanController` processes a movement packet - even while the player is standing
still (idle packets still fire it). Because this fires very frequently, avoid heavy logic here.

### C# Event
```csharp
public static event PlayerMoveDelegate OnPlayerMove;
public delegate void PlayerMoveDelegate(HumanController hc, Vector3 origin, int encoded, ushort stateFlags,
    uLink.NetworkMessageInfo info, Util.PlayerActions action);
```

### Argument(s)
- `HumanController HumanController` - The low-level controller of the moving player.
- `Vector3 Origin` - The reported origin position.
- `int Encoded` - The encoded movement data.
- `ushort StateFlags` - Movement state flags.
- `uLink.NetworkMessageInfo NetworkMessageInfo` - Network packet info.
- `Util.PlayerActions Action` - The current player action (e.g. running, jumping, crouching).

> This hook is C#-only, due to the amount and complexity of the raw parameters, and is not exposed to
> script plugins by default.

### Examples

#### C#
```csharp
public override void Initialize()
{
    Hooks.OnPlayerMove += MoveHandler;
}

public override void DeInitialize()
{
    Hooks.OnPlayerMove -= MoveHandler;
}

public void MoveHandler(HumanController hc, Vector3 origin, int encoded, ushort stateFlags,
    uLink.NetworkMessageInfo info, Util.PlayerActions action)
{
    // Careful: called very often!
}
```
