using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Markup;
using Avalonia.Media;
using Ssz.Utils;
using OwnedDataSerializableAndCloneable = Ssz.Operator.Core.Utils.OwnedDataSerializableAndCloneable;

namespace Ssz.Operator.Core
{
    [TypeConverter(typeof(DsBrushTypeConverter))]
    //[ValueSerializer(typeof(DsBrushValueSerializer))]
    public abstract class DsBrushBase : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region protected functions

        protected abstract Brush? GetBrushInternal();

        #endregion

        #region public functions

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Searchable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public virtual void FindConstants(HashSet<string> constants)
        {
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public Brush? GetBrush(IDsContainer? container)
        {
            if (container is null) return GetBrushInternal();
            var dsBrushClone = (DsBrushBase) Clone();
            dsBrushClone.ReplaceConstants(container);
            return dsBrushClone.GetBrushInternal();
        }


        public static DsBrushBase? GetDsBrush(Brush? brush)
        {
            if (brush is null) return null;
            var solidColorBrush = brush as SolidColorBrush;
            if (solidColorBrush is not null) return new SolidDsBrush(solidColorBrush.Color);
            return new XamlDsBrush(brush);
        }

        #endregion
    }
}