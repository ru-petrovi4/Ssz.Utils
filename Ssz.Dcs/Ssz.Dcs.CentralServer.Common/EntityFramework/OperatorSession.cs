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
        
        [ForeignKey(nameof(OperatorUserId))]
        public User OperatorUser { get; set; } = null!;

        public Int64 OperatorUserId { get; set; }        

        public DateTime StartDateTimeUtc { get; set; }

        public DateTime? FinishDateTimeUtc { get; set; }
        
        [ForeignKey(nameof(ProcessModelingSessionId))]
        public ProcessModelingSession ProcessModelingSession { get; set; } = null!;

        public Int64 ProcessModelingSessionId { get; set; }

        public byte Type { get; set; }

        public byte Rating { get; set; }

        public bool? Succeeded { get; set; }

        public string Task { get; set; } = @"";

        public string Comment { get; set; } = @"";

        public string File { get; set; } = @"";

        public List<ScenarioResult> ScenarioResults { get; set; } = new();

        #endregion
    }
}