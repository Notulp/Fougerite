using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Fougerite
{
    /// <summary>
    /// The <c>Icalls</c> class provides an interface for interacting with native
    /// Mono runtime methods specifically for loading and unloading plugins
    /// in the Fougerite framework.
    /// </summary>
    public sealed class Icalls
    {
        /// <summary>
        /// Loads a plugin into the application domain from the supplied memory buffer.
        /// </summary>
        /// <param name="pluginName">The name of the plugin mapping to its isolated domain.</param>
        /// <param name="data">A pointer to the memory buffer containing the plugin binary data.</param>
        /// <param name="dataLen">The length of the binary data in the memory buffer.</param>
        /// <returns>
        /// An instance of the <see cref="System.Reflection.Assembly"/> representing the loaded plugin.
        /// Returns null if the plugin could not be successfully loaded.
        /// </returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern Assembly mono_fg_load_plugin(string pluginName, IntPtr data, uint dataLen);

        /// <summary>
        /// Unloads a plugin specified by its name.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to be unloaded.</param>
        /// <returns>True if the plugin was successfully unloaded, otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern bool mono_fg_unload_plugin(string pluginName);
    }

    /// <summary>
    /// Provides an interface for interacting with native Mono functionalities
    /// necessary for plugin management within the Fougerite framework.
    /// This is implemented in our custom mono.dll provided in the Reference files.
    /// </summary>
    /// <remarks>
    /// The NativeMono class acts as a bridge to unmanaged Mono operations such as domain creation
    /// and unloading, allowing for dynamic plugin lifecycle management in a managed environment.
    /// </remarks>
    internal static class NativeMono
    {
        /// <summary>
        /// Creates an unmanaged Fougerite Mono domain.
        /// </summary>
        /// <returns>
        /// Returns an integer representing the success or failure of creating the domain.
        /// Typically, 0x1 indicates success and any other value indicates failure.
        /// </returns>
        [DllImport("mono.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int mono_fg_create_domain();

        /// <summary>
        /// Unloads the Mono domain used for running specific plugins or assemblies.
        /// Typically used to release resources associated with the domain and prepare
        /// for a fresh environment.
        /// </summary>
        /// <returns>
        /// An integer indicating whether the domain was successfully unloaded.
        /// A return value of 0x1 indicates success, while any other value denotes failure.
        /// </returns>
        [DllImport("mono.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int mono_fg_unload_domain();
    }
}