using System;
using System.ComponentModel;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.DsPageTypes
{
    public class ZoomboxDsPageType : DsPageTypeBase
    {
        #region construction and destruction

        public ZoomboxDsPageType()
        {
            ShowNavigationPanel = DefaultFalseTrue.Default;            
        }

        #endregion

        #region public functions

        public static readonly Guid TypeGuid = new(@"9D2077B5-1D77-4612-855D-128C4D84D79D");

        public override Guid Guid => TypeGuid;

        public override string Name => @"Zoombox";

        public override string Desc => Resources.ZoomboxDsPageType_Desc;

        public override bool IsFaceplate => false;

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ShowNavigationPanel)]
        [LocalizedDescription(ResourceStrings.ShowNavigationPanelDescription)]
        [PropertyOrder(1)]
        [DefaultValue(DefaultFalseTrue.Default)] // For XAML serialization
        public DefaultFalseTrue ShowNavigationPanel { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write((int)ShowNavigationPanel);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                {
                    try
                    {
                        ShowNavigationPanel = (DefaultFalseTrue)reader.ReadInt32();
                    }
                    catch (BlockEndingException)
                    {
                    }
                }
                else
                {
                    throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion
    }
}