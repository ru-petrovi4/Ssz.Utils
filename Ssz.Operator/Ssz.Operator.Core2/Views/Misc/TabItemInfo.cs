using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;

namespace Ssz.Operator.Core
{
    public class TabItemInfo : MenuItemInfo
    {
        #region construction and destruction

        public TabItemInfo()
            : base(true)
        {
            PageFileRelativePath = @"";
        }

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.BasicCategory),
         DsDisplayName(ResourceStrings.TabItemInfo_PageFileRelativePath)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        //[PropertyOrder(2)]
        public string PageFileRelativePath { get; set; }        

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                base.SerializeOwnedData(writer, context);

                writer.Write(PageFileRelativePath);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            base.DeserializeOwnedDataAsync(reader, context);

                            PageFileRelativePath = reader.ReadString();
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

        public override void FindConstants(HashSet<string> constants)
        {
            base.FindConstants(constants);

            ConstantsHelper.FindConstants(PageFileRelativePath, constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            base.ReplaceConstants(container);

            PageFileRelativePath = ConstantsHelper.ComputeValue(container, PageFileRelativePath)!;
        }

        #endregion
    }
}