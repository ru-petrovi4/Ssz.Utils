using System;
using System.Collections.Generic;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Data
{
    public class DataValueArraysWithAliasEx : DataValueArraysEx
    {
        #region public functions

        public override void Prepare(int doubleArrayCapacity, int uintArrayCapacity, int objectArrayCapacity)
        {
            if (DoubleAlias != null) DoubleAlias.Clear();
            if (doubleArrayCapacity > 0)
            {
                if (DoubleAlias == null) DoubleAlias = new List<uint>(doubleArrayCapacity);
                else if (doubleArrayCapacity > DoubleAlias.Capacity) DoubleAlias.Capacity = doubleArrayCapacity;
            }

            if (UintAlias != null) UintAlias.Clear();
            if (uintArrayCapacity > 0)
            {
                if (UintAlias == null) UintAlias = new List<uint>(uintArrayCapacity);
                else if (uintArrayCapacity > UintAlias.Capacity) UintAlias.Capacity = uintArrayCapacity;
            }

            if (ObjectAlias != null) ObjectAlias.Clear();
            if (objectArrayCapacity > 0)
            {
                if (ObjectAlias == null) ObjectAlias = new List<uint>(objectArrayCapacity);
                else if (objectArrayCapacity > ObjectAlias.Capacity) ObjectAlias.Capacity = objectArrayCapacity;
            }

            base.Prepare(doubleArrayCapacity, uintArrayCapacity, objectArrayCapacity);
        }

        public DataValueArraysWithAlias GetValueArraysWithAlias()
        {
            uint[] doubleAliases = null;
            uint[] doubleStatusCodes = null;
            DateTime[] doubleTimeStamps = null;
            double[] doubleValues = null;
            uint[] uintClientAliases = null;
            uint[] uintStatusCodes = null;
            DateTime[] uintTimeStamps = null;
            uint[] uintValues = null;
            uint[] objectClientAliases = null;
            uint[] objectStatusCodes = null;
            DateTime[] objectTimeStamps = null;
            object[] objectValues = null;

            int count = 0;

            if ((DoubleValues != null) && (DoubleValues.Count > 0))
            {
                count += DoubleAlias.Count;
                doubleAliases = DoubleAlias.ToArray();
                doubleStatusCodes = DoubleStatusCodes.ToArray();
                doubleTimeStamps = DoubleTimeStamps.ToArray();
                doubleValues = DoubleValues.ToArray();
            }
            if ((UintValues != null) && (UintValues.Count > 0))
            {
                count += UintAlias.Count;
                uintClientAliases = UintAlias.ToArray();
                uintStatusCodes = UintStatusCodes.ToArray();
                uintTimeStamps = UintTimeStamps.ToArray();
                uintValues = UintValues.ToArray();
            }
            if ((ObjectValues != null) && (ObjectValues.Count > 0))
            {
                count += ObjectAlias.Count;
                objectClientAliases = ObjectAlias.ToArray();
                objectStatusCodes = ObjectStatusCodes.ToArray();
                objectTimeStamps = ObjectTimeStamps.ToArray();
                objectValues = ObjectValues.ToArray();
            }

            if (count == 0) return null;

            var dvawa = new DataValueArraysWithAlias(ref doubleAliases, ref doubleStatusCodes, ref doubleTimeStamps,
                                                     ref doubleValues, ref uintClientAliases, ref uintStatusCodes,
                                                     ref uintTimeStamps, ref uintValues,
                                                     ref objectClientAliases, ref objectStatusCodes,
                                                     ref objectTimeStamps, ref objectValues);

            if (ErrorInfo.Count > 0)
            {
                dvawa.ErrorInfo = new List<ErrorInfo>(ErrorInfo.Count);
                foreach (ErrorInfo errorInfo in ErrorInfo)
                {
                    dvawa.ErrorInfo.Add(errorInfo);
                }
            }

            return dvawa;
        }

        /// <summary>
        ///   When used in a read context (returned from the server) 
        ///   this is the Client Alias.  When used in a write context 
        ///   (sent to the server) this is the Server Alias.
        /// </summary>
        public List<uint> DoubleAlias { get; private set; }

        /// <summary>
        ///   When used in a read context (returned from the server) 
        ///   this is the Client Alias.  When used in a write context 
        ///   (sent to the server) this is the Server Alias.
        /// </summary>
        public List<uint> UintAlias { get; private set; }

        /// <summary>
        ///   When used in a read context (returned from the server) 
        ///   this is the Client Alias.  When used in a write context 
        ///   (sent to the server) this is the Server Alias.
        /// </summary>
        public List<uint> ObjectAlias { get; private set; }

        #endregion
    }
}