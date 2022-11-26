using System.IO;
using UnityEngine;

namespace Fougerite.Tools
{
    public static class Extensions
    {
        /// <summary>
        /// Combines two path while handling the root check.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static string Combine(this string path1, string path2)
        {
            if (Path.IsPathRooted(path2))
            {
                path2 = path2.TrimStart(Path.DirectorySeparatorChar);
                path2 = path2.TrimStart(Path.AltDirectorySeparatorChar);
            }

            return Path.Combine(path1, path2);
        }

        /// <summary>
        /// A better implementation of getcomponent.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <param name="component"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static bool GetComponent<T>(this GameObject gameObject, out T component) where T : Component
        {
            if (gameObject == null)
            {
                component = default(T);
                return false;
            }

            T tComponent = gameObject.GetComponent<T>();
            if (tComponent == null)
            {
                component = default(T);
                return false;
            }

            component = tComponent;
            return true;
        }
    }
}