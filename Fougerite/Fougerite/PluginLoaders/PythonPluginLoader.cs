using System;
using System.Collections.Generic;
using System.IO;

namespace Fougerite.PluginLoaders
{
    public class PythonPluginLoader : Singleton<PythonPluginLoader>, ISingleton, IPluginLoader
    {
        public PluginType Type = PluginType.Python;
        public const string Extension = ".py";
        public readonly DirectoryInfo PluginDirectory = new DirectoryInfo(Path.Combine(Util.GetRootFolder(), "Save\\PyPlugins"));

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
            List<string> names = new List<string>();
            foreach (DirectoryInfo dirInfo in PluginDirectory.GetDirectories())
            {
                string path = Path.Combine(dirInfo.FullName, dirInfo.Name + Extension);
                if (File.Exists(path))
                {
                    names.Add(dirInfo.Name);
                }
            }

            return names;
        }

        public void LoadPlugin(string name)
        {
            if (Bootstrap.IgnoredPlugins.Contains(name.ToLower()))
            {
                Logger.LogDebug($"[PYPluginLoader] Ignoring plugin {name}.");
                return;
            }
            
            Logger.LogDebug($"[PYPluginLoader] Loading plugin {name}.");

            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                Logger.LogError($"[PYPluginLoader] {name} plugin is already loaded.");
                throw new InvalidOperationException($"[PYPluginLoader] {name} plugin is already loaded.");
            }

            if (PluginLoader.GetInstance().CurrentlyLoadingPlugins.Contains(name))
            {
                Logger.LogWarning($"{name} plugin is already being loaded. Returning.");
                return;
            }

            try
            {
                string code = GetSource(name);
                //Logger.Log("Code: " + code);
                
                DirectoryInfo path =
                    new DirectoryInfo(Path.Combine(PluginDirectory.FullName, name));

                PluginLoader.GetInstance().CurrentlyLoadingPlugins.Add(name);

                new PythonPlugin(name, code, path);
            }
            catch (Exception ex)
            {
                Logger.Log($"[PYPluginLoader] {name} plugin could not be loaded.");
                Logger.LogException(ex);
                if (PluginLoader.GetInstance().CurrentlyLoadingPlugins.Contains(name))
                {
                    PluginLoader.GetInstance().CurrentlyLoadingPlugins.Remove(name);
                }
            }
        }

        public void LoadPlugins()
        {
            if (Config.GetBoolValue("Engines", "EnablePython"))
            {
                foreach (string name in GetPluginNames())
                    LoadPlugin(name);
            }
            else
            {
                Logger.LogDebug("[Fougerite] Python plugins are disabled in Fougerite.cfg.");
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
            Logger.LogDebug($"[PYPluginLoader] Unloading {name} plugin.");

            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                BasePlugin plugin = PluginLoader.GetInstance().Plugins[name];
                if (plugin.DontReload)
                    return;

                PythonPlugin pythonPlugin = (PythonPlugin) plugin;

                if (plugin.CachedGlobals.ContainsKey("On_PluginDeinit"))
                    plugin.Invoke("On_PluginDeinit");

                plugin.KillTimers();
                PluginLoader.GetInstance().RemoveHooks(pythonPlugin);
                if (PluginLoader.GetInstance().Plugins.ContainsKey(name)) 
                {
                    PluginLoader.GetInstance().Plugins.Remove(name);
                }

                Logger.LogDebug($"[PYPluginLoader] {name} plugin was unloaded successfuly.");
            }
            else
            {
                Logger.LogError($"[PYPluginLoader] Can't unload {name}. Plugin is not loaded.");
                throw new InvalidOperationException($"[PYPluginLoader] Can't unload {name}. Plugin is not loaded.");
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
            
            PluginWatcher.GetInstance().AddWatcher(Type, Extension, Path.Combine(Util.GetRootFolder(), "Save"));
            PluginLoader.GetInstance().PluginLoaders.Add(Type, this);
            LoadPlugins();
        }

        public bool CheckDependencies()
        {
            return Config.GetBoolValue("Engines", "EnablePython");
        }
    }
}