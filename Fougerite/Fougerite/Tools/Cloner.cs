using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Fougerite.Tools
{
    /// <summary>
    /// Allows you to shallow copy objects with equivalency to memcpy speed. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public static class Cloner<T>
    {
        private static readonly Func<T, T> ClonerFunc = CreateCloner();

        private static Func<T, T> CreateCloner()
        {
            DynamicMethod cloneMethod = new DynamicMethod("CloneImplementation", typeof(T), new[] { typeof(T) }, true);
            ConstructorInfo defaultCtor = typeof(T).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null, new Type[] { }, null);

            ILGenerator generator = cloneMethod.GetILGenerator();

            LocalBuilder loc1 = generator.DeclareLocal(typeof(T));

            generator.Emit(OpCodes.Newobj, defaultCtor);
            generator.Emit(OpCodes.Stloc, loc1);

            foreach (FieldInfo field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                generator.Emit(OpCodes.Ldloc, loc1);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, field);
                generator.Emit(OpCodes.Stfld, field);
            }

            generator.Emit(OpCodes.Ldloc, loc1);
            generator.Emit(OpCodes.Ret);

            return ((Func<T, T>)cloneMethod.CreateDelegate(typeof(Func<T, T>)));
        }

        public static T Clone(T myObject)
        {
            return ClonerFunc(myObject);
        }
    }
}