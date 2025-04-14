using System.ComponentModel;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core.CustomAttributes
{
    internal class DsCategoryAttribute : CategoryAttribute
    {
        #region construction and destruction

        public DsCategoryAttribute(string categoryResourceName)
            : base(Core.Properties.Resources.ResourceManager.GetString(categoryResourceName, Resources.Culture) ?? "")
        {
        }

        #endregion

        #region protected functions

        protected override string GetLocalizedString(string value)
        {
            return value;
        }

        #endregion
    }
}