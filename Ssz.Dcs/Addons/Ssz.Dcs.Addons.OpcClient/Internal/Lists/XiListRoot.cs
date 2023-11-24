using System;
using System.Collections.Generic;
using Ssz.Xi.Client.Internal.Context;
using Ssz.Xi.Client.Internal.Endpoints;
using Xi.Contracts;
using Xi.Contracts.Constants;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.Lists
{
    /// <summary>
    ///     This abstract class definition allows for the implantation of methods that are
    ///     common to two or more Xi List types.  The Xi Values maintained by this class
    ///     must be a subclass of Xi Value Base.  In general the only time a declaration
    ///     of this type would be used is when the data type can also be processed
    ///     as being of type Xi Value Base.
    /// </summary>
    internal abstract class XiListRoot : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Xi List Base is the common base class for all Xi Lists defined within
        ///     the Client Base Assembly.
        /// </summary>
        /// <param name="context"> </param>
        protected XiListRoot(XiContext context)
        {
            _context = context;
        }

        /// <summary>
        ///     This is the implementation of the IDisposable.Dispose method.  The client
        ///     application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     This method is invoked when the IDisposable.Dispose or Finalize actions are
        ///     requested.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                foreach (XiEndpointRoot endpoint in Endpoints)
                {
                    if (!endpoint.Disposed) endpoint.UnassignList(this);
                }

                _endpoints.Clear();

                if (!_context.ServerContextIsClosing)
                {
                    try
                    {
                        _context.RemoveList(this);
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            // Release unmanaged resources.
            // Set large fields to null.            
            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~XiListRoot()
        {
            Dispose(false);
        }

        #endregion

        /*
        public void Func()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ListRoot.");
        }
        */

        #region public functions

        /// <summary>
        ///     <para>
        ///         This method is used to enable or disable updating of an entire list. When this method is called, the enabled
        ///         state of the list is changed, but the enabled state of the individual elements of the list is unchanged.
        ///     </para>
        ///     <para>
        ///         When a list is disabled, the server excludes it from participating in callbacks and polls. However, at the
        ///         option of the server, the server may continue updating its cache for the elements of the list.
        ///     </para>
        ///     <para> Calling this method also causes the local copy of the list attributes to be updated. </para>
        /// </summary>
        /// <param name="enableUpdating">
        ///     Indicates, when TRUE, that updating of the list is to be enabled, and when FALSE, that
        ///     updating of the list is to be disabled.
        /// </param>
        /// <returns> Returns TRUE if the list was successfully enabled or disabled. </returns>
        public bool EnableListUpdating(bool enableUpdating)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");
            
            if (ServerListId != 0 && (ListAttributes is null || ListAttributes.Enabled != enableUpdating))
            {
                _context.EnableListUpdating(ServerListId, enableUpdating);
                GetListAttributes();
            }
            return ListAttributes == null? false : ListAttributes.Enabled;
        }

        /// <summary>
        ///     This method is used to change the update rate, buffering rate, and/or
        ///     filter set of a list.  The new value replace the old values if they exist.
        /// </summary>
        /// <param name="updateRate">
        ///     The new update rate of the list. The server will negotiate this rate to one that it can
        ///     support. GetListAttributes can be used to obtain the current value of this parameter. Null if the update rate is
        ///     not to be updated.
        /// </param>
        /// <param name="bufferingRate">
        ///     The new buffering rate of the list. The server will negotiate this rate to one that it can
        ///     support. GetListAttributes can be used to obtain the current value of this parameter. Null if the buffering rate is
        ///     not to be updated.
        /// </param>
        /// <param name="filterSet">
        ///     The new set of filters. The server will negotiate these filters to those that it can support.
        ///     GetListAttributes can be used to obtain the current value of this parameter. Null if the filters are not to be
        ///     updated.
        /// </param>
        /// <returns>
        ///     The revised update rate, buffering rate, and filter set. Attributes that were not updated are set to null in
        ///     this response.
        /// </returns>
        public ModifyListAttrsResult? ModifyListAttributes(uint? updateRate, uint? bufferingRate, FilterSet filterSet)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

            ModifyListAttrsResult? modListAttrs = null;
            if (ServerListId != 0)
            {
                _context.ModifyListAttributes(ServerListId, updateRate, bufferingRate, filterSet);
                if (null != modListAttrs)
                {
                    if (ListAttributes is null) throw new InvalidOperationException();
                    if (null != modListAttrs.RevisedUpdateRate)
                        ListAttributes.UpdateRate = modListAttrs.RevisedUpdateRate.Value;
                    if (null != modListAttrs.RevisedFilterSet) ListAttributes.FilterSet = modListAttrs.RevisedFilterSet;
                }
            }            
            return modListAttrs;
        }

        /// <summary>
        ///     This method is used to retrieve the attributes of this list from the server.
        /// </summary>
        /// <returns> The attributes of this list. </returns>
        public void GetListAttributes()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

            try 
            {
                ListAttributes = _context.GetListAttributes(ServerListId);
            }
            catch (Exception) 
            {
            }            
        }

        /// <summary>
        ///     <para> This method is used to cause a list to be "touched". </para>
        ///     <para>
        ///         For lists that contain data objects, this method causes the server to update all data objects in the list
        ///         that are currently enabled (see the XiDataList EnableListElementUpdating() method), mark them as changed (even
        ///         if their values did not change), and then return them all to the client in the next callback or poll.
        ///     </para>
        ///     <para>
        ///         For lists that contain events, this method causes the server to mark all alarms/event in the list as
        ///         changed, and then return them all to the client in the next callback.
        ///     </para>
        /// </summary>
        /// <returns> The result code for the operation. See XiFaultCodes class for standardized result codes. </returns>
        public uint TouchList()
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

            return _context.TouchList(ServerListId);
        }

        /// <summary>
        ///     Use this method to update this Xi List with a replacement Xi Endpoint.
        ///     The method is used when an endpoint loses communication and is replaced
        ///     by a new endpoint
        /// </summary>
        /// <param name="failedEndpoint"> The failed endpoint </param>
        /// <param name="replacementEndpoint"> The endpoint that replaces the failed endpoint </param>
        public void ReplaceEndpointInList(XiEndpointRoot failedEndpoint, XiEndpointRoot replacementEndpoint)
        {
            if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

            if (_endpoints.Contains(failedEndpoint))
            {
                _endpoints.Add(replacementEndpoint);
                _endpoints.Remove(failedEndpoint);
            }
        }

        /// <summary>
        ///     This property provides the Xi Context to which this list belongs.
        /// </summary>
        public XiContext Context
        {
            get { return _context; }
        }

        /// <summary>
        ///     Use this property to obtain the List Type as a string value.
        /// </summary>
        public string ListTypeAsString
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

                if (ListAttributes is null) throw new InvalidOperationException();
                if (ListAttributes.ListType < 4096)
                {
                    var lt = (StandardListType) ListAttributes.ListType;
                    return lt.ToString("G");
                }
                return ListAttributes.ListType.ToString();
            }
        }

        /// <summary>
        ///     The Client LocalId provides a handle by which this Xi List is known within
        ///     the client code.  This value is established by the client code.
        /// </summary>
        public uint ClientListId
        {
            get { return (null == ListAttributes) ? 0 : ListAttributes.ClientId; }
        }

        /// <summary>
        ///     The Server LocalId provides a handle by which this Xi List is known within
        ///     the server code.  This value is established by the server code.
        /// </summary>
        public uint ServerListId
        {
            get { return (null == ListAttributes) ? 0 : ListAttributes.ServerId; }
        }

        /// <summary>
        ///     This property identifies the Standard Xi List Type of this list.
        /// </summary>
        public StandardListType StandardListType { get; protected set; }

        /// <summary>
        ///     This property returns a copy of the Xi List Attributes from the server.
        ///     The ModifyListAttributes method is used oo change the List Attribute.
        /// </summary>
        public ListAttributes? ListAttributes { get; protected set; }

        /// <summary>
        ///     This property is provided for the Xi Client application to associate this list
        ///     with an object of its choosing.
        /// </summary>
        public object? ClientTag { get; set; }

        /// <summary>
        ///     List of Xi Endpoints to which this Xi List has been added.
        /// </summary>
        public IEnumerable<XiEndpointRoot> Endpoints
        {
            get { return _endpoints; }
        }

        /// <summary>
        ///     This property indicates whether this subscription can be used to read values from the server.
        ///     Setting this property to TRUE causes this subscription to open an endpoint with the server
        ///     for reading.
        /// </summary>
        public bool Readable
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

                XiReadEndpoint? readEndpoint = _context.ReadEndpoint;
                if (readEndpoint is not null)
                {
                    if (readEndpoint.Disposed) return false;
                    return readEndpoint.HasListAttached(this);
                }
                return false;
            }
            set
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

                if (value)
                {
                    _context.OpenEndpointForContract(typeof (IRead).Name);
                    AddListToEndpoint(_context.ReadEndpoint);
                }
                else RemoveListFromEndpoint(_context.ReadEndpoint);
            }
        }

        /// <summary>
        ///     This property indicates whether this subscription can be used to write values to the server.
        ///     Setting this property to TRUE causes this subscription to open an endpoint with the server
        ///     for writing.
        /// </summary>
        public bool Writeable
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

                XiWriteEndpoint? writeEndpoint = _context.WriteEndpoint;
                if (writeEndpoint is not null)
                {
                    if (writeEndpoint.Disposed) return false;
                    return writeEndpoint.HasListAttached(this);
                }
                return false;
            }
            set
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

                if (value)
                {
                    _context.OpenEndpointForContract(typeof (IWrite).Name);
                    AddListToEndpoint(_context.WriteEndpoint);
                }
                else RemoveListFromEndpoint(_context.WriteEndpoint);
            }
        }

        /*
        /// <summary>
        ///   This property indicates whether this subscription can receive updates from the server. 
        ///   Setting this property to TRUE causes this subscription to be registered with the server 
        ///   for updates. When TRUE, the client application  will receive updates through an event 
        ///   specific to the type of the subscription.
        /// </summary>
        public bool Subscribeable
        {
            get
            {
                bool bCallbackEndpoint = (_context.CallbackEndpoint is not null);
                bool bPollEndpoind = (_context.PollEndpoint is not null);
                if (bCallbackEndpoint) return _context.CallbackEndpoint.HasListAttached(this);
                if (bPollEndpoind) return _context.PollEndpoint.HasListAttached(this);
                return false;
            }
            set
            {
                bool bCallbackEndpoint = (_context.CallbackEndpoint is not null);
                bool bPollEndpoint = (_context.PollEndpoint is not null);
                if (value)
                {
                    if (!bCallbackEndpoint && !bPollEndpoint)
                    {
                        if (
                            string.Compare(Uri.UriSchemeHttp,
                                           _context.ResourceManagementServiceEndpoint.ListenUri.Scheme, true) ==
                            0 ||
                            string.Compare(Uri.UriSchemeHttps,
                                           _context.ResourceManagementServiceEndpoint.ListenUri.
                                               Scheme, true) == 0)
                        {
                            _context.OpenEndpointForContract(typeof (IPoll).Name);
                            bPollEndpoint = (_context.PollEndpoint is not null);
                        }
                        else
                        {
                            _context.OpenEndpointForContract(typeof (IRegisterForCallback).Name);
                            bCallbackEndpoint = (_context.CallbackEndpoint is not null);
                        }
                    }
                    if (bCallbackEndpoint) AddListToEndpoint(_context.CallbackEndpoint);
                    if (bPollEndpoint)
                    {
                        AddListToEndpoint(_context.PollEndpoint);
                        //_context.StartPolling();
                    }
                }
                else
                {
                    if (bCallbackEndpoint) RemoveListFromEndpoint(_context.CallbackEndpoint);
                    if (bPollEndpoint) RemoveListFromEndpoint(_context.PollEndpoint);
                }
            }
        }*/

        /// <summary>
        ///     This property indicates whether this subscription can be used to read values from the server.
        ///     Setting this property to TRUE causes this subscription to open an endpoint with the server
        ///     for reading.
        /// </summary>
        public bool Callbackable
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

                XiCallbackEndpoint? callbackEndpoint = _context.CallbackEndpoint;
                if (callbackEndpoint is not null)
                {
                    if (callbackEndpoint.Disposed) return false;
                    return callbackEndpoint.HasListAttached(this);
                }
                return false;
            }
            set
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

                if (value)
                {
                    _context.OpenEndpointForContract(typeof (IRegisterForCallback).Name);
                    AddListToEndpoint(_context.CallbackEndpoint);
                }
                else RemoveListFromEndpoint(_context.CallbackEndpoint);
            }
        }

        /// <summary>
        ///     This property indicates whether this subscription can be used to read values from the server.
        ///     Setting this property to TRUE causes this subscription to open an endpoint with the server
        ///     for reading.
        /// </summary>
        public bool Pollable
        {
            get
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

                XiPollEndpoint? pollEndpoint = _context.PollEndpoint;
                if (pollEndpoint is not null)
                {
                    if (pollEndpoint.Disposed) return false;
                    return pollEndpoint.HasListAttached(this);
                }
                return false;
            }
            set
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed XiListRoot.");

                if (value)
                {
                    _context.OpenEndpointForContract(typeof (IPoll).Name);
                    AddListToEndpoint(_context.PollEndpoint);
                }
                else RemoveListFromEndpoint(_context.PollEndpoint);
            }
        }

        public bool Disposed { get; private set; }

        #endregion

        #region protected functions

        /// <summary>
        ///     This method is used to add an XiList to an Xi Endpoint.
        /// </summary>
        /// <param name="endpoint"> The endpoint to which the list is to be added. </param>
        /// <returns> The result code for the operation. See XiFaultCodes class for standardized result codes. </returns>
        protected uint AddListToEndpoint(XiEndpointRoot? endpoint)
        {
            if (ServerListId != 0 && endpoint is not null && !string.IsNullOrEmpty(endpoint.EndpointId))
            {
                if (_endpoints.Contains(endpoint)) return XiFaultCodes.S_OK;

                uint errCode = _context.AddListToEndpoint(ServerListId, endpoint.EndpointId);
                if (errCode == XiFaultCodes.S_OK)
                {
                    _endpoints.Add(endpoint);
                    if (!endpoint.Disposed) endpoint.AssignList(this);
                }
                return errCode;
            }
            return XiFaultCodes.E_ENDPOINTERROR;
        }

        /// <summary>
        ///     This method removes this list from the specified Xi Endpoint.
        /// </summary>
        /// <param name="endpoint"> The endpoint from which this list is to be removed. </param>
        /// <returns> The result code. </returns>
        protected uint RemoveListFromEndpoint(XiEndpointRoot? endpoint)
        {
            if (endpoint is null || !_endpoints.Contains(endpoint)) return XiFaultCodes.S_OK;

            uint errCode = _context.RemoveListFromEndpoint(ServerListId, endpoint.EndpointId);
            if (errCode == XiFaultCodes.S_OK)
            {
                if (!endpoint.Disposed) endpoint.UnassignList(this);
                _endpoints.Remove(endpoint);
            }
            return errCode;
        }

        #endregion

        #region private fields

        /// <summary>
        ///     This data member is the private representation of
        ///     the public Context property.
        /// </summary>
        private readonly XiContext _context;

        /// <summary>
        ///     List of Xi Endpoints to which this Xi List has been added.
        /// </summary>
        private readonly List<XiEndpointRoot> _endpoints = new List<XiEndpointRoot>(10);

        #endregion
    }
}