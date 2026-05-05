using Fougerite.Concurrent;

namespace Fougerite.PluginLoaders
{
    public abstract class Singleton<T> : CountedInstance where T : ISingleton, new()
    {
        private static readonly Lazy<T> Instance = new Lazy<T>(() => new T());

        public static T GetInstance()
        {
            return Instance.Value;
        }

        static Singleton()
        {
            if (Instance.Value.CheckDependencies())
            {
                Instance.Value.Initialize();
            }
            else
            {
                Logger.LogWarning(
                    $"{Instance.GetType()} is disabled in the Fougerite.cfg, and will not load any plugins.");
            }
        }
    }
}