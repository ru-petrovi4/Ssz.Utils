using System;
using System.ComponentModel;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using System.Collections.ObjectModel;
using Ssz.Operator.Core.Constants;
using Ssz.Utils;
using System.Collections.Generic;

namespace Ssz.Operator.Core.DsShapes
{
    public class FrameDsShape : DsShapeBase
    {
        #region construction and destruction

        public FrameDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public FrameDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 100;
            HeightInitial = 100;
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "Frame";
        public static readonly Guid DsShapeTypeGuid = new(@"7EB58611-F541-43F5-B399-D0B4144FCEB4");

        public GenericContainer FrameGenericContainer => _frameGenericContainer;

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.FrameDsShape_FrameName)]
        [LocalizedDescription(ResourceStrings.FrameDsShape_FrameName_Description)]
        [PropertyOrder(1)]
        public string FrameName
        {
            get => _frameName;
            set
            {
                if (value is null) value = @"";
                SetValue(ref _frameName, value);
            }
        }

        [DsCategory(ResourceStrings.MainCategory)]
        [DsDisplayName(ResourceStrings.FrameDsShape_StartDsPageFileRelativePath)]
        [LocalizedDescription(ResourceStrings.FrameDsShape_StartDsPageFileRelativePath_Description)]
        [Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
        [PropertyOrder(2)]
        public string StartDsPageFileRelativePath
        {
            get => _startDsPageFileRelativePath;
            set
            {
                if (value is null) value = @"";
                SetValue(ref _startDsPageFileRelativePath, value);
            }
        }        

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            return DsShapeTypeNameToDisplay;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            base.SerializeOwnedData(writer, context);

            using (writer.EnterBlock(1))
            {
                writer.Write(FrameName);
                writer.Write(StartDsPageFileRelativePath);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            FrameName = reader.ReadString();
                            StartDsPageFileRelativePath = reader.ReadString();
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

        private string _frameName = @"";

        private string _startDsPageFileRelativePath = @"";

        private readonly GenericContainer _frameGenericContainer = new();

        #endregion
    }
}