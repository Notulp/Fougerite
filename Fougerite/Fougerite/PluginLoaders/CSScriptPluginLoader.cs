using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Fougerite.PluginLoaders
{
    /// <summary>
    /// Loads C# script plugins from Modules\Name\Name.cs. Every .cs file in the plugin folder is compiled into one
    /// assembly that exposes a <see cref="Module"/> subclass, the same way DLL modules work.
    /// Use // #require OtherPlugin in a source file to reference another C# script plugin or DLL module.
    /// </summary>
    public class CSScriptPluginLoader : Singleton<CSScriptPluginLoader>, ISingleton, IPluginLoader
    {
        public PluginType Type = PluginType.CSScript;
        public const string Extension = ".cs";

        /// <summary>
        /// Represents the directory where C# script plugins are stored.
        /// By default, it is located at the "Modules\" folder under the root directory
        /// of the application.
        /// </summary>
        public readonly DirectoryInfo PluginDirectory = new DirectoryInfo(Path.Combine(Util.GetRootFolder(), "Modules\\"));

        /// <summary>
        /// Represents the directory used to cache compiled plugin assemblies for the CSScript plugin loader.
        /// </summary>
        public readonly DirectoryInfo CacheDirectory = new DirectoryInfo(Path.Combine(Util.GetRootFolder(), "Save\\.CSScriptCache"));

        /// <summary>
        /// Represents the path to the managed folder, typically used to locate the
        /// "Managed" directory within the root folder of the Rust server. This directory
        /// commonly contains assemblies and other managed resources required for the server's execution.
        /// 
        public readonly string ManagedFolder = Path.Combine(Util.GetRootFolder(), Path.Combine("rust_server_Data", "Managed"));

        /// <summary>
        /// Name of the folder used to store reference assemblies for plugin scripts.
        /// </summary>
        private const string ReferencesFolderName = "References";

        /// <summary>
        /// Represents the name of the directory used for temporary storage during the plugin compilation process.
        /// This folder is utilized to stage intermediate files created during the build and compilation of plugins.
        /// </summary>
        private const string StagingFolderName = ".staging";

        /// <summary>
        /// Represents the format version of the build utilized in hashing and versioning processes.
        /// Used internally to ensure compatibility and consistency between compiled plugins and the loader.
        /// </summary>
        private const string BuildFormatVersion = "1";

        /// <summary>
        /// The timeout in milliseconds for the compilation process of C# script plugins.
        /// If the compilation process exceeds this duration, it will be terminated.
        /// </summary>
        private const int CompilerTimeoutMs = 120000;

        /// <summary>
        /// Specifies the timeout in milliseconds for external tool executions, such as processes executed
        /// during plugin loading or compilation tasks.
        /// </summary
        private const int ToolTimeoutMs = 15000;

        /// <summary>
        /// Specifies the maximum time, in milliseconds, to wait for the output or error stream threads
        /// to complete when processing an external process.
        /// This timeout ensures that the application does not hang indefinitely
        /// if the stream operations do not finish promptly.
        /// </summary
        private const int StreamJoinTimeoutMs = 10000;

        /// <summary>
        /// Represents an array of folder names to be ignored during plugin loading or processing.
        /// These folder names are excluded to prevent unnecessary compilation or interaction,
        /// typically because they contain irrelevant or intermediate build files.
        /// </summary>
        private static readonly string[] IgnoredFolderNames = { "bin", "obj", ReferencesFolderName };

        /// <summary>
        /// Represents a compiled regular expression used to identify "Order" property overrides in
        /// C# plugin code. This is typically utilized for parsing and extracting order values from
        /// method properties within the loaded scripts.
        /// </summary>
        private static readonly Regex OrderProperty = new Regex(@"\boverride\s+(?:(?:System\s*\.\s*)?UInt32|uint)\s+Order\b", RegexOptions.Compiled);

        /// <summary>
        /// A compiled regular expression used to identify and extract numeric order casting expressions
        /// with types such as UInt32, uint, or ulong enclosed within parentheses in source code.
        /// </summary>
        private static readonly Regex OrderCast = new Regex(@"\(\s*(?:(?:System\s*\.\s*)?U?Int32|u?int|u?long)\s*\)", RegexOptions.Compiled);

        // Same as Module.Order when a plugin does not override it.
        /// <summary>
        /// The default execution order assigned to a plugin when it does not explicitly declare a custom order.
        /// </summary>
        private const uint DefaultOrder = uint.MaxValue;

        /// <summary>
        /// Represents a regular expression used to match and extract directives in the format of
        /// "// #require PluginName" from C# script files. These directives allow script files to
        /// declare dependencies on other plugins or DLL modules.
        /// </summary>
        private static readonly Regex RequireDirective = new Regex(@"^\s*//\s*#require\s+""?(?<name>[^\s""]+)""?\s*$", RegexOptions.Compiled);

        /// <summary>
        /// Indicates whether the startup process for loading plugins has been completed.
        /// When set to true, ensures that startup plugins have been loaded and
        /// disables subsequent calls to startup-loading logic.
        /// </summary>
        private static volatile bool _startupCompleted;

        /// <summary>
        /// Serves as a synchronization lock to ensure thread safety when accessing or modifying
        /// assemblies within the CSScriptPluginLoader. This is primarily used to protect operations
        /// involving the registration, unregistration, and resolution of script assemblies.
        /// </summary>
        private readonly object _assemblyLock = new object();

        /// <summary>
        /// A dictionary that maps the names of script assemblies to their corresponding <see cref="System.Reflection.Assembly"/> instances.
        /// Used to manage and track the loaded C# script assemblies for the plugin system.
        /// The keys in this dictionary are case-insensitive.
        /// </summary>
        private readonly Dictionary<string, Assembly> _scriptAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// A dictionary that stores references to loaded assemblies, indexed by their simple names.
        /// It is used to resolve assembly dependencies and facilitate the loading of script assemblies or other
        /// assemblies required by plugins during runtime.
        /// </summary
        private readonly Dictionary<string, Assembly> _referenceAssemblies = new Dictionary<string, Assembly>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Stores a list of framework assembly references used during compilation or execution of plugins.
        /// This collection is populated by analyzing the assemblies present in the managed folder and helps ensure
        /// all necessary dependencies for script plugins are available.
        /// </
        private List<string> _frameworkReferences;

        /// <summary>
        /// Indicates whether the managed core library ("mscorlib.dll") is present in the Managed folder.
        /// </summary>
        private bool _hasManagedCorlib;

        /// <summary>
        /// Stores information about the current compiler being used for C# script plugin compilation.
        /// This field is initialized by detecting the compiler through a specified logic
        /// in the plugin loader and is used for managing script compilation processes.
        /// </summary>
        private CompilerInfo _compiler;
        public const string DomainOwner = "CSScript";

        public CSScriptPluginLoader()
        {
        }

        public static bool IsEngineEnabled
        {
            get { return Config.GetBoolValue("Engines", "EnableCSScript"); }
        }

        /// <summary>
        /// True once the startup batch was loaded. Plugins loaded later do not receive OnModulesLoaded.
        /// </summary>
        public bool StartupCompleted
        {
            get { return _startupCompleted; }
        }

        public string GetExtension()
        {
            return Extension;
        }

        public string GetSource(string pluginname)
        {
            return GetMainFilePath(pluginname);
        }

        public string GetMainFilePath(string pluginname)
        {
            return Path.Combine(GetPluginDirectoryPath(pluginname), pluginname + Extension);
        }

        public string GetPluginDirectoryPath(string name)
        {
            return Path.Combine(PluginDirectory.FullName, name);
        }

        public List<string> GetPluginNames()
        {
            List<string> names = new List<string>();
            if (!Directory.Exists(PluginDirectory.FullName))
            {
                return names;
            }

            foreach (DirectoryInfo dirInfo in PluginDirectory.GetDirectories())
            {
                if (dirInfo.Name.StartsWith(".") || IsModuleFolder(dirInfo.FullName, dirInfo.Name))
                    continue;

                if (File.Exists(Path.Combine(dirInfo.FullName, dirInfo.Name + Extension)))
                {
                    names.Add(dirInfo.Name);
                }
            }

            return names;
        }

        public void LoadPlugin(string name)
        {
            LoadPlugin(name, true);
        }

        public void LoadPlugin(string name, bool init)
        {
            if (PluginLoader.GetInstance().Plugins.ContainsKey(name))
            {
                Logger.LogError($"[CSScriptPluginLoader] {name} plugin is already loaded.");
                throw new InvalidOperationException($"[CSScriptPluginLoader] {name} plugin is already loaded.");
            }

            if (!NativeDomainManager.EnsureCreated(DomainOwner))
            {
                Logger.LogError($"[CSScriptPluginLoader] {name} plugin could not be loaded, the unmanaged Fougerite Mono domain is not available.");
                return;
            }

            LoadSet(new List<string> { name }, init);
        }

        public void LoadPlugins()
        {
            if (!IsEngineEnabled)
            {
                Logger.LogDebug("[CSScriptPluginLoader] C# script plugins are disabled in Fougerite.cfg.");
                return;
            }

            if (!NativeDomainManager.EnsureCreated(DomainOwner))
            {
                Logger.LogError("[CSScriptPluginLoader] Can't load C# script plugins, the unmanaged Fougerite Mono domain is not available.");
                return;
            }

            if (Directory.Exists(PluginDirectory.FullName))
            {
                foreach (DirectoryInfo dirInfo in PluginDirectory.GetDirectories())
                {
                    if (IsModuleFolder(dirInfo.FullName, dirInfo.Name) && File.Exists(Path.Combine(dirInfo.FullName, dirInfo.Name + Extension)))
                    {
                        Logger.LogWarning($"[CSScriptPluginLoader] {dirInfo.Name} has both {dirInfo.Name}.dll and {dirInfo.Name}{Extension}, it is loaded as a DLL module.");
                    }
                }
            }

            LoadSet(GetPluginNames(), true);
        }

        public void ReloadPlugin(string name)
        {
            BasePlugin plugin;
            if (!PluginLoader.GetInstance().Plugins.TryGetValue(name, out plugin) || plugin.DontReload)
                return;

            UnloadPlugin(name);
            LoadPlugin(name, true);
        }

        public void ReloadPlugins()
        {
            foreach (CSScriptPlugin plugin in GetLoadedPlugins())
            {
                if (!plugin.DontReload && PluginLoader.GetInstance().Plugins.ContainsKey(plugin.Name))
                {
                    UnloadPlugin(plugin.Name);
                }
            }

            LoadPlugins();
        }

        public void UnloadPlugin(string name)
        {
            Logger.LogDebug($"[CSScriptPluginLoader] Unloading {name} plugin.");

            BasePlugin basePlugin;
            if (!PluginLoader.GetInstance().Plugins.TryGetValue(name, out basePlugin))
            {
                Logger.LogError($"[CSScriptPluginLoader] Can't unload {name}. Plugin is not loaded.");
                throw new InvalidOperationException($"[CSScriptPluginLoader] Can't unload {name}. Plugin is not loaded.");
            }

            if (basePlugin.DontReload)
                return;

            CSScriptPlugin plugin = basePlugin as CSScriptPlugin;
            if (plugin == null)
            {
                Logger.LogError($"[CSScriptPluginLoader] Can't unload {name}. It is a {basePlugin.Type} plugin.");
                throw new InvalidOperationException($"[CSScriptPluginLoader] Can't unload {name}. It is a {basePlugin.Type} plugin.");
            }

            plugin.DeInitializeEngine();
            PluginLoader.GetInstance().RemoveHooks(plugin);
            plugin.ReleaseResources();
            PluginLoader.GetInstance().Plugins.Remove(name);
            PluginLoader.GetInstance().OnPluginUnloaded(plugin);

            Logger.LogDebug($"[CSScriptPluginLoader] {name} plugin was unloaded successfully.");
        }

        public void UnloadPlugins()
        {
            foreach (CSScriptPlugin plugin in GetLoadedPlugins())
            {
                if (PluginLoader.GetInstance().Plugins.ContainsKey(plugin.Name))
                {
                    UnloadPlugin(plugin.Name);
                }
            }

            NativeDomainManager.ReleaseOwner(DomainOwner);
        }

        public void Initialize()
        {
            if (!PluginDirectory.Exists)
            {
                PluginDirectory.Create();
            }

            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            PluginWatcher.GetInstance().AddWatcher(Type, Extension, PluginDirectory.FullName);
            PluginLoader.GetInstance().PluginLoaders.Add(Type, this);

            if (GetCompiler() == null)
            {
                Logger.LogWarning("[CSScriptPluginLoader] No C# compiler was found, C# script plugins can't be compiled. Install Visual Studio Build Tools, the .NET Framework or Mono, or set CSScriptCompiler in the Engines section of Fougerite.cfg.");
            }

            // With DLL modules enabled, CSharpPluginLoader triggers the startup load before OnModulesLoaded.
            if (!Config.GetBoolValue("Engines", "EnableCSharp"))
            {
                LoadStartupPlugins();
                Hooks.ModulesLoaded();
            }
        }

        public bool CheckDependencies()
        {
            return IsEngineEnabled;
        }

        /// <summary>
        /// Called by the <see cref="CSharpPluginLoader"/> right before OnModulesLoaded, so scripts load after the DLL modules.
        /// </summary>
        internal static void NotifyCSharpModulesLoaded()
        {
            if (!IsEngineEnabled)
                return;

            GetInstance().LoadStartupPlugins();
        }

        public bool TryGetPluginNameFromPath(string fullPath, out string pluginName)
        {
            pluginName = null;
            if (string.IsNullOrEmpty(fullPath) || !string.Equals(Path.GetExtension(fullPath), Extension, StringComparison.OrdinalIgnoreCase))
                return false;

            string root = PluginDirectory.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return false;

            string[] segments = fullPath.Substring(root.Length).Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length < 2 || segments[0].StartsWith("."))
                return false;

            if (IsModuleFolder(GetPluginDirectoryPath(segments[0]), segments[0]))
                return false;

            for (int i = 1; i < segments.Length - 1; i++)
            {
                if (IsIgnoredFolder(segments[i]))
                    return false;
            }

            pluginName = segments[0];
            return true;
        }

        internal void RegisterScriptAssembly(Assembly assembly)
        {
            lock (_assemblyLock)
            {
                _scriptAssemblies[assembly.GetName().Name] = assembly;
            }
        }

        internal void UnregisterScriptAssembly(Assembly assembly)
        {
            lock (_assemblyLock)
            {
                string name = assembly.GetName().Name;
                Assembly current;
                if (_scriptAssemblies.TryGetValue(name, out current) && current == assembly)
                {
                    _scriptAssemblies.Remove(name);
                }
            }
        }

        private void LoadStartupPlugins()
        {
            if (_startupCompleted)
                return;

            try
            {
                LoadPlugins();
            }
            finally
            {
                _startupCompleted = true;
            }
        }

        private void LoadSet(IEnumerable<string> requestedNames, bool init)
        {
            Dictionary<string, BasePlugin> plugins = PluginLoader.GetInstance().Plugins;
            NameIndex index = CreateNameIndex();
            Dictionary<string, ScriptManifest> manifests = new Dictionary<string, ScriptManifest>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> failed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Queue<string> pending = new Queue<string>(requestedNames);

            while (pending.Count > 0)
            {
                string requested = pending.Dequeue();
                string name;
                if (!index.Scripts.TryGetValue(requested, out name))
                {
                    if (failed.Add(requested))
                    {
                        Logger.LogError($"[CSScriptPluginLoader] {requested} plugin was not found. Expected {GetMainFilePath(requested)}.");
                    }
                    continue;
                }

                if (manifests.ContainsKey(name) || failed.Contains(name))
                    continue;

                BasePlugin existing;
                if (plugins.TryGetValue(name, out existing))
                {
                    if (existing.Type != Type)
                    {
                        Logger.LogError($"[CSScriptPluginLoader] {name} can't be loaded, a {existing.Type} plugin with the same name is already loaded.");
                        failed.Add(name);
                    }
                    continue;
                }

                if (Bootstrap.IgnoredPlugins.Contains(name.ToLower()))
                {
                    Logger.LogDebug($"[CSScriptPluginLoader] Ignoring plugin {name}.");
                    failed.Add(name);
                    continue;
                }

                if (PluginLoader.GetInstance().CurrentlyLoadingPlugins.Contains(name))
                {
                    Logger.LogWarning($"{name} plugin is already being loaded. Returning.");
                    continue;
                }

                ScriptManifest manifest = ReadManifest(name, index);
                if (manifest == null)
                {
                    failed.Add(name);
                    continue;
                }

                manifests.Add(name, manifest);
                foreach (string dependency in manifest.ScriptDependencies)
                {
                    pending.Enqueue(dependency);
                }
            }

            List<ScriptManifest> ordered = OrderByPriority(SortByDependencies(manifests, failed),
                manifest => manifest.Name,
                manifest => manifest.DeclaredOrder ?? DefaultOrder,
                manifest => manifest.ScriptDependencies,
                manifest => manifest.SoftDependencies);

            if (ordered.Count > 1)
            {
                Logger.Log($"[CSScriptPluginLoader] Compile order: {string.Join(", ", ordered.Select(DescribeOrder).ToArray())}");
            }

            List<CSScriptPlugin> loaded = new List<CSScriptPlugin>();
            foreach (ScriptManifest manifest in ordered)
            {
                string missing = manifest.ScriptDependencies.Concat(manifest.ModuleDependencies)
                    .FirstOrDefault(dependency => !plugins.ContainsKey(dependency));

                if (missing != null)
                {
                    Logger.LogError($"[CSScriptPluginLoader] {manifest.Name} plugin requires {missing}, which is not loaded.");
                    failed.Add(manifest.Name);
                    continue;
                }

                CSScriptPlugin plugin = LoadSingle(manifest);
                if (plugin == null)
                {
                    failed.Add(manifest.Name);
                    continue;
                }

                loaded.Add(plugin);
            }

            if (init)
            {
                InitializeInOrder(loaded, manifests);
            }
        }

        private CSScriptPlugin LoadSingle(ScriptManifest manifest)
        {
            Logger.LogDebug($"[CSScriptPluginLoader] Loading plugin {manifest.Name}.");

            List<string> currentlyLoading = PluginLoader.GetInstance().CurrentlyLoadingPlugins;
            currentlyLoading.Add(manifest.Name);

            try
            {
                string assemblyPath = Compile(manifest);
                if (assemblyPath == null)
                {
                    currentlyLoading.Remove(manifest.Name);
                    return null;
                }

                List<string> dependencies = manifest.ScriptDependencies.Concat(manifest.ModuleDependencies).ToList();
                CSScriptPlugin plugin = new CSScriptPlugin(manifest.Name, assemblyPath, manifest.Root, dependencies);
                return plugin.State == PluginState.Loaded ? plugin : null;
            }
            catch (Exception ex)
            {
                Logger.Log($"[CSScriptPluginLoader] {manifest.Name} plugin could not be loaded.");
                Logger.LogException(ex);
                currentlyLoading.Remove(manifest.Name);
                return null;
            }
        }

        private static void InitializeInOrder(List<CSScriptPlugin> loaded, Dictionary<string, ScriptManifest> manifests)
        {
            // The compiled module is the authority now, its Order wins over what the source parser saw.
            List<CSScriptPlugin> ordered = OrderByPriority(loaded,
                plugin => plugin.Name,
                plugin => plugin.Engine.Order,
                plugin => plugin.Dependencies,
                plugin => manifests[plugin.Name].SoftDependencies);

            foreach (CSScriptPlugin plugin in ordered)
            {
                plugin.InitializeEngine();
            }
        }

        /// <summary>
        /// Orders plugins by Order, then name. A plugin is never placed before what it #requires, and a required
        /// plugin is pulled forward to just before the plugins needing it. Between plugins with the same Order,
        /// a plugin that mentions another plugin's name in a string, like GetPlugin("Other"), goes after it.
        /// </summary>
        private static List<T> OrderByPriority<T>(List<T> items, Func<T, string> nameOf, Func<T, uint> orderOf,
            Func<T, IEnumerable<string>> requiresOf, Func<T, IEnumerable<string>> mentionsOf)
        {
            Dictionary<string, uint> effective = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
            foreach (T item in items)
            {
                effective[nameOf(item)] = orderOf(item);
            }

            bool changed = true;
            for (int pass = 0; changed && pass <= items.Count; pass++)
            {
                changed = false;
                foreach (T item in items)
                {
                    uint own = effective[nameOf(item)];
                    foreach (string requirement in requiresOf(item))
                    {
                        uint current;
                        if (effective.TryGetValue(requirement, out current) && own < current)
                        {
                            effective[requirement] = own;
                            changed = true;
                        }
                    }
                }
            }

            HashSet<string> placed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Func<string, bool> satisfied = dependency => !effective.ContainsKey(dependency) || placed.Contains(dependency);
            List<T> remaining = items.OrderBy(nameOf, StringComparer.OrdinalIgnoreCase).ToList();
            List<T> result = new List<T>(items.Count);

            while (remaining.Count > 0)
            {
                List<T> ready = remaining.Where(item => requiresOf(item).All(satisfied)).ToList();
                if (ready.Count == 0)
                {
                    ready = remaining;
                }

                uint lowest = ready.Min(item => effective[nameOf(item)]);
                List<T> candidates = ready.Where(item => effective[nameOf(item)] == lowest).ToList();
                int index = candidates.FindIndex(item => mentionsOf(item).All(satisfied));
                T next = candidates[index < 0 ? 0 : index];

                remaining.Remove(next);
                placed.Add(nameOf(next));
                result.Add(next);
            }

            return result;
        }

        private static string DescribeOrder(ScriptManifest manifest)
        {
            return manifest.DeclaredOrder.HasValue && manifest.DeclaredOrder.Value != DefaultOrder
                ? $"{manifest.Name} ({manifest.DeclaredOrder.Value})"
                : manifest.Name;
        }

        private static List<CSScriptPlugin> GetLoadedPlugins()
        {
            return PluginLoader.GetInstance().Plugins.Values.OfType<CSScriptPlugin>().ToList();
        }

        private ScriptManifest ReadManifest(string name, NameIndex index)
        {
            try
            {
                ScriptManifest manifest = new ScriptManifest(name, new DirectoryInfo(GetPluginDirectoryPath(name)));
                CollectSourceFiles(manifest.Root, manifest.SourceFiles);
                manifest.SourceFiles.Sort(StringComparer.OrdinalIgnoreCase);

                // The main file is checked first, so its Order wins if several files declare one.
                string mainFile = GetMainFilePath(name);
                List<string> literals = new List<string>();
                foreach (string file in manifest.SourceFiles.OrderBy(file => string.Equals(file, mainFile, StringComparison.OrdinalIgnoreCase) ? 0 : 1))
                {
                    string text = File.ReadAllText(file);
                    string code = StripCommentsAndStrings(text, literals);
                    if (!manifest.DeclaredOrder.HasValue)
                    {
                        manifest.DeclaredOrder = ParseOrder(code);
                    }

                    foreach (string line in text.Split('\n'))
                    {
                        Match match = RequireDirective.Match(line);
                        if (!match.Success)
                            continue;

                        string requirement = match.Groups["name"].Value;
                        string resolved;
                        if (index.Scripts.TryGetValue(requirement, out resolved))
                        {
                            if (string.Equals(resolved, name, StringComparison.OrdinalIgnoreCase))
                            {
                                Logger.LogWarning($"[CSScriptPluginLoader] {name} requires itself in {Path.GetFileName(file)}, ignoring it.");
                            }
                            else if (!manifest.ScriptDependencies.Contains(resolved))
                            {
                                manifest.ScriptDependencies.Add(resolved);
                            }
                        }
                        else if (index.Modules.TryGetValue(requirement, out resolved))
                        {
                            if (!manifest.ModuleDependencies.Contains(resolved))
                            {
                                manifest.ModuleDependencies.Add(resolved);
                            }
                        }
                        else
                        {
                            Logger.LogError($"[CSScriptPluginLoader] {name} requires {requirement} in {Path.GetFileName(file)}, but there is no C# script plugin or C# module with that name.");
                            return null;
                        }
                    }
                }

                foreach (string literal in literals)
                {
                    string mentioned;
                    if (index.Scripts.TryGetValue(literal, out mentioned) &&
                        !string.Equals(mentioned, name, StringComparison.OrdinalIgnoreCase) &&
                        !manifest.ScriptDependencies.Contains(mentioned) &&
                        !manifest.SoftDependencies.Contains(mentioned))
                    {
                        manifest.SoftDependencies.Add(mentioned);
                    }
                }

                return manifest;
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CSScriptPluginLoader] Failed to read the sources of {name}. {ex}");
                return null;
            }
        }

        /// <summary>
        /// Reads the value of an overridden Order property from source that may not even compile.
        /// Returns null when there is no override or its value is not a plain constant.
        /// </summary>
        private static uint? ParseOrder(string code)
        {
            Match match = OrderProperty.Match(code);
            if (!match.Success)
                return null;

            string body = code.Substring(match.Index + match.Length, Math.Min(400, code.Length - match.Index - match.Length));
            int arrow = body.IndexOf("=>", StringComparison.Ordinal);
            Match returnMatch = Regex.Match(body, @"\breturn\b");
            int keyword = returnMatch.Success ? returnMatch.Index : -1;

            int start = arrow < 0 ? keyword : keyword < 0 ? arrow : Math.Min(arrow, keyword);
            if (start < 0)
                return null;

            start += start == arrow ? 2 : "return".Length;
            int end = body.IndexOfAny(new[] { ';', '}' }, start);
            if (end < 0)
                return null;

            string expression = OrderCast.Replace(body.Substring(start, end - start), string.Empty);
            expression = new string(expression.Where(c => !char.IsWhiteSpace(c) && c != '(' && c != ')' && c != '_').ToArray());

            if (Regex.IsMatch(expression, @"^(?:(?:System\.)?UInt32|uint)\.MaxValue$"))
                return uint.MaxValue;

            if (Regex.IsMatch(expression, @"^(?:(?:System\.)?UInt32|uint)\.MinValue$"))
                return uint.MinValue;

            expression = expression.TrimEnd('u', 'U', 'l', 'L');
            uint value;
            if (expression.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return uint.TryParse(expression.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value) ? value : (uint?) null;
            }

            return uint.TryParse(expression, NumberStyles.None, CultureInfo.InvariantCulture, out value) ? value : (uint?) null;
        }

        /// <summary>
        /// Blanks out comments and string literals, so commented out code is ignored, and collects the content of
        /// plain and verbatim string literals. Never throws on broken source.
        /// </summary>
        private static string StripCommentsAndStrings(string source, List<string> literals)
        {
            StringBuilder code = new StringBuilder(source.Length);
            int length = source.Length;
            int i = 0;

            while (i < length)
            {
                char c = source[i];
                char next = i + 1 < length ? source[i + 1] : '\0';

                if (c == '/' && next == '/')
                {
                    while (i < length && source[i] != '\n')
                        i++;
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    int close = source.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = close < 0 ? length : close + 2;
                    code.Append(' ');
                    continue;
                }

                int prefix = StringPrefixLength(source, i);
                if (prefix >= 0)
                {
                    string prefixText = source.Substring(i, prefix);
                    bool verbatim = prefixText.IndexOf('@') >= 0;
                    bool interpolated = prefixText.IndexOf('$') >= 0;
                    StringBuilder literal = new StringBuilder();
                    i += prefix + 1;

                    while (i < length)
                    {
                        char ch = source[i];
                        if (verbatim)
                        {
                            if (ch == '"')
                            {
                                if (i + 1 < length && source[i + 1] == '"')
                                {
                                    literal.Append('"');
                                    i += 2;
                                    continue;
                                }

                                i++;
                                break;
                            }
                        }
                        else
                        {
                            if (ch == '\\' && i + 1 < length)
                            {
                                literal.Append(source[i + 1]);
                                i += 2;
                                continue;
                            }

                            if (ch == '"')
                            {
                                i++;
                                break;
                            }

                            if (ch == '\n')
                                break;
                        }

                        literal.Append(ch);
                        i++;
                    }

                    if (!interpolated)
                    {
                        literals.Add(literal.ToString());
                    }

                    code.Append("\"\"");
                    continue;
                }

                if (c == '\'')
                {
                    int from = i + (next == '\\' ? 3 : 2);
                    int close = from < length ? source.IndexOf('\'', from) : -1;
                    if (close > i && close - i <= 10)
                    {
                        code.Append("' '");
                        i = close + 1;
                        continue;
                    }
                }

                code.Append(c);
                i++;
            }

            return code.ToString();
        }

        /// <summary>
        /// Length of the string literal prefix before the opening quote at the position, or negative one if no string starts there.
        /// </summary>
        private static int StringPrefixLength(string source, int index)
        {
            for (int prefix = 0; prefix <= 2 && index + prefix < source.Length; prefix++)
            {
                char c = source[index + prefix];
                if (c == '"')
                    return prefix;

                if (c != '@' && c != '$')
                    return -1;
            }

            return -1;
        }

        private static List<ScriptManifest> SortByDependencies(Dictionary<string, ScriptManifest> manifests, HashSet<string> failed)
        {
            List<ScriptManifest> ordered = new List<ScriptManifest>();
            Dictionary<string, bool> visiting = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            List<string> path = new List<string>();

            foreach (string name in manifests.Keys.OrderBy(key => key, StringComparer.OrdinalIgnoreCase).ToList())
            {
                Visit(name, manifests, visiting, path, failed, ordered);
            }

            return ordered;
        }

        private static bool Visit(string name, Dictionary<string, ScriptManifest> manifests, Dictionary<string, bool> visiting,
            List<string> path, HashSet<string> failed, List<ScriptManifest> ordered)
        {
            bool onPath;
            if (visiting.TryGetValue(name, out onPath))
            {
                if (!onPath)
                    return !failed.Contains(name);

                int start = path.FindIndex(entry => string.Equals(entry, name, StringComparison.OrdinalIgnoreCase));
                List<string> cycle = path.Skip(start).ToList();
                Logger.LogError($"[CSScriptPluginLoader] Circular #require detected, none of these plugins will be loaded. {string.Join(" -> ", cycle.Concat(new[] { name }).ToArray())}");
                foreach (string member in cycle)
                {
                    failed.Add(member);
                }
                return false;
            }

            visiting[name] = true;
            path.Add(name);

            bool dependenciesLoaded = true;
            foreach (string dependency in manifests[name].ScriptDependencies)
            {
                if (manifests.ContainsKey(dependency))
                {
                    dependenciesLoaded = Visit(dependency, manifests, visiting, path, failed, ordered) && dependenciesLoaded;
                }
            }

            path.RemoveAt(path.Count - 1);
            visiting[name] = false;

            if (failed.Contains(name))
                return false;

            if (!dependenciesLoaded)
            {
                Logger.LogError($"[CSScriptPluginLoader] {name} plugin will not be loaded because one of its requirements failed.");
                failed.Add(name);
                return false;
            }

            ordered.Add(manifests[name]);
            return true;
        }

        private static bool IsModuleFolder(string directory, string name)
        {
            return File.Exists(Path.Combine(directory, name + CSharpPluginLoader.Extension));
        }

        private static bool IsIgnoredFolder(string name)
        {
            return name.StartsWith(".") || IgnoredFolderNames.Any(ignored => string.Equals(ignored, name, StringComparison.OrdinalIgnoreCase));
        }

        private static void CollectSourceFiles(DirectoryInfo directory, List<string> files)
        {
            foreach (FileInfo file in directory.GetFiles("*" + Extension))
            {
                if (string.Equals(file.Extension, Extension, StringComparison.OrdinalIgnoreCase))
                {
                    files.Add(file.FullName);
                }
            }

            foreach (DirectoryInfo subdirectory in directory.GetDirectories())
            {
                if (!IsIgnoredFolder(subdirectory.Name))
                {
                    CollectSourceFiles(subdirectory, files);
                }
            }
        }

        private static NameIndex CreateNameIndex()
        {
            return new NameIndex(GetInstance().GetPluginNames(), GetModuleNames());
        }

        private static List<string> GetModuleNames()
        {
            if (!Config.GetBoolValue("Engines", "EnableCSharp"))
                return new List<string>();

            try
            {
                return CSharpPluginLoader.GetInstance().GetPluginNames();
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[CSScriptPluginLoader] Could not list the C# modules. {ex.Message}");
                return new List<string>();
            }
        }

        #region Compilation

        private string Compile(ScriptManifest manifest)
        {
            CompilerInfo compiler = GetCompiler();
            if (compiler == null)
            {
                Logger.LogError($"[CSScriptPluginLoader] Can't compile {manifest.Name}, no C# compiler was found. Install Visual Studio Build Tools, the .NET Framework or Mono, or set CSScriptCompiler in the Engines section of Fougerite.cfg.");
                return null;
            }

            List<string> references = BuildReferences(manifest);
            string hash = ComputeBuildHash(compiler, manifest, references);

            // Mono returns the already loaded assembly for a known name, so every build needs a unique name.
            string assemblyName = $"{SanitizeAssemblyName(manifest.Name)}_{hash.Substring(0, 16)}";
            string outputDirectory = Path.Combine(CacheDirectory.FullName, manifest.Name);
            string outputPath = Path.Combine(outputDirectory, assemblyName + ".dll");

            if (File.Exists(outputPath))
            {
                Logger.LogDebug($"[CSScriptPluginLoader] {manifest.Name} is unchanged, using the cached build.");
                return outputPath;
            }

            string stagingDirectory = Path.Combine(outputDirectory, StagingFolderName);
            TryDeleteDirectory(stagingDirectory);
            Directory.CreateDirectory(stagingDirectory);

            string stagingPath = Path.Combine(stagingDirectory, assemblyName + ".dll");
            string responseFile = Path.Combine(stagingDirectory, assemblyName + ".rsp");
            File.WriteAllText(responseFile, BuildResponseFile(compiler, stagingPath, references, manifest.SourceFiles), Encoding.UTF8);

            string logPath = Path.Combine(outputDirectory, manifest.Name + ".log");
            Stopwatch stopwatch = Stopwatch.StartNew();

            // noconfig is ignored inside response files.
            string output;
            int exitCode = RunCompiler(compiler, $"{compiler.Option("noconfig")} @{Quote(responseFile)}", out output);
            stopwatch.Stop();

            File.WriteAllText(logPath, output ?? string.Empty, Encoding.UTF8);
            ReportCompilerOutput(manifest.Name, output);

            if (exitCode != 0 || !File.Exists(stagingPath))
            {
                Logger.LogError($"[CSScriptPluginLoader] Failed to compile {manifest.Name} with the {compiler.Description} (exit code {exitCode}). Full output is in {logPath}.");
                TryDeleteDirectory(stagingDirectory);
                return null;
            }

            DeleteCachedBuilds(outputDirectory);
            File.Move(stagingPath, outputPath);
            TryDeleteDirectory(stagingDirectory);

            Logger.Log($"[CSScriptPluginLoader] Compiled {manifest.Name} ({manifest.SourceFiles.Count} files) in {stopwatch.ElapsedMilliseconds} ms.");
            return outputPath;
        }

        private List<string> BuildReferences(ScriptManifest manifest)
        {
            List<string> references = new List<string>();
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // First assembly with a given name wins, the server's Managed copy comes first.
            Action<string> add = path =>
            {
                if (names.Add(Path.GetFileNameWithoutExtension(path)))
                {
                    references.Add(path);
                }
            };

            foreach (string path in GetFrameworkReferences())
            {
                add(path);
            }

            string referencesFolder = Path.Combine(manifest.Root.FullName, ReferencesFolderName);
            if (Directory.Exists(referencesFolder))
            {
                string[] files = Directory.GetFiles(referencesFolder, "*.dll");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
                foreach (string path in files)
                {
                    add(path);
                }
            }

            Dictionary<string, BasePlugin> plugins = PluginLoader.GetInstance().Plugins;
            foreach (string dependency in manifest.ScriptDependencies)
            {
                BasePlugin plugin;
                if (plugins.TryGetValue(dependency, out plugin) && plugin is CSScriptPlugin scriptPlugin)
                {
                    add(scriptPlugin.AssemblyPath);
                }
            }

            foreach (string dependency in manifest.ModuleDependencies)
            {
                add(CSharpPluginLoader.GetInstance().GetMainFilePath(dependency));
            }

            return references;
        }

        private List<string> GetFrameworkReferences()
        {
            if (_frameworkReferences != null)
                return _frameworkReferences;

            List<string> references = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool hasCorlib = false;

            if (Directory.Exists(ManagedFolder))
            {
                string[] files = Directory.GetFiles(ManagedFolder, "*.dll");
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                foreach (string path in files)
                {
                    AssemblyName assemblyName;
                    try
                    {
                        assemblyName = AssemblyName.GetAssemblyName(path);
                    }
                    catch (BadImageFormatException)
                    {
                        // Native library.
                        continue;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogDebug($"[CSScriptPluginLoader] Skipping reference {Path.GetFileName(path)}. {ex.Message}");
                        continue;
                    }

                    if (!seen.Add(assemblyName.Name))
                        continue;

                    if (string.Equals(assemblyName.Name, "mscorlib", StringComparison.OrdinalIgnoreCase))
                    {
                        hasCorlib = true;
                    }

                    references.Add(path);
                }
            }
            else
            {
                Logger.LogWarning($"[CSScriptPluginLoader] Managed folder {ManagedFolder} does not exist.");
            }

            if (!hasCorlib)
            {
                Logger.LogWarning("[CSScriptPluginLoader] mscorlib.dll was not found in the Managed folder, C# script plugins are compiled against the compiler's own base class library.");
            }

            _hasManagedCorlib = hasCorlib;
            _frameworkReferences = references;
            return references;
        }

        private string BuildResponseFile(CompilerInfo compiler, string outputPath, List<string> references, List<string> sources)
        {
            StringBuilder builder = new StringBuilder();
            if (!compiler.IsMono)
            {
                builder.AppendLine("/nologo");
                builder.AppendLine("/utf8output");
                builder.AppendLine("/nowarn:1701,1702");
            }

            builder.AppendLine(compiler.Option("target:library"));
            builder.AppendLine(compiler.Option("optimize+"));
            builder.AppendLine(compiler.Option("debug-"));

            if (_hasManagedCorlib)
            {
                // Target the server's mscorlib, not the compiler's.
                builder.AppendLine(compiler.Option("nostdlib+"));
            }

            builder.AppendLine(compiler.Option("out:") + Quote(outputPath));

            foreach (string reference in references)
            {
                builder.AppendLine(compiler.Option("reference:") + Quote(reference));
            }

            foreach (string source in sources)
            {
                builder.AppendLine(Quote(source));
            }

            return builder.ToString();
        }

        private static string ComputeBuildHash(CompilerInfo compiler, ScriptManifest manifest, List<string> references)
        {
            using (SHA1 sha = SHA1.Create())
            using (MemoryStream buffer = new MemoryStream())
            {
                WriteHashText(buffer, BuildFormatVersion);
                WriteHashText(buffer, compiler.Path.ToLowerInvariant() + "|" + File.GetLastWriteTimeUtc(compiler.Path).Ticks);

                foreach (string source in manifest.SourceFiles)
                {
                    byte[] content = File.ReadAllBytes(source);
                    WriteHashText(buffer, source.Substring(manifest.Root.FullName.Length).ToLowerInvariant() + "|" + content.Length);
                    buffer.Write(content, 0, content.Length);
                }

                foreach (string reference in references)
                {
                    FileInfo info = new FileInfo(reference);
                    WriteHashText(buffer, info.FullName.ToLowerInvariant() + "|" + info.Length + "|" + info.LastWriteTimeUtc.Ticks);
                }

                byte[] hash = sha.ComputeHash(buffer.ToArray());
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        private static void WriteHashText(Stream stream, string text)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(text + "\n");
            stream.Write(bytes, 0, bytes.Length);
        }

        private static void ReportCompilerOutput(string pluginName, string output)
        {
            if (string.IsNullOrEmpty(output))
                return;

            foreach (string rawLine in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string line = rawLine.Trim();
                if (line.IndexOf("error CS", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    line.IndexOf(": error", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Logger.LogError($"[CSScriptPluginLoader] {pluginName} {line}");
                }
                else if (line.IndexOf("warning CS", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Logger.LogDebug($"[CSScriptPluginLoader] {pluginName} {line}");
                }
            }
        }

        private static void DeleteCachedBuilds(string outputDirectory)
        {
            foreach (string file in Directory.GetFiles(outputDirectory, "*.dll"))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Logger.LogDebug($"[CSScriptPluginLoader] Could not delete old build {file}. {ex.Message}");
                }
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                Logger.LogDebug($"[CSScriptPluginLoader] Could not delete {path}. {ex.Message}");
            }
        }

        private static string SanitizeAssemblyName(string name)
        {
            StringBuilder builder = new StringBuilder(name.Length);
            foreach (char c in name)
            {
                builder.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }

            return builder.ToString();
        }

        private static string Quote(string value)
        {
            return "\"" + value + "\"";
        }

        #endregion

        #region Compiler discovery

        private CompilerInfo GetCompiler()
        {
            if (_compiler != null)
                return _compiler;

            try
            {
                _compiler = FindCompiler();
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CSScriptPluginLoader] Compiler detection failed. {ex}");
            }

            if (_compiler != null)
            {
                Logger.Log($"[CSScriptPluginLoader] Using the {_compiler.Description} at {_compiler.Path}");
            }

            return _compiler;
        }

        private CompilerInfo FindCompiler()
        {
            string configured = Config.GetValue("Engines", "CSScriptCompiler");
            if (!string.IsNullOrEmpty(configured))
            {
                configured = configured.Trim().Trim('"');
                if (File.Exists(configured))
                {
                    return CreateCompiler(configured, "configured compiler");
                }

                Logger.LogWarning($"[CSScriptPluginLoader] CSScriptCompiler {configured} does not exist, falling back to automatic detection.");
            }

            string roslyn = FindMSBuildRoslynCompiler();
            if (roslyn != null)
            {
                return new CompilerInfo(roslyn, null, false, "MSBuild Roslyn compiler");
            }

            // v4 is what MSBuild 4+ uses for .NET 3.5 projects, v3.5 only knows C# 3.
            string windows = Environment.GetEnvironmentVariable("WINDIR") ?? Environment.GetEnvironmentVariable("SystemRoot");
            if (!string.IsNullOrEmpty(windows))
            {
                string frameworkRoot = Path.Combine(windows, "Microsoft.NET");
                foreach (string version in new[] { "v4.0.30319", "v3.5" })
                {
                    foreach (string framework in new[] { "Framework", "Framework64" })
                    {
                        string path = Path.Combine(Path.Combine(frameworkRoot, framework), Path.Combine(version, "csc.exe"));
                        if (File.Exists(path))
                        {
                            return new CompilerInfo(path, null, false, $".NET Framework {version} compiler");
                        }
                    }
                }
            }

            List<string> monoCandidates = new List<string>
            {
                Path.Combine(Util.GetRootFolder(), "mcs.exe"),
                Path.Combine(ManagedFolder, "mcs.exe"),
            };

            foreach (string programFiles in new[] { "ProgramFiles", "ProgramW6432", "ProgramFiles(x86)" }.Select(variable => Environment.GetEnvironmentVariable(variable)).Distinct())
            {
                if (!string.IsNullOrEmpty(programFiles))
                {
                    monoCandidates.Add(Path.Combine(programFiles, "Mono\\lib\\mono\\4.5\\mcs.exe"));
                }
            }

            monoCandidates.Add("/usr/lib/mono/4.5/mcs.exe");
            monoCandidates.Add("/usr/local/lib/mono/4.5/mcs.exe");

            foreach (string candidate in monoCandidates)
            {
                if (File.Exists(candidate))
                {
                    return CreateCompiler(candidate, "Mono mcs compiler");
                }
            }

            return null;
        }

        private static CompilerInfo CreateCompiler(string path, string description)
        {
            bool isMono = string.Equals(Path.GetFileName(path), "mcs.exe", StringComparison.OrdinalIgnoreCase);
            string host = isMono || IsUnix() ? FindMonoHost(path) : null;
            return new CompilerInfo(path, host, isMono, description);
        }

        private static string FindMonoHost(string compilerPath)
        {
            if (IsUnix())
                return "mono";

            // Mono\lib\mono\4.5\mcs.exe
            DirectoryInfo directory = new FileInfo(compilerPath).Directory;
            for (int i = 0; i < 3 && directory != null; i++)
            {
                directory = directory.Parent;
            }

            if (directory == null)
                return null;

            string host = Path.Combine(Path.Combine(directory.FullName, "bin"), "mono.exe");
            return File.Exists(host) ? host : null;
        }

        private static string FindMSBuildRoslynCompiler()
        {
            foreach (string programFiles in new[] { "ProgramFiles(x86)", "ProgramFiles" }.Select(variable => Environment.GetEnvironmentVariable(variable)).Distinct())
            {
                if (string.IsNullOrEmpty(programFiles))
                    continue;

                string vswhere = Path.Combine(programFiles, "Microsoft Visual Studio\\Installer\\vswhere.exe");
                if (!File.Exists(vswhere))
                    continue;

                try
                {
                    string output;
                    int exitCode = RunProcess(vswhere,
                        "-latest -products * -requires Microsoft.Component.MSBuild -find MSBuild\\**\\Bin\\Roslyn\\csc.exe -utf8",
                        ToolTimeoutMs, out output);

                    if (exitCode != 0 || string.IsNullOrEmpty(output))
                        continue;

                    foreach (string line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string candidate = line.Trim();
                        if (File.Exists(candidate))
                            return candidate;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogDebug($"[CSScriptPluginLoader] vswhere failed. {ex.Message}");
                }
            }

            return null;
        }

        private static bool IsUnix()
        {
            int platform = (int) Environment.OSVersion.Platform;
            return platform == 4 || platform == 6 || platform == 128;
        }

        private static int RunCompiler(CompilerInfo compiler, string arguments, out string output)
        {
            return compiler.Host == null
                ? RunProcess(compiler.Path, arguments, CompilerTimeoutMs, out output)
                : RunProcess(compiler.Host, Quote(compiler.Path) + " " + arguments, CompilerTimeoutMs, out output);
        }

        private static int RunProcess(string fileName, string arguments, int timeoutMs, out string output)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo(fileName, arguments ?? string.Empty)
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            using (Process process = Process.Start(startInfo))
            {
                string standardOutput = null;
                string standardError = null;
                Thread outputReader = new Thread(() => standardOutput = process.StandardOutput.ReadToEnd()) { IsBackground = true };
                Thread errorReader = new Thread(() => standardError = process.StandardError.ReadToEnd()) { IsBackground = true };
                outputReader.Start();
                errorReader.Start();

                bool exited = process.WaitForExit(timeoutMs);
                if (!exited)
                {
                    try
                    {
                        process.Kill();
                    }
                    catch (Exception)
                    {
                        // Already gone.
                    }
                }

                outputReader.Join(StreamJoinTimeoutMs);
                errorReader.Join(StreamJoinTimeoutMs);

                output = (standardOutput ?? string.Empty) + (standardError ?? string.Empty);
                if (!exited)
                {
                    output += $"{Environment.NewLine}Process timed out after {timeoutMs} ms and was killed.";
                    return -1;
                }

                return process.ExitCode;
            }
        }

        #endregion

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            try
            {
                if (string.IsNullOrEmpty(args.Name))
                    return null;

                string simpleName = args.Name.Split(',')[0].Trim();
                lock (_assemblyLock)
                {
                    Assembly assembly;
                    if (_scriptAssemblies.TryGetValue(simpleName, out assembly) ||
                        _referenceAssemblies.TryGetValue(simpleName, out assembly))
                    {
                        return assembly;
                    }

                    foreach (string pluginName in GetPluginNames())
                    {
                        string candidate = Path.Combine(Path.Combine(GetPluginDirectoryPath(pluginName), ReferencesFolderName), simpleName + ".dll");
                        if (!File.Exists(candidate))
                            continue;

                        assembly = Assembly.LoadFrom(candidate);
                        _referenceAssemblies[simpleName] = assembly;
                        return assembly;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"[CSScriptPluginLoader] Assembly resolution error: {ex}");
            }

            return null;
        }

        private sealed class CompilerInfo
        {
            public readonly string Path;
            public readonly string Host;
            public readonly bool IsMono;
            public readonly string Description;

            public CompilerInfo(string path, string host, bool isMono, string description)
            {
                Path = path;
                Host = host;
                IsMono = isMono;
                Description = description;
            }

            public string Option(string option)
            {
                return (IsMono ? "-" : "/") + option;
            }
        }

        private sealed class ScriptManifest
        {
            public readonly string Name;
            public readonly DirectoryInfo Root;
            public readonly List<string> SourceFiles = new List<string>();
            public readonly List<string> ScriptDependencies = new List<string>();
            public readonly List<string> ModuleDependencies = new List<string>();
            public readonly List<string> SoftDependencies = new List<string>();
            public uint? DeclaredOrder;

            public ScriptManifest(string name, DirectoryInfo root)
            {
                Name = name;
                Root = root;
            }
        }

        private sealed class NameIndex
        {
            public readonly Dictionary<string, string> Scripts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, string> Modules = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public NameIndex(IEnumerable<string> scripts, IEnumerable<string> modules)
            {
                foreach (string script in scripts)
                {
                    Scripts[script] = script;
                }

                foreach (string module in modules)
                {
                    Modules[module] = module;
                }
            }
        }
    }
}
