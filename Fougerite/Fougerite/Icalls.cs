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
        /// Loads a plugin into its own isolated <c>AppDomain</c> from the supplied assembly bytes.
        /// </summary>
        /// <param name="pluginName">The name of the plugin, used as the key for its domain/context.</param>
        /// <param name="data">A pointer to the memory buffer containing the plugin binary data.</param>
        /// <param name="dataLen">The length of the binary data in the memory buffer.</param>
        /// <returns>
        /// An instance of the <see cref="System.Reflection.Assembly"/> representing the loaded plugin.
        /// Returns null if the plugin could not be successfully loaded.
        /// </returns>
        /// <remarks>
        /// Always unload a plugin before loading it again under the same name, otherwise the previous domain leaks.
        /// </remarks>
        [MethodImpl(MethodImplOptions.InternalCall)]
        public extern Assembly mono_fg_load_plugin(string pluginName, IntPtr data, uint dataLen);

        /// <summary>
        /// Unloads the <c>AppDomain</c> belonging to the named plugin and frees its resources.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to be unloaded.</param>
        /// <returns>True if the plugin was found and unloaded, otherwise, false.</returns>
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
        /// Initializes the native plugin registry used to track each loaded plugin's domain.
        /// Despite the name, it does not create an actual Mono domain itself.
        /// </summary>
        /// <returns>
        /// Returns an integer representing the success or failure of the initialization.
        /// Typically, 0x1 indicates success and any other value indicates failure.
        /// </returns>
        [DllImport("mono.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int mono_fg_create_domain();

        /// <summary>
        /// Unloads all currently tracked plugin domains and releases the native plugin registry.
        /// </summary>
        /// <returns>
        /// An integer indicating whether the operation completed.
        /// A return value of 0x1 indicates success, while any other value denotes failure.
        /// </returns>
        [DllImport("mono.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int mono_fg_unload_domain();
    }
}