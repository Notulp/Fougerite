namespace Fougerite
{
    using System;

    /// <summary>
    /// Represents a Fougerite C# plugin.
    /// </summary>
    public abstract class Module : IDisposable
    {
        public virtual string ModuleFolder { get; set; }

        public virtual string Name
        {
            get { return "None"; }
        }

        public virtual Version Version
        {
            get { return new Version(1, 0); }
        }

        public virtual string Author
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
            this.Dispose(false);
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public abstract void DeInitialize();

        public abstract void Initialize();
    }
}