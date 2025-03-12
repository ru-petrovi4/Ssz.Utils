using System;
using System.Collections.Generic;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.DsShapes
{
    public class EmptyDsShape : DsShapeBase
    {
        #region construction and destruction

        public EmptyDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public EmptyDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(true)
        {
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = @"";
        public static readonly Guid DsShapeTypeGuid = Guid.Empty;

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            return DsShapeTypeNameToDisplay;
        }

        public List<DsCommand>? DsCommands { get; set; }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(DsCommands);
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            DsCommands = reader.ReadList<DsCommand>();
        }

        #endregion
    }
}