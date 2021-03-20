using System;
using System.Collections.Generic;
using System.ServiceModel;
using Ssz.DataGrpc.Server.Core.Lists;
using Xi.Common.Support;
using Xi.Contracts.Constants;

namespace Ssz.DataGrpc.Server.Core.Context
{
    /// <summary>
    ///   This partial class defines the methods to be overridden by the server implementation 
    ///   to support the Context Management methods of the IResourceManagement interface.
    /// </summary>
    public partial class ServerContext        
    {
        #region public functions

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        /// <param name = "negotiatedContextOptions"></param>
        /// <param name = "reInitiateKey"></param>
        public void OnReInitiate(bool allowDifferentClientIpAddress, uint negotiatedContextOptions,
                                 ref string reInitiateKey)
        {
            using (SyncRoot.Enter())
            {
                if (Disposed) throw new ObjectDisposedException("Cannot access a disposed ServerContext.");

                if (ReInitiateKey == null) // Not having an existing ReInitiateKey is an error
                {
                    LastAccessUtc = DateTime.UtcNow - ContextTimeout; // set last access back so it will timeout
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_NOCONTEXT);
                }
                    // otherwise if the reInitiateKey parameter doesn't match the ReIniitateKey of the context
                else if ((reInitiateKey == null) || (string.Compare(reInitiateKey, ReInitiateKey, false) != 0))
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_BADARGUMENT);

                // Validate OperationContext 
                if (OperationContext.Current == null)
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_INVALIDREQUEST, "No OperationsContext");

                // No security?
                if (OperationContext.Current.ServiceSecurityContext == null)
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_INVALIDREQUEST, "No ServiceSecurityContext");

                // Different user name?
                if (OperationContext.Current.ServiceSecurityContext.PrimaryIdentity.Name != Identity.Name)
                    throw RpcExceptionHelper.Create(XiFaultCodes.E_INVALIDREQUEST, "Different User");

                if (allowDifferentClientIpAddress == false)
                    // if the client connection must originate on the same machine as the last
                {
                    // Different client IP Address?
                    string clientIpAddress = GetRemoteIpAddress(); // The ip address used to send the current request.

                    if (string.Compare(_connectedResourceManagementEndpoint.ClientIpAddress, clientIpAddress) != 0)
                        throw RpcExceptionHelper.Create(XiFaultCodes.E_INVALIDREQUEST, "Invalid IP Address");
                }

                NegotiatedContextOptions = negotiatedContextOptions;

                // call implementation-specific processing
                OnReInitiate(OperationContext.Current);
            }
        }

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project. This method is invoked when the context times-out.
        /// </summary>
        public abstract void OnClientKeepAlive();

        /// <summary>
        ///   This method validates that a Read, Write, Poll, or RegisterForCallback endpoint can be used.
        ///   Precondition: Context must be locked.
        /// </summary>
        /// <param name = "endpointEntry">The endpoint item for the Read, Write, Poll, or RegisterForCallback endpoint.</param>
        public void AuthorizeEndpointUse(EndpointEntry<TListRoot> endpointEntry)
        {
            bool success = false;
            string clientIpAddress = GetRemoteIpAddress(); // The ip address used to send the current request.

            if (endpointEntry != null)
            {
                if (endpointEntry.WcfChannel == null)
                    // The WcfChannel will be null for read, write, and poll endpoints the first time through
                {
                    if (string.Compare(_connectedResourceManagementEndpoint.ClientIpAddress, clientIpAddress) == 0)
                    {
                        endpointEntry.IsOpen = true;
                        endpointEntry.WcfChannel = OperationContext.Current.Channel;
                        endpointEntry.SessionId = OperationContext.Current.SessionId;
                        endpointEntry.ClientIpAddress = clientIpAddress;
                        success = true;
                    }
                }
                else if (clientIpAddress == endpointEntry.ClientIpAddress)
                {
                    // if a new session after recovery
                    if (endpointEntry.SessionId != OperationContext.Current.SessionId)
                    {
                        endpointEntry.WcfChannel = OperationContext.Current.Channel;
                        endpointEntry.SessionId = OperationContext.Current.SessionId;
                    }
                    success = true;
                }
            }
            if (success == false)
            {
                // TODO:  Log the authorization failure in the AuthorizeEndpointUse() method
                ChannelCloser.Close(OperationContext.Current.Channel);
            }
        }

        #endregion

        #region protected functions

        /// <summary>
        ///   This method is to be overridden by the context implementation in the 
        ///   Server Implementation project.
        /// </summary>
        protected virtual void OnReInitiate(OperationContext ctx)
        {
            // TODO - Implement an override of the OnReInitiate method for any additional processing associated with OnReInitiate();
        }

        #endregion

        #region private functions

        private void CloseEndpointConnections()
        {
            if ((_connectedResourceManagementEndpoint != null) &&
                (_connectedResourceManagementEndpoint.WcfChannel != null))
            {
                try
                {
                    ChannelCloser.Close(_connectedResourceManagementEndpoint.WcfChannel);
                }
                catch
                {
                }
                _connectedResourceManagementEndpoint.WcfChannel = null;
                _connectedResourceManagementEndpoint.Dispose();
                _connectedResourceManagementEndpoint = null;
            }

            if ((_iRegisterForCallbackEndpointEntry != null) && (_iRegisterForCallbackEndpointEntry.WcfChannel != null))
            {
                try
                {
                    ChannelCloser.Close(_iRegisterForCallbackEndpointEntry.WcfChannel);
                }
                catch
                {
                }
                _iRegisterForCallbackEndpointEntry.WcfChannel = null;
                _iRegisterForCallbackEndpointEntry.Dispose();
                _iRegisterForCallbackEndpointEntry = null;
            }

            foreach (KeyValuePair<string, EndpointEntry<TListRoot>> ep in _endpoints)
            {
                if (ep.Value.WcfChannel != null)
                {
                    try
                    {
                        ChannelCloser.Close(ep.Value.WcfChannel);
                    }
                    catch
                    {
                    }
                    ep.Value.WcfChannel = null;
                    ep.Value.Dispose();
                }
            }
            _endpoints.Clear();
        }

        #endregion
    }
}