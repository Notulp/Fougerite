using System;
using System.Collections.Generic;
using System.IO;

namespace Fougerite.PluginLoaders
{
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