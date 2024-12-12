using System.ComponentModel;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core.CustomAttributes
{
    internal class DsDisplayNameAttribute : DisplayNameAttribute
    {
        #region construction and destruction

        public DsDisplayNameAttribute(string resourceName)
        {
            DisplayName = Resources.ResourceManager.GetString(resourceName) ?? "";
        }

        #endregion

        #region public functions

        public override string DisplayName { get; }

        #endregion
    }
}