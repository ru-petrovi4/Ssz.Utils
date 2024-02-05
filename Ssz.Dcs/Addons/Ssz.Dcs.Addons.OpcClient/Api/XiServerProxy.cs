using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ssz.Dcs.Addons.OpcClient;
using Ssz.Utils;
using Ssz.Xi.Client.Api.Lists;
using Ssz.Xi.Client.Internal;
using Ssz.Xi.Client.Internal.Context;
using Ssz.Xi.Client.Internal.Lists;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    public class XiServerNotExistException : Exception
    {
        #region construction and destruction

        public XiServerNotExistException() :
            base("Xi context doesn't exist - need to connect to server first.")
        {
        }

        #endregion
    }

    /// <summary>
    ///     This class defines the Xi Server entries in the XiClient XiServerList.
    ///     Each XiServer in the list represents an Xi server for which the client application
    ///     can create an XiSubscription.
    /// </summary>
    public class XiServerProxy : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the client application, client base, or
        ///     the destructor of this object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method disposes of the object.  It is invoked by the parameterless Dispose()
        ///     method of this object.  It is expected that the Xi client application
        ///     will perform a Dispose() on each active context to close the connection with the
        ///     Xi Server.  Failure to perform the close will result in the Xi Context remaining
        ///     active until the application exits.
        /// </summary>
        /// <summary>
        /// </summary>
        /// <param name="disposing">
        ///     <para>
        ///         This parameter indicates, when TRUE, this Dispose() method was called directly or indirectly by a user's
        ///         code. When FALSE, this method was called by the runtime from inside the finalizer.
        ///     </para>
        ///     <para>
        ///         When called by user code, references within the class should be valid and should be disposed of properly.
        ///         When called by the finalizer, references within the class are not guaranteed to be valid and attempts to
        ///         dispose of them should not be made.
        ///     </para>
        /// </param>
        /// <returns> Returns TRUE to indicate that the object has been disposed. </returns>
        private void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                ConcludeXiContextInternal();
            }

            _disposed = true;
        }

        /// <summary>
        ///     The standard destructor invoked by the .NET garbage collector during Finalize.
        /// </summary>
        ~XiServerProxy()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method is used to connect to the server and establish a context with it.
        /// </summary>
        public void InitiateXiContext(CaseInsensitiveDictionary<string?> contextParams, string applicationName,
            string workstationName, IDispatcher xiCallbackDoer)
        {
            if (_disposed) throw new ObjectDisposedException(@"Cannot access a disposed XiServerProxy.");

            if (_context is not null) throw new Exception(@"Xi context already exists.");

            try
            {
                _context = new XiContext(contextParams,
                            (uint)_contextTimeout.TotalMilliseconds,
                            _localeId, applicationName, workstationName, _keepAliveSkipCount,
                            _callbackRate, xiCallbackDoer);

                _context.ContextNotifyEvent += XiContext_ContextNotifyEvent;
            }
            catch
            {
                if (_context is not null)
                {
                    _context.ContextNotifyEvent -= XiContext_ContextNotifyEvent;
                    _context.Dispose();
                    _context = null;
                }

                throw;
            }
        }        

        /// <summary>
        ///     This method is used to close a context with the server and disconnect the WCF connection.
        /// </summary>
        public void ConcludeXiContext()
        {
            if (_disposed) return;

            ConcludeXiContextInternal();
        }

        /// <summary>
        ///     This method creates a new data list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public IXiDataListProxy NewDataList(uint updateRate, uint bufferingRate, FilterSet? filterSet)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();

            return new XiDataList(_context, updateRate, bufferingRate, filterSet);
        }

        /// <summary>
        ///     This method creates a new event list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public IXiEventListProxy NewEventList(uint updateRate, uint bufferingRate, FilterSet filterSet)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();

            return new XiEventList(_context, updateRate, bufferingRate, filterSet);
        }

        /// <summary>
        ///     This method creates a new data journal (historical data) list for the context.
        /// </summary>
        /// <param name="updateRate"> The update rate for the list. </param>
        /// <param name="bufferingRate"> The buffering rate for the list. 0 if not used. </param>
        /// <param name="filterSet"> The filter set for the list. Null if not used. </param>
        /// <returns> Returns the new data list. </returns>
        public IXiDataJournalListProxy NewDataJournalList(uint updateRate, uint bufferingRate,
            FilterSet? filterSet)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();

            return new XiDataJournalList(_context, updateRate, bufferingRate, filterSet);
        }

        /*
        /// <summary>
        ///   This method creates a new event journal (historical events) list for the context.
        /// </summary>
        /// <param name = "updateRate">The update rate for the list.</param>
        /// <param name = "bufferingRate">The buffering rate for the list. 0 if not used.</param>
        /// <param name = "filterSet">The filter set for the list. Null if not used.</param>
        /// <returns>Returns the new data list.</returns>
        public IXiEventJournalList NewEventJournalList(uint updateRate, uint bufferingRate, FilterSet filterSet)
        {
        }*/

        /// <summary>
        ///     This method returns the ServerDescription retrieved from the server.
        /// </summary>
        /// <returns>
        ///     Returns the ServerDescription retrieved from the server. An Exception is thrown if there is no open context
        ///     with the server.
        /// </returns>
        public ServerDescription? Identify()
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();

            return _context.Identify();
        }

        /// <summary>
        ///     This method is used to get the state of the server, and
        ///     the state of any wrapped servers.
        /// </summary>
        /// <returns> </returns>
        public IEnumerable<ServerStatus>? Status()
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();

            return _context.Status();
        }

        /// <summary>
        ///     This method returns text descriptions of result codes.
        /// </summary>
        /// <param name="resultCodes"> The result codes for which text descriptions are being requested. </param>
        /// <returns>
        ///     The list of result codes and if a result code indicates success, the requested text descriptions. The size
        ///     and order of this list matches the size and order of the resultCodes parameter.
        /// </returns>
        public IEnumerable<RequestedString>? LookupResultCodes(IEnumerable<uint> resultCodes)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();

            return _context.LookupResultCodes(resultCodes);
        }

        /// <summary>
        ///     <para>
        ///         This method is used to find objects in the server. The client uses the findCriteria parameter to identify a
        ///         starting branch and a set of filter criteria. It also specifies the maximum number of objects to return.
        ///     </para>
        ///     <para>
        ///         The server examines the objects that are children of the specified branch and selects those that match the
        ///         filter criteria. Note that "children" are objects whose root paths can be created by appending their names to
        ///         the path used to identify the starting branch.
        ///     </para>
        ///     <para>
        ///         The object attributes of the selected objects are returned to the client. The number returned is limited by
        ///         the number specified in the numberToReturn parameter. If the number returned is less than than that number,
        ///         then the client can safely assume that the server has no more to return.
        ///     </para>
        ///     <para>
        ///         However, if the number returned is equal to that number, then the client can retrieve the next set of
        ///         results by issuing another FindObjects() call with the findCriteria parameter set to null. A null findCriteria
        ///         indicates to the server to continue returning results from those remaining in the list. The client eventually
        ///         detects the end of the list by receiving a response that returns less than specified by the numberToReturn
        ///         parameter.
        ///     </para>
        /// </summary>
        /// <param name="findCriteria">
        ///     The criteria used by the server to find objects. If this parameter is null, then this call
        ///     is a continuation of the previous find.
        /// </param>
        /// <param name="numberToReturn"> The maximum number of objects to return in a single response. </param>
        /// <returns>
        ///     <para> The list of object attributes for the objects that met the filter criteria. </para>
        ///     <para>
        ///         Returns null if the starting object is a leaf, or no objects were found that meet the filter criteria, or if
        ///         the call was made with a null findCriteria and there are no more objects to return.
        ///     </para>
        /// </returns>
        public IEnumerable<ObjectAttributes>? FindObjects(FindCriteria findCriteria, uint numberToReturn)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();

            return _context.FindObjects(findCriteria, numberToReturn);
        }

        /// <summary>
        ///     This method is used to request summary information for the
        ///     alarms that can be generated for a given event source.
        /// </summary>
        /// <param name="eventSourceId"> The InstanceId for the event source for which alarm summaries are being requested. </param>
        /// <returns> The summaries of the alarms that can be generated by the specified event source. </returns>
        public IEnumerable<AlarmSummary>? GetAlarmSummary(InstanceId eventSourceId)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();

            return _context.GetAlarmSummary(eventSourceId);
        }

        public PassthroughResult? Passthrough(string recipientId,
                                      string passthroughName, ReadOnlyMemory<byte> dataToSend)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();
            
            return _context.Passthrough(recipientId,
                                      passthroughName, dataToSend);
        }

        /// <summary>
        ///     Returns StatusCode <see cref="StatusCodes"/>
        /// </summary>
        /// <param name="recipientId"></param>
        /// <param name="passthroughName"></param>
        /// <param name="dataToSend"></param>
        /// <param name="callbackAction"></param>
        /// <returns></returns>
        public async Task<Task<uint>> LongrunningPassthroughAsync(string recipientId, string passthroughName, ReadOnlyMemory<byte> dataToSend,
            Action<Ssz.Utils.DataAccess.LongrunningPassthroughCallback>? callbackAction)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();
            
            return await _context.LongrunningPassthroughAsync(recipientId, passthroughName, dataToSend, callbackAction);
        }                

        /// <summary>
        ///     This property is the Windows LocaleId (language/culture id) for the context.
        ///     Its default value is automatically set to the LocaleId of the calling client application.
        /// </summary>
        public uint LocaleId
        {
            get { return _localeId; }
            set { if (_context is null) _localeId = value; }
        }

        /// <summary>
        ///     This property specifies how long the context will stay alive in the server after a WCF
        ///     connection failure. The ClientBase will attempt reconnection during this period.
        /// </summary>
        public TimeSpan ContextTimeout
        {
            get { return _contextTimeout; }
            set { if (_context is null) _contextTimeout = value; }
        }

        /// <summary>
        ///     This property indicates, when TRUE, that the client has an open context (session) with
        ///     the server.
        /// </summary>
        public bool ContextExists
        {
            get { return _context is not null; }
        }        

        /// <summary>
        ///     This property contains the client-requested keepAliveSkipCount for the subscription.
        ///     The server may negotiate this value up or down. The keepAliveSkipCount indicates
        ///     the number of consecutive UpdateRate cycles for a list that occur with nothing to
        ///     send before an empty callback is sent to indicate a keep-alive message. For example,
        ///     if the value of this parameter is 1, then a keep-alive callback will be sent each
        ///     UpdateRate cycle for each list assigned to the callback for which there is nothing
        ///     to send.  A value of 0 indicates that keep-alives are not to be sent for any list
        ///     assigned to the callback.
        /// </summary>
        public uint KeepAliveSkipCount
        {
            get { return _keepAliveSkipCount; }
            set { if (_context is null) _keepAliveSkipCount = value; }
        }

        /// <summary>
        ///     <para>
        ///         This property indicates the maximum time between callbacks that are sent to the client. The server may
        ///         negotiate this value up or down, but a null value or a value representing 0 time is not valid.
        ///     </para>
        ///     <para>
        ///         If there are no callbacks to be sent containing data or events for this period of time, an empty callback
        ///         will be sent as a keep-alive. The timer for this time-interval starts when the SetCallback() response is
        ///         returned by the server.
        ///     </para>
        /// </summary>
        public TimeSpan CallbackRate
        {
            get { return _callbackRate; }
            set { if (_context is null) _callbackRate = value; }
        }

        public string ContextId
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

                if (_context is null) throw new XiServerNotExistException();

                return _context.ContextId;
            }
        }

        public void KeepContextAlive(DateTime nowUtc)
        {
            if (_disposed) throw new ObjectDisposedException("Cannot access a disposed XiServerProxy.");

            if (_context is null) throw new XiServerNotExistException();

            _context.KeepContextAlive(nowUtc);
        }

        public static uint NormalizeStatusCode(uint statusCode)
        {
            return StatusCodes.Good;
            //if ((XiStatusCode.StatusBits(statusCode) & (byte)XiStatusCodeStatusBits.GoodNonSpecific) != 0)
            //    return StatusCodes.Good;
            //else
            //    return StatusCodes.Bad;
        }

        #endregion

        #region private functions

        /// <summary>
        ///     This method is used to close a context with the server and disconnect the WCF connection.
        /// </summary>
        private void ConcludeXiContextInternal()
        {
            if (_context is null) return;

            try
            {
                _context.ContextNotifyEvent -= XiContext_ContextNotifyEvent;

                XiListRoot[] lists = _context.ListArray;
                _context.Dispose();
                _context = null;

                // Dispose of all the lists
                foreach (XiListRoot list in lists)
                {
                    list.Dispose();
                }                
            }
            catch
            {
                //Logger?.LogWarning(ex, "ConcludeXiContextInternal exception.");
            }
        }

        private void XiContext_ContextNotifyEvent(object sender, XiContextNotificationData notificationData)
        {
            switch (notificationData.ReasonForNotification)
            {
                case XiContextNotificationType.ClientKeepAliveException:
                case XiContextNotificationType.ServerKeepAliveError:
                case XiContextNotificationType.EndpointDisconnected:
                case XiContextNotificationType.EndpointFail:
                case XiContextNotificationType.GeneralException:
                case XiContextNotificationType.PollException:
                case XiContextNotificationType.ReInitiateException:
                case XiContextNotificationType.ResourceManagementDisconnected:
                case XiContextNotificationType.ResourceManagementFail:
                case XiContextNotificationType.Shutdown:
                    ConcludeXiContext();
                    break;
            }
        }
        
        #endregion

        //private LeveledLock _syncRoot = new LeveledLock(1100);

        #region private fields

        private bool _disposed;
        
        /// <summary>
        ///     This property contains the client-requested keepAliveSkipCount for the subscription.
        ///     The server may negotiate this value up or down. The keepAliveSkipCount indicates
        ///     the number of consecutive UpdateRate cycles for a list that occur with nothing to
        ///     send before an empty callback is sent to indicate a keep-alive message. For example,
        ///     if the value of this parameter is 1, then a keep-alive callback will be sent each
        ///     UpdateRate cycle for each list assigned to the callback for which there is nothing
        ///     to send.  A value of 0 indicates that keep-alives are not to be sent for any list
        ///     assigned to the callback.
        /// </summary>
        private uint _keepAliveSkipCount;

#if DEBUG
        /// <summary>
        ///     This data member is the private representation of the CallbackRate public member.
        /// </summary>
        private TimeSpan _callbackRate = new TimeSpan(10, 0, 0);
#else
    /// <summary>
    ///   This data member is the private representation of the CallbackRate public member.
    /// </summary>
        private TimeSpan _callbackRate = new TimeSpan(0, 0, 10);
#endif        

#if DEBUG
        /// <summary>
        ///     This data member is the private representation of the ContextTimeout public property.
        /// </summary>
        private TimeSpan _contextTimeout = new TimeSpan(0, 30, 0);
#else
    /// <summary>
    ///   This data member is the private representation of the ContextTimeout public property.
    /// </summary>
        private TimeSpan _contextTimeout = new TimeSpan(0, 0, 30);
#endif

        /// <summary>
        ///     This data member is the private representation of the LocaleId public property. It defaults
        ///     to the locale id of the client application.
        /// </summary>
        private uint _localeId = (uint) Thread.CurrentThread.CurrentCulture.LCID;

        /// <summary>
        ///     This data member is the private representation of the XiContext public property.
        /// </summary>
        private XiContext? _context;

        #endregion
    }
}