using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Markup;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.Commands.DsCommandOptions
{
    [TypeConverter(typeof(NameValueCollectionTypeConverter<ShowFaceplateForTagDsCommandOptions>))]
    //[ValueSerializer(typeof(NameValueCollectionValueSerializer<ShowFaceplateForTagDsCommandOptions>))]
    public class ShowFaceplateForTagDsCommandOptions : OwnedDataSerializableAndCloneable,
        IChildWindowInfoDsCommandOptions,
        IDsItem
    {
        #region public functions

        [DsDisplayName(ResourceStrings.ShowFaceplateForTagDsCommandOptions_Tag)]
        [LocalizedDescription(ResourceStrings.ShowFaceplateForTagDsCommandOptions_TagDescription)]
        public string TagName { get; set; } = @"";

        [DsDisplayName(ResourceStrings.ShowFaceplateForTagDsCommandOptions_FaceplateIndex)]
        [LocalizedDescription(ResourceStrings.ShowFaceplateForTagDsCommandOptions_FaceplateIndexDescription)]
        public string FaceplateIndex { get; set; } = @"";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string ChildWindowInfo { get; private set; } = @"";

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
            using (writer.EnterBlock(1))
            {
                writer.Write(TagName);
                writer.Write(FaceplateIndex);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        TagName = reader.ReadString();
                        FaceplateIndex = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override string ToString()
        {
            return TagName ?? @"";
        }

        public virtual void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(TagName,
                constants);
            ConstantsHelper.FindConstants(FaceplateIndex,
                constants);
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            TagName = ConstantsHelper.ComputeValue(container, TagName) ?? @"";
            FaceplateIndex = ConstantsHelper.ComputeValue(container, FaceplateIndex) ?? @"";
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            var computedTagName = ConstantsHelper.ComputeValue(container, TagName);
            if (computedTagName == TagName) ChildWindowInfo = TagName;
            else ChildWindowInfo = TagName + @" [" + computedTagName + @"]";
            var computedFaceplateIndex = ConstantsHelper.ComputeValue(container, FaceplateIndex);
            var iFaceplateIndex = ObsoleteAnyHelper.ConvertTo<int>(computedFaceplateIndex, false);
            var faceplateRelativePath =
                PlayDsProjectView.ShowFaceplateForTagUsingPlayInfoAndAllDsPagesCache(null, computedTagName!,
                    iFaceplateIndex, false);
            if (!string.IsNullOrEmpty(faceplateRelativePath)) ChildWindowInfo += @", " + faceplateRelativePath;
        }

        #endregion
    }
}