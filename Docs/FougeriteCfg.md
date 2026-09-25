### Class
`Fougerite.cfg`

### Description
`Fougerite.cfg` is Fougerite's main configuration file. It lives in the `PublicFolder`
(`Save\Fougerite.cfg` by default - see [`Installation.md`](Installation.md#5-relocating-modules--save-fougeritedirectorycfg)
for how to relocate it), and it is a plain INI file read/written by `Fougerite.Config`
(`Fougerite\Config.cs`) through the `IniParser` class. It controls core server behavior, which C# modules
get loaded, which scripting engines are enabled, and console logging verbosity.

You can edit it with any text editor while the server is stopped, or with the in-game/console
`cfgset`/`cfgget`-style commands if your build exposes them. Lines starting with `;` are comments.

### `[Fougerite]` section

Core behavior toggles and gameplay-affecting settings.

| Key | Default | Description |
|---|---|---|
| `enabled` | `true` | Master on/off switch for Fougerite itself. |
| `tellversion` | `true` | Announce the Fougerite version to players when they connect. |
| `deployabledecay` | *(commented out)* | Enable decay for deployed items (if left commented, decay behaves as vanilla). |
| `structuredecay` | *(commented out)* | Enable decay for placed structures. |
| `RemovePlayersFromCache` | `false` | Remove players from the internal player cache once they disconnect. |
| `BanOnInvalidPacket` | `true` | Automatically ban a client that sends an invalid/malformed network packet (commonly a sign of a hacked client). |
| `EnableDefaultRustDecay` | `false` | Re-enable Rust's own built-in decay system (can cause lag on large/old maps, hence disabled by default). |
| `AutoBanCraft` | `true` | Automatically ban players caught using the crafting hack. If `false`, the craft is only cancelled and logged, no ban. |
| `FloodConnections` | `2` | Maximum number of connections allowed from the same IP within a 3-second window (basic anti-DoS). |
| `SaveTime` | `10` | Interval, in minutes, between automatic server saves. |
| `SaveCopies` | `5` | How many rotating backup copies of the save files to keep before deleting the oldest. **Do not set below 5.** |
| `StopServerOnSaveFail` | `false` | If a save fails, stop the server entirely rather than continuing to run with an un-saved world. |
| `CrucialSavePoint` | `0` | Minutes before an autosave during which manual saves are blocked (so they don't collide with the autosave). Must be **less** than `SaveTime`. `0` disables this protection. |
| `SaveNotification` | `The server is currently saving! ...` | Message shown to a player who tries to place a building part while a save is in progress (placement is briefly blocked during the save's subthread). |
| `RustChat` | `true` | Use Rust's default chat output pipeline for `Player.Message(...)` calls made from plugins. |
| `RPCChat` | `false` | Also send an additional chat RPC packet to clients. Recommended only for **RustBuster** servers. |
| `ClientFunction` | `FougeriteChatSystem` | Name of the client-side RPC method used when `RPCChat` is enabled. |
| `EnableScriptPluginsIntensiveEvents` | `false` | Exposes high-frequency events (`OnPlayerMove`, `OnShoot`, `OnShotgunShoot`, `OnBowShoot`, `OnAnimalMovement`, `OnHeatZoneEnter`, `OnWorkZoneEnter`) to **script** plugins (Python/JS/Lua). These fire very often and can cause server lag - prefer writing performance-sensitive hooks in **C#** instead. Use at your own risk. |
| `SilentConsoleCommands` | `false` | Suppress the default `"Fougerite: Class.Function was executed!"` reply for console commands that don't set an explicit reply text. |
| `ServerMessageName` | `Fougerite` | Display name/title used for server-originated broadcast/system messages. |

### `[Modules]` section

Registers which **C# Modules** get loaded, in the form:

```ini
[Modules]
; module_folder = data_subfolder
FolderName=DataFolderUnderSave
```

- The **key** (left of `=`) must match the module's physical folder name under `Modules\`, i.e.
  `Modules\FolderName\FolderName.dll` (see [`Installation.md`](Installation.md#2-installing-a-c-plugin-module)
  for the full walk-through).
- The **value** (right of `=`) is a folder name under `Save\` that is handed to the module as its
  `ModuleFolder` (for the module's own config/data files). It doesn't need any particular value - "any
  word" works if the module doesn't use a data folder.
- **Only modules listed in this section are loaded.** Comment a line out (prefix with `;`) to disable that
  module without deleting its files.

Example (from a live server):
```ini
[Modules]
RustPP=Rust++
GlitchFix=GlitchFix
PermissionManager=PermissionManager
AuthAllow=AuthAllow
AuthMeServer=AuthMeServer
```

### `[Engines]` section

Master on/off switches for each scripting engine. Disabling an engine means its plugin folder
(`Save\PyPlugins`, `Save\JsPlugins`, `Save\LuaPlugins`) is never scanned/loaded, regardless of what's in
it.

| Key | Default | Description |
|---|---|---|
| `EnableCSharp` | `true` | Enable loading of C# Modules (`Modules\` folder). |
| `EnablePython` | `true` | Enable the IronPython engine (`Save\PyPlugins\`). |
| `EnableJavaScript` | `true` | Enable the Jint JavaScript engine (`Save\JsPlugins\`). |
| `EnableLua` | `true` | Enable the MoonSharp Lua engine (`Save\LuaPlugins\`). |

### `[Logging]` section

Fougerite always writes its logs to files under `PublicFolder\Logs` (`Save\Logs\` by default). These flags
only control what additionally gets echoed to the **server console window** - setting any of them to
`false` does **not** stop that category from being logged to the log file, it just hides it from the
console.

| Key | Default | Description |
|---|---|---|
| `debug` | `false` | Verbose debug-level messages (very noisy, useful when troubleshooting). |
| `error` | `true` | Non-fatal error messages. |
| `exception` | `true` | Exception stack traces thrown by plugins/hooks. |
| `speed` | `false` | Performance/timing measurements. |
| `rpctracer` | `false` | Low-level RPC packet tracing (extremely noisy, only for deep debugging). |

### Full example

```ini
[Fougerite]
RemovePlayersFromCache=false
BanOnInvalidPacket=true
EnableDefaultRustDecay=false
AutoBanCraft=true
FloodConnections=2
SaveTime=10
SaveCopies=5
StopServerOnSaveFail=false
CrucialSavePoint=0
SaveNotification=The server is currently saving! You have to wait before placing an object.
RustChat=true
RPCChat=false
ClientFunction=FougeriteChatSystem
enabled=true
EnableScriptPluginsIntensiveEvents=false
SilentConsoleCommands=false
ServerMessageName=Fougerite

[Modules]
;module = folder
; if a module needs no folder, just use any word, or no word
; you can change folder names to suit your needs
; only modules listed in [Modules] are loaded
; comment out a module to disable it
RustPP=Rust++
GlitchFix=GlitchFix
PermissionManager=PermissionManager
AuthAllow=AuthAllow
AuthMeServer=AuthMeServer

[Engines]
EnableCSharp=true
EnablePython=true
EnableJavaScript=true
EnableLua=true

[Logging]
; Fougerite logs are in PublicFolder\Logs
; Putting these false will still do the logging, but
; the console won't display them
debug=false
error=true
exception=true
speed=false
rpctracer=false
```

### See also
- [`Installation.md`](Installation.md) - where to physically place each type of plugin.
- [`Scripts.md`](Scripts.md) - the `AutoUpdate-Fougerite.ps1` script also touches `.cfg`/`.ini` files under
  `Save\` and will prompt you about overwriting them.
