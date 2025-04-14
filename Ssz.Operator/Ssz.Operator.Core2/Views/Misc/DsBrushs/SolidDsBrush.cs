using System.Collections.Generic;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class SolidDsBrush : DsBrushBase
    {
        #region private fields

        private static readonly Dictionary<Color, SolidColorBrush> Brushes = new();

        #endregion

        #region protected functions

        protected override Brush? GetBrushInternal()
        {
            var color = Color;
            SolidColorBrush? brush;
            Brushes.TryGetValue(color, out brush);
            if (brush is null)
            {
                brush = new SolidColorBrush(color);
                //if (brush.CanFreeze) brush.Freeze();
                Brushes.Add(color, brush);
            }

            return brush;
        }

        #endregion

        #region construction and destruction

        public SolidDsBrush()
        {
        }

        public SolidDsBrush(Color color)
        {
            Color = color;
        }

        #endregion

        #region public functions

        public static SolidColorBrush GetSolidColorBrush(Color color)
        {
            SolidColorBrush? brush;
            Brushes.TryGetValue(color, out brush);
            if (brush is null)
            {
                brush = new SolidColorBrush(color);
                //if (brush.CanFreeze) brush.Freeze();
                Brushes.Add(color, brush);
            }

            return brush;
        }

        [Searchable(false)]
        public Color Color
        {
            get => ObsoleteAnyHelper.ConvertTo<Color>(ColorString, false);
            set => ColorString = ObsoleteAnyHelper.ConvertTo<string>(value, false);
        }

        public string ColorString { get; set; } = @"";

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                writer.Write(ColorString);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 2)
                    try
                    {
                        ColorString = reader.ReadString();
                    }
                    catch (BlockEndingException)
                    {
                    }
                else
                    throw new BlockUnsupportedVersionException();
            }
        }

        public override void FindConstants(HashSet<string> constants)
        {
            base.FindConstants(constants);

            ConstantsHelper.FindConstants(ColorString, constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            base.ReplaceConstants(container);

            ColorString = ConstantsHelper.ComputeValue(container, ColorString)!;
        }

        #endregion
    }
}