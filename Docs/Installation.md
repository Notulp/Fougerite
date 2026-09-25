### Guide
Installing plugins on a Fougerite server

### Description
Fougerite supports 4 plugin engines: **C#** (compiled "Modules"), **Python** (IronPython), **JavaScript**
(Jint) and **Lua** (MoonSharp). Each engine looks for its plugins in a different folder, and each has its
own on/off switch in [`Fougerite.cfg`](FougeriteCfg.md). This guide shows exactly where to drop each type
of plugin so the server picks it up.

All paths below are relative to your Rust Legacy dedicated server's root folder (the folder that contains
`rust_server.exe`).

### 1. Folder layout at a glance

```
<Server Root>\
├── rust_server.exe
├── rust_server_Data\
│   └── Managed\                     (Fougerite.dll, UnityEngine.dll, Assembly-CSharp.dll, ...)
├── Modules\                         <-- C# plugins (Modules) go here
│   └── MyPlugin\
│       └── MyPlugin.dll
└── Save\                            <-- the "PublicFolder": everything script plugins/config live in
    ├── Fougerite.cfg                <-- main Fougerite configuration file, see FougeriteCfg.md
    ├── IgnoredPlugins.txt           <-- one plugin (folder) name per line to skip loading it
    ├── PyPlugins\                   <-- Python plugins go here
    │   └── MyPlugin\
    │       └── MyPlugin.py
    ├── JsPlugins\                   <-- JavaScript plugins go here
    │   └── MyPlugin\
    │       └── MyPlugin.js
    └── LuaPlugins\                  <-- Lua plugins go here
        └── MyPlugin\
            └── MyPlugin.lua
```

> The `Modules\` and `Save\` locations above are the **defaults**. They can be relocated with an optional
> `FougeriteDirectory.cfg` file - see [section 5](#5-relocating-modules--save-fougeritedirectorycfg) below.

### 2. Installing a C# plugin (Module)

1. Build your plugin project. This produces a `.dll` file (see
   [`CSharpPluginTutorial.md`](CSharpPluginTutorial.md) if you haven't written one yet - it can be built
   with either **Visual Studio** or **JetBrains Rider**, both work fine as long as the project targets
   **.NET Framework 3.5**).
2. On the server, create a folder under `Modules\` and copy the `.dll` into it. **The folder name and the
   `.dll` file name (without extension) must match exactly**, e.g.:
   ```
   Modules\MyPlugin\MyPlugin.dll
   ```
3. Open `Save\Fougerite.cfg` and add a line under `[Modules]` in the form `FolderName=DataFolder`:
   ```ini
   [Modules]
   MyPlugin=MyPlugin
   ```
   - The **left side** (`MyPlugin`) must match the folder name you created in step 2 - this is how
     Fougerite finds and loads the `.dll`.
   - The **right side** is the name of a subfolder under `Save\` that gets handed to your plugin as
     `ModuleFolder`, for its own config/data files. It doesn't have to match the left side, but keeping
     them the same is the least confusing.
   - Only modules listed here are loaded. Comment out (`;`) or remove the line to disable a module without
     deleting its files.
4. Restart the server (C# modules are also reloadable at runtime, see
   [`Classes/BasePlugin.md`](Classes/BasePlugin.md) for how reloading works on the custom single-AppDomain
   Mono build).

### 3. Installing a Python / JavaScript / Lua plugin (script plugin)

Script plugins are not compiled - you just drop the source file on the server and Fougerite loads it
directly using its built-in scripting engines.

1. Pick the folder matching your language, under `Save\`:
   - Python -> `Save\PyPlugins\`
   - JavaScript -> `Save\JsPlugins\`
   - Lua -> `Save\LuaPlugins\`
2. Create a subfolder named after your plugin, and put the script file inside it, **named exactly like the
   folder** (this is required - Fougerite looks for `<FolderName>\<FolderName>.<ext>`):
   ```
   Save\PyPlugins\MyPlugin\MyPlugin.py
   Save\JsPlugins\MyPlugin\MyPlugin.js
   Save\LuaPlugins\MyPlugin\MyPlugin.lua
   ```
3. Make sure the corresponding engine is enabled in `Fougerite.cfg` under `[Engines]`:
   ```ini
   [Engines]
   EnableCSharp=true
   EnablePython=true
   EnableJavaScript=true
   EnableLua=true
   ```
4. Script plugins are watched by a file watcher and most engines support reloading them without restarting
   the server (check your loader/console for a `reload <name>` style command). If you just want to disable
   a single plugin without deleting it, add its name to `Save\IgnoredPlugins.txt` (one name per line).

Unlike C# modules, script plugins don't need an entry in `Fougerite.cfg` - simply having the folder/file in
the right place with the matching engine enabled is enough for it to be picked up.

### 4. Plugin dependencies / extra files

- Python plugins that need extra `.py` modules can rely on the interpreter's `Lib\` folder next to `Save\`
  (a full IronPython standard library is shipped there).
- C# modules can ship extra DLL dependencies next to their own `.dll` inside their `Modules\<Name>\` folder
  - Fougerite resolves same-named assemblies only once even if multiple modules reference them.

### 5. Relocating Modules / Save (`FougeriteDirectory.cfg`)

By default:
- C# modules live in `<Server Root>\Modules`.
- Everything else (script plugins, `Fougerite.cfg`, data files) lives in `<Server Root>\Save`.

If you want to move either folder (e.g. onto another drive), place a `FougeriteDirectory.cfg` file inside
`rust_server_Data\` next to `Fougerite.dll`:

```ini
; place this file in the rust_server_Data folder
; the PublicFolder is for module and plugin configuration files
;     and plugin scripts (JintPlugin, MagmaPlugin, IronPythonModule)
; the ModuleFolder is for Fougerite Module dll files and other files not for public access
;     treat these the same as the rust_server_Data files
; %RootFolder% refers to the top level of the Rust server installation
; It can be used instead of the absolute path to the Rust server root folder
[Settings]
PublicFolder=%RootFolder%\Save
ModulesFolder=%RootFolder%\Modules
```

You can point `PublicFolder`/`ModulesFolder` anywhere you like (absolute path, or using the
`%RootFolder%` placeholder). If `FougeriteDirectory.cfg` is missing entirely, Fougerite silently falls back
to the two defaults above - this is the case on most servers.

### 6. Related documentation

- [`FougeriteCfg.md`](FougeriteCfg.md) - full reference for every `Fougerite.cfg` section/key.
- [`Scripts.md`](Scripts.md) - the `AutoUpdate-Fougerite.ps1` and `Collect-Logs.ps1` helper scripts.
- [`CSharpPluginTutorial.md`](CSharpPluginTutorial.md) - writing your first C# module.
- [`Hooks/README.md`](Hooks/README.md) / [`Classes/README.md`](Classes/README.md) - plugin API reference.
