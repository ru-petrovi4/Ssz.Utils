﻿#region License

// <copyright file="ReflectionHelper.cs" company="Giacomo Stelluti Scala">
//   Copyright 2015-2013 Giacomo Stelluti Scala
// </copyright>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#endregion

#region Using Directives

using System;
using System.Collections.Generic;
using System.Reflection;
using Ssz.Utils.Net4.CommandLine.Attributes;

#endregion

namespace Ssz.Utils.Net4.CommandLine.Infrastructure
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
            object cached = ReflectionCache.Instance[key];
            if (cached is null)
            {
                IList<Pair<PropertyInfo, TAttribute>> list = new List<Pair<PropertyInfo, TAttribute>>();
                if (target != null)
                {
                    PropertyInfo[] propertiesInfo = target.GetType().GetProperties();

                    foreach (PropertyInfo property in propertiesInfo)
                    {
                        if (property is null || (!property.CanRead || !property.CanWrite))
                        {
                            continue;
                        }

                        MethodInfo setMethod = property.GetSetMethod();
                        if (setMethod is null || setMethod.IsStatic)
                        {
                            continue;
                        }

                        Attribute attribute = Attribute.GetCustomAttribute(property, typeof (TAttribute), false);
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

        public static Pair<MethodInfo, TAttribute> RetrieveMethod<TAttribute>(object target)
            where TAttribute : Attribute
        {
            var key = new Pair<Type, object>(typeof (Pair<MethodInfo, TAttribute>), target);
            object cached = ReflectionCache.Instance[key];
            if (cached is null)
            {
                MethodInfo[] info = target.GetType().GetMethods();
                foreach (MethodInfo method in info)
                {
                    if (method.IsStatic)
                    {
                        continue;
                    }

                    Attribute attribute = Attribute.GetCustomAttribute(method, typeof (TAttribute), false);
                    if (attribute is null)
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

        public static TAttribute RetrieveMethodAttributeOnly<TAttribute>(object target)
            where TAttribute : Attribute
        {
            var key = new Pair<Type, object>(typeof (TAttribute), target);
            object cached = ReflectionCache.Instance[key];
            if (cached is null)
            {
                MethodInfo[] info = target.GetType().GetMethods();
                foreach (MethodInfo method in info)
                {
                    if (method.IsStatic)
                    {
                        continue;
                    }

                    Attribute attribute = Attribute.GetCustomAttribute(method, typeof (TAttribute), false);
                    if (attribute is null)
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
            object cached = ReflectionCache.Instance[key];
            if (cached is null)
            {
                IList<TAttribute> list = new List<TAttribute>();
                PropertyInfo[] info = target.GetType().GetProperties();

                foreach (PropertyInfo property in info)
                {
                    if (property is null || (!property.CanRead || !property.CanWrite))
                    {
                        continue;
                    }

                    MethodInfo setMethod = property.GetSetMethod();
                    if (setMethod is null || setMethod.IsStatic)
                    {
                        continue;
                    }

                    Attribute attribute = Attribute.GetCustomAttribute(property, typeof (TAttribute), false);
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

        public static TAttribute GetAttribute<TAttribute>()
            where TAttribute : Attribute
        {
            object[] a = AssemblyFromWhichToPullInformation.GetCustomAttributes(typeof (TAttribute), false);
            if (a.Length <= 0)
            {
                return null;
            }

            return (TAttribute) a[0];
        }

        public static Pair<PropertyInfo, TAttribute> RetrieveOptionProperty<TAttribute>(object target, string uniqueName)
            where TAttribute : BaseOptionAttribute
        {
            var key = new Pair<Type, object>(typeof (Pair<PropertyInfo, BaseOptionAttribute>), target);
            object cached = ReflectionCache.Instance[key];
            if (cached is null)
            {
                if (target is null)
                {
                    return null;
                }

                PropertyInfo[] propertiesInfo = target.GetType().GetProperties();

                foreach (PropertyInfo property in propertiesInfo)
                {
                    if (property is null || (!property.CanRead || !property.CanWrite))
                    {
                        continue;
                    }

                    MethodInfo setMethod = property.GetSetMethod();
                    if (setMethod is null || setMethod.IsStatic)
                    {
                        continue;
                    }

                    Attribute attribute = Attribute.GetCustomAttribute(property, typeof (TAttribute), false);
                    var optionAttr = (TAttribute) attribute;
                    if (optionAttr is null || uniqueName != optionAttr.UniqueName)
                    {
                        continue;
                    }

                    var found = new Pair<PropertyInfo, TAttribute>(property, (TAttribute) attribute);
                    ReflectionCache.Instance[key] = found;
                    return found;
                }
            }

            return (Pair<PropertyInfo, TAttribute>) cached;
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