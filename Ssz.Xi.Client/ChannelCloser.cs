/**********************************************************************
 * Copyright © 2009, 2010, 2011, 2012 OPC Foundation, Inc. 
 *
 * The source code and all binaries built with the OPC .NET 3.0 source
 * code are subject to the terms of the Express Interface Public
 * License (Xi-PL).  See http://www.opcfoundation.org/License/Xi-PL/
 *
 * The source code may be distributed from an OPC member company in
 * its original or modified form to its customers and to any others who
 * have software that needs to interoperate with the OPC member's OPC
* .NET 3.0 products. No other redistribution is permitted.
 *
 * You must not remove this notice, or any other, from this software.
 *
 *********************************************************************/

using System;
using System.ServiceModel;

namespace Ssz.Xi.Client
{
    /// <summary>
    ///     This class is used to properly close a WCF client proxy.  It aborts or closes
    ///     the proxy based on the channel status and whether an exception is encountered.
    ///     It has two calling methods, one where the proxy has a short life, and the other
    ///     to be used when the proxy is held over the life of a single method.
    /// </summary>
    /// <example>
    ///     Usage 1: Short lived proxy
    ///     void SomeMethod()
    ///     {
    ///     SomeWcfProxy proxy = new SomeWcfProxy();
    ///     using (new ChannelCloser(proxy))
    ///     {
    ///     proxy.MakeCall();
    ///     ...
    ///     }
    ///     }
    ///     Usage 2: Long lived proxy
    ///     void CreateProxy()
    ///     {
    ///     SomeWcfProxy proxy = new SomeWcfProxy();
    ///     ...
    ///     }
    ///     void DestroyProxy()
    ///     {
    ///     ChannelCloser.Close(proxy);
    ///     }
    /// </example>
    public class ChannelCloser : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Constructs a WCF channel closer object
        /// </summary>
        /// <param name="channelObj">WCF proxy object</param>
        public ChannelCloser(object channelObj)
        {
            if (!(channelObj is ICommunicationObject))
                throw new ArgumentException("Channel object must implement ICommunicationObject");
            _channel = (ICommunicationObject) channelObj;
        }

        /// <summary>
        ///     Properly releases and closes the held WCF proxy
        /// </summary>
        public void Dispose()
        {
            Close(_channel);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This method closes the passed proxy object
        /// </summary>
        /// <param name="obj"></param>
        public static void Close(object obj)
        {
            if (obj != null)
            {
                var channelObj = obj as ICommunicationObject;
                if (channelObj == null)
                    throw new ArgumentException("Channel object must implement ICommunicationObject", "obj");

                // if the channel faults you cannot Close/Dispose it - instead you have to Abort it
                if (channelObj.State == CommunicationState.Faulted)
                    channelObj.Abort();
                else if (channelObj.State != CommunicationState.Closed)
                {
                    try
                    {
                        channelObj.Close();
                    }
                    catch (TimeoutException)
                    {
                        channelObj.Abort();
                    }
                    catch (CommunicationException)
                    {
                        channelObj.Abort();
                    }
                }
            }
        }

        #endregion

        #region private fields

        private readonly ICommunicationObject _channel;

        #endregion
    }
}