
using System.Collections.Generic;
using System.Linq.Dynamic.Core.Validation;
using System.Linq.Expressions;
using System.Reflection;

namespace System.Linq.Dynamic.Core.Parser
{
    internal class TypeFinder : ITypeFinder
    {
        private readonly IAssemblyHelper _assemblyHelper = new DefaultAssemblyHelper();

        private readonly IKeywordsHelper _keywordsHelper;
        private readonly ParsingConfig _parsingConfig;

        public TypeFinder(ParsingConfig parsingConfig, IKeywordsHelper keywordsHelper)
        {
            // Check.NotNull(parsingConfig, nameof(parsingConfig));
            // Check.NotNull(keywordsHelper, nameof(keywordsHelper));

            _keywordsHelper = keywordsHelper;
            _parsingConfig = parsingConfig;
        }        

        public Type? FindTypeByName(string typeName, ParameterExpression?[]? expressions, bool allowToUseAnyType)
        {
            Check.NotEmpty(typeName, nameof(typeName));

            _keywordsHelper.TryGetValue(typeName, out object? type);

            Type? result = type as Type;
            if (result != null)
            {
                return result;
            }

            if (expressions != null && TryResolveTypeUsingExpressions(typeName, expressions, out result))
            {
                return result;
            }

            if (allowToUseAnyType)
            {
                Type? resolvedType = ResolveType(typeName);
                if (resolvedType != null)
                {
                    return resolvedType;
                }

                // In case the type is not found based on fullname, try to get the type on simplename if allowed
                if (_parsingConfig.ResolveTypesBySimpleName)
                {
                    IEnumerable<Assembly> assemblies = _assemblyHelper.GetAssemblies();
                    return TypeHelper.ResolveTypeBySimpleName(assemblies, typeName);                    
                }
            }
                
            return null;
        }

        /// <summary>
        /// Resolve any type by fullname which is registered in the current application domain.
        /// </summary>
        /// <param name="typeFullName">The typename to resolve.</param>
        /// <returns>A resolved <see cref="Type"/> or null when not found.</returns>
        private Type? ResolveType(string typeFullName)
        {
            Check.NotEmpty(typeFullName, nameof(typeFullName));

            IEnumerable<Assembly> assemblies = _assemblyHelper.GetAssemblies();
            return TypeHelper.ResolveType(assemblies, typeFullName);
        }          

        private bool TryResolveTypeUsingExpressions(string name, ParameterExpression?[] expressions, out Type? result)
        {
            foreach (var expression in expressions.OfType<ParameterExpression>())
            {
                if (name == expression.Type.Name)
                {
                    result = expression.Type;
                    return true;
                }

                if (name == $"{expression.Type.Namespace}.{expression.Type.Name}")
                {
                    result = expression.Type;
                    return true;
                }

                if (_parsingConfig.ResolveTypesBySimpleName)
                {
                    string possibleFullName = $"{expression.Type.Namespace}.{name}";
                    var resolvedType = ResolveType(possibleFullName);
                    if (resolvedType != null)
                    {
                        result = resolvedType;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }
    }
}
