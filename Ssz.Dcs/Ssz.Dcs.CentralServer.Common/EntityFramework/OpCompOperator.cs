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
    public class OpCompOperator: Identifiable<Int64>
    {
        #region public functions        

        [Attr]
        public Guid OpCompUserId { get; set; }

        [Attr]
        public string OpCompUserNameToDisplay { get; set; } = @"";

        /// <summary>
        ///     Domain\UserName
        /// </summary>
        [Attr]
        public string OpCompUserWindowsUserName { get; set; } = @"";

        [HasOne]
        [ForeignKey(nameof(OperatorId))]       
        public User Operator { get; set; } = null!;
        
        public Int64? OperatorId { get; set; }

        #endregion
    }
}
