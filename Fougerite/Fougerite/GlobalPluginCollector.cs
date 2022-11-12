using System;
using System.Collections.Generic;
using System.Linq;

namespace Fougerite
{
    /// <summary>
    /// This class collects plugins from the engines.
    /// </summary>
    [Obsolete("GlobalPluginCollector will be removed from the future releases. Use PluginLoader.GetInstance()", false)]
    public class GlobalPluginCollector
    {
        private readonly Dictionary<string, object> AllPlugins;
        private readonly Dictionary<string, string> Types;
        private static GlobalPluginCollector pcollector;

        public GlobalPluginCollector()
        {
            AllPlugins = new Dictionary<string, object>();
            Types = new Dictionary<string, string>();
        }

        /// <summary>
        /// Gets the instance of the GlobalPluginCollector
        /// </summary>
        /// <returns></returns>
        public static GlobalPluginCollector GetPluginCollector()
        {
            if (pcollector == null)
            {
                pcollector = new GlobalPluginCollector();
            }
            return pcollector;
        }

        /// <summary>
        /// Adds a plugin, this is used by the interpreters or the module loader.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="plugin"></param>
        /// <param name="typename"></param>
        public void AddPlugin(string name, object plugin, string typename)
        {
            if (AllPlugins.ContainsKey(name))
            {
                Logger.LogError(
                    $"[Fougerite AddPlugin] Tried adding a plugin to the GlobalPluginCollector, with the same name? Rename the duplicate plugin! {name}");
                return;
            }
            AllPlugins[name] = plugin;
            Types[name] = typename;
        }

        /// <summary>
        /// Removes a plugin from the list.
        /// </summary>
        /// <param name="name"></param>
        public void RemovePlugin(string name)
        {
            if (AllPlugins.Keys.Contains(name))
            {
                AllPlugins.Remove(name);
            }
            if (Types.Keys.Contains(name))
            {
                Types.Remove(name);
            }
        }

        /// <summary>
        /// Gets the plugin's object. This can be anything depending on It's language.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object GetPlugin(string name)
        {
            if (AllPlugins.Keys.Contains(name))
            {
                return AllPlugins[name];
            }
            return null;
        }

        /// <summary>
        /// Gets the plugin's language type by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetPluginType(string name)
        {
            if (Types.Keys.Contains(name))
            {
                return Types[name];
            }
            return null;
        }
    }
}
