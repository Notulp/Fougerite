using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// Manages file system watchers for tracking changes and creation of different plugin types.
    /// </summary>
    public class PluginWatcher : Singleton<PluginWatcher>, ISingleton
    {
        /// <summary>
        /// A list containing the registered active <see cref="PluginTypeWatcher"/> instances.
        /// </summary>
        public readonly List<PluginTypeWatcher> Watchers = new List<PluginTypeWatcher>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWatcher"/> class.
        /// </summary>
        public PluginWatcher()
        {
        }

        /// <summary>
        /// Adds a file system watcher for a specific plugin type if one does not already exist.
        /// </summary>
        /// <param name="type">The type of plugin to watch.</param>
        /// <param name="filter">The file extension filter (".dll", ".py").</param>
        /// <param name="path">The root directory path to watch.</param>
        public void AddWatcher(PluginType type, string filter, string path)
        {
            foreach (PluginTypeWatcher watch in Watchers)
                if (watch.Type == type)
                    return;

            PluginTypeWatcher watcher = new PluginTypeWatcher(type, filter, path);
            Watchers.Add(watcher);
        }

        /// <summary>
        /// Initializes the singleton instance.
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// Checks if the dependencies required by this component are met.
        /// </summary>
        /// <returns>Always returns true.</returns>
        public bool CheckDependencies()
        {
            return true;
        }
    }

    /// <summary>
    /// Wrapper for a <see cref="FileSystemWatcher"/> configured to monitor files of a specific <see cref="PluginType"/>.
    /// </summary>
    public class PluginTypeWatcher : CountedInstance
    {
        /// <summary>
        /// The plugin type assigned to this watcher.
        /// </summary>
        public PluginType Type;

        /// <summary>
        /// The underlying unmanaged system file watcher component.
        /// </summary>
        public readonly FileSystemWatcher Watcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginTypeWatcher"/> class.
        /// </summary>
        /// <param name="type">The plugin type context.</param>
        /// <param name="filter">The file extension filter sequence.</param>
        /// <param name="custompath">The absolute directory path to monitor.</param>
        public PluginTypeWatcher(PluginType type, string filter, string custompath)
        {
            Type = type;
            Watcher = new FileSystemWatcher(custompath, $"*{filter}");
            Watcher.EnableRaisingEvents = true;
            Watcher.IncludeSubdirectories = true;
            Watcher.Changed += OnPluginChanged;
            Watcher.Created += OnPluginCreated;
        }

        /// <summary>
        /// Returns a string representation of the current watcher context.
        /// </summary>
        /// <returns>A string specifying the watched plugin type.</returns>
        public override string ToString()
        {
            return $"PluginTypeWatcher<{Type}>";
        }

        /// <summary>
        /// Tries to dynamically hot-reload or load a plugin on the main thread safely.
        /// </summary>
        /// <param name="name">The name of the plugin script or assembly target.</param>
        /// <param name="type">The type of the plugin configuration.</param>
        /// <returns>True if the loading invocation succeeds, otherwise false.</returns>
        private bool TryLoadPlugin(string name, PluginType type)
        {
            try
            {
                BasePlugin plugin = null;
                if (PluginLoader.GetInstance().Plugins.TryGetValue(name, out plugin))
                    PluginLoader.GetInstance().ReloadPlugin(plugin);
                else
                    PluginLoader.GetInstance().LoadPlugin(name, type);

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[PluginWatcher] Error: {ex}");
                return false;
            }
        }

        /// <summary>
        /// Evaluates whether the modified or created file extension maps to a valid framework plugin type.
        /// </summary>
        /// <param name="path">The file path or name string target.</param>
        /// <returns>True if the string maps to a matching script or library footprint extension.</returns>
        private bool IsAPlugin(string path)
        {
            return path.EndsWith(".py") || path.EndsWith(".lua") || path.EndsWith(".dll") || path.EndsWith(".js");
        }

        /// <summary>
        /// Triggered when a monitored directory creates a file matching the extension pattern.
        /// </summary>
        /// <param name="sender">The source sender component.</param>
        /// <param name="e">The file event arguments wrapper data.</param>
        private void OnPluginCreated(object sender, FileSystemEventArgs e)
        {
            Loom.QueueOnMainThread(() =>
            {
                try
                {
                    string filename = Path.GetFileNameWithoutExtension(e.Name);
                    string dir = Path.GetDirectoryName(e.FullPath).Split(Path.DirectorySeparatorChar).Last();

                    if (filename == dir && IsAPlugin(e.Name))
                    {
                        if (!TryLoadPlugin(filename, Type))
                        {
                            Logger.Log(string.Format("[PluginWatcher] Couldn't load: {0}{3}{1}.{2}", dir, filename,
                                Type, Path.DirectorySeparatorChar));
                        }
                        else
                        {
                            Logger.Log($"[PluginWatcher] Detected new plugin {filename}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PluginWatcher] OnPluginCreated error: {ex}");
                }
            });
        }

        /// <summary>
        /// Triggered when a monitored directory modifies a file matching the extension pattern.
        /// </summary>
        /// <param name="sender">The source sender component.</param>
        /// <param name="e">The file event arguments wrapper data.</param>
        private void OnPluginChanged(object sender, FileSystemEventArgs e)
        {
            Loom.QueueOnMainThread(() =>
            {
                try
                {
                    string filename = Path.GetFileNameWithoutExtension(e.Name);
                    string dir = Path.GetDirectoryName(e.FullPath).Split(Path.DirectorySeparatorChar).Last();

                    string assumedPluginPathFromDir =
                        Path.Combine(Path.Combine(Watcher.Path, dir), dir + Path.GetExtension(e.Name));

                    if (filename == dir && IsAPlugin(e.Name))
                    {
                        if (File.Exists(e.FullPath))
                        {
                            if (!TryLoadPlugin(filename, Type))
                            {
                                Logger.Log(string.Format("[PluginWatcher] Couldn't load: {0}{3}{1}.{2}", dir,
                                    filename, Type, Path.DirectorySeparatorChar));
                            }
                            else
                            {
                                Logger.Log($"[PluginWatcher] Reloaded plugin {filename}");
                            }
                        }
                    }
                    else if (File.Exists(assumedPluginPathFromDir) && IsAPlugin(e.Name))
                    {
                        if (!TryLoadPlugin(dir, Type))
                        {
                            Logger.Log(string.Format("[PluginWatcher] Couldn't load: {0}{3}{1}.{2}", dir, filename,
                                Type, Path.DirectorySeparatorChar));
                        }
                        else
                        {
                            Logger.Log($"[PluginWatcher] Reloaded plugin {filename}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"[PluginWatcher] OnPluginChanged error: {ex}");
                }
            });
        }
    }
}