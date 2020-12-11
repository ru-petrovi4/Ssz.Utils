using Ssz.Xi.Client.Api;
using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Internal.ListItems
{
    /// <summary>
    ///     This class is used to represent a vendor-specific field of an event message.
    /// </summary>
    internal class XiEventMessageFieldValue
    {
        #region construction and destruction

        /// <summary>
        ///     The constructor for the XiEventMessageFieldValue
        /// </summary>
        /// <param name="name"> The name of the field. </param>
        /// <param name="dataTypeId"> The data type of the field. </param>
        public XiEventMessageFieldValue(string name, TypeId dataTypeId)
        {
            Name = name;
            DataTypeId = dataTypeId;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This property contains the name of the Event Message field.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        ///     This property indicates, when TRUE, that this field has been selected by the
        ///     client application to be returned in the Event Message.
        /// </summary>
        public bool Selected { get; internal set; }

        /// <summary>
        ///     This property defines the data type of the field.
        /// </summary>
        public TypeId DataTypeId { get; set; }

        /// <summary>
        ///     This property contains the value of the field.
        /// </summary>
        public XiValueStatusTimestamp? Value { get; set; }

        #endregion
    }
}