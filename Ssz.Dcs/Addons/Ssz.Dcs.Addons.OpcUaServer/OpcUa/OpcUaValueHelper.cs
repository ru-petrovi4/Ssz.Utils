using Opc.Ua;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;

namespace Ssz.Dcs.Addons.OpcUaServer.OpcUa
{
    /// <summary>
    ///     Conversions between Ssz values/status codes and OPC UA values/status codes.
    /// </summary>
    internal static class OpcUaValueHelper
    {
        /// <summary>
        ///     Converts an Ssz value to a value that can be stored in an OPC UA Variant.
        /// </summary>
        public static object? ToOpcUaValue(Any any)
        {
            switch (any.ValueTypeCode)
            {
                case Any.TypeCode.Empty:
                case Any.TypeCode.DBNull:
                    return null;
                case Any.TypeCode.Boolean:
                case Any.TypeCode.SByte:
                case Any.TypeCode.Byte:
                case Any.TypeCode.Int16:
                case Any.TypeCode.UInt16:
                case Any.TypeCode.Int32:
                case Any.TypeCode.UInt32:
                case Any.TypeCode.Int64:
                case Any.TypeCode.UInt64:
                case Any.TypeCode.Single:
                case Any.TypeCode.Double:
                case Any.TypeCode.String:
                    return any.ValueAsObject();
                case Any.TypeCode.Char:
                    return any.ValueAsString(false);
                case Any.TypeCode.Decimal:
                    return any.ValueAsDouble(false);
                case Any.TypeCode.DateTimeOffset:
                    {
                        object? o = any.ValueAsObject();
                        if (o is DateTimeOffset dto)
                            return dto.UtcDateTime;
                        return any.ValueAsString(false);
                    }
                default:
                    {
                        object? o = any.ValueAsObject();
                        switch (o)
                        {
                            case null:
                                return null;
                            case DateTime dt:
                                return dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime();
                            case DateTimeOffset dto:
                                return dto.UtcDateTime;
                            case Guid guid:
                                return guid;
                            case byte[] bytes:
                                return bytes;
                            default:
                                return any.ValueAsString(false);
                        }
                    }
            }
        }

        /// <summary>
        ///     Converts a value written by an OPC UA client to an Ssz value.
        /// </summary>
        public static Any ToAny(object? value)
        {
            switch (value)
            {
                case null:
                    return new Any();
                case Variant variant:
                    return ToAny(variant.Value);
                case DateTime dt:
                    return new Any(new DateTimeOffset(dt.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(dt, DateTimeKind.Utc) : dt.ToUniversalTime()));
                case Guid guid:
                    return new Any(guid.ToString());
                case Uuid uuid:
                    return new Any(uuid.ToString());
                case byte[] bytes:
                    return new Any(Convert.ToBase64String(bytes));
                case LocalizedText localizedText:
                    return new Any(localizedText.Text ?? @"");
                case QualifiedName qualifiedName:
                    return new Any(qualifiedName.ToString());
                case NodeId nodeId:
                    return new Any(nodeId.ToString());
                case ExpandedNodeId expandedNodeId:
                    return new Any(expandedNodeId.ToString());
                case StatusCode statusCode:
                    return new Any(statusCode.Code);
                case ExtensionObject extensionObject:
                    return new Any(extensionObject.ToString());
                case System.Xml.XmlElement xmlElement:
                    return new Any(xmlElement.OuterXml);
                default:
                    return new Any(value);
            }
        }

        /// <summary>
        ///     Ssz status codes use the OPC UA numeric layout, so they are passed through.
        /// </summary>
        public static StatusCode ToOpcUaStatusCode(uint sszStatusCode)
        {
            if (Ssz.Utils.StatusCodes.IsGood(sszStatusCode))
                return Opc.Ua.StatusCodes.Good;
            return (StatusCode)sszStatusCode;
        }

        public static DataValue ToDataValue(ValueStatusTimestamp vst)
        {
            object? value = ToOpcUaValue(vst.Value);
            DateTime timestamp = vst.TimestampUtc == default ? DateTime.UtcNow : DateTime.SpecifyKind(vst.TimestampUtc, DateTimeKind.Utc);
            return new DataValue(new Variant(value), ToOpcUaStatusCode(vst.StatusCode), timestamp, timestamp);
        }

        /// <summary>
        ///     Maps Ssz event priority to OPC UA severity (1..1000).
        /// </summary>
        public static ushort ToSeverity(uint priority)
        {
            if (priority == 0)
                return (ushort)EventSeverity.Medium;
            if (priority > 1000)
                return 1000;
            return (ushort)priority;
        }
    }
}
