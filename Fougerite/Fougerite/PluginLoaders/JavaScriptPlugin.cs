using System;
using System.IO;
using System.Linq;
using Fougerite.Permissions;
using Jint;
using Jint.Expressions;

namespace Fougerite.PluginLoaders
{
    public class JavaScriptPlugin : BasePlugin
    {
        public JintEngine Engine;
        public Jint.Expressions.Program Program;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pluton.JSPlugin"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="code">Code.</param>
        /// <param name="rootdir">Rootdir.</param>
        public JavaScriptPlugin(string name, string code, DirectoryInfo rootdir) : base(name, rootdir)
        {
            Type = PluginType.JavaScript;

            Load(code);
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
                    object result = (object) null;

                    using (new Stopper(Type + " " + Name, func))
                    {
                        result = Engine.CallFunction(func, args);
                    }

                    return result;
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
            try
            {
                Engine = new JintEngine(Options.Ecmascript5)
                    .AllowClr(true);

                Engine.SetParameter("Plugin", this)
                    .SetParameter("Server", Fougerite.Server.GetServer())
                    .SetParameter("DataStore", DataStore.GetInstance())
                    .SetParameter("Data", Data.GetData())
                    .SetParameter("Web", Web.GetInstance())
                    .SetParameter("Util", Util.GetUtil())
                    .SetParameter("World", World.GetWorld())
                    #pragma warning disable 618
                    .SetParameter("PluginCollector", GlobalPluginCollector.GetPluginCollector())
                    #pragma warning restore 618
                    .SetParameter("Loom", Fougerite.Loom.Current)
                    .SetParameter("JSON", Fougerite.JsonAPI.GetInstance)
                    .SetParameter("MySQL", Fougerite.MySQLConnector.GetInstance)
                    .SetParameter("SQLite", Fougerite.SQLiteConnector.GetInstance)
                    .SetParameter("PermissionSystem", PermissionSystem.GetPermissionSystem())
                    .SetFunction("importClass", new importit(importClass));
                Program = JintEngine.Compile(code, false);

                Globals = (from statement in Program.Statements
                    where statement.GetType() == typeof(FunctionDeclarationStatement)
                    select ((FunctionDeclarationStatement) statement).Name).ToList<string>();

                Engine.Run(Program);

                object author = GetGlobalObject("Author");
                object about = GetGlobalObject("About");
                object version = GetGlobalObject("Version");
                Author = author == null || (string) author == "undefined" ? "Unknown" : author.ToString();
                About = about == null || (string) about == "undefined" ? "" : about.ToString();
                Version = version == null || (string) version == "undefined" ? "1.0" : version.ToString();

                State = PluginState.Loaded;
            }
            catch (Exception ex)
            {
                Logger.LogError("[Error] Failed to load lua plugin: " + ex);
                State = PluginState.FailedToLoad;
                PluginLoader.GetInstance().CurrentlyLoadingPlugins.Remove(Name);
            }

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }

        public object GetGlobalObject(string identifier)
        {
            return Engine.Run(string.Format("return {0};", identifier));
        }

        public delegate Jint.Native.JsInstance importit(string t);

        public Jint.Native.JsInstance importClass(string type)
        {
            Engine.SetParameter(type.Split('.').Last(), Util.GetUtil().TryFindReturnType(type));
            Jint.Native.JsDictionaryObject jsobj = (Engine.Global as Jint.Native.JsDictionaryObject);
            
            return jsobj != null ? jsobj[type.Split('.').Last()] : null;
        }
    }
}