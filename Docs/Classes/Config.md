### Class
`Fougerite.Config` / `Fougerite.Bootstrap`

### Description
`Config` is the static reader/writer for `Fougerite.cfg` (and the smaller `FougeriteDirectory.cfg` that tells
Fougerite where everything else lives). `Bootstrap` is the `MonoBehaviour` that starts Fougerite itself
(attaches to a `GameObject`, reads `Fougerite.cfg` into the various runtime switches below, sets up the global
unhandled-exception logger, and kicks off plugin loading) - a plugin almost never needs to touch `Bootstrap`
directly, but its `public static` fields are the actual, live values of every global Fougerite setting, handy
to read (not typically to write) from a plugin.

### Config
- `GetValue(string Section, string Setting)` - reads a raw string setting from `Fougerite.cfg`.
- `GetBoolValue(string Section, string Setting)` - reads a setting as a `bool`.
- `AddSetting(string Section, string Setting, string Value, string Document = null)` - writes/updates a
  setting; if `Document` is given and the key is new, it's inserted as one or more `;`-prefixed comment lines
  above the key (supports multi-line via `\n`).
- `AddDefault(string Section, string Setting, string DefaultValue, string Document)` - writes a setting only
  if it doesn't already exist (never overwrites a value the server operator already edited). **This writes into
  the shared `Fougerite.cfg`** - it exists for Fougerite's own startup options, not for a plugin's own settings
  (see the warning below).
- `Save()` - flushes `Fougerite.cfg` to disk (call after one or more `AddDefault`/`AddSetting` calls).
- `GetModulesFolder()` / `GetPublicFolder()` - resolves the modules/public (`Save`) folder paths from
  `FougeriteDirectory.cfg`, expanding the `%RootFolder%` placeholder.
- `FougeriteConfig` / `FougeriteDirectoryConfig` - the raw `IniParser` instances, for anything not covered by
  the helpers above.

### Bootstrap
A selection of the most useful `public static` fields read from `Fougerite.cfg` at startup (full list is
larger - these are the ones most relevant to plugin authors):
- `Version` - the running Fougerite version string (e.g. `"1.9.94"`).
- `RustChat` - whether Fougerite's own chat system backs `Player.Message`.
- `RPCChat` / `RPCChatMethod` - whether/which client RPC additionally receives chat (RustBuster-specific).
- `EnableScriptPluginsIntensiveEvents` - whether high-frequency hooks (`On_PlayerMove`, `On_Shoot`, ...) are
  also dispatched to Python/JS/Lua plugins (always on for C#); see [`PluginLoaders`](PluginLoaders.md).
- `SilentConsoleCommands` - suppresses the default "Fougerite: Class.Function was executed!" console reply.
- `AutoBanCraft` / `BI` / `CR` - anti-cheat/anti-abuse toggles (craft-hack auto-ban, invalid-packet ban, cache
  cleanup on disconnect).
- `EnableDefaultRustDecay` - whether the game's built-in decay is left enabled.
- `FloodConnections` - max connection attempts per IP per second (see [`Flood`](Flood.md) equivalent logic).
- `IgnoredPlugins` - `ConcurrentList<string>` of plugin names currently excluded from loading (backed by
  `Save/IgnoredPlugins.txt`, hot-reloaded via a `FileSystemWatcher`).
- `SteamAuthenticationMode` / `SteamWebAPITimeout` / `SteamWebAPIFailOpen` - see [`SteamAuth`](SteamAuth.md).
- `AttachBootstrap()` - internal entry point called by the patcher; not meant to be called by plugins.

> **Plugins must NOT use `Config`/`Fougerite.cfg` for their own settings.** `Config.AddDefault`/`Config.GetValue`/
> `Config.AddSetting` read and write the single, shared `Fougerite.cfg` (`Config.FougeriteConfig`) - that file
> belongs to Fougerite itself (the Bootstrap switches listed above). A plugin that calls `Config.AddDefault(...)`
> for its *own* settings is dumping its section into that shared file alongside every other plugin's settings
> and Fougerite's own core options, which doesn't scale, can collide between plugins, and makes a plugin's
> settings impossible to ship/remove/back up on their own. Every plugin must create and own **its own** config
> file instead - `IniParser` directly in C#, or the built-in `GetIni`/`CreateIni`/`IniExists` helpers in any
> engine (C#, Python, JS, Lua). `Config`/`Bootstrap` is only for reading Fougerite's own global settings.

### Example - C# (a plugin's own dedicated config file, via `IniParser`)
`IniParser` is the class `Config` itself is built on (`Config.FougeriteConfig` is just an `IniParser` instance
pointed at `Fougerite.cfg`), and it's public - a plugin can `new` up its own instance pointed at its own file,
keeping its settings completely separate from `Fougerite.cfg` and from every other plugin.

**Where to put it:** not in the shared `Save\` folder - every C# module/CSScript plugin already has **its own
folder under `Modules\`** (e.g. `Modules\MyPlugin\`, the same folder `MyPlugin.dll` or `MyPlugin.cs` lives in,
resolved via `Config.GetModulesFolder()`). Plugins should create their config **inside that module folder**,
not dump it into `Save\` alongside `Fougerite.cfg`/other plugins' data.

**Important:** unlike `Config.AddDefault` (which lazily creates `Fougerite.cfg` itself at startup),
`new IniParser(path)` throws a `FileNotFoundException` if the file doesn't exist yet - so the plugin must
create its module folder and an empty file first (once) before constructing the parser:
```csharp
using System.IO;

public class MyPlugin : Fougerite.PluginLoaders.Module
{
    private IniParser _config;

    public override void Initialize()
    {
        string moduleFolder = Path.Combine(Config.GetModulesFolder(), "MyPlugin");
        if (!Directory.Exists(moduleFolder))
        {
            Directory.CreateDirectory(moduleFolder);
        }

        string path = Path.Combine(moduleFolder, "MyPlugin.cfg");
        if (!File.Exists(path))
        {
            File.Create(path).Dispose();
        }

        _config = new IniParser(path);
        _config.AddDefault("MyPlugin", "WelcomeMessage", "Welcome to the server!",
            "The message shown to players on connect.");
        _config.AddDefault("MyPlugin", "KitCooldownSeconds", "3600", "Cooldown between kit claims, in seconds.");
        _config.Save();

        string welcome = _config.GetSetting("MyPlugin", "WelcomeMessage");
        int cooldown = Data.GetData().ToInt(_config.GetSetting("MyPlugin", "KitCooldownSeconds"));
    }
}
```
`Config.GetModulesFolder()` resolves the `Modules\` root from `FougeriteDirectory.cfg` (expanding
`%RootFolder%`) - combining it with the plugin's own name gives exactly the folder the plugin itself is
loaded from (`Modules\MyPlugin\MyPlugin.dll` for a DLL module, or `Modules\MyPlugin\MyPlugin.cs` + any other
source files for a CSScript plugin), so `MyPlugin.cfg` ships and travels together with the plugin instead of
being scattered into the shared `Save\` folder. `IniParser` also exposes `GetBoolSetting`, `AddSetting` (no
"only if missing" check), `ContainsSetting`/`ContainsValue`, and `SaveSettings(newPath)` for a copy-to-another-
file save, in addition to the `AddDefault`/`GetSetting`/`Save` used above.

This is exactly the pattern real plugins use, e.g. an `AirDrops` plugin loading its own `Settings.ini` next to
itself. `ModuleFolder` here isn't a hand-built path like `moduleFolder` above - it's a built-in
`Fougerite.Module` property, already pointing at the plugin's own folder under `Modules\` by the time
`Initialize()` runs, so a DLL module plugin can use it directly without resolving the folder itself:
```csharp
private IniParser Settings;

private void ReloadConfig()
{
    string path = Path.Combine(ModuleFolder, "Settings.ini");
    if (!File.Exists(path))
    {
        File.Create(path).Dispose();
        Settings = new IniParser(path);
        Settings.AddSetting("Settings", "TimedAirdrop", "True");
        Settings.AddSetting("Settings", "AirdropTime", "1800000");
        Settings.AddSetting("Settings", "MinPlayers", "20");
        Settings.Save();
    }
    else
    {
        Settings = new IniParser(path);
    }

    TimedAirdrop = Settings.GetBoolSetting("Settings", "TimedAirdrop");
    AirdropTime = int.Parse(Settings.GetSetting("Settings", "AirdropTime"));
    MinPlayers = int.Parse(Settings.GetSetting("Settings", "MinPlayers"));
}
```
Note `AddSetting` (not `AddDefault`) is used here since this branch only runs once, when the file is brand new -
either works for this first-run case, but prefer `AddDefault` if you ever add new settings to an *existing*
`Settings.ini` in a later plugin version, so you don't overwrite values the server operator already changed.

### Example - Python/JS/Lua (a plugin's own dedicated config file, via `GetIni`/`CreateIni`/`IniExists`)
Script plugins don't have direct filesystem access the way a C# plugin does, so `BasePlugin` exposes the same
"own config file" capability through a few helpers instead, all resolved **relative to the plugin's own folder**
automatically (no manual path-joining, no `FileNotFoundException` to work around) - `path` never includes an
extension, `.ini` is always appended for you:
- `IniExists(path)` - `true` if `path.ini` already exists in this plugin's folder.
- `CreateIni(path = None)` - creates `path.ini` (or `<PluginName>.ini` if `path` is omitted) if missing, and
  returns an `IniParser` for it either way (new or pre-existing).
- `GetIni(path)` - returns an `IniParser` for an existing `path.ini`, or `None`/`null` if it doesn't exist.

This is the standard pattern used by Python/JS/Lua plugins instead of touching `Config` - for example a
`ClansConfig` helper (Python):
```python
def ClansConfig(self):
    if not self.IniExists("ClansConfig"):
        ini = self.CreateIni("ClansConfig")
        ini.AddSetting("Settings", "Cost", "0")
        ini.AddSetting("Settings", "AllowSelfDamage", "True")
        ini.AddSetting("Settings", "ClanMemberLimit", "0")
        ini.Save()
    return self.GetIni("ClansConfig")
```
`ClansConfig.ini` ends up inside this plugin's own folder (e.g. `Save\PyPlugins\Clans\ClansConfig.ini`), fully
separate from `Fougerite.cfg` and from any other plugin's settings - never shared, never collided with.

### Example - C# (checking the running Fougerite version)
```csharp
public override void Initialize()
{
    Logger.Log("Running on Fougerite v" + Bootstrap.Version);
}
```

See also: [`SteamAuth`](SteamAuth.md) · [`PluginLoaders`](PluginLoaders.md) · [`Util`](Util.md)
