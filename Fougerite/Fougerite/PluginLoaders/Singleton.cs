using System;

namespace Fougerite.PluginLoaders
{
    public abstract class Singleton<T> : CountedInstance where T : ISingleton
    {
        private static readonly T Instance;

        public static T GetInstance()
        {
            return Instance;
        }

        static Singleton()
        {
            Instance = Activator.CreateInstance<T>();
            if (Instance.CheckDependencies())
            {
                Instance.Initialize();
            }
            else
            {
                Logger.LogWarning(Instance.GetType() + " is disabled in the Fougerite.cfg, and will not load any plugins.");
            }
        }
    }
}