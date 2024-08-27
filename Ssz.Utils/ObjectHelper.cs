using System; using System.Collections; using System.Collections.Generic; using System.ComponentModel; using System.Linq; using System.Linq.Dynamic.Core; using System.Linq.Expressions; using System.Reflection; using System.Text.RegularExpressions;  namespace Ssz.Utils {     /// <summary>     ///      /// </summary>     public static class ObjectHelper     {         #region public functions          /// <summary>         ///     Returns all fields except that with Searchable(false) attribute.                 /// </summary>         /// <param name="obj"></param>         /// <returns></returns>         public static IEnumerable<FieldInfo> GetAllFields(object obj)         {             Type? type = obj.GetType();             var fields = new List<FieldInfo>();             while (type is not null)             {                 foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.GetField | BindingFlags.Instance | BindingFlags.Public |                                    BindingFlags.NonPublic))                 {                     SearchableAttribute? searchableAttribute = Attribute.GetCustomAttribute(fieldInfo, typeof(SearchableAttribute)) as SearchableAttribute;                     if (searchableAttribute is not null)                     {                         if (searchableAttribute.Searchable) fields.Add(fieldInfo);                         continue;                     }                      fields.Add(fieldInfo);                 }                  type = type.BaseType;             }             return fields;         }          /// <summary>         ///     Returns value of 'obj' concatenated with expression.         ///     If ivalid expression returns null.         ///     expression examples: ".Name", ".SubObjects[0].Name"                 /// </summary>         /// <param name="obj"></param>         /// <param name="expression"></param>         /// <returns></returns>         public static object? GetValue(object obj, string? expression)         {             if (String.IsNullOrEmpty(expression)) return null;              try             {                 ParameterExpression pObj = Expression.Parameter(obj.GetType(), @"obj");                 LambdaExpression e = DynamicExpressionParser.ParseLambda(new[] { pObj },                     null,                     @"obj" + expression);                 Delegate d = e.Compile();                 return d.DynamicInvoke(obj);             }             catch (Exception)             {                 return null;             }         }          /// <summary>         ///     Sets value of 'obj' concatenated with expression.         ///     Returns True if succeeded.         ///     expression examples: ".Name", ".SubObject.Name"                 /// </summary>         /// <param name="obj"></param>         /// <param name="expression"></param>         /// <param name="value"></param>         /// <returns></returns>         public static bool SetValue(object obj, string? expression, object? value)         {             if (String.IsNullOrEmpty(expression)) return false;              object o = obj;             string[] parts = expression!.Split(new[] { '.' }, StringSplitOptions.None);             if (parts.Length < 2 || !String.IsNullOrEmpty(parts[0])) return false;             for (int i = 1; i < parts.Length - 1; i++)             {                 Type t = o.GetType();                 PropertyInfo? pi = t.GetProperty(parts[i]);                 if (pi is null || !pi.CanRead) return false;                 object? v = pi.GetValue(o);                 if (v is null)                 {                     if (!pi.CanWrite) return false;                     try                     {                         v = Activator.CreateInstance(pi.PropertyType);                     }                     catch                     {                         return false;                     }                     if (v is null) return false;                     pi.SetValue(o, v);                 }                 o = v;             }              return SetPropertyValue(o, parts[parts.Length - 1], value);         }          /// <summary>         ///     Returns True if succeeded.         /// </summary>         /// <param name="obj"></param>         /// <param name="propertyName"></param>         /// <param name="value"></param>         /// <returns></returns>         public static bool SetPropertyValue(object obj, string? propertyName, object? value)         {             try             {                 Type t = obj.GetType();                 PropertyInfo? pi;                                if (String.IsNullOrEmpty(propertyName))
                {
                    DefaultPropertyAttribute? defaultPropertyAttribute = t.GetCustomAttribute<DefaultPropertyAttribute>();                     if (String.IsNullOrEmpty(defaultPropertyAttribute?.Name))                         return false;
                    pi = t.GetProperty(defaultPropertyAttribute!.Name!);
                }                 else                 {
                    pi = t.GetProperty(propertyName);
                }                  if (pi is null || !pi.CanWrite)                      return false;                  if (value is null)                 {                                         pi.SetValue(obj, null);                 }                                             else                 {                     pi.SetValue(obj, new Any(value).ValueAs(pi.PropertyType, false));                 }                 return true;             }             catch (Exception)             {                 return false;             }         }          /// <summary>         ///     Searches in properties with [Searchable(true)] or [Browsable(true)] attributes.         ///     [Searchable(true)] attribute has higher priority.         ///     If regex is null matches all properties.                 /// </summary>         public static List<StringPropertyInfo> FindInStringBrowsableProperties(object obj, Regex? regex)         {             var result = new List<StringPropertyInfo>();              foreach (PropertyDescriptor propertyDescriptor in GetProperties(obj))             {                 object? value = propertyDescriptor.GetValue(obj);                  if (value is null) continue;                  var stringValue = value as string;                 if (stringValue is not null)                 {                     bool match = regex is not null ? regex.Match(stringValue).Success : true;                     if (match)                     {                         result.Add(new StringPropertyInfo                         {                             PropertyPath = propertyDescriptor.Name,                             PropertyValue = stringValue,                         });                     }                     continue;                 }                  var listValue = value as IList;                 if (listValue is not null)                 {                     for (int i = 0; i < listValue.Count; i++)                     {                         object? item = listValue[i];                         if (item is null) continue;                         foreach (                             StringPropertyInfo subPropertyInfo in                                 FindInStringBrowsableProperties(item, regex))                         {                             subPropertyInfo.PropertyPath = propertyDescriptor.Name + @"[" + i + @"]." +                                                            subPropertyInfo.PropertyPath;                             result.Add(subPropertyInfo);                         }                     }                     continue;                 }                  foreach (StringPropertyInfo subPropertyInfo in                     FindInStringBrowsableProperties(value, regex))                 {                     subPropertyInfo.PropertyPath = propertyDescriptor.Name + @"." +                                                    subPropertyInfo.PropertyPath;                     result.Add(subPropertyInfo);                 }             }              return result;         }          /// <summary>         ///     Replaces in properties with [Searchable(true)] or [Browsable(true)] attributes.         ///     [Searchable(true)] attribute has higher priority.                 /// </summary>         public static List<StringPropertyInfo> ReplaceInStringBrowsableProperties(object obj, Regex regex,             string replacement)         {             if (replacement is null) replacement = @"";              var result = new List<StringPropertyInfo>();              foreach (PropertyDescriptor propertyDescriptor in GetProperties(obj))             {                 object? value = propertyDescriptor.GetValue(obj);                  if (value is null) continue;                  var stringValue = value as string;                 if (stringValue is not null)                 {                     if (!propertyDescriptor.IsReadOnly)                     {                         string newStringValue = regex.Replace(stringValue, replacement);                         if (newStringValue != stringValue)                         {                             propertyDescriptor.SetValue(obj, newStringValue);                             result.Add(new StringPropertyInfo                             {                                 PropertyPath = propertyDescriptor.Name,                                 PropertyValue = newStringValue,                             });                         }                     }                     continue;                 }                  var listValue = value as IList;                 if (listValue is not null)                 {                     for (int i = 0; i < listValue.Count; i++)                     {                         object? item = listValue[i];                         if (item is not null)                         {                             foreach (                             StringPropertyInfo subPropertyInfo in                                 ReplaceInStringBrowsableProperties(item, regex, replacement))                             {                                 subPropertyInfo.PropertyPath = propertyDescriptor.Name + @"[" + i + @"]." +                                                                subPropertyInfo.PropertyPath;                                 result.Add(subPropertyInfo);                             }                         }                                             }                     continue;                 }                  foreach (StringPropertyInfo subPropertyInfo in                     ReplaceInStringBrowsableProperties(value, regex, replacement))                 {                     subPropertyInfo.PropertyPath = propertyDescriptor.Name + @"." +                                                     subPropertyInfo.PropertyPath;                     result.Add(subPropertyInfo);                 }             }              return result;         }

        /// <summary>
        /// Search for a method by name and parameter types.  
        /// Unlike GetMethod(), does 'loose' matching on generic
        /// parameter types, and searches base interfaces.
        /// </summary>
        /// <exception cref="AmbiguousMatchException"/>
        public static MethodInfo? GetMethodExt(Type thisType,
                                                string name,
                                                params Type[] parameterTypes)
        {
            return GetMethodExt(thisType,
                                name,
                                BindingFlags.Instance
                                | BindingFlags.Static
                                | BindingFlags.Public
                                | BindingFlags.NonPublic
                                | BindingFlags.FlattenHierarchy,
                                parameterTypes);
        }

        /// <summary>
        /// Search for a method by name, parameter types, and binding flags.  
        /// Unlike GetMethod(), does 'loose' matching on generic
        /// parameter types, and searches base interfaces.
        /// </summary>
        /// <exception cref="AmbiguousMatchException"/>
        public static MethodInfo? GetMethodExt(Type thisType,
                                                string name,
                                                BindingFlags bindingFlags,
                                                params Type[] parameterTypes)
        {
            MethodInfo? matchingMethod = null;

            // Check all methods with the specified name, including in base classes
            GetMethodExt(ref matchingMethod, thisType, name, bindingFlags, parameterTypes);

            // If we're searching an interface, we have to manually search base interfaces
            if (matchingMethod == null && thisType.IsInterface)
            {
                foreach (Type interfaceType in thisType.GetInterfaces())
                    GetMethodExt(ref matchingMethod,
                                 interfaceType,
                                 name,
                                 bindingFlags,
                                 parameterTypes);
            }

            return matchingMethod;
        }

        private static void GetMethodExt(ref MethodInfo? matchingMethod,
                                            Type type,
                                            string name,
                                            BindingFlags bindingFlags,
                                            params Type[] parameterTypes)
        {
            // Check all methods with the specified name, including in base classes
            foreach (MethodInfo methodInfo in type.GetMember(name,
                                                             MemberTypes.Method,
                                                             bindingFlags))
            {
                // Check that the parameter counts and types match, 
                // with 'loose' matching on generic parameters
                ParameterInfo[] parameterInfos = methodInfo.GetParameters();
                if (parameterInfos.Length == parameterTypes.Length)
                {
                    int i = 0;
                    for (; i < parameterInfos.Length; ++i)
                    {
                        if (!IsSimilarType(parameterInfos[i].ParameterType, parameterTypes[i]))
                            break;
                    }
                    if (i == parameterInfos.Length)
                    {
                        if (matchingMethod == null)
                            matchingMethod = methodInfo;
                        else
                            throw new AmbiguousMatchException(
                                   "More than one matching method found!");
                    }
                }
            }
        }

        /// <summary>
        /// Special type used to match any generic parameter type in GetMethodExt().
        /// </summary>
        public class T
        { }

        /// <summary>
        /// Determines if the two types are either identical, or are both generic 
        /// parameters or generic types with generic parameters in the same
        ///  locations (generic parameters match any other generic paramter,
        /// but NOT concrete types).
        /// </summary>
        public static bool IsSimilarType(Type thisType, Type type)
        {
            // Ignore any 'ref' types
            if (thisType.IsByRef)
                thisType = thisType.GetElementType()!;
            if (type.IsByRef)
                type = type.GetElementType()!;

            // Handle array types
            if (thisType.IsArray && type.IsArray)
                return IsSimilarType(thisType.GetElementType()!, type.GetElementType()!);

            // If the types are identical, or they're both generic parameters 
            // or the special 'T' type, treat as a match
            if (thisType == type || ((thisType.IsGenericParameter || thisType == typeof(T)) // (!thisType.IsGenericType && !type.IsGenericType && thisType.Name == type.Name && thisType.Namespace == type.Namespace)
                                 && (type.IsGenericParameter || type == typeof(T))))
                return true;

            // Handle any generic arguments
            if (thisType.IsGenericType && type.IsGenericType)
            {
                Type[] thisArguments = thisType.GetGenericArguments();
                Type[] arguments = type.GetGenericArguments();
                if (thisArguments.Length == arguments.Length)
                {
                    for (int i = 0; i < thisArguments.Length; ++i)
                    {
                        if (!IsSimilarType(thisArguments[i], arguments[i]))
                            return false;
                    }
                    return true;
                }
            }

            return false;
        }

        #endregion 
        #region private functions 
        /// <summary>         ///     Returns all Browsable properties of object.         ///     SearchableAttribute can explicitly set whether to return or not the property.                 /// </summary>         private static IEnumerable<PropertyDescriptor> GetProperties(object obj)         {             var result = new List<PropertyDescriptor>();             foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(obj)                 .OfType<PropertyDescriptor>())             {                 SearchableAttribute? searchableAttribute =                     propertyDescriptor.Attributes.OfType<SearchableAttribute>().FirstOrDefault();                 if (searchableAttribute is not null)                 {                     if (searchableAttribute.Searchable) result.Add(propertyDescriptor);                     continue;                 }                 if (propertyDescriptor.IsBrowsable)                 {                     result.Add(propertyDescriptor);                 }             }             return result;         }          #endregion     }      /// <summary>     ///      /// </summary>     public class StringPropertyInfo     {         #region public functions          /// <summary>         ///     Without '.' at the beginning.         /// </summary>         public string PropertyPath = "";          /// <summary>         ///          /// </summary>         public string PropertyValue = "";          #endregion     }      /// <summary>     ///     For find and replace in string properties support.     /// </summary>     public sealed class SearchableAttribute : Attribute     {         #region construction and destruction          /// <summary>         ///          /// </summary>         /// <param name="searchable"></param>         public SearchableAttribute(bool searchable)         {             Searchable = searchable;         }          #endregion          #region public functions          /// <summary>         ///          /// </summary>         public bool Searchable { get; private set; }          #endregion     } }