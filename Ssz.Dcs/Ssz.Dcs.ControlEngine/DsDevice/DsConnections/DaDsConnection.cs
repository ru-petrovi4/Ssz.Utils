using Ssz.Utils;
using Ssz.Utils.DataAccess;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public class DaDsConnection : DsConnectionBase, IValueSubscription
    {
        #region construction and destruction        

        public DaDsConnection(string connectionTypeString, byte connectionType, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) :
            base(connectionTypeString, connectionType, parentModule, parentComponentDsBlock)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                if (_subscribedElementId != @"")
                {
                    ParentModule.Device.ProcessDataAccessProvider.RemoveItem(this);
                    _subscribedElementId = @"";
                }
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        public string ElementId
        {
            get => _elementId;
            set => _elementId = value;
        }

        /// <summary>
        ///     Runtime field.
        /// </summary>
        public bool IsSubscribed => _subscribedElementId != @"";
        
        public void Update(string mappedElementIdOrConst)
        {
        }

        /// <summary>
        ///     Runtime field.
        /// </summary>
        public ValueStatusTimestamp ValueStatusTimestamp { get; private set; } = new ValueStatusTimestamp { StatusCode = StatusCodes.Uncertain };

        public void Update(ValueStatusTimestamp valueStatusTimestamp)
        {
            ValueStatusTimestamp = valueStatusTimestamp;
        }

        /// <summary>
        ///     Returns True if subscribed.
        /// </summary>
        /// <returns></returns>
        public bool Subscribe()
        {
            if (_subscribedElementId != @"")
            {
                if (_elementId != _subscribedElementId)
                {
                    ParentModule.Device.ProcessDataAccessProvider.RemoveItem(this);
                    _subscribedElementId = _elementId;                    
                    if (_subscribedElementId != @"")
                    {
                        ParentModule.Device.ProcessDataAccessProvider.AddItem(_subscribedElementId, this);
                    }
                }
            }
            else
            {
                _subscribedElementId = _elementId;
                if (_subscribedElementId != @"")
                {
                    ParentModule.Device.ProcessDataAccessProvider.AddItem(_subscribedElementId, this);                    
                }
            }
            return _subscribedElementId != @"";
        }

        /// <summary>
        ///     Sets unsubscribed state, without actual unsubscription.
        /// </summary>
        public void SetUnsubscribed()
        {
            _subscribedElementId = @"";
        }

        public override string ConnectionString
        {
            get => _elementId;
            set => _elementId = value;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            writer.Write(ElementId);            
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            ElementId = reader.ReadString();            
        }

        public override Any GetValue()
        {
            Subscribe();
            return ValueStatusTimestamp.Value;
        }

        /// <summary>
        ///     Returns Status Code (see Ssz.Utils.StatusCodes)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public override async Task<ResultInfo> SetValueAsync(Any value)
        {
            if (!Subscribe())
                return new ResultInfo { StatusCode = StatusCodes.BadInvalidState };

            return await ParentModule.Device.ProcessDataAccessProvider.WriteAsync(this, new ValueStatusTimestamp(value, StatusCodes.Good, DateTime.UtcNow), null);                       
        }

        #endregion

        #region private fields

        private string _elementId = @"";

        private string _subscribedElementId = @"";

        #endregion
    }
}
