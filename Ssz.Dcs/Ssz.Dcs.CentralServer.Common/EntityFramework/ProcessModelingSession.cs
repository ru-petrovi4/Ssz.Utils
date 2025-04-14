using JsonApiDotNetCore.Resources;
using JsonApiDotNetCore.Resources.Annotations;
using Microsoft.EntityFrameworkCore;
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
    public class ProcessModelingSession : Identifiable<Int64>
    {
        #region public functions        

        [HasOne]
        [ForeignKey(nameof(InstructorUserId))]
        public User InstructorUser { get; set; } = null!;

        public Int64 InstructorUserId { get; set; }

        [Attr]
        public DateTime StartDateTimeUtc { get; set; }

        [Attr]
        public DateTime? FinishDateTimeUtc { get; set; }

        [Attr]
        public byte Type { get; set; }

        [HasOne]
        [ForeignKey(nameof(ProcessModelId))]
        public ProcessModel? ProcessModel { get; set; } = null!;

        public Int64? ProcessModelId { get; set; }        

        #endregion
    }
}
