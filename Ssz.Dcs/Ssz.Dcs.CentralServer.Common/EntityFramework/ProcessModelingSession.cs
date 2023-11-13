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
    [Resource]
    public class ProcessModelingSession : Identifiable<Int64>
    {
        #region public functions        
        
        [ForeignKey(nameof(InstructorUserId))]
        public User InstructorUser { get; set; } = null!;

        public Int64 InstructorUserId { get; set; }

        public DateTime StartDateTimeUtc { get; set; }

        public DateTime? FinishDateTimeUtc { get; set; }

        public byte Type { get; set; }
        
        public string ProcessModelName { get; set; } = @"";

        /// <summary>
        ///     Предприятие
        /// </summary>
        public string Enterprise { get; set; } = @"";

        /// <summary>
        ///     Производство
        /// </summary>
        public string Plant { get; set; } = @"";

        /// <summary>
        ///     Установка
        /// </summary>
        public string Unit { get; set; } = @"";

        #endregion
    }
}
