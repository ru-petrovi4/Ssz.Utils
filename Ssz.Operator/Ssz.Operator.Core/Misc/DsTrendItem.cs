using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;


using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using Ssz.Utils;

namespace Ssz.Operator.Core.DsShapes.Trends
{
    public class DsTrendItem :
        OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region construction and destruction

        public DsTrendItem() // For XAML serialization
            : this(true)
        {
        }

        public DsTrendItem(bool visualDesignMode)
        {
            TagName = "";
            TagType = "";
            PropertyPath = "";            
            ValueFormatInfo = new TextDataBinding(visualDesignMode, true) { ConstValue = @"" };
            DescriptionInfo = new TextDataBinding(visualDesignMode, true) { ConstValue = @"" };
            DsBrush = new BrushDataBinding(visualDesignMode, true)
            {
                ConstValue = new SolidDsBrush
                {
                    Color = Colors.White
                }
            };
        }

        /// <summary>
        ///     Makes ParentItem = GenericContainer
        /// </summary>
        /// <param name="hdaId"></param>
        public DsTrendItem(string hdaId) :
            this()
        {
            var s = hdaId.Split(new[] {'.'});
            if (s.Length >= 1)
                TagName = s[0];
            if (s.Length >= 2)
                PropertyPath = '.' + s[1];

            var parentContainer = new GenericContainer();
            parentContainer.ParentItem = DsProject.Instance;
            parentContainer.DsConstantsCollection.Add(new DsConstant
            {
                Name = DataEngineBase.TagConstant,
                Value = TagName
            });
            
            ParentItem = parentContainer;
        }

        public DsTrendItem(string hdaId, Color plotLineColor) :
            this(hdaId)
        {
            Color = plotLineColor;
        }

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.DsTrendItemTag)]
        [PropertyOrder(1)]
        public string TagName { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoTagType)]
        [PropertyOrder(2)]
        public string TagType { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfoPropertyPath)]
        [ItemsSource(typeof(PropertyPathsItemsSource), true)]
        [PropertyOrder(3)]
        public string PropertyPath { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.DsTrendItem_ValueFormat)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(4)]
        public TextDataBinding ValueFormatInfo { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.DsTrendItemHdaId)]
        [PropertyOrder(5)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        // For XAML serialization of collections
        public string HdaId => TagName + PropertyPath;

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.ModelTagPropertyInfo_DescriptionInfo)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        [PropertyOrder(6)]
        public TextDataBinding DescriptionInfo { get; set; }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.DsTrendItemColor)]
        [PropertyOrder(7)]
        [ExpandableObject]
        [ValuePropertyPath(@"ConstValue")]
        [IsValueEditorEnabledPropertyPath(@"IsConst")]
        [Editor(typeof(SolidBrushTypeEditor), typeof(SolidBrushTypeEditor))]
        public BrushDataBinding DsBrush { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Color Color
        {
            set =>
                DsBrush.ConstValue = new SolidDsBrush
                {
                    Color = value
                };
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

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(6))
            {
                writer.Write(TagName);
                writer.Write(TagType);
                writer.Write(PropertyPath);
                writer.Write(ValueFormatInfo, context);
                writer.Write(DescriptionInfo, context);
                writer.Write(DsBrush, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 5:
                        try
                        {
                            TagName = reader.ReadString();
                            TagType = reader.ReadString();
                            PropertyPath = reader.ReadString();
                            reader.ReadOwnedData(DescriptionInfo, context);
                            reader.ReadOwnedData(DsBrush, context);
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    case 6:
                        try
                        {
                            TagName = reader.ReadString();
                            TagType = reader.ReadString();
                            PropertyPath = reader.ReadString();
                            reader.ReadOwnedData(ValueFormatInfo, context);
                            reader.ReadOwnedData(DescriptionInfo, context);
                            reader.ReadOwnedData(DsBrush, context);
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(TagName, constants);
            ConstantsHelper.FindConstants(TagType, constants);
            ConstantsHelper.FindConstants(PropertyPath, constants);
            ValueFormatInfo.FindConstants(constants);
            DescriptionInfo.FindConstants(constants);
            DsBrush.FindConstants(constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            TagName = ConstantsHelper.ComputeValue(container, TagName)!;
            TagType = ConstantsHelper.ComputeValue(container, TagType)!;
            PropertyPath = ConstantsHelper.ComputeValue(container, PropertyPath)!;
            ValueFormatInfo.ReplaceConstants(container);
            DescriptionInfo.ReplaceConstants(container);
            DsBrush.ReplaceConstants(container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.RefreshForPropertyGrid(ValueFormatInfo, container);
            ItemHelper.RefreshForPropertyGrid(DescriptionInfo, container);
            ItemHelper.RefreshForPropertyGrid(DsBrush, container);
        }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(HdaId))
                return HdaId;
            return "Trend Seria";
        }

        #endregion
    }

    public class PropertyPathsItemsSource : IItemsSource
    {
        #region public functions

        public ItemCollection GetValues()
        {
            var collection = new ItemCollection();
            var genericDataEngine = DsProject.Instance.DataEngine;
            foreach (ProcessModelPropertyInfo modelTagPropertyInfo in genericDataEngine.ModelTagPropertyInfosCollection)
                collection.Add(modelTagPropertyInfo.PropertyPath,
                    modelTagPropertyInfo.TagType + @": " + modelTagPropertyInfo.PropertyPathToDisplay);
            return collection;
        }

        #endregion
    }
}