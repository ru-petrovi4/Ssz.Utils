using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Xi.Client
{
    public interface IDataSource
    {
        /// <summary>        
        ///     Returns id actully used for OPC subscription
        ///     valueSubscription.Update() is called from UI thread.        
        /// </summary>
        /// <param name="id"></param>
        /// <param name="valueSubscription"></param>
        string AddItem(string id, IValueSubscription valueSubscription);

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
