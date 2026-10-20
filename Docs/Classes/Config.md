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
- `GetModulesFolder()` - resolves the `Modules\` folder (where a C# module's/CSScript plugin's own `.dll`/`.cs`
  files live) from `FougeriteDirectory.cfg`, expanding the `%RootFolder%` placeholder.
- `GetPublicFolder()` - resolves the `Save\` folder (script plugins, `Fougerite.cfg`, and every module's own
  data/config folder - see `ModuleFolder` below) the same way.
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

**Where to put it:** not in the shared `Save\` folder next to `Fougerite.cfg` - every C# Module/CSScript plugin
already gets **its own data folder under `Save\`**, exposed to it as the built-in `ModuleFolder` property
(`Fougerite.Module.ModuleFolder`, see [`Modules.md`](Modules.md)). Despite the name, `ModuleFolder` does **not**
live under `Modules\` (that folder only ever holds the `.dll`/`.cs` source the plugin is *loaded from*, resolved
via `Config.GetModulesFolder()`) - it is resolved under `Save\` instead (`Config.GetPublicFolder()`), by default
`Save\<PluginName>\`, or `Save\<DataFolder>\` if the plugin's `[Modules]` entry in `Fougerite.cfg` maps it to a
different `DataFolder` (see [`FougeriteCfg.md`](FougeriteCfg.md#modules-section)). It's already set by the
loader (DLL Module or CSScript alike) by the time `Initialize()` runs, and the folder already exists on disk at
that point - **never build this path yourself** with `Path.Combine(Config.GetModulesFolder(), ...)`, that
points at the wrong folder (and at the plugin's own `.dll`/`.cs`, not a writable data directory) - just use
`ModuleFolder` directly:

**Important:** unlike `Config.AddDefault` (which lazily creates `Fougerite.cfg` itself at startup),
`new IniParser(path)` throws a `FileNotFoundException` if the file doesn't exist yet - so the plugin must
create the (empty) file first (once) before constructing the parser. `ModuleFolder` itself never needs to be
created by the plugin - the loader already did that:
```csharp
using System.IO;

public class MyPlugin : Fougerite.PluginLoaders.Module
{
    private IniParser _config;

    public override void Initialize()
    {
        string path = Path.Combine(ModuleFolder, "MyPlugin.cfg");
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
`MyPlugin.cfg` ends up at `Save\MyPlugin\MyPlugin.cfg` (or `Save\<DataFolder>\MyPlugin.cfg` for a custom
`[Modules]` mapping), fully separate from `Fougerite.cfg` and from every other plugin's settings. `IniParser`
also exposes `GetBoolSetting`, `AddSetting` (no "only if missing" check), `ContainsSetting`/`ContainsValue`, and
`SaveSettings(newPath)` for a copy-to-another-file save, in addition to the `AddDefault`/`GetSetting`/`Save`
used above.

This is exactly the pattern real plugins use, e.g. an `AirDrops` plugin loading its own `Settings.ini` next to
itself, via `ModuleFolder` directly as shown below - there's no folder to resolve or create, it's already
pointing at the plugin's own `Save\AirDrops\` by the time `Initialize()` runs.

**Don't trust the file's contents.** `Settings.ini` is a plain text file a server operator can (and will) hand-edit -
a typo'd number, a missing key after a bad merge, or a half-written file from a crash mid-save are all normal
things that happen in production. `int.Parse` throws `FormatException`/`OverflowException` on bad input and
`ArgumentNullException` if the setting is missing entirely (`GetSetting` can return `null`), and any one of those
exceptions escaping `ReloadConfig()` takes down the whole plugin (or worse, if called from a hook, the hook
dispatch). Use `int.TryParse` with a sane fallback default for every numeric setting, and wrap the whole read in
a `try/catch` as a last line of defense:
```csharp
private IniParser Settings;

private void ReloadConfig()
{
    try
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

        // int.TryParse never throws - it returns false and leaves "AirdropTime" at its previous/default
        // value if the setting is missing, empty, or isn't a valid number (e.g. the operator typed "18oo000").
        if (!int.TryParse(Settings.GetSetting("Settings", "AirdropTime"), out AirdropTime))
        {
            Logger.LogWarning("[AirDrops] 'AirdropTime' is missing or invalid, falling back to 1800000.");
            AirdropTime = 1800000;
        }

        if (!int.TryParse(Settings.GetSetting("Settings", "MinPlayers"), out MinPlayers))
        {
            Logger.LogWarning("[AirDrops] 'MinPlayers' is missing or invalid, falling back to 20.");
            MinPlayers = 20;
        }
    }
    catch (Exception ex)
    {
        // Catches anything TryParse doesn't cover - e.g. a corrupted/unreadable Settings.ini, a locked
        // file, or a permissions error on the module folder. Never let config loading crash the plugin.
        Logger.LogError("[AirDrops] Error while reading the config: " + ex);
    }
}
```
Note `AddSetting` (not `AddDefault`) is used here since this branch only runs once, when the file is brand new -
either works for this first-run case, but prefer `AddDefault` if you ever add new settings to an *existing*
`Settings.ini` in a later plugin version, so you don't overwrite values the server operator already changed.
The same `TryParse`-then-fallback approach applies to every other type you read out of an `IniParser`
(`double.TryParse`, `bool.TryParse` instead of trusting `GetBoolSetting` blindly, etc.) and to script plugins
too - Python's `int(x)` raises `ValueError` on bad input just like `int.Parse` does in C#, so wrap script-side
parsing in a `try/except` (or validate with `.isdigit()`/a regex first) rather than letting a bad `.ini` value
crash `On_PluginInit`.

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

### Example - Python/JS/Lua (safely parsing config values on read)
`IniParser.GetSetting`/`GetBoolSetting` return raw/boolean values straight from the `.ini` file - there's no
schema, so a hand-edited typo, a missing key, or a half-written file after a crash all turn into bad data at
runtime. Converting a setting to a number in a script engine has the exact same failure mode as `int.Parse` in
C#: Python's `int(x)` raises `ValueError`, JavaScript's `Number(x)`/`parseInt(x)` silently returns `NaN` instead
of throwing (arguably worse, since it won't even trip a `try/except`), and Lua's `tonumber(x)` returns `nil`.
None of these should be allowed to crash `On_PluginInit`/a hook or quietly produce garbage - always validate the
result and fall back to a sane default, and wrap the whole load in error handling:

**Python** - `int()`/`float()` throw `ValueError` on bad input, so wrap the conversion in `try/except`:
```python
def ClansConfig(self):
    if not self.IniExists("ClansConfig"):
        ini = self.CreateIni("ClansConfig")
        ini.AddSetting("Settings", "Cost", "0")
        ini.AddSetting("Settings", "AllowSelfDamage", "True")
        ini.AddSetting("Settings", "ClanMemberLimit", "0")
        ini.Save()
    return self.GetIni("ClansConfig")

def On_PluginInit(self):
    try:
        ini = self.ClansConfig()

        try:
            self.Cost = int(ini.GetSetting("Settings", "Cost"))
        except (TypeError, ValueError):
            Logger.LogWarning("[Clans] 'Cost' is missing or invalid, falling back to 0.")
            self.Cost = 0

        # GetBoolSetting is already safe (never throws), so it's fine to use directly.
        self.AllowSelfDamage = ini.GetBoolSetting("Settings", "AllowSelfDamage")
    except Exception as ex:
        Logger.LogError("[Clans] Error while reading the config: " + str(ex))
```

**JavaScript** - `Number`/`parseInt` don't throw, they return `NaN`, so you must check with `isNaN(...)` (a
`try/catch` alone won't catch a bad value here):
```javascript
function On_PluginInit() {
    try {
        var ini = Plugin.IniExists("ClansConfig") ? Plugin.GetIni("ClansConfig") : Plugin.CreateIni("ClansConfig");

        var cost = parseInt(ini.GetSetting("Settings", "Cost"), 10);
        if (isNaN(cost)) {
            Plugin.Log("Clans", "'Cost' is missing or invalid, falling back to 0.");
            cost = 0;
        }
        this.Cost = cost;

        // GetBoolSetting is already safe (never throws), so it's fine to use directly.
        this.AllowSelfDamage = ini.GetBoolSetting("Settings", "AllowSelfDamage");
    } catch (ex) {
        Plugin.LogError("Clans", "Error while reading the config: " + ex);
    }
}
```

**Lua** - `tonumber(x)` returns `nil` instead of throwing, so check for `nil` the same way; `pcall` is the
Lua equivalent of `try/catch` for anything else that could fail:
```lua
function On_PluginInit()
    local ok, err = pcall(function()
        local ini
        if Plugin:IniExists("ClansConfig") then
            ini = Plugin:GetIni("ClansConfig")
        else
            ini = Plugin:CreateIni("ClansConfig")
        end

        local cost = tonumber(ini:GetSetting("Settings", "Cost"))
        if cost == nil then
            Plugin:Log("Clans", "'Cost' is missing or invalid, falling back to 0.")
            cost = 0
        end
        Cost = cost

        -- GetBoolSetting is already safe (never throws), so it's fine to use directly.
        AllowSelfDamage = ini:GetBoolSetting("Settings", "AllowSelfDamage")
    end)
    if not ok then
        Plugin:Log("Clans", "Error while reading the config: " .. tostring(err))
    end
end
```
Same rule in every engine: never trust a converted config value without checking it succeeded, and never let a
config-loading error escape and take the whole plugin/hook down with it.

### Example - C# (checking the running Fougerite version)
```csharp
public override void Initialize()
{
    Logger.Log("Running on Fougerite v" + Bootstrap.Version);
}
```

See also: [`SteamAuth`](SteamAuth.md) · [`PluginLoaders`](PluginLoaders.md) · [`Util`](Util.md)
