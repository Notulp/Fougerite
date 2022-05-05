using System;
using System.Collections.Generic;

namespace Fougerite.PluginLoaders
{
    [Serializable]
    public class CountedInstance
    {
        [NonSerialized] public static readonly Dictionary<Type, Counts> InstanceTypes;

        ~CountedInstance()
        {
            RemoveCount(GetType());
        }

        public CountedInstance()
        {
            AddCount(GetType());
        }

        static CountedInstance()
        {
            InstanceTypes = new Dictionary<Type, Counts>();
        }

        internal static void AddCount(Type type)
        {
            Counts counts;
            if (InstanceTypes.TryGetValue(type, out counts))
            {
                counts.Created++;
                return;
            }

            InstanceTypes.Add(type, new Counts());
        }

        internal static void RemoveCount(Type type)
        {
            Counts counts;
            if (InstanceTypes.TryGetValue(type, out counts))
            {
                counts.Destroyed++;
            }
        }

        public static string InstanceReportText()
        {
            string text = "";
            foreach (KeyValuePair<Type, Counts> current in InstanceTypes)
            {
                object obj = text;
                text = String.Concat(obj, current.Key.FullName, Environment.NewLine + "\tCurrently:\t",
                    current.Value.Created - current.Value.Destroyed, Environment.NewLine + "\tCreated:  \t",
                    current.Value.Created, Environment.NewLine);
            }

            return text;
        }

        [Serializable]
        public class Counts
        {
            public int Created = 1;
            public int Destroyed;
        }
    }
}