using System;

namespace Fougerite
{
    /// <summary>
    /// Wraps a loaded <see cref="Module"/> instance together with its load state, used internally by
    /// <see cref="ModuleManager"/> to track and (de)initialize modules.
    /// </summary>
    public class ModuleContainer : IDisposable
    {
        /// <summary>
        /// The wrapped module instance.
        /// </summary>
        public Module Plugin
        {
            get;
            protected set;
        }

        /// <summary>
        /// Whether <see cref="Initialize"/> has been called on this container without a matching
        /// <see cref="DeInitialize"/> call since.
        /// </summary>
        public bool Initialized
        {
            get;
            protected set;
        }

        /// <summary>
        /// Whether the module was loaded from a compiled .dll (as opposed to a scripting language module).
        /// </summary>
        public bool Dll
        {
            get;
            set;
        }

        /// <summary>
        /// Creates a container for the given module, defaulting <see cref="Dll"/> to true.
        /// </summary>
        /// <param name="plugin">The module instance to wrap.</param>
        public ModuleContainer(Module plugin) : this(plugin, true)
        {
        }

        /// <summary>
        /// Creates a container for the given module.
        /// </summary>
        /// <param name="plugin">The module instance to wrap.</param>
        /// <param name="dll">Whether the module was loaded from a compiled .dll.</param>
        public ModuleContainer(Module plugin, bool dll)
        {
            Plugin = plugin;
            Initialized = false;
            Dll = dll;
        }

        /// <summary>
        /// Initializes the wrapped module and marks this container as initialized.
        /// </summary>
        public void Initialize()
        {
            Plugin.Initialize();
            Initialized = true;
        }

        /// <summary>
        /// De-initializes the wrapped module and marks this container as not initialized.
        /// </summary>
        public void DeInitialize()
        {
            Initialized = false;
            Plugin.DeInitialize();
        }

        /// <summary>
        /// Disposes the wrapped module.
        /// </summary>
        public void Dispose()
        {
            Plugin.Dispose();
        }
    }
}
