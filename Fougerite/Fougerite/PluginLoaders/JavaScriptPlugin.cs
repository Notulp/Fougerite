using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fougerite.Caches;
using Fougerite.Concurrent;
using Fougerite.Permissions;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;

namespace Fougerite.PluginLoaders
{
    public class JavaScriptPlugin : BasePlugin
    {
        public Engine Engine;
        public readonly ConcurrentDictionary<string, ICallable> CallableGlobals = new ConcurrentDictionary<string, ICallable>();

        /// <summary>
        /// Initializes a new instance of the <see cref="JavaScriptPlugin"/> class.
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
        /// <param name="args">Arguments.</param>
        /// <param name="func">Func.</param>
        public override object Invoke(string func, params object[] args)
        {
            try
            {
                ICallable callable;
                if (State == PluginState.Loaded && CallableGlobals.TryGetValue(func, out callable))
                {
                    using (new Stopper($"{Type} {Name}", func))
                    {
                        var jsArgs = args != null
                            ? args.Select(x => JsValue.FromObject(Engine, x)).ToArray()
                            : new JsValue[0];

                        return callable
                            .Call((JsValue)(ObjectInstance)Engine.Global, jsArgs)
                            .ToObject();
                    }
                }
            }
            catch (Exception ex)
            {
                string errorHeader = $"[Error] {Name}<{Type}>.{func}() failed:";
                string errorMessage;
                
                switch (ex)
                {
                    case Jint.Runtime.JavaScriptException jintEx:
                        errorMessage = $"Line: {jintEx.LineNumber} Col: {jintEx.Column} - {jintEx.Message}{Environment.NewLine}JS Stacktrace: {jintEx.StackTrace}";
                        break;
                    case Jint.Parser.ParserException parseEx:
                        errorMessage = $"Line: {parseEx.LineNumber} Col: {parseEx.Column} - Syntax Error: {parseEx.Description}";
                        break;
                    default:
                        errorMessage = FormatException(ex);
                        break;
                }

                HasErrors = true;
                LastError = errorMessage;
                Logger.LogError($"{errorHeader}{Environment.NewLine}{errorMessage}");
            }
            return null;
        }

        public override void Load(string code = "")
        {
            try
            {
                Engine = new Engine(cfg =>
                {
                    cfg.AllowClr(AppDomain.CurrentDomain.GetAssemblies().ToArray());
                    cfg.LimitRecursion(1000000);
                });

                // Some compatibility memes
                Engine.Execute(@"
                    Object.defineProperty(Array.prototype, 'Length', {
                        get: function () { return this.length; },
                        configurable: true
                    });

                    Object.defineProperty(String.prototype, 'Length', {
                        get: function () { return this.length; },
                        configurable: true
                    });

                    Object.defineProperty(Object.prototype, 'ToString', {
                        value: function () { return '' + this; },
                        writable: true,
                        configurable: true
                    });
                ");

                Engine.SetValue("Plugin", this);
                Engine.SetValue("Server", Server.GetServer());
                Engine.SetValue("DataStore", DataStore.GetInstance());
                Engine.SetValue("Data", Data.GetData());
                Engine.SetValue("Web", Web.GetInstance());
                Engine.SetValue("WinHttpClient", WinHttpClient.GetInstance());
                Engine.SetValue("Util", Util.GetUtil());
                Engine.SetValue("World", World.GetWorld());
#pragma warning disable 618
                Engine.SetValue("PluginCollector", GlobalPluginCollector.GetPluginCollector());
#pragma warning restore 618
                Engine.SetValue("Loom", Loom.Current);
                Engine.SetValue("JSONAPI", JsonAPI.GetInstance);
                Engine.SetValue("MySQL", MySQLConnector.GetInstance);
                Engine.SetValue("SQLite", SQLiteConnector.GetInstance);
                Engine.SetValue("PermissionSystem", PermissionSystem.GetPermissionSystem());
                Engine.SetValue("PlayerCache", PlayerCache.GetPlayerCache());
                Engine.SetValue("EntityCache", EntityCache.GetInstance());
                Engine.SetValue("SleeperCache", SleeperCache.GetInstance());
                Engine.SetValue("NPCCache", NPCCache.GetInstance());
                
                Engine.SetValue("importClass", new Func<string, JsValue>(importClass));
                
                Engine.Execute(code);

                // Get function names from global scope
                Globals = new ConcurrentList<string>();
                CallableGlobals.Clear();
                
                foreach (KeyValuePair<string, PropertyDescriptor> property in Engine.Global.GetOwnProperties())
                {
                    ICallable callable = property.Value.Value?.TryCast<ICallable>();
                    if (callable != null)
                    {
                        Globals.Add(property.Key);
                        CallableGlobals.Add(property.Key, callable);
                    }
                }

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
                Logger.LogError($"[Error] Failed to load javascript plugin: {ex}");
                State = PluginState.FailedToLoad;
                PluginLoader.GetInstance().CurrentlyLoadingPlugins.Remove(Name);
            }

            PluginLoader.GetInstance().OnPluginLoaded(this);
        }

        public object GetGlobalObject(string identifier)
        {
            try
            {
                return Engine.GetValue(identifier).ToObject();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public JsValue importClass(string type)
        {
            var className = type.Split('.').Last();
            var resolvedType = Util.GetUtil().TryFindReturnType(type);
            var typeRef = Jint.Runtime.Interop.TypeReference.CreateTypeReference(Engine, resolvedType);
            Engine.SetValue(className, typeRef);
            return typeRef;
        }
    }
}