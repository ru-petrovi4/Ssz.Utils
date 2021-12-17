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
		
		public string InvokeId = @"";

		public double ProgressPercent;

		public string ProgressLabel = @"";

		public string ProgressDetail = @"";

		public bool Succeeded;

		#endregion
	}
}
