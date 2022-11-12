using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Fougerite
{
    public static class ReflectionExtensions
    {
        //Instanced
        public static object CallMethod(this object obj, string methodName, params object[] args)
        {
            var metInf = GetMethodInfo(obj, methodName);

            if (metInf == null)
                throw new Exception($"Couldn't find method '{methodName}' using reflection.");

            if (metInf is MethodInfo methodInfo)
                return methodInfo.Invoke(obj, args);

            return null;
        }

        public static object CallMethodOnBase(this object obj, string methodName, params object[] args)
        {
            Type Base = obj.GetType().BaseType;
            if (Base != null)
            {
                return CallMethodOnBase(obj, GetMethodInfo(Base, methodName), args);
            }

            return null;
        }

        public static object CallMethodOnBase(this object obj, Type Base, string methodname, params object[] args)
        {
            return CallMethodOnBase(obj, GetMethodInfo(Base, methodname), args);
        }

        public static object CallMethodOnBase(this object obj, MethodInfo method, params object[] args)
        {
            var parameters = method.GetParameters();

            if (parameters.Length == 0)
            {
                if (args != null && args.Length != 0)
                    throw new Exception("Arguments count doesn't match");
            }
            else
            {
                if (parameters.Length != args.Length)
                    throw new Exception("Arguments count doesn't match");
            }

            Type returnType = null;
            if (method.ReturnType != typeof(void))
            {
                returnType = method.ReturnType;
            }

            Type type = obj.GetType();
            DynamicMethod dynamicMethod = new DynamicMethod("", returnType, new[] { type, typeof(Object) }, type);

            var iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);

            for (var i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];

                iLGenerator.Emit(OpCodes.Ldarg_1);

                iLGenerator.Emit(OpCodes.Ldc_I4_S, i);
                iLGenerator.Emit(OpCodes.Ldelem_Ref);

                Type parameterType = parameter.ParameterType;
                if (parameterType.IsPrimitive)
                {
                    iLGenerator.Emit(OpCodes.Unbox_Any, parameterType);
                }
                else if (parameterType == typeof(Object))
                {
                }
                else
                {
                    iLGenerator.Emit(OpCodes.Castclass, parameterType);
                }
            }

            iLGenerator.Emit(OpCodes.Call, method);
            iLGenerator.Emit(OpCodes.Ret);

            return dynamicMethod.Invoke(null, new[] { obj, args });
        }

        public static object GetFieldValue(this object obj, string fieldName)
        {
            MemberInfo memInf = GetFieldInfo(obj, fieldName);

            if (memInf == null)
                throw new Exception($"Couldn't find field '{fieldName}' using reflection.");

            if (memInf is PropertyInfo propertyInfo)
                return propertyInfo.GetValue(obj, null);

            if (memInf is FieldInfo fieldInfo)
                return fieldInfo.GetValue(obj);

            throw new Exception();
        }

        public static object GetFieldValueChain(this object obj, params string[] args)
        {
            foreach (string arg in args)
            {
                obj = obj.GetFieldValue(arg);
            }

            return obj;
        }

        public static void SetFieldValue(this object obj, string fieldName, object newValue)
        {
            MemberInfo memInf = GetFieldInfo(obj, fieldName);

            if (memInf == null)
                throw new Exception($"Couldn't find field '{fieldName}' using reflection.");

            if (memInf is PropertyInfo propertyInfo)
                propertyInfo.SetValue(obj, newValue, null);
            else if (memInf is FieldInfo fieldInfo)
                fieldInfo.SetValue(obj, newValue);
            else
                throw new Exception();
        }

        private static MethodInfo GetMethodInfo(Type classType, string methodName)
        {
            return classType.GetMethod(methodName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                BindingFlags.Static);
        }

        private static MethodInfo GetMethodInfo(object obj, string methodName)
        {
            return GetMethodInfo(obj.GetType(), methodName);
        }

        private static MemberInfo GetFieldInfo(Type objType, string fieldName)
        {
            List<PropertyInfo> prps = new List<PropertyInfo>();

            prps.Add(objType.GetProperty(fieldName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                BindingFlags.Static));

            prps = prps.Where(i => !ReferenceEquals(i, null)).ToList();

            if (prps.Count != 0)
                return prps[0];

            List<FieldInfo> flds = new List<FieldInfo>();

            flds.Add(objType.GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy |
                BindingFlags.Static));

            flds = flds.Where(i => !ReferenceEquals(i, null)).ToList();

            if (flds.Count != 0)
                return flds[0];

            // if not found on the current type, check the base
            if (objType.BaseType != null)
            {
                return GetFieldInfo(objType.BaseType, fieldName);
            }

            return null;
        }

        private static MemberInfo GetFieldInfo(object obj, string fieldName)
        {
            return GetFieldInfo(obj.GetType(), fieldName);
        }

        //Static
        public static void CallStaticMethod(this Type classType, string methodName, params object[] args)
        {
            var metInf = GetMethodInfo(classType, methodName);

            if (metInf == null)
                throw new Exception($"Couldn't find method '{methodName}' using reflection.");

            if (metInf is MethodInfo methodInfo)
            {
                methodInfo.Invoke(null, args);
            }
        }

        public static object GetStaticFieldValue(this Type classType, string fieldName)
        {
            MemberInfo memInf = GetFieldInfo(classType, fieldName);

            if (memInf == null)
                throw new Exception($"Couldn't find field '{fieldName}' using reflection.");

            if (memInf is PropertyInfo propertyInfo)
                return propertyInfo.GetValue(null, null);

            if (memInf is FieldInfo fieldInfo)
                return fieldInfo.GetValue(null);

            throw new Exception();
        }

        public static void SetFieldValueValue(this Type classType, string fieldName, object newValue)
        {
            MemberInfo memInf = GetFieldInfo(classType, fieldName);

            if (memInf == null)
                throw new Exception($"Couldn't find field '{fieldName}' using reflection.");

            if (memInf is PropertyInfo propertyInfo)
                propertyInfo.SetValue(null, newValue, null);
            else if (memInf is FieldInfo fieldInfo)
                fieldInfo.SetValue(null, newValue);
            else
                throw new Exception();
        }
    }
}