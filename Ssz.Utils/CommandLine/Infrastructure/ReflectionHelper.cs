using System;
using System.Collections.Generic;
using System.Reflection;
using Ssz.Utils.CommandLine.Attributes;

namespace Ssz.Utils.CommandLine.Infrastructure
{
    internal static class ReflectionHelper
    {
        #region construction and destruction

        static ReflectionHelper()
        {
            AssemblyFromWhichToPullInformation = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        }

        #endregion

        #region public functions

        public static IList<Pair<PropertyInfo, TAttribute>> RetrievePropertyList<TAttribute>(object target)
            where TAttribute : Attribute
        {
            var key = new Pair<Type, object>(typeof (Pair<PropertyInfo, TAttribute>), target);
            object? cached = ReflectionCache.Instance[key];
            if (cached == null)
            {
                IList<Pair<PropertyInfo, TAttribute>> list = new List<Pair<PropertyInfo, TAttribute>>();
                if (target != null)
                {
                    PropertyInfo[] propertiesInfo = target.GetType().GetProperties();

                    foreach (PropertyInfo property in propertiesInfo)
                    {
                        if (property == null || (!property.CanRead || !property.CanWrite))
                        {
                            continue;
                        }

                        MethodInfo? setMethod = property.GetSetMethod();
                        if (setMethod == null || setMethod.IsStatic)
                        {
                            continue;
                        }

                        Attribute? attribute = Attribute.GetCustomAttribute(property, typeof (TAttribute), false);
                        if (attribute != null)
                        {
                            list.Add(new Pair<PropertyInfo, TAttribute>(property, (TAttribute) attribute));
                        }
                    }
                }

                ReflectionCache.Instance[key] = list;
                return list;
            }

            return (IList<Pair<PropertyInfo, TAttribute>>) cached;
        }

        public static Pair<MethodInfo, TAttribute>? RetrieveMethod<TAttribute>(object target)
            where TAttribute : Attribute
        {
            var key = new Pair<Type, object>(typeof (Pair<MethodInfo, TAttribute>), target);
            object? cached = ReflectionCache.Instance[key];
            if (cached == null)
            {
                MethodInfo[] info = target.GetType().GetMethods();
                foreach (MethodInfo method in info)
                {
                    if (method.IsStatic)
                    {
                        continue;
                    }

                    Attribute? attribute = Attribute.GetCustomAttribute(method, typeof (TAttribute), false);
                    if (attribute == null)
                    {
                        continue;
                    }

                    var data = new Pair<MethodInfo, TAttribute>(method, (TAttribute) attribute);
                    ReflectionCache.Instance[key] = data;
                    return data;
                }

                return null;
            }

            return (Pair<MethodInfo, TAttribute>) cached;
        }

        public static TAttribute? RetrieveMethodAttributeOnly<TAttribute>(object target)
            where TAttribute : Attribute
        {
            var key = new Pair<Type, object>(typeof (TAttribute), target);
            object? cached = ReflectionCache.Instance[key];
            if (cached == null)
            {
                MethodInfo[] info = target.GetType().GetMethods();
                foreach (MethodInfo method in info)
                {
                    if (method.IsStatic)
                    {
                        continue;
                    }

                    Attribute? attribute = Attribute.GetCustomAttribute(method, typeof (TAttribute), false);
                    if (attribute == null)
                    {
                        continue;
                    }

                    var data = (TAttribute) attribute;
                    ReflectionCache.Instance[key] = data;
                    return data;
                }

                return null;
            }

            return (TAttribute) cached;
        }

        public static IList<TAttribute> RetrievePropertyAttributeList<TAttribute>(object target)
            where TAttribute : Attribute
        {
            var key = new Pair<Type, object>(typeof (IList<TAttribute>), target);
            object? cached = ReflectionCache.Instance[key];
            if (cached == null)
            {
                IList<TAttribute> list = new List<TAttribute>();
                PropertyInfo[] info = target.GetType().GetProperties();

                foreach (PropertyInfo property in info)
                {
                    if (property == null || (!property.CanRead || !property.CanWrite))
                    {
                        continue;
                    }

                    MethodInfo? setMethod = property.GetSetMethod();
                    if (setMethod == null || setMethod.IsStatic)
                    {
                        continue;
                    }

                    Attribute? attribute = Attribute.GetCustomAttribute(property, typeof (TAttribute), false);
                    if (attribute != null)
                    {
                        list.Add((TAttribute) attribute);
                    }
                }

                ReflectionCache.Instance[key] = list;
                return list;
            }

            return (IList<TAttribute>) cached;
        }

        public static TAttribute? GetAttribute<TAttribute>()
            where TAttribute : Attribute
        {
            object[] a = AssemblyFromWhichToPullInformation.GetCustomAttributes(typeof (TAttribute), false);
            if (a.Length <= 0)
            {
                return null;
            }

            return (TAttribute) a[0];
        }

        public static Pair<PropertyInfo, TAttribute>? RetrieveOptionProperty<TAttribute>(object target, string uniqueName)
            where TAttribute : BaseOptionAttribute
        {
            var key = new Pair<Type, object>(typeof (Pair<PropertyInfo, BaseOptionAttribute>), target);
            object? cached = ReflectionCache.Instance[key];
            if (cached == null)
            {
                if (target == null)
                {
                    return null;
                }

                PropertyInfo[] propertiesInfo = target.GetType().GetProperties();

                foreach (PropertyInfo property in propertiesInfo)
                {
                    if (property == null || (!property.CanRead || !property.CanWrite))
                    {
                        continue;
                    }

                    MethodInfo? setMethod = property.GetSetMethod();
                    if (setMethod == null || setMethod.IsStatic)
                    {
                        continue;
                    }

                    Attribute? attribute = Attribute.GetCustomAttribute(property, typeof (TAttribute), false);
                    var optionAttr = attribute as TAttribute;
                    if (optionAttr == null || uniqueName != optionAttr.UniqueName)
                    {
                        continue;
                    }

                    var found = new Pair<PropertyInfo, TAttribute>(property, optionAttr);
                    ReflectionCache.Instance[key] = found;
                    return found;
                }
            }

            return cached as Pair<PropertyInfo, TAttribute>;
        }

        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof (Nullable<>);
        }

        #endregion

        #region internal functions

        /// <summary>
        ///     Gets or sets the assembly from which to pull information. Setter provided for testing purpose.
        /// </summary>
        internal static Assembly AssemblyFromWhichToPullInformation { get; set; }

        #endregion
    }
}