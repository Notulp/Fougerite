using System;
using Fougerite.PluginLoaders;

namespace Fougerite
{
    /// <summary>
    /// Represents a Fougerite C# plugin.
    /// </summary>
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