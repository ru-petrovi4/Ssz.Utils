using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

namespace Ssz.Operator.Core.Addons
{
    public class ZoomboxAddon : AddonBase
    {
        #region construction and destruction

        public ZoomboxAddon()
        {
            ShowNavigationPanel = DefaultFalseTrue.True;
        }

        #endregion

        #region public functions

        public static readonly Guid AddonGuid = new(@"78DCC70E-D3E4-4729-944F-05CAC7BDF9C3");

        public override Guid Guid => AddonGuid;

        public override string Name => @"Zoombox";

        public override string Desc => Resources.ZoomboxAddon_Desc;

        public override string Version => "1.0";

        public override string SszOperatorVersion => SszOperatorVersionConst;

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.ShowNavigationPanel)]
        [LocalizedDescription(ResourceStrings.ShowNavigationPanelDescription)]
        //[PropertyOrder(1)]
        [DefaultValue(DefaultFalseTrue.Default)] // For XAML serialization
        public DefaultFalseTrue ShowNavigationPanel { get; set; }

        public override IEnumerable<DsPageTypeBase> GetDsPageTypes()
        {
            return _dsPageTypes;
        }

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
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            ShowNavigationPanel = (DefaultFalseTrue)reader.ReadInt32();
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

        #endregion

        #region private fields

        private readonly DsPageTypeBase[] _dsPageTypes = { new ZoomboxDsPageType() };

        #endregion
    }
}