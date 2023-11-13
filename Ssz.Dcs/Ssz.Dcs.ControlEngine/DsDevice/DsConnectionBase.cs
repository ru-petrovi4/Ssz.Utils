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
    public abstract class DsConnectionBase : IOwnedDataSerializable, IDisposable
    {
        #region construction and destruction

        protected DsConnectionBase(string connectionTypeString, byte connectionType, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {
            ConnectionTypeString = connectionTypeString;
            ConnectionType = connectionType;            
            _parentModule = parentModule;
            _parentComponentDsBlock = parentComponentDsBlock;
        }

        // <summary>
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
            _parentModule = null!;
            _parentComponentDsBlock = null;

            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~DsConnectionBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>        
        public bool Disposed { get; private set; }

        #endregion

        #region public function

        public string ConnectionTypeString { get; }

        public byte ConnectionType { get; }

        public DsModule ParentModule => _parentModule;

        public ComponentDsBlock? ParentComponentDsBlock => _parentComponentDsBlock;

        public abstract string ConnectionString { get; set; }

        public string ConnectionStringWithConnectionTypePrefix => ConnectionTypeString + "::" + ConnectionString;

        public abstract void SerializeOwnedData(SerializationWriter writer, object? context);

        public abstract void DeserializeOwnedData(SerializationReader reader, object? context);

        public abstract Any GetValue();
        
        public abstract Task<ResultInfo> SetValueAsync(Any value);

        #endregion

        #region private fields

        private DsModule _parentModule;

        private ComponentDsBlock? _parentComponentDsBlock;

        #endregion
    }
}
