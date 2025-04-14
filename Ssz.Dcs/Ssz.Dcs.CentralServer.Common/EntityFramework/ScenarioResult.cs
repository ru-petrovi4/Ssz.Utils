using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common.EntityFramework
{
    //[Resource]
    public class ScenarioResult : Identifiable<Int64>, IOwnedDataSerializable
    {
        #region public functions        

        [HasOne]
        [ForeignKey(nameof(ProcessModelingSessionId))]
        public ProcessModelingSession ProcessModelingSession { get; set; } = null!;

        public Int64 ProcessModelingSessionId { get; set; }

        [HasMany]
        public List<OperatorSession> OperatorSessions { get; set; } = new();

        [Attr]
        public DateTime StartDateTimeUtc { get; set; }

        [Attr]
        public DateTime? FinishDateTimeUtc { get; set; }

        [Attr]
        public UInt64 StartProcessModelTimeSeconds { get; set; }

        [Attr]
        public UInt64 FinishProcessModelTimeSeconds { get; set; }

        [Attr]
        public string ScenarioName { get; set; } = @"";

        [Attr]
        public string InitialConditionName { get; set; } = @"";

        /// <summary>
        ///     Штрафные баллы
        /// </summary>
        [Attr]
        public int Penalty { get; set; }

        [Attr]
        public int MaxPenalty { get; set; }

        [Attr]
        public UInt64 ScenarioMaxProcessModelTimeSeconds { get; set; }

        /// <summary>
        ///     Оценка
        /// </summary>
        [Attr]
        public string Status { get; set; } = @"";

        public string Details { get; set; } = @"";

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(1))
            {
                writer.Write(StartDateTimeUtc);
                writer.WriteNullable(FinishDateTimeUtc);
                writer.Write(StartProcessModelTimeSeconds);
                writer.Write(FinishProcessModelTimeSeconds);
                writer.Write(ScenarioName);
                writer.Write(InitialConditionName);
                writer.Write(Penalty);
                writer.Write(MaxPenalty);
                writer.Write(ScenarioMaxProcessModelTimeSeconds);
                writer.Write(Status);
                writer.Write(Details);
            }            
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        StartDateTimeUtc = reader.ReadDateTime();
                        FinishDateTimeUtc = reader.ReadNullable<DateTime>();
                        StartProcessModelTimeSeconds = reader.ReadUInt64();
                        FinishProcessModelTimeSeconds = reader.ReadUInt64();
                        ScenarioName = reader.ReadString();
                        InitialConditionName = reader.ReadString();
                        Penalty = reader.ReadInt32();
                        MaxPenalty = reader.ReadInt32();
                        ScenarioMaxProcessModelTimeSeconds = reader.ReadUInt64();
                        Status = reader.ReadString();
                        Details = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }            
        }

        #endregion
    }
}
