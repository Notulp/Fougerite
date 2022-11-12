using System;

namespace Fougerite
{
    public class ModuleContainer : IDisposable
    {
        public Module Plugin
        {
            get;
            protected set;
        }

        public bool Initialized
        {
            get;
            protected set;
        }

        public bool Dll
        {
            get;
            set;
        }

        public ModuleContainer(Module plugin) : this(plugin, true)
        {
        }

        public ModuleContainer(Module plugin, bool dll)
        {
            Plugin = plugin;
            Initialized = false;
            Dll = dll;
        }

        public void Initialize()
        {
            Plugin.Initialize();
            Initialized = true;
        }

        public void DeInitialize()
        {
            Initialized = false;
            Plugin.DeInitialize();
        }

        public void Dispose()
        {
            Plugin.Dispose();
        }
    }
}
