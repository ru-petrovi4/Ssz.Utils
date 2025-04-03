using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.ControlsPlay.GenericPlay;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsPageTypes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.Addons
{
    public class GenericEmulationAddon : AddonBase
    {
        #region construction and destruction

        public GenericEmulationAddon()
        {               
            TrendsInGroupCount = 8;
        }

        #endregion

        #region public functions

        public const string TrendGroups_FileName = @"GenericEmulation_TrendGroups.csv";

        public static readonly Guid AddonGuid = new(@"4FBAE868-6CA6-43BC-8A8D-88789803ED79");

        public override Guid Guid => AddonGuid;

        public override string Name => @"Generic";

        public override string Desc => Resources.GenericEmulationAddon_Desc;

        public override string Version => "1.0";

        public override string CoreLibraryVersion => CoreLibraryVersionConst;

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.GenericAddon_FileRelativePath)]
        [LocalizedDescription(ResourceStrings.GenericAddon_FileRelativePath_Description)]
        //[Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        public string FramesDsPageFileRelativePath { get; set; } = @"";

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.GenericAddonTrendsInGroupCount)]
        public int TrendsInGroupCount { get; set; }

        [Browsable(false)]
        public AlarmsListViewModel AlarmsListViewModel
        {
            get
            {
                if (_alarmsListViewModel is null) 
                    _alarmsListViewModel = new AlarmsListViewModel();
                return _alarmsListViewModel;
            }
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {                
                writer.Write(FramesDsPageFileRelativePath);
                writer.Write(TrendsInGroupCount);
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
                            FramesDsPageFileRelativePath = reader.ReadString();
                            TrendsInGroupCount = reader.ReadInt32();
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

        public override IEnumerable<DsPageTypeBase> GetDsPageTypes()
        {
            return _dsPageTypes;
        }

        #endregion

        #region private fields        

        private readonly DsPageTypeBase[] _dsPageTypes =
            {new GenericGraphicDsPageType(), new GenericFaceplateDsPageType()};        
        private AlarmsListViewModel? _alarmsListViewModel;

        #endregion
    }
}