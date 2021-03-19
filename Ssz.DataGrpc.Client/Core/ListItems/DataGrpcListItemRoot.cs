using System;

namespace Ssz.DataGrpc.Client.Core.ListItems
{
    /// <summary>
    ///     This is the base class for elements of all DataGrpcLists (e.g. ElementValueList, EventList).
    ///     DataGrpcLists maintain their elements in a Keyed Collection.
    /// </summary>
    public abstract class DataGrpcListItemRoot : IDisposable
    {
        #region construction and destruction

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

            //if (disposing)
            //{
            //    // Release and Dispose managed resources.            
            //}

            // Release unmanaged resources.
            // Set large fields to null.            
            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~DataGrpcListItemRoot()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public bool Disposed { get; private set; }

        /// <summary>
        ///     This property is provided for the DataGrpc Client
        ///     application to associate this list element with an
        ///     object of its choosing.
        /// </summary>
        public object? Obj { get; set; }

        #endregion
    }
}

/*
    /// <summary>
    /// This enumeration defines the allowable states of and DataGrpcList element.
    /// </summary>
    [Flags]
    enum DataGrpcListItemState : ushort
    {
        None = 0x00,
        InClientList = 0x01,
        InServerList = 0x02,
        Enabled = 0x4,
        MarkedForAddToServer = 0x8,
        MarkedForRemoveFromServer = 0x10,        
    }    
    public enum DataGrpcListElementState
    {
        /// <summary>
        /// This state indicates that the list element has just been created and added to the
        /// DataGrpc Client List.  However, it has not been added to the DataGrpc Server List.
        /// </summary>
        AddedElement = 1,

        /// <summary>
        /// This state indicates that the list element has been added to the DataGrpc Server List 
        /// and is currently not Enabled.
        /// </summary>
        Disabled = 2,

        /// <summary>
        /// This state indicates that the list element has been added to the DataGrpc Server List 
        /// and is currently Enabled.
        /// </summary>
        Enabled = 3,

        /// <summary>
        /// This state indicates that the list element may be removed from the DataGrpc Server's List.
        /// </summary>
        RemoveableFromServer = 4,

        /// <summary>
        /// This state indicates that the list element has been removed from both the DataGrpc Server's 
        /// List and the DataGrpc Client's List and is ready for Dispose or Finalize.  Note: It is not 
        /// valid to Dispose / Finalize (garbage collect) a list element before its state is 
        /// Disposable Value.
        /// </summary>
        Disposable = 5,
    }*/


/*
        /// <summary>
        /// This property indicates when TRUE that this list element 
        /// is in a state in which it may be disposed.
        /// </summary>
        /// <returns>Returns TRUE if this list element may be disposed</returns>
        public bool IsDisposable
        {
            get
            {
                return (DataGrpcListElementState.Disposable == State
                    || DataGrpcListElementState.AddedElement == State);
            }
        }

        /// <summary>
        /// This method sets the state of this list element to disposable.
        /// </summary>
        /// <returns>Returns TRUE if the state was successfully set to disposable.</returns>
        public protected bool SetStateToDisposable()
        {
            if (IsDisposable)
                return true;

            State = DataGrpcListElementState.Disposable;

            return IsDisposable;
        }

        /// <summary>
        /// This data member is the private representation of 
        /// the public State property.
        /// </summary>
        protected DataGrpcListElementState _state;        

        */