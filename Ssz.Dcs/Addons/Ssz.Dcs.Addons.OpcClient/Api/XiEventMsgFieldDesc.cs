using Xi.Contracts.Data;

namespace Ssz.Xi.Client.Api
{
    /// <summary>
    ///     This class is used to describe optional, server-specific event message fields supported by the server.
    /// </summary>
    public class XiEventMsgFieldDesc
    {
        #region construction and destruction

        /// <summary>
        ///     This constructor creates an instance of the event message field.
        /// </summary>
        /// <param name="name"> The name of the field. </param>
        /// <param name="description"> The description of the field </param>
        /// <param name="objectTypeId"> The object type of the field </param>
        /// <param name="dataTypeId"> The data type of the field </param>
        public XiEventMsgFieldDesc(string name, string description, TypeId objectTypeId, TypeId dataTypeId)
        {
            _name = name;
            _description = description;
            _objectTypeId = objectTypeId;
            _dataTypeId = dataTypeId;
            Selected = false;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The display name of the parameter, field, or property.  Names
        ///     are not permitted to contain the forward slash ('/') character.
        ///     This name is used as the FilterOperand in FilterCriterion.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        ///     The object type of the field.
        /// </summary>
        public TypeId ObjectTypeId
        {
            get { return _objectTypeId; }
        }

        /// <summary>
        ///     The data type of the field.
        /// </summary>
        public TypeId DataTypeId
        {
            get { return _dataTypeId; }
        }

        /// <summary>
        ///     This property indicates, when TRUE, that the field has been selected to be returned in event messages.
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        ///     The optional description of the field.  Null if unused.
        /// </summary>
        public string Description
        {
            get { return _description; }
        }

        #endregion

        #region private fields

        /// <summary>
        ///     The private representation of the DataTypeId property
        /// </summary>
        private readonly TypeId _dataTypeId;

        /// <summary>
        ///     The private representation of the Description property
        /// </summary>
        private readonly string _description;

        /// <summary>
        ///     The private representation of the Name property
        /// </summary>
        private readonly string _name;

        /// <summary>
        ///     The private representation of the ObjectTypeId property
        /// </summary>
        private readonly TypeId _objectTypeId;

        #endregion
    }
}