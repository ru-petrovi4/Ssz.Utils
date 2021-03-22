using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.DataGrpc.Server
{
    public class ElementValueArraysManager
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="dblCount"></param>
        /// <param name="uintCount"></param>
        /// <param name="objCount"></param>
        public ElementValueArraysManager(int dblCount, int uintCount, int objCount)
        {            
            if (dblCount > 0)
            {
                _elementValueArrays.DoubleAliases.Capacity = dblCount;
                _elementValueArrays.DoubleStatusCodes.Capacity = dblCount;
                _elementValueArrays.DoubleTimestamps.Capacity = dblCount;
                _elementValueArrays.DoubleValues.Capacity = dblCount;
            }
            if (uintCount > 0)
            {
                _elementValueArrays.UintAliases.Capacity = uintCount;
                _elementValueArrays.UintStatusCodes.Capacity = uintCount;
                _elementValueArrays.UintTimestamps.Capacity = uintCount;
                _elementValueArrays.UintValues.Capacity = uintCount;
            }
            if (objCount > 0)
            {
                _elementValueArrays.ObjectAliases.Capacity = objCount;
                _elementValueArrays.ObjectStatusCodes.Capacity = objCount;
                _elementValueArrays.ObjectTimestamps.Capacity = objCount;
                _objectValuesMemoryStream = new MemoryStream(1024);
                _objectValuesSerializationWriter = new SerializationWriter(_objectValuesMemoryStream, true);
            }
        }

        public void AddDouble(uint serverAlias, uint statusCode, DateTime timestampUtc, double storageDouble)
        {
            _elementValueArrays.DoubleAliases.Add(serverAlias);
            _elementValueArrays.DoubleStatusCodes.Add(statusCode);
            _elementValueArrays.DoubleTimestamps.Add(Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(timestampUtc));
            _elementValueArrays.DoubleValues.Add(storageDouble);
        }

        public void AddUint(uint serverAlias, uint statusCode, DateTime timestampUtc, uint storageUInt32)
        {
            _elementValueArrays.UintAliases.Add(serverAlias);
            _elementValueArrays.UintStatusCodes.Add(statusCode);
            _elementValueArrays.UintTimestamps.Add(Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(timestampUtc));
            _elementValueArrays.UintValues.Add(storageUInt32);
        }

        public void AddObject(uint serverAlias, uint statusCode, DateTime timestampUtc, object? storageObject)
        {
            _elementValueArrays.ObjectAliases.Add(serverAlias);
            _elementValueArrays.ObjectStatusCodes.Add(statusCode);
            _elementValueArrays.ObjectTimestamps.Add(Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(timestampUtc));
            if (_objectValuesSerializationWriter == null) throw new InvalidOperationException();
            _objectValuesSerializationWriter.WriteObject(storageObject);
        }

        /// <summary>
        ///     Must be called once.
        /// </summary>
        /// <returns></returns>
        public ElementValueArrays GetElementValueArrays()
        {
            if (_objectValuesMemoryStream != null && _objectValuesSerializationWriter != null)
            {
                _objectValuesSerializationWriter.Dispose();
                _objectValuesMemoryStream.Position = 0;
                _elementValueArrays.ObjectValues = Google.Protobuf.ByteString.FromStream(_objectValuesMemoryStream);                
                _objectValuesMemoryStream.Dispose();
            }
            return _elementValueArrays;
        }

        #region private fields

        private ElementValueArrays _elementValueArrays = new ElementValueArrays();
        private MemoryStream? _objectValuesMemoryStream;
        private SerializationWriter? _objectValuesSerializationWriter;

        #endregion
    }
}
