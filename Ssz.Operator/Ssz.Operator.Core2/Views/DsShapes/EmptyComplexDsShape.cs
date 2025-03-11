using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Utils.Serialization;

namespace Ssz.Operator.Core.DsShapes
{
    public class EmptyComplexDsShape : ComplexDsShape
    {
        #region construction and destruction

        public EmptyComplexDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public EmptyComplexDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(true)
        {
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "EmptyComplexDsShape";
        public new static readonly Guid DsShapeTypeGuid = new(@"BB31CA03-F898-42FD-8EE7-6E29C1B9715F");

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(DsShapeDrawingGuid);
            writer.Write(DsShapeDrawingName);

            writer.Write(DsConstantsCollection.ToList());
            writer.WriteDsShapes(DsShapes, context);
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            DsShapeDrawingGuid = reader.ReadGuid();
            DsShapeDrawingName = reader.ReadString();

            List<DsConstant> dsConstantsCollection = reader.ReadList<DsConstant>();
            DsConstantsCollection.Clear();
            foreach (DsConstant dsConstant in dsConstantsCollection) DsConstantsCollection.Add(dsConstant);
            DsShapes = reader.ReadDsShapes(context, VisualDesignMode, LoadXamlContent);
        }

        #endregion
    }
}