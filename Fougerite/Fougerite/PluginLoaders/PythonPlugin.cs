using System;
using System.Collections.Generic;
using System.IO;
using Fougerite.Caches;
using Fougerite.Permissions;
using IronPython.Hosting;
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
        /// <param name="method">Method.</param>
        /// <param name="args">Arguments.</param>
        /// <param name="func">Func.</param>
        public override object Invoke(string func, params object[] args)
        {
            try
            {
                if (State == PluginState.Loaded && Globals.Contains(func))
                {
                    object result = null;

                    using (new Stopper(Type + " " + Name, func))
                    {
                        result = Engine.Operations.InvokeMember(Class, func, args);
                    }

                    return result;
                }
                return null;
            }
            catch (Microsoft.Scripting.ArgumentTypeException) // Maintain compatibility for old plugins.
            {
                if (func == "On_EntityDeployed")
                {
                    return this.Invoke(func, new object[] {args[0], args[1]});
                }
            }
            catch (Exception ex)
            {
                string fileinfo = ("[Error] Failed to invoke: " + string.Format("{0}<{1}>.{2}()", Name, Type, func) + Environment.NewLine);
                Logger.LogError(fileinfo + FormatException(ex));
            }
            return null;
        }

        public override void Load(string code = "")
        {
            Engine = IronPython.Hosting.Python.CreateEngine();
            Engine.SetSearchPaths(new string[] {ManagedFolder, LibPath});
            Engine.GetBuiltinModule().RemoveVariable("exit");
            Engine.GetBuiltinModule().RemoveVariable("reload");
            Scope = Engine.CreateScope();
            Scope.SetVariable("Plugin", this);
            Scope.SetVariable("Server", Fougerite.Server.GetServer());
            Scope.SetVariable("DataStore", DataStore.GetInstance());
            Scope.SetVariable("Data", Data.GetData());
            Scope.SetVariable("Web", Web.GetInstance());
            Scope.SetVariable("Util", Util.GetUtil());
            Scope.SetVariable("World", World.GetWorld());
            #pragma warning disable 618
            Scope.SetVariable("PluginCollector", GlobalPluginCollector.GetPluginCollector());
            #pragma warning restore 618
            Scope.SetVariable("Loom", Fougerite.Loom.Current);
            Scope.SetVariable("JSON", Fougerite.JsonAPI.GetInstance);
            Scope.SetVariable("MySQL", Fougerite.MySQLConnector.GetInstance);
            Scope.SetVariable("SQLite", Fougerite.SQLiteConnector.GetInstance);
            Scope.SetVariable("PermissionSystem", PermissionSystem.GetPermissionSystem());
            Scope.SetVariable("PlayerCache", PlayerCache.GetPlayerCache());

            try
            {
                Engine.Execute(code, Scope);
                Class = Engine.Operations.Invoke(Scope.GetVariable(Name));
                Globals = Engine.Operations.GetMemberNames(Class);

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
                Logger.LogError("[Error] Failed to load Python plugin: " + ex);
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