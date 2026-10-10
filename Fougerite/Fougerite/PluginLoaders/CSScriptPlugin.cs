using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Fougerite.Concurrent;

namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// Lifetime proxy for a C# script plugin, works like <see cref="CSPlugin"/> and wraps the compiled
    /// <see cref="Module"/> through the <see cref="Engine"/> field.
    /// </summary>
    public class CSScriptPlugin : BasePlugin
    {
        public Module Engine;

        public Assembly CompiledAssembly { get; private set; }

        public readonly string AssemblyPath;

        public readonly List<string> Dependencies;

        public bool IsInitialized { get; private set; }

        private ModuleContainer _container;

        /// <summary>
        /// Initializes a new instance of the <see cref="CSScriptPlugin"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="assemblyPath">Compiled assembly path.</param>
        /// <param name="rootdir">Rootdir.</param>
        /// <param name="dependencies">Required plugins.</param>
        public CSScriptPlugin(string name, string assemblyPath, DirectoryInfo rootdir, IEnumerable<string> dependencies) : base(name, rootdir)
        {
            Type = PluginType.CSScript;
            AssemblyPath = assemblyPath;
            Dependencies = new List<string>(dependencies);

            Load(assemblyPath);
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
                    object result = null;

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
                if (ex is TargetInvocationException && ex.InnerException != null)
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
                Assembly assembly = Assembly.Load(File.ReadAllBytes(code));
                CompiledAssembly = assembly;
                CSScriptPluginLoader.GetInstance().RegisterScriptAssembly(assembly);

                Type moduleType = FindModuleType(assembly);
                Logger.LogDebug($"[CSScriptPlugin] Checked {moduleType.FullName}");

                Module instance = (Module) Activator.CreateInstance(moduleType);
                instance.ModuleFolder = GetDataFolder(instance);
                instance.RootDir = new DirectoryInfo(instance.ModuleFolder);

                if (!Directory.Exists(instance.ModuleFolder))
                {
                    Directory.CreateDirectory(instance.ModuleFolder);
                }

                Author = instance.Author;
                About = instance.Description;
                Version = instance.Version.ToString();

                _container = new ModuleContainer(instance);
                #pragma warning disable 618
                ModuleManager.Modules.Add(_container);
                #pragma warning restore 618

                Engine = instance;
                Globals = new ConcurrentList<string>(moduleType.GetMethods().Select(method => method.Name).ToList());
                State = PluginState.Loaded;

                Logger.LogDebug($"[CSScriptPlugin] Module added: {Name} ({moduleType.FullName})");
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CSScriptPlugin] Failed to load plugin \"{Name}\". The plugin will not be considered loaded and all its resources will be released. {DescribeLoadException(ex)}");
                State = PluginState.FailedToLoad;
                Engine = null;
                ReleaseModule();
            }

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }

        internal void InitializeEngine()
        {
            if (State != PluginState.Loaded || Engine == null || IsInitialized)
                return;

            IsInitialized = true;
            try
            {
                Engine.Initialize();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CSScriptPlugin] Module \"{Engine.Name}\" has thrown an exception during initialization. {ex}");
            }
        }

        internal void DeInitializeEngine()
        {
            if (!IsInitialized || Engine == null)
                return;

            IsInitialized = false;
            try
            {
                Engine.DeInitialize();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CSScriptPlugin] Module \"{Engine.Name}\" has thrown an exception while being deinitialized. {ex}");
            }
        }

        internal void ReleaseResources()
        {
            KillTimers();
            DisposeWebSockets(this);

            if (Engine != null)
            {
                Engine.KillTimers();
                DisposeWebSockets(Engine);
            }

            ReleaseModule();
        }

        private static void DisposeWebSockets(BasePlugin owner)
        {
            foreach (var socket in owner.WebSockets.ToList())
            {
                owner.RemoveWebSocket(socket.SocketId);
            }
        }

        private void ReleaseModule()
        {
            if (_container != null)
            {
                #pragma warning disable 618
                ModuleManager.Modules.Remove(_container);
                #pragma warning restore 618
                _container = null;
            }

            if (CompiledAssembly != null)
            {
                CSScriptPluginLoader.GetInstance().UnregisterScriptAssembly(CompiledAssembly);
            }
        }

        private Type FindModuleType(Assembly assembly)
        {
            List<Type> candidates = assembly.GetExportedTypes()
                .Where(type => type.IsPublic && !type.IsAbstract && type.IsSubclassOf(typeof(Module)))
                .ToList();

            if (candidates.Count == 1)
                return candidates[0];

            if (candidates.Count == 0)
                throw new InvalidOperationException($"No public, non abstract {nameof(Module)} subclass was found in the plugin sources.");

            Type named = candidates.FirstOrDefault(type => string.Equals(type.Name, Name, StringComparison.OrdinalIgnoreCase));
            if (named != null)
                return named;

            throw new InvalidOperationException(
                $"Found {candidates.Count} {nameof(Module)} subclasses ({string.Join(", ", candidates.Select(type => type.FullName).ToArray())}). Name the main one {Name} so the loader can tell which one to use.");
        }

        private string GetDataFolder(Module instance)
        {
            string configured = Config.GetValue("Modules", Name) ?? Config.GetValue("Modules", instance.Name);
            string folder = string.IsNullOrEmpty(configured) ? Name : configured.Trim().TrimStart('\\', '/');
            return Path.Combine(Util.GetRootFolder(), $"Save\\{folder}");
        }

        private static string DescribeLoadException(Exception ex)
        {
            if (ex is TargetInvocationException && ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            ReflectionTypeLoadException typeLoadException = ex as ReflectionTypeLoadException;
            if (typeLoadException == null || typeLoadException.LoaderExceptions == null)
                return ex.ToString();

            StringBuilder builder = new StringBuilder(ex.ToString());
            foreach (string message in typeLoadException.LoaderExceptions.Where(inner => inner != null).Select(inner => inner.Message).Distinct())
            {
                builder.AppendLine().Append(message);
            }

            return builder.ToString();
        }
    }
}
