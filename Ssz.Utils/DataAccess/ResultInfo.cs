using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.DataAccess
{
    public class ResultInfo
    {
        /// <summary>
        ///     Status code <see cref="JobStatusCodes"/>
        /// </summary>
        public uint StatusCode { get; set; }

        /// <summary>
        ///     Info (Invariant culture).
        /// </summary>
        public string Info { get; set; } = @"";

        /// <summary>
        ///     Label (Culture-specific).
        /// </summary>
        public string Label { get; set; } = @"";

        /// <summary>
        ///     Details (Culture-specific).
        /// </summary>
        public string Details { get; set; } = @"";

        /// <summary>
        ///     Unspecified OK result.
        /// </summary>
        public static readonly ResultInfo OkResultInfo = new ResultInfo { StatusCode = JobStatusCodes.OK };

        /// <summary>
        ///     Unspecified Cancelled result.
        /// </summary>
        public static readonly ResultInfo CancelledResultInfo = new ResultInfo { StatusCode = JobStatusCodes.Cancelled };

        /// <summary>
        ///     Unspecified Unknown result.
        /// </summary>
        public static readonly ResultInfo UnknownResultInfo = new ResultInfo { StatusCode = JobStatusCodes.Unknown };
    }
}
