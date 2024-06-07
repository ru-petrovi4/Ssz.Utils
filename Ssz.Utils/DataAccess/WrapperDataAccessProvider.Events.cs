using System;
using System.Collections.Generic;
using System.Linq;
using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Microsoft.Extensions.Logging;

namespace Ssz.Utils.DataAccess
{
    public partial class WrapperDataAccessProvider
    {
        #region public functions

        /// <summary>
        ///     Is called using сallbackDispatcher, see Initialize(..).        
        /// </summary>
        public override event EventHandler<EventMessagesCallbackEventArgs> EventMessagesCallback = delegate { };        

        //public override void AckAlarms(string operatorName, string comment, Ssz.Utils.DataAccess.EventId[] eventIdsToAck)
        //{
        //    WorkingThreadSafeDispatcher.BeginInvokeEx(async ct =>
        //    {
        //        ClientEventList? clientEventList =
        //            _clientEventListManager.GetRelatedClientEventList(OnClientEventListManager_EventMessagesCallbackInternal);

        //        if (clientEventList is null) return;

        //        try
        //        {
        //            if (clientEventList.Disposed) return;

        //            await clientEventList.AckAlarmsAsync(operatorName, comment, eventIdsToAck);
        //        }
        //        catch (Exception ex)
        //        {
        //            LoggersSet.Logger.LogError(ex, "Exception");
        //        }
        //    }
        //    );
        //}

        #endregion        

        #region private functions

        private void DataAccessProvider_OnEventMessagesCallback(object? sender, EventMessagesCallbackEventArgs args)
        {
            EventMessagesCallback(sender, args);
        }

        #endregion        
    }
}