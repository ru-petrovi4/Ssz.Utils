using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.DataAccess
{
    public class LongrunningPassthroughCallback
    {
		#region public functions		
		
		public string JobId = @"";

		public uint ProgressPercent;

		public string? ProgressLabel;

		public string? ProgressDetail;

        /// <summary>
        ///     OK = 0, Cancelled = 1, UnknownError = 2, Error >= 2.
		///     See consts in JobStatusCodes
        /// </summary>
        public uint JobStatusCode;

		#endregion
	}
}
