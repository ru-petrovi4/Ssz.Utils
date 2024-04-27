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
    [Resource]
    public class Scenario : Identifiable<Int64>
    {
        #region public functions

        [HasOne]
        [ForeignKey(nameof(ProcessModelId))]
        public ProcessModel ProcessModel { get; set; } = null!;

        public Int64 ProcessModelId { get; set; }

        [Attr]
        public string ScenarioName { get; set; } = @"";

        [Attr]
        public string InitialConditionName { get; set; } = @"";        

        [Attr]
        public int MaxPenalty { get; set; }

        [Attr]
        public UInt64 ScenarioMaxProcessModelTimeSeconds { get; set; }        

        #endregion
    }
}
