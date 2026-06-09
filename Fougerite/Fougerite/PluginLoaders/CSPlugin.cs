using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Fougerite.Concurrent;

namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// Internal engine-side lifetime proxy and isolated AppDomain workspace manager for C# plugins.
    /// </summary>
    /// <remarks>
    /// Architecture Layout:
    /// <code>
    ///        BasePlugin
    ///         /      \
    ///        /        \
    ///     Module    CSPlugin 
    ///                  │
    ///                  └──> (Wraps via field) ──> public Module Engine;
    /// </code>
    /// This design is the result of a core plugin loader subsystem rework implemented years ago. 
    /// To preserve strict backwards compatibility with legacy scripts compiled against the <see cref="Module"/> class, 
    /// the system shifts engine-side registration logic into this proxy layer.
    /// 
    /// <see cref="CSPlugin"/> fulfills the internal runtime lifecycle, unmanaged tracking, and memory assembly layout mechanics, 
    /// encapsulating the custom developer context via composition using the <see cref="Engine"/> reference field.
    /// </remarks>
    public class CSPlugin : BasePlugin
    {
        public Module Engine;
        public string ModuleFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSPlugin"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="code">Code.</param>
        /// <param name="rootdir">Rootdir.</param>
        public CSPlugin(string name, string code, DirectoryInfo rootdir) : base(name, rootdir)
        {
            Type = PluginType.CSharp;

            Load(code);
        }

        /// <summary>
        /// Invoke the specified method and args.
        /// </summary>
        /// <param name="args">Arguments.</param>
        /// <param name="func">Func.</param>
        public override object Invoke(string func, params object[] args)
        {
            try
            {
                if (State == PluginState.Loaded && Globals.Contains(func))
                {
                    object result = (object) null;

                    using (new Stopper($"{Type} {Name}", func))
                    {
                        result = Engine.CallMethod(func, args);
                    }

                    return result;
                }
            }
            catch (Exception ex)
            {
                string fileinfo = ("[Error] Failed to invoke: " + $"{Name}<{Type}>.{func}()" + Environment.NewLine);
                HasErrors = true;
                if (ex is TargetInvocationException)
                {
                    LastError = FormatException(ex.InnerException);
                    Logger.LogError(fileinfo + FormatException(ex.InnerException));
                }
                else
                {
                    LastError = FormatException(ex);
                    Logger.LogError(fileinfo + FormatException(ex));
                }
            }
            return null;
        }

        public override void Load(string code = "")
        {
            try
            {
                byte[] bin = File.ReadAllBytes(code);
                FileInfo FileInfo = new FileInfo(Path.Combine(RootDir.FullName, $"{Name}.dll"));
                
                IntPtr pluginMem = Marshal.AllocHGlobal(bin.Length);
                Assembly assembly;
                try
                {
                    Marshal.Copy(bin, 0, pluginMem, bin.Length);
                    var icalls = new Icalls();
                    assembly = icalls.mono_fg_load_plugin(Name, pluginMem, (uint)bin.Length);
                }
                finally
                {
                    if (pluginMem != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(pluginMem);
                    }
                }

                if (assembly == null)
                {
                    throw new Exception("Native mono plugin domain loading returned null.");
                }

                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (!type.IsSubclassOf(typeof(Module)) || !type.IsPublic || type.IsAbstract)
                        continue;
                    Logger.LogDebug($"[Modules] Checked {type.FullName}");
                    

                    Module PluginInstance = null;
                    try
                    {
                        PluginInstance = (Module) Activator.CreateInstance(type);
                        PluginInstance.ModuleFolder = Path.Combine(Util.GetRootFolder(), $"Save\\{Name}");
                        PluginInstance.RootDir = new DirectoryInfo(PluginInstance.ModuleFolder);
                        
                        if (Config.GetValue("Modules", PluginInstance.Name) != null)
                        {
                            PluginInstance.ModuleFolder = Path.Combine(Util.GetRootFolder(),
                                $"Save\\{Config.GetValue("Modules", Name).TrimStart('\\', '/').Trim()}");
                        }

                        Author = PluginInstance.Author;
                        About = PluginInstance.Description;
                        Version = PluginInstance.Version.ToString();
                        
                        if (!Directory.Exists(PluginInstance.ModuleFolder))
                        {
                            Directory.CreateDirectory(PluginInstance.ModuleFolder);
                        }
                        
                        Logger.LogDebug($"[Modules] Instance created: {type.FullName}");
                    }
                    catch (Exception ex)
                    {
                        // Broken plugins better stop the entire server init.
                        Logger.LogError(
                            $"[Modules] Could not create an instance of plugin class \"{type.FullName}\". {ex}");
                    }
                    
                    if (PluginInstance != null)
                    {
                        ModuleContainer Container = new ModuleContainer(PluginInstance);
                        #pragma warning disable 618
                        ModuleManager.Modules.Add(Container);
                        #pragma warning restore 618
                        Engine = PluginInstance;
                        Logger.LogDebug($"[Modules] Module added: {FileInfo.Name}");
                        Globals = new ConcurrentList<string>(type.GetMethods().Select(method => method.Name).ToList());
                        break;
                    }
                }

                State = PluginState.Loaded;
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
                State = PluginState.FailedToLoad;
            }

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }

        public void LoadReferences()
        {
            List<string> dllpaths = GetRefDllPaths().ToList();
            foreach (Assembly ass in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (dllpaths.Contains(ass.FullName))
                {
                    dllpaths.Remove(ass.FullName);
                }
            }

            dllpaths.ForEach(path => { Assembly.LoadFile(path); });
        }

        IEnumerable<string> GetRefDllPaths()
        {
            string refpath = Path.Combine(RootDir.FullName, "References");
            if (Directory.Exists(refpath))
            {
                DirectoryInfo refdir = new DirectoryInfo(refpath);
                FileInfo[] files = refdir.GetFiles("*.dll");
                foreach (FileInfo file in files)
                {
                    yield return file.FullName;
                }
            }
        }
    }
}