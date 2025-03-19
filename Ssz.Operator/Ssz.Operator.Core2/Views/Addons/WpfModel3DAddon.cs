using System;
using System.ComponentModel;
using Avalonia.Media;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Utils.Wpf;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.Addons
{
    public class WpfModel3DAddon : AddonBase
    {
        #region public functions

        public static readonly Guid AddonGuid = new(@"28CAC283-0B87-4A37-BC89-8D23F3734339");

        public override Guid Guid => AddonGuid;

        public override string Name => @"Model3D";

        public override string Desc => Resources.WpfModel3DAddon_Desc;

        public override string Version => "1.0";

        public override string CoreLibraryVersion => CoreLibraryVersionConst;

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.TextDsShapeDsFont)]
        //[Editor(typeof(DsFontTypeEditor), typeof(DsFontTypeEditor))]
        public DsFont? DsFont { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.WpfModel3DPlayWindowParamsBackgroundDsBrush)]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        public DsBrushBase? BackgroundDsBrush { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.WpfModel3DPlayWindowParamsTitleDsBrush)]
        //[Editor(typeof(BrushTypeEditor), typeof(BrushTypeEditor))]
        public DsBrushBase? TitleDsBrush { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.WpfModel3DPlayWindowParamsAmbientLightColor)]
        //[Editor(typeof(ColorEditor), typeof(ColorEditor))]
        public Color AmbientLightColor { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(NameValueCollectionValueSerializer<DsFont>.Instance.ConvertToString(DsFont, null));
                writer.WriteObject(BackgroundDsBrush);
                writer.WriteObject(TitleDsBrush);
                writer.Write(AmbientLightColor);
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
                            string dsFontString = reader.ReadString();
                            DsFont =
                                NameValueCollectionValueSerializer<DsFont>.Instance.ConvertFromString(dsFontString,
                                        null) as
                                    DsFont;
                            BackgroundDsBrush = reader.ReadObject() as DsBrushBase;
                            TitleDsBrush = reader.ReadObject() as DsBrushBase;
                            AmbientLightColor = reader.ReadColor();
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
    }
}