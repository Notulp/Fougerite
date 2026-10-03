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
  if it doesn't already exist (never overwrites a value the server operator already edited) - the standard
  way for a plugin to register its own config defaults on first run.
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

### Example - C# (registering a plugin's own config defaults)
```csharp
public override void Initialize()
{
    Config.AddDefault("MyPlugin", "WelcomeMessage", "Welcome to the server!",
        "The message shown to players on connect.");
    Config.AddDefault("MyPlugin", "KitCooldownSeconds", "3600", "Cooldown between kit claims, in seconds.");
    Config.Save();

    string welcome = Config.GetValue("MyPlugin", "WelcomeMessage");
    int cooldown = Data.GetData().ToInt(Config.GetValue("MyPlugin", "KitCooldownSeconds"));
}
```

### Example - C# (checking the running Fougerite version)
```csharp
public override void Initialize()
{
    Logger.Log("Running on Fougerite v" + Bootstrap.Version);
}
```

See also: [`SteamAuth`](SteamAuth.md) · [`PluginLoaders`](PluginLoaders.md) · [`Util`](Util.md)
