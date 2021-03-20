using System;
using Ssz.Utils;
using Xi.Common.Support;

namespace Ssz.DataGrpc.Server.Core.ListItems
{
    /// <summary>
    ///   This class provides common basic values for the Data List Values.
    ///   A Data List is used to represent real time process values.  This
    ///   base class provides some common properties runtime Data Values.
    /// </summary>
    public class ServerElementValueListItem : ServerElementListItemBase
    {
        #region construction and destruction

        /// <summary>
        ///   Constructs a new instance of the <see cref = "ElementValueListItem" /> class.
        /// </summary>
        public ElementValueListItem(uint clientAlias, uint serverAlias)
            : base(clientAlias, serverAlias)
        {
        }

        #endregion

        /*
		protected override void Dispose(bool disposing)
		{
			if (Disposed) return;
			if (disposing)
			{
				// Release and Dispose managed resources.
			}
			// Release unmanaged resources.
			// Set large fields to null.
			base.Dispose(disposing);
		}*/
        /*
		/// <summary>
		/// The actual Data Value is simply stored as a .NET object.  Thus
		/// it may represent any data value from a simple intrisic data type, 
		/// an array of intrisic data types or an instance of a class.  
		/// Note that if a class is to be represented it must be defined 
		/// with a Data Contract and Data Memebers.
		/// </summary>
		public object Value
		{ 
			get 
			{
				switch (ValueTransportTypeKey)
				{
					case TransportDataType.Double:
						return _doubleValue;
					case TransportDataType.Uint:
						return _uintValue;
					case TransportDataType.Object:
						return _objectValue;
					default:
						break;
				}
				return null; 
			} 
			//set { _value = value; }
		}*/

        #region public functions

        /// <summary>
        ///   This property is provides the data type used to transported the value.
        /// </summary>
        public override TransportDataType ValueTransportTypeKey { get
        {
            switch (Value.ValueStorageType)
            {
                case Any.StorageType.UInt32:
                    return TransportDataType.Uint;
                case Any.StorageType.Double:
                    return TransportDataType.Double;
                case Any.StorageType.Object:
                    return TransportDataType.Object;
                default:
                    return TransportDataType.Unknown;
            }
        } }

        /// <summary>
        ///   This property is used to track or indicate the 
        ///   active or inactive state of this Data List Value.
        /// </summary>
        public bool UpdatingEnabled { get; set; }

        /// <summary>
        ///   The Time Stamp is a UTC time.
        /// </summary>
        public DateTime TimestampUtc { get; set; }

        /// <summary>
        ///   Value.
        /// </summary>
        public Any Value;

        #endregion
    }
}