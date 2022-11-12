using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fougerite.PluginLoaders
{
    public class PluginWatcher : Singleton<PluginWatcher>, ISingleton
    {
        public readonly List<PluginTypeWatcher> Watchers = new List<PluginTypeWatcher>();

        public PluginWatcher()
        {
        }

        public void AddWatcher(PluginType type, string filter, string path)
        {
            foreach (PluginTypeWatcher watch in Watchers)
                if (watch.Type == type)
                    return;

            PluginTypeWatcher watcher = new PluginTypeWatcher(type, filter, path);
            Watchers.Add(watcher);
        }

        public void Initialize()
        {
        }

        public bool CheckDependencies()
        {
            return true;
        }
    }

    public class PluginTypeWatcher : CountedInstance
    {
        public PluginType Type;

        public readonly FileSystemWatcher Watcher;

        public PluginTypeWatcher(PluginType type, string filter, string custompath)
        {
            Type = type;
            Watcher = new FileSystemWatcher(custompath, $"*{filter}");
            Watcher.EnableRaisingEvents = true;
            Watcher.IncludeSubdirectories = true;
            Watcher.Changed += OnPluginChanged;
            Watcher.Created += OnPluginCreated;
        }

        public override string ToString()
        {
            return $"PluginTypeWatcher<{Type}>";
        }

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

        private bool IsAPlugin(string path)
        {
            return path.EndsWith(".py") || path.EndsWith(".lua") || path.EndsWith(".dll") || path.EndsWith(".js");
        }

        private void OnPluginCreated(object sender, FileSystemEventArgs e)
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
                    Loom.QueueOnMainThread(() => {
                        Logger.Log($"[PluginWatcher] Detected new plugin {filename}");
                    });
                }
            }
        }

        private void OnPluginChanged(object sender, FileSystemEventArgs e)
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
                        Loom.QueueOnMainThread(() => {
                            Logger.Log($"[PluginWatcher] Reloaded plugin {filename}");
                        });
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
                    Loom.QueueOnMainThread(() => {
                        Logger.Log($"[PluginWatcher] Reloaded plugin {filename}");
                    });
                }
            }
        }
    }
}