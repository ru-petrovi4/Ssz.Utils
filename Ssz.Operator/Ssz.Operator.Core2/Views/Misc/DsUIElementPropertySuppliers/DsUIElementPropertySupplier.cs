namespace Ssz.Operator.Core
{
    public abstract class DsUIElementPropertySupplier
    {
        #region public functions

        public const string DefaultTypeString = "Default";
        public const string CustomTypeString = "Custom";
        public const string NoneTypeString = "None";

        public abstract string[] GetTypesStrings();


        public abstract string? GetPropertyXamlString(DsUIElementProperty propertyInfo, IDsContainer? container);

        public abstract string GetTypeString(string propertyXamlString);

        #endregion
    }
}