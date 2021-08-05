using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Ssz.Utils
{
    public static class ObjectExtensions
    {
        #region public functions

        /// <summary>
        ///     Returns all fields except that with Searchable(false) attribute.
        ///     obj != null, result != null
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IEnumerable<FieldInfo> GetAllFields(this object obj)
        {
            if (obj == null) throw new ArgumentNullException(@"obj");

            Type type = obj.GetType();
            var fields = new List<FieldInfo>();
            while (type != null)
            {
                foreach (var fieldInfo in type.GetFields(BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Public |
                                   BindingFlags.NonPublic))
                {
                    SearchableAttribute searchableAttribute = Attribute.GetCustomAttribute(fieldInfo, typeof(SearchableAttribute)) as SearchableAttribute;
                    if (searchableAttribute != null)
                    {
                        if (searchableAttribute.Searchable) fields.Add(fieldInfo);
                        continue;
                    }

                    fields.Add(fieldInfo);
                }

                type = type.BaseType;
            }
            return fields;
        }        

        /// <summary>
        ///     Returns value of 'obj' concatenated with expression.
        ///     If ivalid expression returns null.
        ///     expression examples: ".Name", ".SubObjects[0].Name"
        ///     obj != null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static object GetValue(this object obj, string expression)
        {
            if (obj == null) throw new ArgumentNullException(@"obj");

            if (String.IsNullOrEmpty(expression)) return null;

            try
            {
                ParameterExpression pObj = Expression.Parameter(obj.GetType(), @"obj");
                LambdaExpression e = DynamicExpression.ParseLambda(new[] { pObj },
                    null,
                    @"obj" + expression);
                Delegate d = e.Compile();
                return d.DynamicInvoke(obj);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        ///     Sets value of 'obj' concatenated with expression.
        ///     Returns True if succeeded.
        ///     expression examples: ".Name", ".SubObjects[0].Name"
        ///     obj != null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="expression"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetValue(this object obj, string expression, object value)
        {
            if (obj == null) throw new ArgumentNullException(@"obj");

            if (String.IsNullOrEmpty(expression)) return false;
            
            object o = obj;
            string[] parts = expression.Split(new [] { '.' }, StringSplitOptions.None);
            if (parts.Length < 2 || !String.IsNullOrEmpty(parts[0])) return false;
            for (int i = 1; i < parts.Length - 1; i++)
            {
                Type t = o.GetType();
                PropertyInfo pi = t.GetProperty(parts[i]);
                if (pi == null || !pi.CanRead) return false;
                object v = pi.GetValue(o);                
                if (v == null)
                {      
                    if (!pi.CanWrite) return false;
                    try
                    {
                        v = Activator.CreateInstance(pi.PropertyType);
                    }
                    catch
                    {
                        return false;
                    }
                    if (v == null) return false;
                    pi.SetValue(o, v);
                }
                o = v;
            }

            return SetPropertyValue(o, parts[parts.Length - 1], value);
        }

        /// <summary>
        ///     Returns True if succeeded.        
        ///     obj != null
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool SetPropertyValue(this object obj, string propertyName, object value)
        {
            if (obj == null) throw new ArgumentNullException(@"obj");

            if (String.IsNullOrEmpty(propertyName)) return false;

            try
            {
                Type t = obj.GetType();
                PropertyInfo pi = t.GetProperty(propertyName);
                if (pi == null || !pi.CanWrite) return false;
                if (value == null)
                {
                    pi.SetValue(obj, null);
                }
                else
                {
                    pi.SetValue(obj, Any.ConvertTo(value, pi.PropertyType, false));
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        ///     Searches in properties with [Searchable(true)] or [Browsable(true)] attributes.
        ///     [Searchable(true)] attribute has higher priority.
        ///     If regex == null matches all properties.
        ///     obj != null, result != null
        /// </summary>
        public static List<StringPropertyInfo> FindInStringBrowsableProperties(this object obj, Regex regex)
        {
            if (obj == null) throw new ArgumentNullException(@"obj");

            var result = new List<StringPropertyInfo>();

            foreach (PropertyDescriptor propertyDescriptor in GetProperties(obj))
            {
                object value = propertyDescriptor.GetValue(obj);

                if (value == null) continue;

                var stringValue = value as string;
                if (stringValue != null)
                {
                    bool match = regex != null ? regex.Match(stringValue).Success : true;
                    if (match)
                    {
                        result.Add(new StringPropertyInfo
                        {
                            PropertyPath = propertyDescriptor.Name,
                            PropertyValue = stringValue,
                        });
                    }
                    continue;
                }

                var listValue = value as IList;
                if (listValue != null)
                {
                    for (int i = 0; i < listValue.Count; i++)
                    {
                        object item = listValue[i];
                        if (item == null) continue;
                        foreach (
                            StringPropertyInfo subPropertyInfo in
                                FindInStringBrowsableProperties(item, regex))
                        {
                            subPropertyInfo.PropertyPath = propertyDescriptor.Name + @"[" + i + @"]." +
                                                           subPropertyInfo.PropertyPath;
                            result.Add(subPropertyInfo);
                        }
                    }
                    continue;
                }

                foreach (StringPropertyInfo subPropertyInfo in
                    FindInStringBrowsableProperties(value, regex))
                {
                    subPropertyInfo.PropertyPath = propertyDescriptor.Name + @"." +
                                                   subPropertyInfo.PropertyPath;
                    result.Add(subPropertyInfo);
                }
            }

            return result;
        }

        /// <summary>
        ///     Replaces in properties with [Searchable(true)] or [Browsable(true)] attributes.
        ///     [Searchable(true)] attribute has higher priority.
        ///     obj != null, regex != null
        /// </summary>
        public static List<StringPropertyInfo> ReplaceInStringBrowsableProperties(this object obj, Regex regex,
            string replacement)
        {
            if (obj == null) throw new ArgumentNullException(@"obj");
            if (regex == null) throw new ArgumentNullException(@"regex");

            if (replacement == null) replacement = @"";

            var result = new List<StringPropertyInfo>();

            foreach (PropertyDescriptor propertyDescriptor in GetProperties(obj))
            {
                object value = propertyDescriptor.GetValue(obj);

                if (value == null) continue;

                var stringValue = value as string;
                if (stringValue != null)
                {
                    if (!propertyDescriptor.IsReadOnly)
                    {
                        string newStringValue = regex.Replace(stringValue, replacement);
                        if (newStringValue != stringValue)
                        {
                            propertyDescriptor.SetValue(obj, newStringValue);
                            result.Add(new StringPropertyInfo
                            {
                                PropertyPath = propertyDescriptor.Name,
                                PropertyValue = newStringValue,
                            });
                        }
                    }
                    continue;
                }

                var listValue = value as IList;
                if (listValue != null)
                {
                    for (int i = 0; i < listValue.Count; i++)
                    {
                        object item = listValue[i];
                        foreach (
                            StringPropertyInfo subPropertyInfo in
                                ReplaceInStringBrowsableProperties(item, regex, replacement))
                        {
                            subPropertyInfo.PropertyPath = propertyDescriptor.Name + @"[" + i + @"]." +
                                                           subPropertyInfo.PropertyPath;
                            result.Add(subPropertyInfo);
                        }
                    }
                    continue;
                }

                foreach (StringPropertyInfo subPropertyInfo in
                    ReplaceInStringBrowsableProperties(value, regex, replacement))
                {
                    subPropertyInfo.PropertyPath = propertyDescriptor.Name + @"." +
                                                    subPropertyInfo.PropertyPath;
                    result.Add(subPropertyInfo);
                }
            }

            return result;
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Returns all Browsable properties of object.
        ///     SearchableAttribute can explicitly set whether to return or not the property.        
        /// </summary>
        private static IEnumerable<PropertyDescriptor> GetProperties(object obj)
        {
            var result = new List<PropertyDescriptor>();
            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(obj)
                .OfType<PropertyDescriptor>())
            {
                SearchableAttribute searchableAttribute =
                    propertyDescriptor.Attributes.OfType<SearchableAttribute>().FirstOrDefault();
                if (searchableAttribute != null)
                {
                    if (searchableAttribute.Searchable) result.Add(propertyDescriptor);
                    continue;
                }
                if (propertyDescriptor.IsBrowsable)
                {
                    result.Add(propertyDescriptor);
                }
            }
            return result;
        }

        #endregion
    }

    public class StringPropertyInfo
    {
        #region public functions

        /// <summary>
        ///     Without '.' at the beginning.
        /// </summary>
        public string PropertyPath;

        public string PropertyValue;

        #endregion
    }

    /// <summary>
    ///     For find and replace in string properties support.
    /// </summary>
    public sealed class SearchableAttribute : Attribute
    {
        #region construction and destruction

        public SearchableAttribute(bool searchable)
        {
            Searchable = searchable;
        }

        #endregion

        #region public functions

        public bool Searchable { get; private set; }

        #endregion
    }
}