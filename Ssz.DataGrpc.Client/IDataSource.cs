using Ssz.DataGrpc.Common;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Client
{
    public interface IDataSource
    {
        /// <summary>        
        ///     Returns id actully used for OPC subscription
        ///     valueSubscription.Update() is called from UI thread.        
        /// </summary>
        /// <param name="elementId"></param>
        /// <param name="valueSubscription"></param>
        string AddItem(string elementId, IValueSubscription valueSubscription);

        /// <summary>        
        ///     If valueSubscription is not subscribed - does nothing.
        /// </summary>
        /// <param name="valueSubscription"></param>
        void RemoveItem(IValueSubscription valueSubscription);

        /// <summary>        
        ///     To use this method, AddItem(...) must be called first for this valueSubscription.
        ///     You cannot write values after RemoveItem(...) is called.
        /// </summary>
        void Write(IValueSubscription valueSubscription, Any value);        

        /// <summary>
        ///     Is called from UI thread.
        /// </summary>
        event Action ValueSubscriptionsUpdated;
    }
}
