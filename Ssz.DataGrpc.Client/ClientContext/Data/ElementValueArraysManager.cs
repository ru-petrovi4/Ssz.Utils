using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public class ElementValuesCollectionManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dblCount"></param>
        /// <param name="uintCount"></param>
        /// <param name="objCount"></param>
        public ElementValuesCollectionManager(int dblCount, int uintCount, int objCount)
        {            
            if (dblCount > 0)
            {
                _elementValuesCollection.DoubleAliases.Capacity = dblCount;
                _elementValuesCollection.DoubleStatusCodes.Capacity = dblCount;
                _elementValuesCollection.DoubleTimestamps.Capacity = dblCount;
                _elementValuesCollection.DoubleValues.Capacity = dblCount;
            }
            if (uintCount > 0)
            {
                _elementValuesCollection.UintAliases.Capacity = uintCount;
                _elementValuesCollection.UintStatusCodes.Capacity = uintCount;
                _elementValuesCollection.UintTimestamps.Capacity = uintCount;
                _elementValuesCollection.UintValues.Capacity = uintCount;
            }
            if (objCount > 0)
            {
                _elementValuesCollection.ObjectAliases.Capacity = objCount;
                _elementValuesCollection.ObjectStatusCodes.Capacity = objCount;
                _elementValuesCollection.ObjectTimestamps.Capacity = objCount;
                _objectValuesMemoryStream = new MemoryStream(1024);
                _objectValuesSerializationWriter = new SerializationWriter(_objectValuesMemoryStream, true);
            }
        }

        public void AddDouble(uint serverAlias, uint statusCode, DateTime timestampUtc, double storageDouble)
        {
            _elementValuesCollection.DoubleAliases.Add(serverAlias);
            _elementValuesCollection.DoubleStatusCodes.Add(statusCode);
            _elementValuesCollection.DoubleTimestamps.Add(Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(timestampUtc));
            _elementValuesCollection.DoubleValues.Add(storageDouble);
        }

        public void AddUint(uint serverAlias, uint statusCode, DateTime timestampUtc, uint storageUInt32)
        {
            _elementValuesCollection.UintAliases.Add(serverAlias);
            _elementValuesCollection.UintStatusCodes.Add(statusCode);
            _elementValuesCollection.UintTimestamps.Add(Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(timestampUtc));
            _elementValuesCollection.UintValues.Add(storageUInt32);
        }

        public void AddObject(uint serverAlias, uint statusCode, DateTime timestampUtc, object? storageObject)
        {
            _elementValuesCollection.ObjectAliases.Add(serverAlias);
            _elementValuesCollection.ObjectStatusCodes.Add(statusCode);
            _elementValuesCollection.ObjectTimestamps.Add(Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(timestampUtc));
            if (_objectValuesSerializationWriter == null) throw new InvalidOperationException();
            _objectValuesSerializationWriter.WriteObject(storageObject);
        }

        /// <summary>
        ///     Must be called once.
        /// </summary>
        /// <returns></returns>
        public ElementValuesCollection GetElementValuesCollection()
        {
            if (_objectValuesMemoryStream != null && _objectValuesSerializationWriter != null)
            {
                _objectValuesSerializationWriter.Dispose();
                _objectValuesMemoryStream.Position = 0;
                _elementValuesCollection.ObjectValues = Google.Protobuf.ByteString.FromStream(_objectValuesMemoryStream);                
                _objectValuesMemoryStream.Dispose();
            }
            return _elementValuesCollection;
        }

        #region private fields

        private ElementValuesCollection _elementValuesCollection = new ElementValuesCollection();
        private MemoryStream? _objectValuesMemoryStream;
        private SerializationWriter? _objectValuesSerializationWriter;

        #endregion
    }
}
