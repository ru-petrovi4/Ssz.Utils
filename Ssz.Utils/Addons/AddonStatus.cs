using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.Addons
{
    public class AddonStatus : IOwnedDataSerializable
    {
        #region public functions

        /// <summary>        
        ///     
        /// </summary>        
        public string SourcePath { get; set; } = @"";

        /// <summary>        
        ///     
        /// </summary>        
        public string SourceId { get; set; } = @"";

        /// <summary>        
        ///     
        /// </summary>        
        public string SourceIdToDisplay { get; set; } = @"";

        public Guid AddonGuid { get; set; }

        public string AddonIdentifier { get; set; } = @"";

        public string AddonInstanceId { get; set; } = @"";
        
        public DateTime? LastWorkTimeUtc { get; set; }

        /// <summary>
        ///     See consts in <see cref="AddonStateCodes"/>
        /// </summary>
        public uint StateCode { get; set; }

        /// <summary>
        ///     State Info (Invariant culture)
        /// </summary>
        public string Info { get; set; } = @"";

        /// <summary>
        ///      User-friendly label (Configured UI culture)
        /// </summary>
        public string Label { get; set; } = @"";

        /// <summary>
        ///      User-friendly details (Configured UI culture)
        /// </summary>
        public string Details { get; set; } = @"";

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(SourcePath);
            writer.Write(SourceId);
            writer.Write(SourceIdToDisplay);
            writer.Write(AddonGuid);
            writer.Write(AddonIdentifier);
            writer.Write(AddonInstanceId);
            writer.WriteNullable(LastWorkTimeUtc);
            writer.Write(StateCode);            
            writer.Write(Info);
            writer.Write(Label);
            writer.Write(Details);
        }

        public virtual void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            SourcePath = reader.ReadString();
            SourceId = reader.ReadString();
            SourceIdToDisplay = reader.ReadString();
            AddonGuid = reader.ReadGuid();
            AddonIdentifier = reader.ReadString();
            AddonInstanceId = reader.ReadString();
            LastWorkTimeUtc = reader.ReadNullable<DateTime>();
            StateCode = reader.ReadUInt32();
            Info = reader.ReadString();
            Label = reader.ReadString();
            Details = reader.ReadString();
        }

        #endregion
    }

    public static class AddonStateCodes
    {
        public const uint STATE_OPERATIONAL = 0;
        public const uint STATE_DIAGNOSTIC = 1;
        public const uint STATE_INITIALIZING = 2;
        public const uint STATE_FAULTED = 3;
        public const uint STATE_NEEDS_CONFIGURATION = 4;
        public const uint STATE_OUT_OF_SERVICE = 5;
        public const uint STATE_NOT_CONNECTED = 6;
        public const uint STATE_ABORTING = 7;
        public const uint STATE_NOT_OPERATIONAL = 8;
    }
}
