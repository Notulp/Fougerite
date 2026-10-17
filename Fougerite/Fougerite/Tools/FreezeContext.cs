using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Fougerite.PluginLoaders;

namespace Fougerite.Tools
{
    /// <summary>
    /// Tracks which subscriber is currently executing for every event raised through
    /// <see cref="Hooks.ExecuteSubscribers"/>, and reports that context to the native server freeze monitor
    /// so a freeze/hang can be attributed to the plugin or module that caused it.
    /// </summary>
    internal static class FreezeContext
    {
        /// <summary>
        /// Signature of the native callback invoked by the freeze monitor when its state changes.
        /// </summary>
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void NativeFreezeCallback(int type, int frozenSeconds, IntPtr info);

        /// <summary>
        /// Registers the managed callback that the native freeze monitor invokes on a state change.
        /// </summary>
        [DllImport("librust", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreezeMonitor_SetCallback(NativeFreezeCallback callback);

        /// <summary>
        /// Tells the native freeze monitor which context is currently executing, so that It can be
        /// reported if the server freezes while that context is running.
        /// </summary>
        [DllImport("librust", CallingConvention = CallingConvention.Cdecl)]
        private static extern void FreezeMonitor_SetContext(IntPtr context, [MarshalAs(UnmanagedType.I1)] bool record);

        private struct SubscriberKey
        {
            public readonly MethodInfo Method;
            public readonly object Target;
            public readonly string EventName;

            public SubscriberKey(MethodInfo method, object target, string eventName)
            {
                Method = method;
                Target = target;
                EventName = eventName;
            }
        }

        private sealed class SubscriberKeyComparer : IEqualityComparer<SubscriberKey>
        {
            public bool Equals(SubscriberKey a, SubscriberKey b)
            {
                return ReferenceEquals(a.Method, b.Method) && ReferenceEquals(a.Target, b.Target) &&
                       string.Equals(a.EventName, b.EventName);
            }

            public int GetHashCode(SubscriberKey key)
            {
                unchecked
                {
                    int hash = key.Method != null ? RuntimeHelpers.GetHashCode(key.Method) : 0;
                    hash = hash * 31 + (key.Target != null ? RuntimeHelpers.GetHashCode(key.Target) : 0);
                    hash = hash * 31 + (key.EventName != null ? key.EventName.GetHashCode() : 0);
                    return hash;
                }
            }
        }

        private sealed class SubscriberEntry
        {
            public readonly IntPtr Context;
            public readonly Assembly Assembly;

            public SubscriberEntry(IntPtr context, Assembly assembly)
            {
                Context = context;
                Assembly = assembly;
            }
        }

        private static readonly object Sync = new object();

        private static readonly Dictionary<string, IntPtr> Interned =
            new Dictionary<string, IntPtr>(StringComparer.Ordinal);

        private static readonly Dictionary<SubscriberKey, SubscriberEntry> Subscribers =
            new Dictionary<SubscriberKey, SubscriberEntry>(new SubscriberKeyComparer());

        private static readonly NativeFreezeCallback Callback = Hooks.OnNativeFreeze;

        private static volatile bool _disabled;

        [ThreadStatic] 
        private static IntPtr _current;

        /// <summary>
        /// Interned pointer for the ChatRaw event name, used to avoid re-allocating native strings.
        /// </summary>
        internal static readonly IntPtr ChatRawEvent = Intern("ChatRawEvent");

        /// <summary>
        /// Interned pointer for the Chat event name, used to avoid re-allocating native strings.
        /// </summary>
        internal static readonly IntPtr ChatEvent = Intern("ChatEvent");

        /// <summary>
        /// Interned pointer for the OnConsoleReceivedWithCancel event name, used to avoid re-allocating native strings.
        /// </summary>
        internal static readonly IntPtr ConsoleReceivedWithCancelEvent = Intern("OnConsoleReceivedWithCancel");

        /// <summary>
        /// Interned pointer for the ConsoleReceived event name, used to avoid re-allocating native strings.
        /// </summary>
        internal static readonly IntPtr ConsoleReceivedEvent = Intern("ConsoleReceived");

        static FreezeContext()
        {
            try
            {
                FreezeMonitor_SetCallback(Callback);
            }
            catch (Exception)
            {
                _disabled = true;
            }
        }

        /// <summary>
        /// Interns a string so It can be passed to native code as a stable <see cref="IntPtr"/>,
        /// reusing the same pointer for repeated calls with the same name.
        /// </summary>
        /// <param name="name">The string to intern.</param>
        /// <returns>A native pointer to the interned ANSI string.</returns>
        internal static IntPtr Intern(string name)
        {
            lock (Sync)
            {
                IntPtr ptr;
                if (!Interned.TryGetValue(name, out ptr))
                {
                    ptr = Marshal.StringToHGlobalAnsi(name);
                    Interned.Add(name, ptr);
                }

                return ptr;
            }
        }

        /// <summary>
        /// Resolves (and caches) the native context pointer that identifies a given event subscriber,
        /// so the freeze monitor can report which subscriber was executing on a freeze.
        /// </summary>
        /// <param name="subscriber">The event subscriber delegate.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        /// <returns>A native context pointer for the subscriber, or <see cref="IntPtr.Zero"/> if not tracked.</returns>
        internal static IntPtr ForSubscriber(Delegate subscriber, string eventName)
        {
            if (_disabled)
            {
                return IntPtr.Zero;
            }

            SubscriberKey key = new SubscriberKey(subscriber.Method, subscriber.Target as PluginLoaders.BasePlugin,
                eventName);
            SubscriberEntry entry;
            lock (Sync)
            {
                if (Subscribers.TryGetValue(key, out entry))
                {
                    return entry.Context;
                }
            }

            Type owner = OuterType(subscriber.Method);
            IntPtr context = owner == typeof(Hooks)
                ? IntPtr.Zero
                : Intern(eventName + " " + Describe(subscriber));

            lock (Sync)
            {
                Subscribers[key] = new SubscriberEntry(context, owner != null ? owner.Assembly : null);
            }

            return context;
        }

        /// <summary>
        /// Marks the given context as currently executing and reports It to the native freeze monitor.
        /// </summary>
        /// <param name="context">The context being entered, as returned by <see cref="ForSubscriber"/>.</param>
        /// <returns>The previously active context, to be restored via <see cref="Leave"/>.</returns>
        internal static IntPtr Enter(IntPtr context)
        {
            IntPtr previous = _current;
            if (_disabled)
            {
                return previous;
            }

            _current = context;
            try
            {
                FreezeMonitor_SetContext(context, true);
            }
            catch (Exception)
            {
                _disabled = true;
            }

            return previous;
        }

        /// <summary>
        /// Restores the previously active context after a subscriber finishes executing.
        /// </summary>
        /// <param name="previous">The context to restore, as returned by <see cref="Enter"/>.</param>
        internal static void Leave(IntPtr previous)
        {
            if (_disabled)
            {
                return;
            }

            _current = previous;
            try
            {
                FreezeMonitor_SetContext(previous, false);
            }
            catch (Exception)
            {
                _disabled = true;
            }
        }

        /// <summary>
        /// Clears all cached subscriber contexts. Called when all hooks are reset.
        /// </summary>
        internal static void ClearSubscribers()
        {
            lock (Sync)
            {
                Subscribers.Clear();
            }
        }

        /// <summary>
        /// Removes all cached contexts belonging to the given plugin (and its underlying engine/assemblies),
        /// so stale references aren't kept around after the plugin is unloaded.
        /// </summary>
        /// <param name="plugin">The plugin being unloaded.</param>
        internal static void ForgetPlugin(PluginLoaders.BasePlugin plugin)
        {
            if (plugin == null)
            {
                return;
            }

            Module engine = null;
            List<Assembly> assemblies = new List<Assembly>();

            if (plugin is CSPlugin csPlugin)
            {
                engine = csPlugin.Engine;
            }

            if (plugin is CSScriptPlugin scriptPlugin)
            {
                engine = scriptPlugin.Engine;
                if (scriptPlugin.CompiledAssembly != null)
                {
                    assemblies.Add(scriptPlugin.CompiledAssembly);
                }
            }

            lock (Sync)
            {
                Assembly fougeriteAssembly = typeof(Hooks).Assembly;
                if (engine != null)
                {
                    foreach (KeyValuePair<SubscriberKey, SubscriberEntry> pair in Subscribers)
                    {
                        Assembly assembly = pair.Value.Assembly;
                        if (ReferenceEquals(pair.Key.Target, engine) && assembly != null &&
                            !ReferenceEquals(assembly, fougeriteAssembly) && !ContainsReference(assemblies, assembly))
                        {
                            assemblies.Add(assembly);
                        }
                    }
                }

                List<SubscriberKey> stale = new List<SubscriberKey>();
                foreach (KeyValuePair<SubscriberKey, SubscriberEntry> pair in Subscribers)
                {
                    if (ReferenceEquals(pair.Key.Target, plugin) ||
                        (engine != null && ReferenceEquals(pair.Key.Target, engine)) ||
                        (pair.Value.Assembly != null && ContainsReference(assemblies, pair.Value.Assembly)))
                    {
                        stale.Add(pair.Key);
                    }
                }

                foreach (SubscriberKey key in stale)
                {
                    Subscribers.Remove(key);
                }
            }
        }

        private static bool ContainsReference(List<Assembly> assemblies, Assembly assembly)
        {
            foreach (Assembly candidate in assemblies)
            {
                if (ReferenceEquals(candidate, assembly))
                {
                    return true;
                }
            }

            return false;
        }

        private static Type OuterType(MethodInfo method)
        {
            Type owner = method != null ? method.DeclaringType : null;
            while (owner != null && owner.DeclaringType != null)
            {
                owner = owner.DeclaringType;
            }

            return owner;
        }

        private static string Describe(Delegate subscriber)
        {
            Module module = subscriber.Target as Module;
            if (module != null)
            {
                return ModuleName(module) + "." + subscriber.Method.Name;
            }

            PluginLoaders.BasePlugin plugin = subscriber.Target as PluginLoaders.BasePlugin;
            if (plugin != null)
            {
                return (string.IsNullOrEmpty(plugin.Name) ? plugin.GetType().Name : plugin.Name) + "." +
                       subscriber.Method.Name;
            }

            Type owner = OuterType(subscriber.Method);
            return (owner != null ? owner.FullName : "?") + "." + subscriber.Method.Name;
        }

        private static string ModuleName(Module module)
        {
            try
            {
                string name = module.Name;
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }
            catch (Exception)
            {
            }

            return module.GetType().Name;
        }
    }
}