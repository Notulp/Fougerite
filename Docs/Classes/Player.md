### Class
`Fougerite.Player`

### Description
Wraps a connected (or recently connected) Rust client. You receive `Player` instances as arguments to most
hooks (see [Hooks reference](../Hooks/README.md)), or look them up via `Server`/`PlayerCache`.

### Finding a Player
- `Server.GetServer().FindPlayer(string search)` / `FindPlayer(ulong steamId)` - By name (partial, case
  insensitive) or SteamID.
- `Server.GetServer().Players` - `List<Player>` of everyone currently online.
- `Server.GetServer().PlayersCache` - `Dictionary<ulong, Player>` cache (includes players seen previously in
  this session).
- `Player.Find(string search)` / `Player.Search(string search)` / `Player.FindByName(string search)` -
  Static helpers, similar to the above.
- `Player.FindBySteamID(string search)` / `Player.FindByGameID(string search)`.
- `PlayerCache` (script global) - Offline-capable lookups (works even for players who aren't currently
  connected).

### Properties
- `Name` - Display name.
- `UID` / `SteamID` / `GameID` - Various ID formats (`UID` is the numeric SteamID as `ulong`).
- `IP` - Player's IP address.
- `IsOnline` - Whether the player is currently connected.
- `IsAlive` - Whether the character is alive (not dead/sleeping).
- `IsDisconnecting` - `true` while a disconnect is in progress.
- `IsSteamUser` / `SteamUserType` - Steam account validation info.
- `Admin` / `Moderator` - Rust's built-in admin/moderator flags.
- `Health` - Current health.
- `Location` / `X` / `Y` / `Z` - World position.
- `Rotation` / `LocalRotation` - Orientation.
- `Inventory` - `PlayerInv` wrapper around the player's main/wear/belt inventories.
- `Character` - The underlying Rust `Character` component.
- `PlayerClient` - The underlying Rust `PlayerClient`.
- `NetworkPlayer` - The underlying uLink `NetworkPlayer`.
- `Sleeper` - The player's associated [`Sleeper`](Sleeper.md) (sleeping bag/bed), if any.
- `Ping` - Current network ping.
- `ConnectedAt` / `ConnectedAtSeconds` / `TimeOnline` - Connection timing info.
- `DisconnectTime` / `DisconnectLocation` - Set when the player disconnects.
- `CalorieLevel`, `WaterLevel`, `RadLevel`, `AntiRadLevel`, `PoisonLevel`, `CoreTemperature`,
  `BleedingLevel` - Metabolism stats (see `On_MetabolismUpdate`).
- `IsBleeding`, `IsCold`, `IsInjured`, `IsRadPoisoned`, `IsWarm`, `IsPoisoned`, `IsStarving`, `IsHungry` -
  Convenience booleans derived from the metabolism stats.
- `Structures`, `Deployables`, `Shelters`, `Storage`, `Fires` - Arrays of [`Entity`](Entity.md) objects owned
  by the player.
- `IsOnGround`, `IsNearStructure`, `IsOnDeployable`, `IsInShelter`, `AtHome` - Positional convenience flags.
- `CommandCancelList` / `ConsoleCommandCancelList` - Per-player restricted command lists.
- `FallDamage` - The player's `FallDamage` component wrapper.
- `HumanBodyTakeDmg` - Damage component wrapper.

### Methods
- `Message(string arg)` / `MessageFrom(string playername, string arg)` - Chat message to this player.
- `Notice(string arg)` / `Notice(string icon, string text, float duration = 4f)` - HUD notice popup.
- `InventoryNotice(string arg)` - Inventory-area notice.
- `SendConsoleMessage(string msg)` - Sends a raw console message to the client.
- `SendCommand(string cmd)` - Runs a client console command on the player's client.
- `Damage(float dmg)` - Deals damage to the player.
- `Kill()` - Kills the player.
- `Disconnect()` / `Disconnect(bool sendNotification, NetError reason)` - Kicks/disconnects the player.
  Internally jumps to the main thread automatically if called from another thread, so it's safe to call
  from a [System Timer](Timers.md) callback without `Loom.QueueOnMainThread`.
- `TeleportTo(Player p, float distance = 1.5f, bool callhook = true)` /
  `TeleportTo(float x, float y, float z, bool callhook = true)` /
  `TeleportTo(Vector3 target, bool callhook = true)` - Teleports the player, firing `On_PlayerTeleport`
  unless `callhook` is `false`.
- `SafeTeleportTo(...)` overloads - Like `TeleportTo`, but performs extra ground/safety checks first.
- `TeleportToTheClosestSpawnpoint(Vector3 target, bool callhook = true)`.
- `RestrictCommand(string cmd)` / `UnRestrictCommand(string cmd)` / `CleanRestrictedCommands()` - Per-player
  chat command restriction (fires `On_CommandRestriction`).
- `RestrictConsoleCommand(string cmd)` / `UnRestrictConsoleCommand(string cmd)` /
  `CleanRestrictedConsoleCommands()` - Same, for console commands.
- `ForceAdminOff(bool state)` / `ForceModeratorOff(bool state)`.
- `IsAtWorkbench()` / `IsCrafting()`.
- `HasBlueprint(string name)` / `HasBlueprint(BlueprintDataBlock dataBlock)` / `Blueprints()`.
- `AdjustCalorieLevel(float amount)`, `AddRads(float amount)`, `AddAntiRad(float amount)`,
  `AddWater(float litres)`, `AdjustPoisonLevel(float amount)` - Modify metabolism stats.

### Example - C#
```csharp
public void CommandHandler(Fougerite.Player player, string cmd, string[] args)
{
    if (cmd == "heal")
    {
        player.Health = 100f;
        player.Notice("", "You have been healed!", 4f);
    }
}
```

### Example - Python
```python
def On_Command(self, Player, Command, Args):
    if Command == "heal":
        Player.Health = 100
        Player.Notice("", "You have been healed!", 4)
```
