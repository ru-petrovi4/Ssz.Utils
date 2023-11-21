using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Ssz.Dcs.CentralServer.Common.EntityFramework
{
    [Resource]
    public class OperatorSession : Identifiable<Int64>
    {
        #region public functions        

        [HasOne]
        [ForeignKey(nameof(OperatorUserId))]
        public User OperatorUser { get; set; } = null!;
        
        public Int64 OperatorUserId { get; set; }

        [Attr]
        public DateTime StartDateTimeUtc { get; set; }

        [Attr]
        public DateTime? FinishDateTimeUtc { get; set; }

        [HasOne]
        [ForeignKey(nameof(ProcessModelingSessionId))]
        public ProcessModelingSession ProcessModelingSession { get; set; } = null!;

        public Int64 ProcessModelingSessionId { get; set; }

        [Attr]
        public byte Type { get; set; }

        [Attr]
        public byte Rating { get; set; }

        [Attr]
        public bool? Succeeded { get; set; }

        [Attr]
        public string Task { get; set; } = @"";

        [Attr]
        public string Comment { get; set; } = @"";

        [Attr]
        public string File { get; set; } = @"";

        [HasMany]
        public List<ScenarioResult> ScenarioResults { get; set; } = new();

        #endregion
    }
}