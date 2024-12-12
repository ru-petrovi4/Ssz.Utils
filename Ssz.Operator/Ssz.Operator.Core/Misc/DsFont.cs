using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core
{
    [TypeConverter(typeof(NameValueCollectionTypeConverter<DsFont>))]
    [ValueSerializer(typeof(NameValueCollectionValueSerializer<DsFont>))]
    public class DsFont :
        IDsItem
    {
        #region protected functions

        protected bool Equals(DsFont other)
        {
            return Equals(Family, other.Family) && string.Equals(Size, other.Size) && Equals(Style, other.Style) &&
                   Equals(Stretch, other.Stretch) && Equals(Weight, other.Weight);
        }

        #endregion

        #region public functions

        [DefaultValue(null)] public FontFamily? Family { get; set; }

        [DefaultValue(null)] public string? Size { get; set; }

        [DefaultValue(null)] public FontStyle? Style { get; set; }

        [DefaultValue(null)] public FontStretch? Stretch { get; set; }

        [DefaultValue(null)] public FontWeight? Weight { get; set; }


        public override string ToString()
        {
            var sb = new StringBuilder();

            if (Family is not null) sb.Append(Family);
            if (Size is not null)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(Size);
            }

            if (Style is not null)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(Style);
            }

            if (Stretch is not null)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(Stretch);
            }

            if (Weight is not null)
            {
                if (sb.Length > 0) sb.Append("; ");
                sb.Append(Weight);
            }

            return sb.ToString();
        }

        public override bool Equals(object? obj)
        {
            var other = obj as DsFont;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(Size, constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            Size = ConstantsHelper.ComputeValue(container, Size);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        #endregion
    }
}