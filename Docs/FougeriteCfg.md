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
| `DisableFacePunchTruthPunish` | `false` | Disables `truth.punish`, Facepunch's original speedhack/flyhack validations. You may enable (`true`) this on a RustBuster server, since RustBuster clients can trip these false-positively. |
| `SteamAuthMode` | `Legacy` | Decides who may join when native Steam auth rejects the connection ticket (RustBuster clients on Spacewar 480, cracked clients). See the dedicated table below and [`SteamAuth`](Classes/SteamAuth.md) for full details. |
| `SteamWebAPIKey` | *(empty)* | Steam Web API key from <https://steamcommunity.com/dev/apikey>. Required by `RustOwners`/`SteamPaidAccounts`/`SteamAccounts`, ignored by every other mode. Keep it private, never share your logs/config with it filled in. |
| `SteamWebAPITimeout` | `10` | Seconds to wait for one Steam Web API request (`1`-`45`). The connecting player waits this long while the request is in flight; `RustOwners`/`SteamPaidAccounts` can take up to roughly twice this value plus ~6 seconds, since they make a second call for the ownership/limited-account check. |
| `SteamWebAPIFailOpen` | `false` | `RustOwners`/`SteamPaidAccounts`/`SteamAccounts` only. If the Steam Web API can't be reached (outage, rate limit, timeout), let Spacewar players in anyway (`true`) or reject them (`false`). `true` means a forged ticket gets in during an outage; a rejected API key (HTTP 401/403) is never covered by this and always denies. |

#### `SteamAuthMode` values (safest to least safe)

| Value | Safety rating | Behavior |
|---|---|---|
| `RustOnly` | **Most Safe** | Only native Rust (252490) players. Enforces strict native Steam authentication and blocks all Spacewar, emulated or cracked clients. |
| `RustOwners` | **High Safety** | Allows Spacewar tickets, but uses the Steam Web API to verify the account actually owns paid Rust. Requires the player's game details to be public. |
| `SteamPaidAccounts` | **High Safety** | Allows Spacewar players whose ticket is verified through the Steam Web API and whose Steam account is not limited (i.e. has spent at least 5 USD on Steam). Rust ownership is **not** required - this keeps out freshly-made alt accounts without requiring the player to own Rust. Private profiles are fine, but the player must have set up a Steam Community profile at least once. |
| `SteamAccounts` | **Medium Safety** | Verifies via the Web API that the Spacewar ticket belongs to a legitimate Steam account, but does not require Rust ownership or a non-limited account. **This is the most recommended setting to use with RustBuster.** |
| `SteamAccountsUnverified` | **Low Safety** | Performs basic offline checks to filter out sloppy emulators; bypassable by well-forged tickets spoofing any SteamID. |
| `Legacy` | **Very Low Safety** | Performs no ticket verification on its own, delegating access decisions entirely to plugins (e.g. the old AuthAllow plugin via `SteamDenyEvent.ForceAllow`). |
| `AllowAll` | **Zero Safety** | Disables all checks and allows anyone to connect, including clients without Steam. |

`RustOwners` and `SteamPaidAccounts` have their own profile requirement (public game details, and a
Steam Community profile having existed at least once) - the startup log prints a warning about this for
whichever of the two is active, so server owners aren't surprised by rejected players. Outside of `Legacy`,
a plugin can still deny a player through `On_SteamDeny`, but it can never let in a connection the configured
mode already rejected.

#### `TrustedSteamIDs`: whitelisting specific players for `RustOwners`/`SteamPaidAccounts`
`RustOwners` and `SteamPaidAccounts` can reject a perfectly legitimate player simply because their Steam
profile doesn't meet that mode's requirement (doesn't own Rust, or the account is flagged as limited/private
game details) - `TrustedSteamIDs` lets you override that for individual SteamID64s without lowering
`SteamAuthMode` for everyone else.

It's backed by `Save\TrustedSteamIDs.json` (next to `Fougerite.cfg`, not a `Fougerite.cfg` key itself), a
plain JSON array of SteamID64 numbers, created automatically on first startup with two example entries:
```json
[
  76561190000000000,
  76561190000000001
]
```
Replace the examples with the real SteamID64s you want to trust, then save the file - it's safe to hand-edit
while the server is stopped. A SteamID listed here is let in by `SteamTicketValidator.Evaluate` even if:
- `RustOwners` - the Steam Web API says the account doesn't own Rust.
- `SteamPaidAccounts` - the Steam Web API says the account is limited (never spent 5 USD on Steam).

It has **no effect** on any other `SteamAuthMode` value - `RustOnly` never calls the Web API at all, and
`SteamAccounts`/`SteamAccountsUnverified`/`Legacy`/`AllowAll` don't need this kind of override. Also exposed
to Python/JS/Lua plugins as the global `TrustedSteamIDs` variable (same pattern as `PermissionSystem`), and
in C# via `Fougerite.Tools.TrustedSteamIDs.GetInstance()` - see [`SteamAuth.md`](Classes/SteamAuth.md#trustedsteamids)
for the full API (`Contains`/`Add`/`Remove`/`GetAll`/`Reload`).

Example - C# (trusting a SteamID at runtime, e.g. from an admin command):
```csharp
using Fougerite.Tools;

if (TrustedSteamIDs.GetInstance().Add(76561198000000123))
{
    Logger.Log("Added 76561198000000123 to TrustedSteamIDs.");
}
```

#### Genuine tickets rejected with "101 Invalid ticket"
When Steam just issued the ticket, its Web API can briefly answer `101 Invalid ticket` for a perfectly valid
Spacewar ticket before the ticket is fully registered on Steam's side. Fougerite now retries the Web API call
for about 5 seconds to cover this window, and also strips the buffer padding some clients append after the
ticket before sending it, which could previously trigger the same false rejection. No client-side changes are
needed; this applies automatically to `RustOwners`, `SteamPaidAccounts` and `SteamAccounts`.

#### Upgrading from an older `Fougerite.cfg`
If your existing `Fougerite.cfg` still shows the old Steam comments (without `SteamPaidAccounts`), delete the
four Steam lines (`SteamAuthMode`, `SteamWebAPIKey`, `SteamWebAPITimeout`, `SteamWebAPIFailOpen`) once - they,
and the up-to-date comment block above them, are written back automatically on the next startup.

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
| `EnableCSScript` | `true` | Enable the C# Script (CSScript) engine. Compiles C# source files from scratch at startup, `Modules\Name\Name.cs` plus every other `.cs` file in that same folder, into one in-memory plugin assembly - no external IDE/build step needed on your side. See [`CSScriptPluginTutorial.md`](CSScriptPluginTutorial.md) for the full guide. |
| `CSScriptCompiler` | *(empty)* | Optional full path to a `csc.exe`/`mcs.exe` executable to force a specific C# compiler for CSScript plugins. Leave empty to auto-detect (see below). |

#### How `CSScriptCompiler` / compiler auto-detection works

When `CSScriptCompiler` is empty, `CSScriptPluginLoader` looks for a compiler in this exact order, and logs
which one it picked on startup:

1. **MSBuild's Roslyn `csc.exe`** - found through `vswhere.exe` (ships with any Visual Studio 2017+
   installation, including the free **Build Tools for Visual Studio**), requires the *MSBuild* workload to
   be installed.
2. **The .NET Framework's own `csc.exe`** - looked up directly under
   `%WINDIR%\Microsoft.NET\Framework(64)\v4.0.30319\csc.exe` first, then `...\v3.5\csc.exe`. This ships with
   Windows itself on any machine that has .NET Framework 4.x installed (practically every Windows install),
   no extra download needed.
3. **Mono's `mcs.exe`** - checked, in order, next to `rust_server.exe`, in `rust_server_Data\Managed\`, in
   `<Program Files>\Mono\lib\mono\4.5\mcs.exe` (Windows Mono install), and finally
   `/usr/lib/mono/4.5/mcs.exe` / `/usr/local/lib/mono/4.5/mcs.exe` (Linux).

If none of the above is found, CSScript plugins fail to compile with a clear error telling you to install
one of them or set `CSScriptCompiler` explicitly. You only need **one** working compiler, not all three.

**Getting a compiler, step by step:**
- **MSBuild (recommended on Windows)** - download **Build Tools for Visual Studio** (free, no full IDE
  required) from <https://visualstudio.microsoft.com/downloads/> (scroll to "Tools for Visual Studio").
  Run the installer and, in the **Individual components** tab, tick:
  - **MSBuild**
  - **.NET Framework 3.5 development tools** (or just **.NET Framework 3.5 targeting pack**) - this gives
    you the `v3.5` reference assemblies/compiler so the compiled plugin matches the server's own .NET 3.5
    Mono runtime.
  If you already have full Visual Studio installed with the ".NET desktop development" workload, MSBuild is
  already included - nothing else to do.
- **Mono `mcs` (Windows or Linux, lighter alternative)**:
  - *Windows*: download the Mono installer from <https://www.mono-project.com/download/stable/> and install
    it with defaults; `mcs.exe` ends up at `C:\Program Files\Mono\lib\mono\4.5\mcs.exe`, which is one of the
    auto-detected paths above.
  - *Linux*: install your distro's Mono package, e.g. `sudo apt install mono-mcs` (Debian/Ubuntu) or
    `sudo dnf install mono-core` (Fedora). This also places `mcs` where auto-detection expects it.
  - If auto-detection doesn't find it (non-standard install path), set it explicitly:
    ```ini
    CSScriptCompiler=C:\Program Files\Mono\lib\mono\4.5\mcs.exe
    ```
- **.NET Framework `csc.exe`** - usually already present if .NET Framework 4.x is installed (default on
  modern Windows). If you specifically want the `v3.5` compiler and it's missing, enable **.NET Framework
  3.5 (includes .NET 2.0 and 3.0)** under *Control Panel -> Programs -> Turn Windows features on or off*.

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
DisableFacePunchTruthPunish=false
SteamAuthMode=SteamAccounts
SteamWebAPIKey=
SteamWebAPITimeout=10
SteamWebAPIFailOpen=false

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
; Compiles C# script plugins from source at startup, Modules\Name\Name.cs plus every other .cs file in that folder.
; Use // #require OtherPlugin in a source file to reference another C# script plugin or DLL module at compile time.
EnableCSScript=true
; Optional full path to csc.exe or mcs.exe used for C# script plugins.
; Leave empty to detect it, MSBuild Roslyn first, then the .NET Framework compilers, then Mono mcs.
CSScriptCompiler=

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
- [`CSScriptPluginTutorial.md`](CSScriptPluginTutorial.md) - writing/compiling C# Script (CSScript) plugins,
  the `#require` directive, and the `EnableCSScript`/`CSScriptCompiler` keys in practice.
- [`Scripts.md`](Scripts.md) - the `AutoUpdate-Fougerite.ps1` script also touches `.cfg`/`.ini` files under
  `Save\` and will prompt you about overwriting them.
- [`SteamAuth`](Classes/SteamAuth.md) - the full `SteamAuthMode` pipeline (ticket parsing, Steam Web API
  validation, `On_SteamDeny`).
