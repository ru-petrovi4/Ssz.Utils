using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine.ServerListItems
{
    public class ProcessElementValueListItem : ElementValueListItemBase
    {
        #region construction and destruction

        public ProcessElementValueListItem(uint clientAlias, uint serverAlias, string elementId)
            : base(clientAlias, serverAlias, elementId)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;
            
			if (disposing)
			{
                Connection?.Dispose();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public RefDsConnection? Connection { get; set; }

        public bool InvalidElementId { get; set; }

        #endregion        
    }
}