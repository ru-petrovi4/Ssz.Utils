using System;
using System.Collections.Generic;
using System.Text;

namespace Ssz.Utils.DataAccess
{
    public class ResultInfo
    {
        /// <summary>
        ///     Status code <see cref="StatusCodes"/>
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
        ///     Unspecified Good result.
        /// </summary>
        public static readonly ResultInfo GoodResultInfo = new ResultInfo { StatusCode = StatusCodes.Good };        

        /// <summary>
        ///     Unspecified Uncertain result.
        /// </summary>
        public static readonly ResultInfo UncertainResultInfo = new ResultInfo { StatusCode = StatusCodes.Uncertain };
    }
}
