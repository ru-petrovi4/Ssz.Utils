using System.Collections.Generic;
using Avalonia.Media;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public class XamlDsBrush : DsBrushBase
    {
        #region protected functions

        protected override Brush? GetBrushInternal()
        {
            if (string.IsNullOrWhiteSpace(_brushXaml)) return null;
            Brush? brush;
            Brushes.TryGetValue(_brushXaml, out brush);
            if (brush is null)
            {
                brush = Brush;
                if (brush is not null)
                {
                    //if (brush.CanFreeze) brush.Freeze();
                    Brushes.Add(_brushXaml, brush);
                }
            }

            return brush;
        }

        #endregion

        #region construction and destruction

        public XamlDsBrush()
        {
        }

        public XamlDsBrush(Brush brush)
        {
            Brush = brush;
        }

        #endregion

        #region public functions

        [Searchable(false)]
        public Brush? Brush
        {
            get => (Brush?) XamlHelper.Load(_brushXaml);
            set => _brushXaml = XamlHelper.Save(value);
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(_brushXaml);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                if (block.Version == 1)
                    try
                    {
                        _brushXaml = reader.ReadString();
                    }
                    catch (BlockEndingException)
                    {
                    }
                else
                    throw new BlockUnsupportedVersionException();
            }
        }

        #endregion

        #region private fields

        private static readonly Dictionary<string, Brush> Brushes = new();

        [Searchable(false)] private string _brushXaml = @"";

        #endregion
    }
}