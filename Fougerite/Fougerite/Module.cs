using System;
using Fougerite.PluginLoaders;

namespace Fougerite
{
    /// <summary>
    /// Represents the high-level public API contract exposed to third-party developers for C# plugins.
    /// </summary>
    /// <remarks>
    /// Architecture Layout:
    /// <code>
    ///        BasePlugin
    ///         /      \
    ///        /        \
    ///     Module    CSPlugin 
    ///                  │
    ///                  └──> (Wraps via field) ──> public Module Engine;
    /// </code>
    /// This design comes from a  rework of the plugin loader subsystem years ago. 
    /// Because a vast ecosystem of legacy C# plugins strictly rely on deriving from the <see cref="Module"/> class, 
    /// breaking backwards compatibility was not an option.
    /// 
    /// By having <see cref="Module"/> inherit directly from <see cref="BasePlugin"/>, developers retain
    /// access to all base utility execution structures (timers, logging, websockets), while keeping 
    /// the public API clean and completely decoupled from engine-side deployment workers.
    /// </remarks>
    public abstract class Module : BasePlugin, IDisposable
    {
        /// <summary>
        /// Only available from the call of Initialize.
        /// </summary>
        public virtual string ModuleFolder { get; set; }

        public new virtual string Name
        {
            get { return "None"; }
        }

        public new virtual Version Version
        {
            get { return new Version(1, 0); }
        }

        public new virtual string Author
        {
            get { return "None"; }
        }

        public virtual string Description
        {
            get { return "None"; }
        }

        public virtual bool Enabled { get; set; }

        /// <summary>
        /// Priority of the plugin's loading.
        /// </summary>
        public virtual uint Order
        {
            get { return uint.MaxValue; }
        }

        public virtual string UpdateURL
        {
            get { return ""; }
        }

        ~Module()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public abstract void DeInitialize();

        public abstract void Initialize();
    }
}