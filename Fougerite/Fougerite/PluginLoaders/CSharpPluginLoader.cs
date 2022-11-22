using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fougerite.PluginLoaders
{
    public class CSharpPluginLoader : Singleton<CSharpPluginLoader>, ISingleton, IPluginLoader
    {
        public PluginType Type = PluginType.CSharp;
        public const string Extension = ".dll";
        public readonly DirectoryInfo PluginDirectory = new DirectoryInfo(Path.Combine(Util.GetRootFolder(), "Modules\\"));

        public CSharpPluginLoader()
        {
        }

        public string GetExtension()
        {
            return Extension;
        }

        public string GetSource(string pluginname)
        {
            return GetMainFilePath(pluginname);
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
                Logger.LogDebug($"[CSharpPluginLoader] Ignoring plugin {name}.");
                return;
            }
            
            Logger.LogDebug($"[CSharpPluginLoader] Loading plugin {name}.");

            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                Logger.LogError($"[CSharpPluginLoader] {name} plugin is already loaded.");
                throw new InvalidOperationException($"[CSharpPluginLoader] {name} plugin is already loaded.");
            }

            if (PluginLoader.GetInstance().CurrentlyLoadingPlugins.Contains(name))
            {
                Logger.LogWarning($"{name} plugin is already being loaded. Returning.");
                return;
            }

            try
            {
                string code = GetSource(name);

                DirectoryInfo path =
                    new DirectoryInfo(Path.Combine(PluginDirectory.FullName, name));

                PluginLoader.GetInstance().CurrentlyLoadingPlugins.Add(name);

                new CSPlugin(name, code, path);
            }
            catch (Exception ex)
            {
                Logger.Log($"[CSharpPluginLoader] {name} plugin could not be loaded.");
                Logger.LogException(ex);
                if (PluginLoader.GetInstance().CurrentlyLoadingPlugins.Contains(name))
                {
                    PluginLoader.GetInstance().CurrentlyLoadingPlugins.Remove(name);
                }
            }
        }

        public void LoadPlugins()
        {
            if (Config.GetBoolValue("Engines", "EnableCSharp"))
            {
                foreach (string name in GetPluginNames())
                {
                    LoadPlugin(name);
                }

                List<Module> OrderedModuleSelector = new List<Module>();
                foreach (var x in PluginLoader.GetInstance().Plugins.Values)
                {
                    CSPlugin plugin = x as CSPlugin;
                    if (plugin != null)
                    {
                        OrderedModuleSelector.Add(plugin.Engine);
                    }
                }

                OrderedModuleSelector = OrderedModuleSelector.OrderBy(x => x.Order).ToList();

                foreach (Module CurrentModule in OrderedModuleSelector)
                {
                    try
                    {
                        CurrentModule.Initialize();
                    }
                    catch (Exception ex)
                    {
                        // Broken modules better stop the entire server init.
                        Logger.LogError(
                            $"[CSharpPlugin] Module \"{CurrentModule.Name}\" has thrown an exception during initialization. {ex}");
                    }

                    //Logger.Log(string.Format(
                    //    "[CSharpPlugin] Module {0} v{1} (by {2}) initiated.", CurrentModule.Name, CurrentModule.Version, CurrentModule.Author));
                }

                Hooks.ModulesLoaded();
            }
            else
            {
                Logger.LogDebug("[CSharpPluginLoader] C# plugins are disabled in Fougerite.cfg.");
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
                    }
                }
            }
            LoadPlugins();
        }

        public void UnloadPlugin(string name)
        {
            Logger.LogDebug($"[CSharpPluginLoader] Unloading {name} plugin.");

            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                BasePlugin plugin = PluginLoader.GetInstance().Plugins[name];
                if (plugin.DontReload)
                    return;

                CSPlugin csPlugin = (CSPlugin) plugin;

                try
                {
                    csPlugin.Engine.DeInitialize();
                }
                catch (Exception ex)
                {
                    Logger.LogError(
                        $"[Modules] Module \"{csPlugin.Name}\" has thrown an exception while being deinitialized:\n{ex}");
                }

                plugin.KillTimers();
                //PluginLoader.GetInstance().RemoveHooks(csPlugin);
                if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
                {
                    PluginLoader.GetInstance().Plugins.Remove(name);
                }

                #pragma warning disable 618
                foreach (ModuleContainer x in ModuleManager.Plugins)
                {
                    if (x.Plugin == csPlugin.Engine)
                    {
                        ModuleManager.Modules.Remove(x);
                        #pragma warning restore 618
                        break;
                    }
                }

                Logger.LogDebug($"[CSharpPluginLoader] {name} plugin was unloaded successfuly.");
            }
            else
            {
                Logger.LogError($"[CSharpPluginLoader] Can't unload {name}. Plugin is not loaded.");
                throw new InvalidOperationException($"[CSharpPluginLoader] Can't unload {name}. Plugin is not loaded.");
            }
        }

        public void UnloadPlugins()
        {
            #pragma warning disable 618
            ModuleManager.Modules.Clear();
            #pragma warning restore 618
            foreach (string name in PluginLoader.GetInstance().Plugins.Keys)
                UnloadPlugin(name);
        }

        public void Initialize()
        {
            PluginWatcher.GetInstance().AddWatcher(Type, Extension, Path.Combine(Util.GetRootFolder(), "Modules"));
            PluginLoader.GetInstance().PluginLoaders.Add(Type, this);
            LoadPlugins();
        }

        public bool CheckDependencies()
        {
            return Config.GetBoolValue("Engines", "EnableCSharp");
        }
    }
}