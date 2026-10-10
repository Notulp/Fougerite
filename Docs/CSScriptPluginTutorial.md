### Guide
Writing a C# Script (CSScript) plugin for Fougerite

### Description
**C# Script** plugins (internally called "CSScript") give you the convenience of script plugins
(Python/JS/Lua - just drop source files on the server, no IDE/build step on your side) combined with the
performance and full API access of a compiled C# [`Module`](Classes/Modules.md). Fougerite compiles your
`.cs` file(s) on the server itself at startup (or on demand/hot-reload), the same way a C# IDE would, and
loads the result exactly like a hand-built `.dll` module.

This is implemented by `Fougerite.PluginLoaders.CSScriptPluginLoader` (which discovers, compiles and caches
the plugin) and `Fougerite.PluginLoaders.CSScriptPlugin` (the loaded plugin instance, works like `CSPlugin`
for DLL modules). See [`Classes/PluginLoaders.md`](Classes/PluginLoaders.md) for the class reference.

If you'd rather compile the `.dll` yourself in Visual Studio/Rider and ship only the binary, use a regular
DLL [`Module`](CSharpPluginTutorial.md) instead - the two systems can be used side by side on the same
server, and even reference each other (see [`#require`](#5-require-referencing-other-plugins-and-modules)
below).

### 1. Enabling the engine and getting a compiler

Open `Save\Fougerite.cfg` and make sure the `[Engines]` section has:
```ini
[Engines]
EnableCSScript=true
CSScriptCompiler=
```

`CSScriptCompiler` can stay empty - Fougerite auto-detects a compiler in this order: **MSBuild's Roslyn
`csc.exe`**, then **the .NET Framework's own `csc.exe`**, then **Mono's `mcs.exe`**. The very first startup
log line tells you which one it picked:
```
[CSScriptPluginLoader] Using the MSBuild Roslyn compiler at C:\Program Files (x86)\Microsoft Visual Studio\...\csc.exe
```

If nothing is found, pick one:
- **Windows, recommended**: install **Build Tools for Visual Studio** (free, no full IDE) from
  <https://visualstudio.microsoft.com/downloads/>. In the installer's **Individual components** tab, tick
  **MSBuild** and **.NET Framework 3.5 development tools** (so the compiler ships the `v3.5` reference
  assemblies matching the server's own Mono runtime).
- **Windows/Linux, lighter alternative**: install **Mono**. On Windows, grab the installer from
  <https://www.mono-project.com/download/stable/> (defaults put `mcs.exe` at
  `C:\Program Files\Mono\lib\mono\4.5\mcs.exe`, which is auto-detected). On Linux,
  `sudo apt install mono-mcs` (Debian/Ubuntu) or `sudo dnf install mono-core` (Fedora).
- If your install lives somewhere non-standard, point `CSScriptCompiler` directly at it:
  ```ini
  CSScriptCompiler=C:\Program Files\Mono\lib\mono\4.5\mcs.exe
  ```

Full key reference: [`FougeriteCfg.md`](FougeriteCfg.md#engines-section).

### 2. Folder layout

Like a DLL module, a CSScript plugin lives under `Modules\<Name>\`, except the main file is `.cs` source
instead of a prebuilt `.dll`:
```
Modules\
└── HelloScriptTest\
    ├── HelloScriptTest.cs     <-- required, same name as the folder
    ├── Helpers.cs             <-- optional, any extra .cs file is compiled in too
    └── References\            <-- optional, extra .dll files to compile/run against
        └── SomeLibrary.dll
```
- **Every** `.cs` file found anywhere under `Modules\<Name>\` (recursively, except `bin\`, `obj\` and
  `References\`) is compiled into **one** assembly - split your plugin into as many files as you like, there
  is no `using`/partial-class gymnastics required, it behaves like one project.
- Exactly one public, non-abstract class in that assembly must derive from `Fougerite.Module`. If you have
  more than one (e.g. a couple of small helper `Module`-derived test classes), name the one you want loaded
  exactly like the plugin (`HelloScriptTest` in the example above) so the loader can tell which is the "main" one.
- If a folder contains **both** `Name.dll` and `Name.cs`, Fougerite loads it as a DLL module and logs a
  warning - it never compiles a `.cs` file next to an existing `.dll`.
- `Modules\<Name>\References\*.dll` are referenced automatically at compile time (and loaded at runtime),
  handy for a 3rd-party library your plugin needs that isn't already part of
  `rust_server_Data\Managed\` (which is referenced automatically too, so you get `Fougerite.dll`,
  `UnityEngine.dll`, `Assembly-CSharp.dll`, etc. for free - no manual "Add Reference" step).

### 3. The Module skeleton

Exactly the same base class as a DLL module - see [`CSharpPluginTutorial.md`](CSharpPluginTutorial.md#3-the-module-skeleton)
and [`Classes/BasePlugin.md`](Classes/BasePlugin.md) for everything it inherits (timers, logging, dictionaries,
cross-plugin messaging, ...). `Order` works the same way too (lower loads/compiles first, matters when
multiple plugins use `#require` on each other).

### 4. Compiling, caching and hot-reload

- Compilation happens once per unique combination of source code + references + compiler (a content hash),
  the result is cached under `Save\.CSScriptCache\<Name>\`. Editing a `.cs` file and reloading recompiles
  only what changed; unchanged plugins reuse the cached `.dll` instantly, so startup stays fast even with
  many CSScript plugins.
- The full compiler output (even on success) is written to `Save\.CSScriptCache\<Name>\<Name>.log` - check
  it first if a plugin fails to load with a compile error.
- CSScript plugins are watched by the same file watcher as script plugins
  ([`PluginWatcher`](Classes/PluginLoaders.md#pluginwatcher)) - editing/saving a `.cs` file under
  `Modules\<Name>\` triggers an automatic recompile + reload, no server restart needed.
- `CSScriptPluginLoader` shares the same unmanaged native plugin registry as DLL modules
  (`CSharpPluginLoader`), through [`NativeDomainManager`](Classes/PluginLoaders.md#nativedomainmanager). It
  is created lazily the first time either `EnableCSharp` or `EnableCSScript` plugins actually load, and only
  released once **both** engines have finished unloading their own plugins - so disabling DLL modules
  (`EnableCSharp=false`) while only using CSScript plugins (or vice versa) still works correctly, and
  unloading one engine's plugins never tears anything out from under the other engine's still-loaded ones.

### 5. `#require`: referencing other plugins and modules

A realistic CSScript plugin usually wants to call into another plugin, whether that's a DLL `Module` someone
else built, or another CSScript plugin. Add a special comment directive, on its own line, anywhere in any
`.cs` file of your plugin:
```csharp
// #require OtherPlugin
```
`OtherPlugin` must be the plugin/module **name** (its folder name), not a file path. This does two things:
1. **Compile-time reference** - the other plugin's compiled assembly (`Modules\OtherPlugin\OtherPlugin.dll`
   for a DLL module, or the other CSScript plugin's own compiled output) is added as a reference, so you can
   `using OtherPluginNamespace;` and call its public types/methods directly, with full IntelliSense-style
   compile-time checking (no reflection needed).
2. **Load-order dependency** - the loader makes sure `OtherPlugin` is compiled and loaded **before** your
   plugin, automatically, regardless of what order the folders/`Order` values would otherwise suggest.

Example - a plugin that type-safely calls into another already-loaded CSScript plugin:
```csharp
// #require EconomyCore
using Fougerite;
using EconomyCore; // the other plugin's own namespace

namespace ShopPlugin
{
    public class ShopPlugin : Module
    {
        public override void Initialize()
        {
            // EconomyPlugin is EconomyCore's Module subclass - fully typed, not a dictionary lookup.
            EconomyPlugin economy = (EconomyPlugin) Fougerite.PluginLoaders.PluginLoader
                .GetInstance().Plugins["EconomyCore"]
                .GetType()
                .GetProperty("Engine")
                .GetValue(((Fougerite.PluginLoaders.CSScriptPlugin) Fougerite.PluginLoaders.PluginLoader
                    .GetInstance().Plugins["EconomyCore"]), null);
        }
    }
}
```
That reflection dance is only needed because `PluginLoader.Plugins` stores the generic `BasePlugin` wrapper.
In practice it's much simpler to just cast the wrapper and read its public `Engine` field directly, since
`#require` already guarantees `EconomyCore` compiled and loaded first:
```csharp
// #require EconomyCore
using Fougerite;
using Fougerite.PluginLoaders;
using EconomyCore;

namespace ShopPlugin
{
    public class ShopPlugin : Module
    {
        private EconomyPlugin _economy;

        public override void Initialize()
        {
            CSScriptPlugin wrapper = (CSScriptPlugin) PluginLoader.GetInstance().Plugins["EconomyCore"];
            _economy = (EconomyPlugin) wrapper.Engine;
            _economy.AddBalance("76561198000000000", 100);
        }
    }
}
```
`#require` also works against a DLL `Module` by name the exact same way - the loader looks the name up
among both CSScript plugins and `Modules\*.dll` names, so you don't need to know in advance which kind the
dependency is.

**Soft dependencies (no `#require` needed):** if your code merely *mentions* another plugin's name as a
string literal somewhere (e.g. `PluginLoader.GetInstance().Plugins["EconomyCore"]` without a cast, used
purely through reflection/duck-typing, which is also perfectly valid and common when you don't want a hard
compile-time reference), the loader still notices the literal and treats it as an ordering hint - it tries
to load that plugin first if possible, without adding a compiler reference. Use `#require` when you want to
use the other plugin's types directly; rely on the soft dependency when you only need "best effort" ordering.

### 6. Reacting to other plugins loading/unloading

Two hooks fire for **every** plugin, of **any** type (DLL module, CSScript, Python, JS, Lua) - handy
precisely for CSScript plugins that use `#require`/soft dependencies and want to know when a dependency came
online or went away at runtime (e.g. re-resolving a reference after a dependency reloads):
- [`Hooks.OnPluginLoaded`](Hooks/Server/On_PluginLoaded.md) - fires right after a plugin finishes loading.
- [`Hooks.OnPluginUnloaded`](Hooks/Server/On_PluginUnloaded.md) - fires right after a plugin unloads
  (including the "unload" half of a reload).

```csharp
public override void Initialize()
{
    Hooks.OnPluginLoaded += HandlePluginLoaded;
    Hooks.OnPluginUnloaded += HandlePluginUnloaded;
}

public override void DeInitialize()
{
    Hooks.OnPluginLoaded -= HandlePluginLoaded;
    Hooks.OnPluginUnloaded -= HandlePluginUnloaded;
}

private void HandlePluginLoaded(BasePlugin plugin)
{
    if (plugin.Name == "EconomyCore")
    {
        // Re-resolve our reference, in case EconomyCore was just (re)loaded after we were.
    }
}

private void HandlePluginUnloaded(BasePlugin plugin)
{
    if (plugin.Name == "EconomyCore")
    {
        _economy = null;
    }
}
```

### 7. Full example: `HelloScriptTest`

A complete, working multi-file CSScript plugin. It demonstrates: the `Module` skeleton, a helper type living
in its own file (proving every `.cs` file in the folder gets compiled), and both new hooks.

> The example is named `HelloScriptTest` rather than `HelloScript` because Rust's own `Assembly-CSharp.dll`
> already ships a (legacy, unrelated) `HelloScript` type - reusing that name only triggers a harmless
> `CS0435` namespace/type conflict warning at compile time, but it's cleaner to just pick a name that
> doesn't collide with anything the game itself already defines.

```
Modules\
└── HelloScriptTest\
    └── HelloScriptTest.cs
```

```csharp
using System;
using Fougerite;
using Fougerite.PluginLoaders;
using HelloScriptTest.Helpers;

namespace HelloScriptTest
{
    /// <summary>
    /// Minimal C# script plugin used to test the CSScript loader.
    /// </summary>
    public class HelloScriptTest : Module
    {
        public override string Name
        {
            get { return "HelloScriptTest"; }
        }

        public override string Author
        {
            get { return "Fougerite"; }
        }

        public override string Description
        {
            get { return "Logs a few lines to verify multi file C# script plugins."; }
        }

        public override Version Version
        {
            get { return new Version(1, 0); }
        }

        public override uint Order
        {
            get { return 500000; }
        }

        public override void Initialize()
        {
            Logger.Log(Greeter.Hello(Name));
            Hooks.OnModulesLoaded += HandleModulesLoaded;
            Hooks.OnPluginLoaded += HandlePluginLoaded;
            Hooks.OnPluginUnloaded += HandlePluginUnloaded;
        }

        public override void DeInitialize()
        {
            Hooks.OnModulesLoaded -= HandleModulesLoaded;
            Hooks.OnPluginLoaded -= HandlePluginLoaded;
            Hooks.OnPluginUnloaded -= HandlePluginUnloaded;
            Logger.Log(Greeter.Goodbye(Name));
        }

        private void HandleModulesLoaded()
        {
            Logger.Log(Greeter.Format(Name, "all C# modules are loaded."));
        }

        private void HandlePluginLoaded(BasePlugin plugin)
        {
            Logger.Log(Greeter.Format(Name, "plugin loaded " + plugin.Name + " (" + plugin.Type + ")."));
        }

        private void HandlePluginUnloaded(BasePlugin plugin)
        {
            Logger.Log(Greeter.Format(Name, "plugin unloaded " + plugin.Name + " (" + plugin.Type + ")."));
        }
    }
}

namespace HelloScriptTest.Helpers
{
    /// <summary>
    /// Lives in a second file to verify that every .cs file in the plugin folder is compiled.
    /// </summary>
    public static class Greeter
    {
        public static string Hello(string pluginName)
        {
            return Format(pluginName, "hello from " + typeof(Greeter).Assembly.GetName().Name + ".");
        }

        public static string Goodbye(string pluginName)
        {
            return Format(pluginName, "goodbye.");
        }

        public static string Format(string pluginName, string message)
        {
            return "[" + pluginName + "] " + message;
        }
    }
}
```

> This single file actually declares two namespaces (`HelloScriptTest` and `HelloScriptTest.Helpers`). You can
> just as well split them into `HelloScriptTest.cs` and `Helpers.cs` inside `Modules\HelloScriptTest\` - both
> layouts compile identically, since the loader simply gathers every `.cs` file in the folder before compiling.

Drop the file(s) in `Modules\HelloScriptTest\HelloScriptTest.cs`, make sure `EnableCSScript=true` in
`Fougerite.cfg`, and start/reload the server. You should see something like:
```
[CSScriptPluginLoader] Using the MSBuild Roslyn compiler at ...
[CSScriptPluginLoader] Compiled HelloScriptTest (1 files) in 180 ms.
[PluginLoader] Module HelloScriptTest<CSScript> v1.0 (by Fougerite) initiated.
[HelloScriptTest] hello from HelloScriptTest.
[HelloScriptTest] all C# modules are loaded.
```

### 8. Where to go next
- [`CSharpPluginTutorial.md`](CSharpPluginTutorial.md) - the DLL-module tutorial; CSScript plugins share the
  exact same `Module`/`BasePlugin` API, this tutorial only covers what's different about CSScript.
- [`Hooks/README.md`](Hooks/README.md) - every hook/event, including
  [`On_PluginLoaded`](Hooks/Server/On_PluginLoaded.md) and [`On_PluginUnloaded`](Hooks/Server/On_PluginUnloaded.md).
- [`Classes/PluginLoaders.md`](Classes/PluginLoaders.md) - `CSScriptPluginLoader`/`CSScriptPlugin` and the
  rest of the plugin-loading machinery.
- [`FougeriteCfg.md`](FougeriteCfg.md#engines-section) - the full `[Engines]` reference, including
  `EnableCSScript`/`CSScriptCompiler`.
- [`Installation.md`](Installation.md) - where plugin folders live, and how to relocate `Modules`/`Save`.
