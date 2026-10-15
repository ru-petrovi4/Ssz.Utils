using Opc.Ua;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;

namespace Ssz.Dcs.Addons.OpcUaClient
{
    /// <summary>
    ///     Conversions between OPC UA values and Ssz values.
    /// </summary>
    internal static class OpcUaValueHelper
    {
        /// <summary>
        ///     Converts a value received from an OPC UA server to an Ssz value.
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

        public static ValueStatusTimestamp ToValueStatusTimestamp(DataValue? dataValue)
        {
            if (dataValue is null)
                return new ValueStatusTimestamp { StatusCode = Ssz.Utils.StatusCodes.Uncertain };

            DateTime timestampUtc;
            if (dataValue.SourceTimestamp != DateTime.MinValue)
                timestampUtc = dataValue.SourceTimestamp;
            else if (dataValue.ServerTimestamp != DateTime.MinValue)
                timestampUtc = dataValue.ServerTimestamp;
            else
                timestampUtc = DateTime.UtcNow;
            if (timestampUtc.Kind != DateTimeKind.Utc)
                timestampUtc = timestampUtc.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(timestampUtc, DateTimeKind.Utc) : timestampUtc.ToUniversalTime();

            // OPC UA and Ssz status codes share the same numeric layout.
            return new ValueStatusTimestamp(ToAny(dataValue.Value), dataValue.StatusCode.Code, timestampUtc);
        }

        /// <summary>
        ///     Converts an Ssz value to a .NET value of the OPC UA built-in type of the target variable.
        /// </summary>
        public static object? ToOpcUaValue(Any value, BuiltInType? builtInType)
        {
            object? raw = value.ValueAsObject();
            if (raw is DateTimeOffset dto)
                raw = dto.UtcDateTime;

            if (raw is null || builtInType is null || builtInType == BuiltInType.Null || builtInType == BuiltInType.Variant)
                return raw;

            try
            {
                return TypeInfo.Cast(raw, builtInType.Value);
            }
            catch (Exception)
            {
                return raw;
            }
        }

        public static string ToSymbolicId(uint statusCode)
        {
            return ((StatusCode)statusCode).SymbolicId ?? @"";
        }
    }
}
