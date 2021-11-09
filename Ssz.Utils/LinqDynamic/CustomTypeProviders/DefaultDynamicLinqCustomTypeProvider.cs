using System.Collections.Generic;
using System.Linq.Dynamic.Core.Parser;
using System.Linq.Dynamic.Core.Validation;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Linq.Dynamic.Core.CustomTypeProviders
{
    /// <summary>
    /// The default implementation for <see cref="IDynamicLinqCustomTypeProvider"/>.
    /// 
    /// Scans the current AppDomain for all types marked with <see cref="DynamicLinqTypeAttribute"/>, and adds them as custom Dynamic Link types.
    ///
    /// Also provides functionality to resolve a Type in the current Application Domain.
    ///
    /// This class is used as default for full .NET Framework, so not for .NET Core
    /// </summary>
    public class DefaultDynamicLinqCustomTypeProvider : IDynamicLinqCustomTypeProvider
    {
        private readonly IAssemblyHelper _assemblyHelper = new DefaultAssemblyHelper();
        /// <summary>
        ///     Defines whether to cache the CustomTypes (including extension methods) which are found in the Application Domain.
        /// </summary>
        private readonly bool _useCache = true;

        private HashSet<Type>? _cachedCustomTypes;
        private Dictionary<Type, List<MethodInfo>>? _cachedExtensionMethods;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultDynamicLinqCustomTypeProvider"/> class.
        /// </summary>        
        public DefaultDynamicLinqCustomTypeProvider()
        {
        }

        /// <inheritdoc cref="IDynamicLinqCustomTypeProvider.GetCustomTypes"/>
        public virtual HashSet<Type> GetCustomTypes()
        {
            if (_useCache)
            {
                if (_cachedCustomTypes is null)
                {
                    _cachedCustomTypes = GetCustomTypesInternal();
                }

                return _cachedCustomTypes;
            }

            return GetCustomTypesInternal();
        }

        /// <inheritdoc cref="IDynamicLinqCustomTypeProvider.GetExtensionMethodsFromCustomTypes"/>
        public Dictionary<Type, List<MethodInfo>> GetExtensionMethodsFromCustomTypes()
        {
            if (_useCache)
            {
                if (_cachedExtensionMethods is null)
                {
                    _cachedExtensionMethods = GetExtensionMethodsFromCustomTypesInternal();
                }

                return _cachedExtensionMethods;
            }

            return GetExtensionMethodsFromCustomTypesInternal();
        }        

        private HashSet<Type> GetCustomTypesInternal()
        {
            IEnumerable<Assembly> assemblies = _assemblyHelper.GetAssemblies().Where(a => !a.IsDynamic);
            return new HashSet<Type>(TypeHelper.GetAssemblyTypesWithDynamicLinqTypeAttribute(assemblies).Distinct());
        }

        private Dictionary<Type, List<MethodInfo>> GetExtensionMethodsFromCustomTypesInternal()
        {
            var types = GetCustomTypes();

            List<Tuple<Type, MethodInfo>> list = new List<Tuple<Type, MethodInfo>>();

            foreach (var type in types)
            {
                var extensionMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                    .Where(x => x.IsDefined(typeof(ExtensionAttribute), false)).ToList();

                extensionMethods.ForEach(x => list.Add(new Tuple<Type, MethodInfo>(x.GetParameters()[0].ParameterType, x)));
            }

            return list.GroupBy(x => x.Item1, tuple => tuple.Item2).ToDictionary(key => key.Key, methods => methods.ToList());
        }
    }
}
