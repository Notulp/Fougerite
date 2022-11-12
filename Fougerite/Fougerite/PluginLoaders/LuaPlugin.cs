using System;
using System.IO;
using Fougerite.Caches;
using Fougerite.Permissions;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Interop;

namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// Lua plugin.
    /// </summary>
    public class LUAPlugin : BasePlugin
    {

        /// <summary>
        /// LUA Tables
        /// </summary>
        public Table Tables;
        public Script script;

        /// <summary>
        /// Initializes a new instance of the <see cref="LUAPlugin"/> class.
        /// </summary>
        /// <param name="name">Name.</param>
        /// <param name="code">Code.</param>
        /// <param name="rootdir">Rootdir.</param>
        public LUAPlugin(string name, string code, DirectoryInfo rootdir)
            : base(name, rootdir)
        {
            Type = PluginType.Lua;

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
                    object result = (object)null;

                    using (new Stopper($"{Type} {Name}", func))
                    {
                        result = script.Call(script.Globals[func], args);
                    }
                    return result;
                }
            }
            catch (Exception ex)
            {
                string fileinfo = ("[Error] Failed to invoke: " + $"{Name}<{Type}>.{func}()" + Environment.NewLine);
                Logger.LogError(fileinfo + FormatException(ex));
            }
            return null;
        }

        public override string FormatException(Exception ex)
        {
            return base.FormatException(ex) +
                (ex is ScriptRuntimeException ? Environment.NewLine + (ex as ScriptRuntimeException).DecoratedMessage : "");
        }

        public override void Load(string code = "")
        {
            try
            {
                UserData.RegistrationPolicy = InteropRegistrationPolicy.Automatic;
                script = new Script();
                script.DoString(code);
                script.Globals.Set("Util", UserData.Create(Util.GetUtil()));
                script.Globals.Set("Plugin", UserData.Create(this));
                script.Globals.Set("Server", UserData.Create(Server.GetServer()));
                script.Globals.Set("DataStore", UserData.Create(DataStore.GetInstance()));
                script.Globals.Set("Data", UserData.Create(Data.GetData()));
                script.Globals.Set("Web", UserData.Create(Web.GetInstance()));
                script.Globals.Set("World", UserData.Create(World.GetWorld()));
                #pragma warning disable 618
                script.Globals.Set("PluginCollector", UserData.Create(GlobalPluginCollector.GetPluginCollector()));
                #pragma warning restore 618
                script.Globals.Set("Loom", UserData.Create(Loom.Current));
                script.Globals.Set("JSON", UserData.Create(JsonAPI.GetInstance));
                script.Globals.Set("MySQL", UserData.Create(MySQLConnector.GetInstance));
                script.Globals.Set("SQLite", UserData.Create(SQLiteConnector.GetInstance));
                script.Globals.Set("PermissionSystem", UserData.Create(PermissionSystem.GetPermissionSystem()));
                script.Globals.Set("PlayerCache", UserData.Create(PlayerCache.GetPlayerCache()));
                foreach (DynValue v in script.Globals.Keys)
                {
                    Globals.Add(v.ToString().Replace("\"", ""));
                }
                Author = string.IsNullOrEmpty(script.Globals.Get("Author").String) ? "Unknown" : script.Globals.Get("Author").String;
                Version = string.IsNullOrEmpty(script.Globals.Get("Version").String) ? "1.0" : script.Globals.Get("Version").String;
                About = script.Globals.Get("About").String;

                State = PluginState.Loaded;
                Tables = script.Globals;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[Error] Failed to load Lua plugin: {ex}");
                State = PluginState.FailedToLoad;
            }

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }
    }
}