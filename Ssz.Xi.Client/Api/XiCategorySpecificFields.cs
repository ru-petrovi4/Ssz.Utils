using System.Collections.Generic;

namespace Ssz.Xi.Client.Api
{
    /// <summary>
    ///     This class defines the list of category=specific fields that the client can request the server to add to event
    ///     messages.
    /// </summary>
    public class XiCategorySpecificFields
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates an empty (no fields) XiCategorySpecificFields instance for the specified category.
        /// </summary>
        /// <param name="categoryId"> </param>
        public XiCategorySpecificFields(uint categoryId)
        {
            _categoryId = categoryId;
            _optionalEventMsgFields = new List<XiEventMsgFieldDesc>();
        }

        #endregion

        #region public functions

        /// <summary>
        ///     This property identifies the category to which the event message fields belong.
        /// </summary>
        public uint CategoryId
        {
            get { return _categoryId; }
        }

        /// <summary>
        ///     This property contains the server-specific fields supported for the specified category.
        /// </summary>
        public List<XiEventMsgFieldDesc> OptionalEventMsgFields
        {
            get { return _optionalEventMsgFields; }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     The private representation of the CategoryId property
        /// </summary>
        private readonly uint _categoryId;

        /// <summary>
        ///     The private representation of the OptionalEventMsgFields property
        /// </summary>
        private readonly List<XiEventMsgFieldDesc> _optionalEventMsgFields;

        #endregion
    }
}