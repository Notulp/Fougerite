### Class
`Fougerite.Server`

### Description
Singleton wrapper around the running Rust server. Accessible via `Server.GetServer()` in C#, or the global
`Server` variable in script plugins.

### Properties
- `Players` - `List<Player>` of everyone currently online.
- `PlayersCache` - `Dictionary<ulong, Player>` cache of players seen this session (including offline ones).
- `Sleepers` - `List<Sleeper>` of every sleeping bag/bed known to the server. See [`Sleeper`](Sleeper.md).
- `Items` - `ItemsBlocks`, the loaded item datablocks (see `On_ItemsLoaded`).
- `ChatHistoryMessages` / `ChatHistoryUsers` - Recent chat history.
- `CommandCancelList` / `ConsoleCommandCancelList` - Globally-restricted chat/console commands.
- `GlobalBanList` - `IniParser` of the ban list file.
- `Version` - Fougerite's version string.
- `ServerLoaded` / `ServerInitialized` / `IsShuttingDown` - Server lifecycle state flags.
- `HasRustPP` - Whether the RustPP compatibility extension is loaded.
- `server_message_name` - The name shown as the sender for `Server.Broadcast`/`BroadcastFrom` (default:
  `"Fougerite"`).

### Methods
- `GetServer()` (`static`) - Returns the singleton `Server` instance.
- `Broadcast(string arg)` - Sends a chat message to everyone, from `server_message_name`.
- `BroadcastFrom(string name, string arg)` - Sends a chat message to everyone, from a custom sender name.
- `BroadcastNotice(string s)` - Sends a HUD notice popup to everyone.
- `BroadcastInv(string s)` - Sends an inventory-area notice to everyone.
- `FindPlayer(string search)` / `FindPlayer(ulong search)` - Finds an online player by (partial) name or
  SteamID.
- `FindByNetworkPlayer(uLink.NetworkPlayer np)` / `FindByPlayerClient(PlayerClient pc)`.
- `GetCachePlayer(ulong id)` - Looks up a player in `PlayersCache` even if they're offline.
- `BanPlayer(Player player, string Banner = "Console", string reason = "...", Player Sender = null, bool AnnounceToServer = false)`,
  `BanPlayerIP(...)`, `BanPlayerID(...)`, `BanPlayerIPandID(...)` - Ban a player (fires `On_PlayerBan`).
- `UnbanByName(string name, ...)`, `UnbanByIP(string ip)`, `UnbanByID(string id)`.
- `IsBannedIP(string ip)` / `IsBannedID(string id)`.
- `FindIPsOfName(string name)` / `FindIDsOfName(string name)`.
- `RestrictCommand(string cmd)` / `UnRestrictCommand(string cmd)` / `CleanRestrictedCommands()` - Global
  chat command restriction (fires `On_CommandRestriction`).
- `RestrictConsoleCommand(string cmd)` / `UnRestrictConsoleCommand(string cmd)` /
  `CleanRestrictedConsoleCommands()` - Same, for console commands.
- `RunServerCommand(string s)` - Runs a raw server console command.
- `Save()` - Triggers a server save (fires `On_ServerSaved`).
- `LookForRustPP()` / `GetRustPPAPI()` - RustPP compatibility layer.

### Example - C#
```csharp
public void ChatHandler(Fougerite.Player player, ref ChatString chatString)
{
    Server.GetServer().Broadcast($"{player.Name} said: {chatString.OriginalMessage}");
}
```

### Example - Python
```python
def On_Chat(self, Player, ChatEvent):
    Server.Broadcast(Player.Name + " said: " + ChatEvent.OriginalMessage)
```

See also: [`Player`](Player.md) · [`Sleeper`](Sleeper.md) · [`Entity`](Entity.md) · [`Util`](Util.md)
