using System;

namespace Ssz.Xi.Client.Internal.ListItems
{
    /// <summary>
    ///     This is the base class for elements of all XiLists (e.g. DataList, EventList).
    ///     XiLists maintain their elements in a Keyed Collection.
    /// </summary>
    internal abstract class XiListItemRoot : IDisposable
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

            if (disposing)
            {
                // Release and Dispose managed resources.            
            }

            // Release unmanaged resources.
            // Set large fields to null.            
            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~XiListItemRoot()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This property is provided for the Xi Client
        ///     application to associate this list element with an
        ///     object of its choosing.
        /// </summary>
        public object? Obj { get; set; }

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        /*
        /// <summary>
        /// This property indicates when TRUE that this list element 
        /// is in a state in which it may be disposed.
        /// </summary>
        /// <returns>Returns TRUE if this list element may be disposed</returns>
        internal bool IsDisposable
        {
            get
            {
                return (XiListElementState.Disposable == State
                    || XiListElementState.AddedElement == State);
            }
        }

        /// <summary>
        /// This method sets the state of this list element to disposable.
        /// </summary>
        /// <returns>Returns TRUE if the state was successfully set to disposable.</returns>
        internal protected bool SetStateToDisposable()
        {
            if (IsDisposable)
                return true;

            State = XiListElementState.Disposable;

            return IsDisposable;
        }

        /// <summary>
        /// This data member is the private representation of 
        /// the public State property.
        /// </summary>
        protected XiListElementState _state;        

        */
    }
}

/*
    /// <summary>
    /// This enumeration defines the allowable states of and XiList element.
    /// </summary>
    [Flags]
    enum XiListItemState : ushort
    {
        None = 0x00,
        InClientList = 0x01,
        InServerList = 0x02,
        Enabled = 0x4,
        MarkedForAddToServer = 0x8,
        MarkedForRemoveFromServer = 0x10,        
    }    
    public enum XiListElementState
    {
        /// <summary>
        /// This state indicates that the list element has just been created and added to the
        /// Xi Client List.  However, it has not been added to the Xi Server List.
        /// </summary>
        AddedElement = 1,

        /// <summary>
        /// This state indicates that the list element has been added to the Xi Server List 
        /// and is currently not Enabled.
        /// </summary>
        Disabled = 2,

        /// <summary>
        /// This state indicates that the list element has been added to the Xi Server List 
        /// and is currently Enabled.
        /// </summary>
        Enabled = 3,

        /// <summary>
        /// This state indicates that the list element may be removed from the Xi Server's List.
        /// </summary>
        RemoveableFromServer = 4,

        /// <summary>
        /// This state indicates that the list element has been removed from both the Xi Server's 
        /// List and the Xi Client's List and is ready for Dispose or Finalize.  Note: It is not 
        /// valid to Dispose / Finalize (garbage collect) a list element before its state is 
        /// Disposable Value.
        /// </summary>
        Disposable = 5,
    }*/