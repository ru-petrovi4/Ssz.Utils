using System;
using System.Collections.Generic;
using Xi.Contracts.Data;

namespace Ssz.DataGrpc.Server.Data
{
    public class DataValueArraysEx
    {
        #region public functions

        public virtual void Prepare(int doubleArrayCapacity, int uintArrayCapacity, int objectArrayCapacity)
        {
            if (DoubleValues != null)
            {
                DoubleStatusCodes.Clear();
                DoubleTimeStamps.Clear();
                DoubleValues.Clear();
            }
            if (doubleArrayCapacity > 0)
            {
                if (DoubleValues == null)
                {
                    DoubleStatusCodes = new List<uint>(doubleArrayCapacity);
                    DoubleTimeStamps = new List<DateTime>(doubleArrayCapacity);
                    DoubleValues = new List<double>(doubleArrayCapacity);
                }
                else
                {
                    if (doubleArrayCapacity > DoubleValues.Capacity)
                    {
                        DoubleStatusCodes.Capacity = doubleArrayCapacity;
                        DoubleTimeStamps.Capacity = doubleArrayCapacity;
                        DoubleValues.Capacity = doubleArrayCapacity;
                    }
                }
            }

            if (UintValues != null)
            {
                UintStatusCodes.Clear();
                UintTimeStamps.Clear();
                UintValues.Clear();
            }
            if (uintArrayCapacity > 0)
            {
                if (UintValues == null)
                {
                    UintStatusCodes = new List<uint>(uintArrayCapacity);
                    UintTimeStamps = new List<DateTime>(uintArrayCapacity);
                    UintValues = new List<uint>(uintArrayCapacity);
                }
                else
                {
                    if (uintArrayCapacity > UintValues.Capacity)
                    {
                        UintStatusCodes.Capacity = uintArrayCapacity;
                        UintTimeStamps.Capacity = uintArrayCapacity;
                        UintValues.Capacity = uintArrayCapacity;
                    }
                }
            }

            if (ObjectValues != null)
            {
                ObjectStatusCodes.Clear();
                ObjectTimeStamps.Clear();
                ObjectValues.Clear();
            }
            if (objectArrayCapacity > 0)
            {
                if (ObjectValues == null)
                {
                    ObjectStatusCodes = new List<uint>(objectArrayCapacity);
                    ObjectTimeStamps = new List<DateTime>(objectArrayCapacity);
                    ObjectValues = new List<object>(objectArrayCapacity);
                }
                else
                {
                    if (objectArrayCapacity > ObjectValues.Capacity)
                    {
                        ObjectStatusCodes.Capacity = objectArrayCapacity;
                        ObjectTimeStamps.Capacity = objectArrayCapacity;
                        ObjectValues.Capacity = objectArrayCapacity;
                    }
                }
            }

            if (ErrorInfo == null) ErrorInfo = new List<ErrorInfo>(50);
            else ErrorInfo.Clear();
        }

        public DataValueArrays GetDataValueArrays()
        {
            uint[] doubleStatusCodes = null;
            DateTime[] doubleTimeStamps = null;
            double[] doubleValues = null;
            uint[] uintStatusCodes = null;
            DateTime[] uintTimeStamps = null;
            uint[] uintValues = null;
            uint[] objectStatusCodes = null;
            DateTime[] objectTimeStamps = null;
            object[] objectValues = null;

            int count = 0;

            if ((DoubleValues != null) && (DoubleValues.Count > 0))
            {
                count += DoubleValues.Count;
                doubleStatusCodes = DoubleStatusCodes.ToArray();
                doubleTimeStamps = DoubleTimeStamps.ToArray();
                doubleValues = DoubleValues.ToArray();
            }
            if ((UintValues != null) && (UintValues.Count > 0))
            {
                count += UintValues.Count;
                uintStatusCodes = UintStatusCodes.ToArray();
                uintTimeStamps = UintTimeStamps.ToArray();
                uintValues = UintValues.ToArray();
            }
            if ((ObjectValues != null) && (ObjectValues.Count > 0))
            {
                count += ObjectValues.Count;
                objectStatusCodes = ObjectStatusCodes.ToArray();
                objectTimeStamps = ObjectTimeStamps.ToArray();
                objectValues = ObjectValues.ToArray();
            }

            if (count == 0) return null;

            var dva = new DataValueArrays(ref doubleStatusCodes, ref doubleTimeStamps, ref doubleValues,
                                          ref uintStatusCodes, ref uintTimeStamps, ref uintValues, ref objectStatusCodes,
                                          ref objectTimeStamps,
                                          ref objectValues);

            if (ErrorInfo.Count > 0)
            {
                dva.ErrorInfo = new List<ErrorInfo>(ErrorInfo.Count);
                foreach (ErrorInfo errorInfo in ErrorInfo)
                {
                    dva.ErrorInfo.Add(errorInfo);
                }
            }

            return dva;
        }

        /// <summary>
        ///   The array of status codes. Status code values are defined by 
        ///   the XiStatusCode class.
        /// </summary>
        public List<uint> DoubleStatusCodes { get; private set; }

        /// <summary>
        ///   The array of timestamps.  All timestamps are UTC.
        /// </summary>
        public List<DateTime> DoubleTimeStamps { get; private set; }

        /// <summary>
        ///   The array of values. 
        ///   Used to transfer single and double floating point values.
        /// </summary>
        public List<double> DoubleValues { get; private set; }

        /// <summary>
        ///   The array of status codes. Status code values are defined by 
        ///   the XiStatusCode class.
        /// </summary>
        public List<uint> UintStatusCodes { get; private set; }

        /// <summary>
        ///   The array of timestamps.  All timestamps are UTC.
        /// </summary>
        public List<DateTime> UintTimeStamps { get; private set; }

        /// <summary>
        ///   The array of integer values.
        ///   Used to transfer byte, sbyte, short, ushort, int and uint values.
        /// </summary>
        public List<uint> UintValues { get; private set; }

        /// <summary>
        ///   The array of status codes. Status code values are defined by 
        ///   the XiStatusCode class.
        /// </summary>
        public List<uint> ObjectStatusCodes { get; private set; }

        /// <summary>
        ///   The array of timestamps.  All timestamps are UTC.
        /// </summary>
        public List<DateTime> ObjectTimeStamps { get; private set; }

        /// <summary>
        ///   The array of values.
        ///   Used to transfer type that do not conform to the integer or float values.
        /// </summary>
        public List<object> ObjectValues { get; private set; }

        /// <summary>
        ///   <para>The error message to be returned when the Context has been 
        ///     opened with ContextOptions set to DebugErrorMessages. This list is 
        ///     always null if the Context was not opened with ContextOptions set 
        ///     to DebugErrorMessages.</para>
        ///   <para>When ContextOptions is set to DebugErrorMessages, the server 
        ///     can provide an error message that provides additional information about 
        ///     bad values.  If additional error information is not provided for any 
        ///     values, then the list is set to null.</para>
        /// </summary>
        public List<ErrorInfo> ErrorInfo { get; private set; }

        #endregion
    }
}