using System.ComponentModel;
using Ssz.Operator.Core.Properties;

namespace Ssz.Operator.Core.CustomAttributes
{
    internal class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        #region construction and destruction

        public LocalizedDescriptionAttribute(string resourceName)
        {
            Description = Resources.ResourceManager.GetString(resourceName) ?? "";
        }

        #endregion

        #region public functions

        public override string Description { get; }

        #endregion

        #region private fields

        #endregion
    }
}