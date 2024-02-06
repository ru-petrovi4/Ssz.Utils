using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.DataAccess
{
    public class ElementValuesJournal : IOwnedDataSerializable
    {
        public Any.TypeCode ValuesTypeCode { get; set; }

        public List<object?> ObjectValues { get; set; } = new();
        public List<uint> ObjectStatusCodes { get; set; } = new();
        public List<DateTime> ObjectTimestamps { get; set; } = new();

        public List<double> DoubleValues { get; set; } = new();
        public List<uint> DoubleStatusCodes { get; set; } = new();
        public List<DateTime> DoubleTimestamps { get; set; } = new();

        public List<uint> UintValues { get; set; } = new();
        public List<uint> UintStatusCodes { get; set; } = new();
        public List<DateTime> UintTimestamps { get; set; } = new();

        public bool IsEmpty()
        {
            return ObjectValues.Count == 0 && DoubleValues.Count == 0 && UintValues.Count == 0;
        }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write((int)ValuesTypeCode);

                writer.WriteList(ObjectValues);
                writer.WriteList(ObjectStatusCodes);
                writer.WriteList(ObjectTimestamps);

                writer.WriteList(DoubleValues);
                writer.WriteList(DoubleStatusCodes);
                writer.WriteList(DoubleTimestamps);

                writer.WriteList(UintValues);
                writer.WriteList(UintStatusCodes);
                writer.WriteList(UintTimestamps);
            }
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        ValuesTypeCode = (Any.TypeCode)reader.ReadInt32();

                        ObjectValues = reader.ReadList<object?>()!;
                        ObjectStatusCodes = reader.ReadList<uint>()!;
                        ObjectTimestamps = reader.ReadList<DateTime>()!;

                        DoubleValues = reader.ReadList<double>()!;
                        DoubleStatusCodes = reader.ReadList<uint>()!;
                        DoubleTimestamps = reader.ReadList<DateTime>()!;

                        UintValues = reader.ReadList<uint>()!;
                        UintStatusCodes = reader.ReadList<uint>()!;
                        UintTimestamps = reader.ReadList<DateTime>()!;
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public ValueStatusTimestamp[] ToValueStatusTimestams()
        {
            var result = new List<ValueStatusTimestamp>();

            for (int index = 0; index < ObjectStatusCodes.Count; index += 1)
            {
                result.Add(new ValueStatusTimestamp
                {
                    Value = new Any(ObjectValues[index]),
                    StatusCode = ObjectStatusCodes[index],
                    TimestampUtc = ObjectTimestamps[index]
                }
                );
            }
            for (int index = 0; index < DoubleStatusCodes.Count; index += 1)
            {
                result.Add(new ValueStatusTimestamp
                {
                    Value = AnyHelper.GetAny(DoubleValues[index], ValuesTypeCode, false),
                    StatusCode = DoubleStatusCodes[index],
                    TimestampUtc = DoubleTimestamps[index]
                }
                );
            }
            for (int index = 0; index < UintStatusCodes.Count; index += 1)
            {
                result.Add(new ValueStatusTimestamp
                {
                    Value = AnyHelper.GetAny(UintValues[index], ValuesTypeCode, false),
                    StatusCode = UintStatusCodes[index],
                    TimestampUtc = UintTimestamps[index]
                }
                );
            }

            return result.ToArray();
        }

        public static ElementValuesJournal From(ValueStatusTimestamp[] valueStatusTimestamps)
        {
            ElementValuesJournal elementValuesJournal = new ElementValuesJournal
            {
                ValuesTypeCode = Any.TypeCode.Object
            };

            foreach (ValueStatusTimestamp vst in valueStatusTimestamps)
            {
                switch (AnyHelper.GetTransportType(vst.Value))
                {
                    case TransportType.Object:
                        elementValuesJournal.ObjectValues.Add(vst.Value.ValueAsObject());
                        elementValuesJournal.ObjectStatusCodes.Add(vst.StatusCode);
                        elementValuesJournal.ObjectTimestamps.Add(vst.TimestampUtc);
                        break;
                    case TransportType.Double:
                        elementValuesJournal.DoubleValues.Add(vst.Value.ValueAsDouble(false));
                        elementValuesJournal.DoubleStatusCodes.Add(vst.StatusCode);
                        elementValuesJournal.DoubleTimestamps.Add(vst.TimestampUtc);
                        break;
                    case TransportType.UInt32:
                        elementValuesJournal.UintValues.Add(vst.Value.ValueAsUInt32(false));
                        elementValuesJournal.UintStatusCodes.Add(vst.StatusCode);
                        elementValuesJournal.UintTimestamps.Add(vst.TimestampUtc);
                        break;                    
                }
            }

            return elementValuesJournal;
        }
    }
}
