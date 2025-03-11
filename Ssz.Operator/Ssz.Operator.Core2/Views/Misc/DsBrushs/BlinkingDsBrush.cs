using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Operator.Core.ControlsCommon;
using Ssz.Utils;
using Avalonia.Data;

namespace Ssz.Operator.Core
{
    public class BlinkingDsBrush : DsBrushBase
    {
        #region private fields

        private static readonly Dictionary<Tuple<Color, Color>, SolidColorBrush> Brushes =
            new();

        #endregion

        #region protected functions

        protected override Brush? GetBrushInternal()
        {
            var firstColor = FirstColor;
            var secondColor = SecondColor;
            SolidColorBrush? brush;
            Brushes.TryGetValue(Tuple.Create(firstColor, secondColor), out brush);
            if (brush is null)
            {
                brush = new SolidColorBrush(firstColor);

                brush.Bind(SolidColorBrush.ColorProperty, new Binding
                {
                    Source =
                        DsProject.Instance,
                    Path = @"GlobalUITimerPhase",
                    Converter =
                        Int32ToColorConverter
                            .Instanse,
                    ConverterParameter =
                        new[]
                            {firstColor, secondColor}
                });

                Brushes.Add(Tuple.Create(firstColor, secondColor), brush);
            }

            return brush;
        }

        #endregion

        #region public functions

        public static SolidColorBrush GetBrush(Color firstColor, Color secondColor)
        {
            SolidColorBrush? brush;
            Brushes.TryGetValue(Tuple.Create(firstColor, secondColor), out brush);
            if (brush is null)
            {
                brush = new SolidColorBrush(firstColor);

                brush.Bind(SolidColorBrush.ColorProperty, new Binding
                {
                    Source =
                        DsProject.Instance,
                    Path =
                        @"GlobalUITimerPhase",
                    Converter =
                        Int32ToColorConverter
                            .Instanse,
                    ConverterParameter =
                        new[]
                            {firstColor, secondColor}
                });

                Brushes.Add(Tuple.Create(firstColor, secondColor), brush);
            }

            return brush;
        }

        [Searchable(false)]
        public Color FirstColor
        {
            get => ObsoleteAnyHelper.ConvertTo<Color>(FirstColorString, false);
            set => FirstColorString = ObsoleteAnyHelper.ConvertTo<string>(value, false);
        }

        [Searchable(false)]
        public Color SecondColor
        {
            get => ObsoleteAnyHelper.ConvertTo<Color>(SecondColorString, false);
            set => SecondColorString = ObsoleteAnyHelper.ConvertTo<string>(value, false);
        }

        public string FirstColorString { get; set; } = @"";

        public string SecondColorString { get; set; } = @"";

        public override void FindConstants(HashSet<string> constants)
        {
            base.FindConstants(constants);

            ConstantsHelper.FindConstants(FirstColorString, constants);
            ConstantsHelper.FindConstants(SecondColorString, constants);
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            base.ReplaceConstants(container);

            FirstColorString = ConstantsHelper.ComputeValue(container, FirstColorString)!;
            SecondColorString = ConstantsHelper.ComputeValue(container, SecondColorString)!;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                writer.Write(FirstColorString);
                writer.Write(SecondColorString);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 2)
                    try
                    {
                        FirstColorString = reader.ReadString();
                        SecondColorString = reader.ReadString();
                    }
                    catch (BlockEndingException)
                    {
                    }
                else
                    throw new BlockUnsupportedVersionException();
            }
        }

        #endregion
    }
}