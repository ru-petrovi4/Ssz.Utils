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

		public string? ProgressDetails;

        /// <summary>        
		///     See consts in <see cref="StatusCodes"/>
        /// </summary>
        public uint StatusCode;

		#endregion
	}
}
