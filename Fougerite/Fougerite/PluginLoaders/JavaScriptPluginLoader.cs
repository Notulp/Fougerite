using System;
using System.Collections.Generic;
using System.IO;

namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// Patched Jint.Runtime.Interop.ObjectWrapper (v2.7.1.0)
    /// 
    /// MODIFICATIONS:
    /// 1. Put(string, JsValue, bool):
    ///    - Disabled strict "Unknown member" exceptions.
    ///    - Modified IL flow to allow value assignment even if the member isn't explicitly 
    ///      defined in the initial property map, enabling dynamic-style assignment 
    ///      to underlying .NET descriptors.
    /// 
    /// 2. GetOwnProperty(string):
    ///    - Added "Auto-Marshalling" for .NET Collections:
    ///       - IDictionary Support: Automatically converts .NET Dictionaries to JsObjects 
    ///         populated with native JS values and a 'Count' property.
    ///       - IEnumerable Support: Automatically converts .NET Enumerables/Lists to 
    ///         JsArrays with indexed access and a 'length' property.
    ///    - Flattened Reflection: Optimized the lookup for Properties, Fields, and Methods 
    ///      by removing overhead-heavy Linq-delegates and anonymous methods (b__6 to b__12) 
    ///      in favor of direct IL branch logic.
    /// 
    /// Patched Jint.Runtime.StatementInterpreter (v2.7.1.0)
    /// 
    /// MODIFICATIONS:
    /// 1. ExecuteForInStatement(ForInStatement):
    ///    - Enhanced Descriptor Resolution: Updated the 'for...in' iteration logic to 
    ///      check for PropertyDescriptor.Value availability.
    ///    - Logic: If a descriptor exists but doesn't have a direct value, it now 
    ///      triggers a fallback to ObjectInstance.Get(name). This ensures that 
    ///      Specialized Descriptors (used in Fougerite/Rust interop) return 
    ///      correct values during loops.
    ///
    /// Javascript (Jint 2.7.1) is currently the slowest script language in the framework.
    /// </summary>
    public class JavaScriptPluginLoader : Singleton<JavaScriptPluginLoader>, ISingleton, IPluginLoader
    {
        public PluginType Type = PluginType.JavaScript;
        public const string Extension = ".js";
        public readonly DirectoryInfo PluginDirectory = new DirectoryInfo(Path.Combine(Util.GetRootFolder(), "Save\\JsPlugins"));

        public JavaScriptPluginLoader()
        {
        }

        public string GetExtension()
        {
            return Extension;
        }

        public string GetSource(string pluginname)
        {
            using (FileStream fs = new FileStream(GetMainFilePath(pluginname), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (StreamReader reader = new StreamReader(fs))
            {
                return reader.ReadToEnd();
            }
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
                Logger.LogDebug($"[JSPluginLoader] Ignoring plugin {name}.");
                return;
            }
            
            Logger.LogDebug($"[JSPluginLoader] Loading plugin {name}.");

            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                Logger.LogError($"[JSPluginLoader] {name} plugin is already loaded.");
                throw new InvalidOperationException($"[JSPluginLoader] {name} plugin is already loaded.");
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

                new JavaScriptPlugin(name, code, path);
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Error] Failed to load JavaScript plugin: {ex}");
                if (PluginLoader.GetInstance().CurrentlyLoadingPlugins.Contains(name))
                {
                    PluginLoader.GetInstance().CurrentlyLoadingPlugins.Remove(name);
                }
            }
        }

        public void LoadPlugins()
        {
            if (Config.GetBoolValue("Engines", "EnableJavaScript"))
            {
                foreach (string name in GetPluginNames())
                    LoadPlugin(name);
            }
            else
            {
                Logger.LogDebug("[JSPluginLoader] Javascript plugins are disabled in Core.cfg.");
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
            Logger.LogDebug($"[JSPluginLoader] Unloading {name} plugin.");

            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                BasePlugin plugin = PluginLoader.GetInstance().Plugins[name];
                if (plugin.DontReload)
                    return;
                JavaScriptPlugin jsplugin = (JavaScriptPlugin) plugin;

                if (plugin.Globals.Contains("On_PluginDeinit"))
                    plugin.Invoke("On_PluginDeinit");

                plugin.KillTimers();
                PluginLoader.GetInstance().RemoveHooks(jsplugin);
                if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
                {
                    PluginLoader.GetInstance().Plugins.Remove(name);
                }

                Logger.LogDebug($"[JSPluginLoader] {name} plugin was unloaded successfuly.");
            }
            else
            {
                Logger.LogError($"[JSPluginLoader] Can't unload {name}. Plugin is not loaded.");
                throw new InvalidOperationException($"[JSPluginLoader] Can't unload {name}. Plugin is not loaded.");
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
            return Config.GetBoolValue("Engines", "EnableJavaScript");
        }
    }
}