using System;
using System.IO;
using Fougerite.Caches;
using Fougerite.Permissions;
using IronPython.Hosting;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;

namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// PY plugin.
    /// </summary>
    public class PythonPlugin : BasePlugin
    {
        /// <summary>
        /// LibraryPath for python plugins.
        /// </summary>
        public readonly string LibPath = Path.Combine(Util.GetRootFolder(), Path.Combine("Save", "Lib"));

        public readonly string ManagedFolder =
            Path.Combine(Util.GetRootFolder(), Path.Combine("rust_server_Data", "Managed"));

        public readonly string Code;
        public object Class;
        
        public ScriptEngine Engine;
        public ScriptScope Scope;

        /// <summary>
        /// Initializes a new instance of the <see cref="PYPlugin"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="code">Code.</param>
        /// <param name="rootdir">Rootdir.</param>
        public PythonPlugin(string name, string code, DirectoryInfo rootdir) : base(name, rootdir)
        {
            Type = PluginType.Python;

            Load(code);
        }

        /// <summary>
        /// Format exceptions to give meaningful reports.
        /// </summary>
        /// <returns>String representation of the exception.</returns>
        /// <param name="ex">The exception object.</param>
        public override string FormatException(Exception ex)
        {
            return base.FormatException(ex) + Environment.NewLine +
                   Engine.GetService<ExceptionOperations>().FormatException(ex);
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
                object functionToCall = null;
                if (State == PluginState.Loaded && CachedGlobals.TryGetValue(func, out functionToCall))
                {
                    object result = null;

                    using (new Stopper($"{Type} {Name}", func))
                    {
                        result = Engine.Operations.Invoke(functionToCall, args);
                    }

                    return result;
                }
                return null;
            }
            catch (ArgumentTypeException ex) // Maintain compatibility for old plugins.
            {
                if (func == "On_EntityDeployed")
                {
                    return Invoke(func, new object[] {args[0], args[1]});
                }
                string fileinfo = $"[Error] Failed to invoke: {Name}<{Type}>.{func}(){Environment.NewLine}";
                Logger.LogError(fileinfo + FormatException(ex));
            }
            catch (Exception ex)
            {
                string fileinfo = $"[Error] Failed to invoke: {Name}<{Type}>.{func}(){Environment.NewLine}";
                Logger.LogError(fileinfo + FormatException(ex));
            }
            return null;
        }

        public override void Load(string code = "")
        {
            Engine = Python.CreateEngine();
            Engine.SetSearchPaths(new string[] {ManagedFolder, LibPath});
            Engine.GetBuiltinModule().RemoveVariable("exit");
            Engine.GetBuiltinModule().RemoveVariable("reload");
            Scope = Engine.CreateScope();
            Scope.SetVariable("Plugin", this);
            Scope.SetVariable("Server", Server.GetServer());
            Scope.SetVariable("DataStore", DataStore.GetInstance());
            Scope.SetVariable("Data", Data.GetData());
            Scope.SetVariable("Web", Web.GetInstance());
            Scope.SetVariable("Util", Util.GetUtil());
            Scope.SetVariable("World", World.GetWorld());
            #pragma warning disable 618
            Scope.SetVariable("PluginCollector", GlobalPluginCollector.GetPluginCollector());
            #pragma warning restore 618
            Scope.SetVariable("Loom", Loom.Current);
            Scope.SetVariable("JSON", JsonAPI.GetInstance);
            Scope.SetVariable("MySQL", MySQLConnector.GetInstance);
            Scope.SetVariable("SQLite", SQLiteConnector.GetInstance);
            Scope.SetVariable("PermissionSystem", PermissionSystem.GetPermissionSystem());
            Scope.SetVariable("PlayerCache", PlayerCache.GetPlayerCache());
            Scope.SetVariable("EntityCache", EntityCache.GetInstance());
            Scope.SetVariable("NPCCache", NPCCache.GetInstance());
            Scope.SetVariable("SleeperCache", SleeperCache.GetInstance());
            
            try
            {
                ScriptSource source = Engine.CreateScriptSourceFromString(code, Path.GetFileName(RootDir.FullName), SourceCodeKind.Statements);
                CompiledCode compiled = source.Compile();
                compiled.Execute(Scope);
                
                Class = Engine.Operations.Invoke(Scope.GetVariable(Name));
                Globals = Engine.Operations.GetMemberNames(Class);
                
                foreach (string name in Globals)
                {
                    object func;
                    if (!Engine.Operations.TryGetMember(Class, name, out func) || !Engine.Operations.IsCallable(func))
                    {
                        continue;
                    }

                    CachedGlobals.Add(name, func);
                }

                object author = GetGlobalObject("__author__");
                object about = GetGlobalObject("__about__");
                object version = GetGlobalObject("__version__");
                Author = author == null ? "" : author.ToString();
                About = about == null ? "" : about.ToString();
                Version = version == null ? "" : version.ToString();

                State = PluginState.Loaded;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Error] Failed to load Python plugin: {ex}");
                State = PluginState.FailedToLoad;
            }

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }

        public object GetGlobalObject(string identifier)
        {
            try
            {
                return Scope.GetVariable(identifier);
            }
            catch
            {
                return null;
            }
        }
    }
}