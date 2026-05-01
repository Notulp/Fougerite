using System;
using System.Collections.Generic;
using System.IO;

namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// The LuaPluginLoader class is responsible for managing the loading, unloading, reloading,
    /// and initialization of Lua-based plugins for the application. It provides methods to
    /// handle plugin file paths, read plugin source files, and manage plugin instances during runtime.
    /// The MoonSharp 2.0.0.0 was manually edited to work with shadowed declarations.
    /// https://gist.github.com/dretax/baf77c26c3e0fc4c497eea427577e556
    /// Interpreters like IronPython and Jint work differently because they are built on the DLR or use highly dynamic reflection.
    /// They resolve member access at runtime and if they find multiple members with the same name, they use a binder to pick the "best" match or provide a collection.
    /// MoonSharp is a static-mapping engine designed for raw speed.
    /// It uses a Dictionary(string, IMemberDescriptor) to map names to members.
    /// Since a Dictionary cannot have duplicate keys, MoonSharp is hard-coded to throw an ArgumentException,
    /// which I changed to mangle the name of the nth member by appending a prefix of the type name or "Base".
    /// </summary>
    public class LuaPluginLoader : Singleton<LuaPluginLoader>, ISingleton, IPluginLoader
    {
        public PluginType Type = PluginType.Lua;
        public const string Extension = ".lua";
        public readonly DirectoryInfo PluginDirectory = new DirectoryInfo(Path.Combine(Util.GetRootFolder(), "Save\\LuaPlugins"));

        public LuaPluginLoader()
        {
           
        }

        public string GetExtension()
        {
            return Extension;
        }

        public string GetSource(string pluginname)
        {
            return File.ReadAllText(GetMainFilePath(pluginname));
        }

        public string GetMainFilePath(string pluginname)
        {
            return Path.Combine(GetPluginDirectoryPath(pluginname), pluginname + Extension);
        }

        public string GetPluginDirectoryPath(string name)
        {
            return Path.Combine(PluginDirectory.FullName, name);
        }

        public List<string> GetPluginNames()
        {
            List<string> Data = new List<string>();
            foreach (DirectoryInfo dirInfo in PluginDirectory.GetDirectories())
            {
                string path = Path.Combine(dirInfo.FullName, dirInfo.Name + Extension);
                if (File.Exists(path))
                {
                    Data.Add(dirInfo.Name);
                }
            }

            return Data;
        }

        public void LoadPlugin(string name)
        {
            if (Bootstrap.IgnoredPlugins.Contains(name.ToLower()))
            {
                Logger.LogDebug($"[LUAPluginLoader] Ignoring plugin {name}.");
                return;
            }
            
            Logger.LogDebug($"[LUAPluginLoader] Loading plugin {name}.");

            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                Logger.LogError($"[LUAPluginLoader] {name} plugin is already loaded.");
                throw new InvalidOperationException($"[LUAPluginLoader] {name} plugin is already loaded.");
            }

            if (PluginLoader.GetInstance().CurrentlyLoadingPlugins.Contains(name)) {
                Logger.LogWarning($"{name} plugin is already being loaded. Returning.");
                return;
            }

            try
            {
                string code = GetSource(name);
                DirectoryInfo path = new DirectoryInfo(Path.Combine(PluginDirectory.FullName, name));

                PluginLoader.GetInstance().CurrentlyLoadingPlugins.Add(name);

                new LUAPlugin(name, code, path);

            }
            catch (Exception ex)
            {
                Logger.Log($"[LUAPluginLoader] {name} plugin could not be loaded.");
                Logger.LogException(ex);
                if (PluginLoader.GetInstance().CurrentlyLoadingPlugins.Contains(name)) {
                    PluginLoader.GetInstance().CurrentlyLoadingPlugins.Remove(name);
                }
            }
        }

        public void LoadPlugins()
        {
            if (Config.GetBoolValue("Engines", "EnableLua"))
            {
                foreach (string name in GetPluginNames())
                    LoadPlugin(name);
            }
            else
            {
                Logger.LogDebug("[LUAPluginLoader] Lua plugins are disabled in Fougerite.cfg.");
            }
        }

        public void ReloadPlugin(string name)
        {
            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                UnloadPlugin(name);
                LoadPlugin(name);
            }
        }

        public void ReloadPlugins()
        {
            foreach (BasePlugin plugin in PluginLoader.GetInstance().Plugins.Values)
            {
                if (!plugin.DontReload)
                {
                    if (plugin.Type == Type)
                    {
                        UnloadPlugin(plugin.Name);
                        LoadPlugin(plugin.Name);
                    }
                }
            }
        }

        public void UnloadPlugin(string name)
        {
            Logger.LogDebug($"[LUAPluginLoader] Unloading {name} plugin.");

            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                BasePlugin plugin = PluginLoader.GetInstance().Plugins[name];
                if (plugin.DontReload)
                    return;

                LUAPlugin luaplugin = (LUAPlugin) plugin;
                
                if (plugin.Globals.Contains("On_PluginDeinit"))
                    plugin.Invoke("On_PluginDeinit");

                plugin.KillTimers();
                PluginLoader.GetInstance().RemoveHooks(luaplugin);
                if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
                {
                    PluginLoader.GetInstance().Plugins.Remove(name);
                }

                Logger.LogDebug($"[LUAPluginLoader] {name} plugin was unloaded successfuly.");
            }
            else
            {
                Logger.LogError($"[LUAPluginLoader] Can't unload {name}. Plugin is not loaded.");
                throw new InvalidOperationException($"[LUAPluginLoader] Can't unload {name}. Plugin is not loaded.");
            }
        }

        public void UnloadPlugins()
        {
            foreach (string name in PluginLoader.GetInstance().Plugins.Keys)
                UnloadPlugin(name);
        }

        public void Initialize()
        {
            if (!PluginDirectory.Exists)
            {
                PluginDirectory.Create();
            }
            
            typeof(MoonSharp.Interpreter.Platforms.PlatformAutoDetector).SetFieldValueValue("m_AutoDetectionsDone", true);
            typeof(MoonSharp.Interpreter.Platforms.PlatformAutoDetector).SetFieldValueValue("<IsRunningOnUnity>k__BackingField", true);
            typeof(MoonSharp.Interpreter.Platforms.PlatformAutoDetector).SetFieldValueValue("<IsRunningOnMono>k__BackingField", true);
            typeof(MoonSharp.Interpreter.Platforms.PlatformAutoDetector).SetFieldValueValue("<IsRunningOnClr4>k__BackingField", true);
            PluginWatcher.GetInstance().AddWatcher(Type, Extension, Path.Combine(Util.GetRootFolder(), "Save"));
            PluginLoader.GetInstance().PluginLoaders.Add(Type, this);
            LoadPlugins();
        }

        public bool CheckDependencies()
        {
            return Config.GetBoolValue("Engines", "EnableLua");
        }
    }
}