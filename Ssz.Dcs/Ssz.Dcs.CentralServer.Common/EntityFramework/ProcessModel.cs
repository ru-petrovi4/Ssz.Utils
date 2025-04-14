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
    public class ProcessModel : Identifiable<Int64>
    {
        #region public functions

        [Attr]
        public string ProcessModelName { get; set; } = @"";

        /// <summary>
        ///     Предприятие
        /// </summary>
        [Attr]
        public string Enterprise { get; set; } = @"";

        /// <summary>
        ///     Производство
        /// </summary>
        [Attr]
        public string Plant { get; set; } = @"";

        /// <summary>
        ///     Установка
        /// </summary>
        [Attr]
        public string Unit { get; set; } = @"";

        [HasMany]
        public List<Scenario> Scenarios { get; set; } = new();

        #endregion
    }
}
