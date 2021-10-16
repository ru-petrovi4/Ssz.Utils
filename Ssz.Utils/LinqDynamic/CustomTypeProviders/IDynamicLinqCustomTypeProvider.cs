
using System.Collections.Generic;
using System.Reflection;

namespace System.Linq.Dynamic.Core.CustomTypeProviders
{
    /// <summary>
    /// Interface for providing functionality to find custom types for or resolve any type.
    /// </summary>
    public interface IDynamicLinqCustomTypeProvider
    {
        /// <summary>
        /// Returns a list of custom types that System.Linq.Dynamic.Core will understand.
        /// </summary>
        /// <returns>A <see cref="HashSet{Type}" /> list of custom types.</returns>
        HashSet<Type> GetCustomTypes();

        /// <summary>
        /// Returns a list of custom extension methods that System.Linq.Dynamic.Core will understand.
        /// </summary>
        /// <returns>A list of custom extension methods that System.Linq.Dynamic.Core will understand.</returns>
        Dictionary<Type, List<MethodInfo>> GetExtensionMethodsFromCustomTypes();
    }
}
