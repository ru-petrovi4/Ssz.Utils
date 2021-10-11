using System.Reflection;

namespace System.Linq.Dynamic.Core
{
    internal class DefaultAssemblyHelper : IAssemblyHelper
    {
        public Assembly[] GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }
    }
}
