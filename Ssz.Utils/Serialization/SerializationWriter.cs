//#define EXTDEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;


namespace Ssz.Utils.Serialization
{
    /// <summary>
    ///     Class which defines the writer for serialized data using the fast serialization optimization.
    ///     A SerializationWriter instance is used to store values and objects in a byte array.
    /// </summary>
    public class SerializationWriter : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Creates a SerializationWriter around the specified stream.
        ///     baseStream.CanSeek must be true.
        /// </summary>
        /// <param name="baseStream"></param>
        /// <param name="writeStringsDictionary"></param>
        public SerializationWriter(Stream baseStream, bool writeStringsDictionary = false)            
        {            
            if (!baseStream.CanSeek) throw new ArgumentException("baseStream must be seekable.");

            _baseStream = baseStream;            
#if NET5_0_OR_GREATER
            _binaryWriter = new BinaryWriter(_baseStream);            
#else
            _binaryWriter = new BinaryWriterEx(_baseStream);
#endif

            if (!writeStringsDictionary)
            {
                _binaryWriter.Write7BitEncodedInt(6); // Version
            }
            else
            {
                _binaryWriter.Write7BitEncodedInt(7); // Version

                _stringsDictionary = new Dictionary<string, int>();
                _stringsList = new List<string>();                

                _stringsListPositionPlaceholder_Position = _baseStream.Position;

                // String list position placeholder
                _binaryWriter.Write((long)0);
            }
        }

        /// <summary>
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
            if (Disposed) return;

            if (disposing)
            {
                if (_stringsList is not null && _stringsList.Count > 0)
                {
                    long stringsListPosition = _baseStream.Position;

                    WriteOptimized((long)_stringsList.Count);
                    foreach (string s in _stringsList)
                    {
                        _binaryWriter.Write(s);
                    }

                    long streamEndPosition = _baseStream.Position;

                    _baseStream.Seek(_stringsListPositionPlaceholder_Position, SeekOrigin.Begin);
                    _binaryWriter.Write(stringsListPosition - _stringsListPositionPlaceholder_Position);
                    _baseStream.Seek(streamEndPosition, SeekOrigin.Begin);
                }
            }
            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~SerializationWriter()
        {
            Dispose(false);
        }

#endregion

#region public functions

        /// <summary>
        ///     Holds the highest Int16 that can be optimized into less than the normal 2 bytes
        /// </summary>
        public const short HighestOptimizable16BitValue = 127; // 0x7F

        /// <summary>
        ///     Holds the highest Int32 that can be optimized into less than the normal 4 bytes
        /// </summary>
        public const int HighestOptimizable32BitValue = 2097151; // 0x001FFFFF

        /// <summary>
        ///     Holds the highest Int64 that can be optimized into less than the normal 8 bytes
        /// </summary>
        public const long HighestOptimizable64BitValue = 562949953421311; // 0x0001FFFFFFFFFFFF

        public bool Disposed { get; private set; }
       
        public Stream BaseStream { get { return _baseStream; } }

        /// <summary>
        ///     Begins Block.       
        /// </summary>
        /// <param name="version"></param>
        public void BeginBlock(int version = -1)
        {
            if (version >= 0)
            {
                WriteSerializedType(SerializedType.BlockBeginWithVersion);
                // it will store the block length
                _binaryWriter.Write((long)0);
                // Store the begin position of the block
                _blockBeginPositionsStack.Push((_baseStream.Position, false));                
                _binaryWriter.Write7BitEncodedInt(version);
            }
            else
            {
                WriteSerializedType(SerializedType.BlockBegin);
                // it will store the block length
                _binaryWriter.Write((long)0);
                // Store the begin position of the block
                _blockBeginPositionsStack.Push((_baseStream.Position, false));                
            }
        }

        /// <summary>
        ///     Begins short Block.
        /// </summary>
        /// <param name="version"></param>
        public void BeginShortBlock(int version = -1)
        {
            if (version >= 0)
            {
                WriteSerializedType(SerializedType.ShortBlockBeginWithVersion);
                // It will store the block length
                _binaryWriter.Write((int)0);
                // Store the begin position of the block
                _blockBeginPositionsStack.Push((_baseStream.Position, true));
                _binaryWriter.Write7BitEncodedInt(version);                
            }
            else
            {
                WriteSerializedType(SerializedType.ShortBlockBegin);
                // It will store the block length
                _binaryWriter.Write((int)0);
                // Store the begin position of the block
                _blockBeginPositionsStack.Push((_baseStream.Position, true));                
            }
        }

        /// <summary>
        ///     Ends Block
        /// </summary>
        public void EndBlock()
        {
            long endPosition = _baseStream.Position;
            var (beginPosition, isShortBlock) = _blockBeginPositionsStack.Pop();
            if (isShortBlock)
            {
                _baseStream.Position = beginPosition - 4;
                if ((endPosition - beginPosition) < int.MaxValue)
                    _binaryWriter.Write((int)(endPosition - beginPosition));
                else
                    throw new BlockSizeException(endPosition - beginPosition);
            }
            else
            {
                _baseStream.Position = beginPosition - 8;
                _binaryWriter.Write(endPosition - beginPosition);
            }
            _baseStream.Position = endPosition;
            WriteSerializedType(SerializedType.BlockEnd);
        }

        /// <summary>
        ///     Enters Block
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public IDisposable EnterBlock(int version = -1)
        {
            return new Block(this, version);
        }
        /// <summary>
        ///     Enters short Block
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public IDisposable EnterShortBlock(int version = -1)
        {
            return new Block(this, version, true);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Write(bool value)
        {
            _binaryWriter.Write(value);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Write(sbyte value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void Write(byte value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///     Writes a Unicode character to the current stream and advances the current position
        ///     of the stream in accordance with the Encoding used and the specific characters
        ///     being written to the stream.
        /// </summary>
        /// <param name="ch"></param>
        public void Write(char ch)
        {
            _binaryWriter.Write(ch);
        }

        /// <summary>
        ///     Writes a two-byte signed integer to the current stream and advances the stream
        ///     position by two bytes.
        /// </summary>
        /// <param name="value"></param>
        public void Write(short value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///     Writes a two-byte unsigned integer to the current stream and advances the stream
        ///     position by two bytes.
        /// </summary>
        /// <param name="value"></param>        
        public void Write(ushort value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///    Writes a four-byte signed integer to the current stream and advances the stream
        ///     position by four bytes.
        /// </summary>
        /// <param name="value"></param>
        public void Write(int value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///     Writes a four-byte unsigned integer to the current stream and advances the stream
        ///     position by four bytes.
        /// </summary>
        /// <param name="value"></param>        
        public void Write(uint value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///     Writes an eight-byte signed integer to the current stream and advances the stream
        ///     position by eight bytes.
        /// </summary>
        /// <param name="value"></param>
        public void Write(long value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///     Writes an eight-byte unsigned integer to the current stream and advances the
        ///     stream position by eight bytes.
        /// </summary>
        /// <param name="value"></param>        
        public void Write(ulong value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///     Use ReadSingle() for reading.
        ///     Writes a four-byte floating-point value to the current stream and advances the
        ///     stream position by four bytes.
        /// </summary>
        /// <param name="value"></param>
        public void Write(float value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///     Use ReadDouble() for reading.
        ///     Writes an eight-byte floating-point value to the current stream and advances
        ///     the stream position by eight bytes.
        /// </summary>
        /// <param name="value"></param>
        public void Write(double value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///     Use ReadDecimal() for reading.
        ///     Writes a decimal value to the current stream and advances the stream position
        ///     by sixteen bytes.
        /// </summary>
        /// <param name="value"></param>
        public void Write(decimal value)
        {
            _binaryWriter.Write(value);
        }

        /// <summary>
        ///     Use ReadDateTimeOffset() for reading.
        ///     Writes a DateTimeOffset value into the stream.
        ///     Stored Size: 16 bytes
        /// </summary>
        /// <param name="value"> The DateTime value to store. </param>
        public void Write(DateTimeOffset value)
        {
            _binaryWriter.Write(value.Ticks);
            _binaryWriter.Write(value.Offset.Ticks);
        }

        /// <summary>
        ///     Use ReadDateTime() for reading.
        ///     Writes a DateTime value into the stream.
        ///     Stored Size: 8 bytes
        /// </summary>
        /// <param name="value"> The DateTime value to store. </param>
        public void Write(DateTime value)
        {
            _binaryWriter.Write(value.ToBinary());
        }

        /// <summary>
        ///     Use ReadTimeSpan() for reading.
        ///     Writes a TimeSpan value into the stream.
        ///     Stored Size: 8 bytes
        /// </summary>
        /// <param name="value"> The TimeSpan value to store. </param>
        public void Write(TimeSpan value)
        {
            _binaryWriter.Write(value.Ticks);
        }

        /// <summary>
        ///     Writes a Guid into the stream.
        ///     Stored Size: 16 bytes.
        /// </summary>
        /// <param name="value"> </param>
        public void Write(Guid value)
        {
            _binaryWriter.Write(value.ToByteArray());
        }

        /// <summary>
        ///     Use ReadString() for reading.
        /// </summary>
        /// <param name="value"> The string to store. </param>
        public void Write(string value)
        {
            WriteOptimizedOrNot(value);
        }

        /// <summary>
        ///     Use ReadNullableString() for reading.
        /// </summary>
        /// <param name="value"> The string to store. </param>
        public void WriteNullableString(string? value)
        {
            WriteOptimizedOrNot(value);
        }

        /// <summary>
        ///     Use ReadOptimizedInt16() for reading.
        /// </summary>
        /// <param name="value"></param>
        public void WriteOptimized(short value)
        {
            _binaryWriter.Write7BitEncodedInt(value);
        }

        /// <summary>
        ///     Use ReadOptimizedUInt16() for reading.
        /// </summary>
        /// <param name="value"></param>
        public void WriteOptimized(ushort value)
        {
            _binaryWriter.Write7BitEncodedInt(value);
        }

        /// <summary>
        ///     Write an Int32 value using the fewest number of bytes possible.
        ///     Use ReadOptimizedInt32() for reading.
        /// </summary>
        /// <remarks>
        ///     0x00000000 - 0x0000007f (0 to 127) takes 1 byte
        ///     0x00000080 - 0x000003FF (128 to 16,383) takes 2 bytes
        ///     0x00000400 - 0x001FFFFF (16,384 to 2,097,151) takes 3 bytes
        ///     0x00200000 - 0x0FFFFFFF (2,097,152 to 268,435,455) takes 4 bytes
        ///     ----------------------------------------------------------------
        ///     0x10000000 - 0x07FFFFFF (268,435,456 and above) takes 5 bytes
        ///     All negative numbers take 5 bytes
        ///     Only call this method if the value is known to be between 0 and
        ///     268,435,455 otherwise use Write(Int32 value)
        /// </remarks>
        /// <param name="value"> The Int32 to store. Must be between 0 and 268,435,455 inclusive. </param>
        public void WriteOptimized(int value)
        {
            _binaryWriter.Write7BitEncodedInt(value);
        }

        /// <summary>
        ///     Write a UInt32 value using the fewest number of bytes possible.
        ///     Use ReadOptimizedUInt32() for reading.
        /// </summary>
        /// <remarks>
        ///     0x00000000 - 0x0000007f (0 to 127) takes 1 byte
        ///     0x00000080 - 0x000003FF (128 to 16,383) takes 2 bytes
        ///     0x00000400 - 0x001FFFFF (16,384 to 2,097,151) takes 3 bytes
        ///     0x00200000 - 0x0FFFFFFF (2,097,152 to 268,435,455) takes 4 bytes
        ///     ----------------------------------------------------------------
        ///     0x10000000 - 0xFFFFFFFF (268,435,456 and above) takes 5 bytes
        ///     Only call this method if the value is known to  be between 0 and
        ///     268,435,455 otherwise use Write(UInt32 value)
        /// </remarks>
        /// <param name="value"> The UInt32 to store. Must be between 0 and 268,435,455 inclusive. </param>
        public void WriteOptimized(uint value)
        {
            _binaryWriter.Write7BitEncodedInt(unchecked((int)value));
        }

        /// <summary>
        ///     Write an Int64 value using the fewest number of bytes possible.
        ///     Use ReadOptimizedInt64() for reading.
        /// </summary>
        /// <remarks>
        ///     0x0000000000000000 - 0x000000000000007f (0 to 127) takes 1 byte
        ///     0x0000000000000080 - 0x00000000000003FF (128 to 16,383) takes 2 bytes
        ///     0x0000000000000400 - 0x00000000001FFFFF (16,384 to 2,097,151) takes 3 bytes
        ///     0x0000000000200000 - 0x000000000FFFFFFF (2,097,152 to 268,435,455) takes 4 bytes
        ///     0x0000000010000000 - 0x00000007FFFFFFFF (268,435,456 to 34,359,738,367) takes 5 bytes
        ///     0x0000000800000000 - 0x000003FFFFFFFFFF (34,359,738,368 to 4,398,046,511,103) takes 6 bytes
        ///     0x0000040000000000 - 0x0001FFFFFFFFFFFF (4,398,046,511,104 to 562,949,953,421,311) takes 7 bytes
        ///     0x0002000000000000 - 0x00FFFFFFFFFFFFFF (562,949,953,421,312 to 72,057,594,037,927,935) takes 8 bytes
        ///     ------------------------------------------------------------------
        ///     0x0100000000000000 - 0x7FFFFFFFFFFFFFFF (72,057,594,037,927,936 to 9,223,372,036,854,775,807) takes 9 bytes
        ///     0x7FFFFFFFFFFFFFFF - 0xFFFFFFFFFFFFFFFF (9,223,372,036,854,775,807 and above) takes 10 bytes
        ///     All negative numbers take 10 bytes
        ///     Only call this method if the value is known to be between 0 and
        ///     72,057,594,037,927,935 otherwise use Write(Int64 value)
        /// </remarks>
        /// <param name="value"> The Int64 to store. Must be between 0 and 72,057,594,037,927,935 inclusive. </param>
        public void WriteOptimized(long value)
        {
            _binaryWriter.Write7BitEncodedInt64(value);
        }

        /// <summary>
        ///     Write a UInt64 value using the fewest number of bytes possible.
        ///     Use ReadOptimizedUInt64() for reading.
        /// </summary>
        /// <remarks>
        ///     0x0000000000000000 - 0x000000000000007f (0 to 127) takes 1 byte
        ///     0x0000000000000080 - 0x00000000000003FF (128 to 16,383) takes 2 bytes
        ///     0x0000000000000400 - 0x00000000001FFFFF (16,384 to 2,097,151) takes 3 bytes
        ///     0x0000000000200000 - 0x000000000FFFFFFF (2,097,152 to 268,435,455) takes 4 bytes
        ///     0x0000000010000000 - 0x00000007FFFFFFFF (268,435,456 to 34,359,738,367) takes 5 bytes
        ///     0x0000000800000000 - 0x000003FFFFFFFFFF (34,359,738,368 to 4,398,046,511,103) takes 6 bytes
        ///     0x0000040000000000 - 0x0001FFFFFFFFFFFF (4,398,046,511,104 to 562,949,953,421,311) takes 7 bytes
        ///     0x0002000000000000 - 0x00FFFFFFFFFFFFFF (562,949,953,421,312 to 72,057,594,037,927,935) takes 8 bytes
        ///     ------------------------------------------------------------------
        ///     0x0100000000000000 - 0x7FFFFFFFFFFFFFFF (72,057,594,037,927,936 to 9,223,372,036,854,775,807) takes 9 bytes
        ///     0x7FFFFFFFFFFFFFFF - 0xFFFFFFFFFFFFFFFF (9,223,372,036,854,775,807 and above) takes 10 bytes
        ///     Only call this method if the value is known to be between 0 and
        ///     72,057,594,037,927,935 otherwise use Write(UInt64 value)
        /// </remarks>
        /// <param name="value"> The UInt64 to store. Must be between 0 and 72,057,594,037,927,935 inclusive. </param>
        public void WriteOptimized(ulong value)
        {
            _binaryWriter.Write7BitEncodedInt64(unchecked((long)value));
        }

        /// <summary>
        ///     Writes a BitArray into the stream using the fewest number of bytes possible.
        ///     Stored Size: 1 byte upwards depending on data content
        ///     Notes:
        ///     An empty BitArray takes 1 byte.
        ///     value is not null
        ///     Use ReadOptimizedBitArray() for reading.
        /// </summary>
        /// <param name="value"> The BitArray value to store. Must not be null. </param>
        public void WriteOptimized(BitArray value)
        {
            WriteOptimized((long)value.Length);            

            if (value.Length > 0)
            {
                var data = new byte[(value.Length + 7) / 8];
                value.CopyTo(data, 0);
                _binaryWriter.Write(data, 0, data.Length);
            }
        }

        /// <summary>
        ///     Writes a DateTime value into the stream using the fewest number of bytes possible.
        ///     Stored Size: 3 bytes to 7 bytes (.Net is 8 bytes)
        ///     Notes:
        ///     A DateTime containing only a date takes 3 bytes
        ///     (except a .NET 2.0 Date with a specified DateTimeKind which will take a minimum
        ///     of 5 bytes - no further optimization for this situation felt necessary since it
        ///     is unlikely that a DateTimeKind would be specified without hh:mm also)
        ///     Date plus hh:mm takes 5 bytes.
        ///     Date plus hh:mm:ss takes 6 bytes.
        ///     Date plus hh:mm:ss.fff takes 7 bytes.
        ///     Use ReadOptimizedDateTime() for reading.
        /// </summary>
        /// <param name="value"> The DateTime value to store. Must not contain sub-millisecond data. </param>
        public void WriteOptimized(DateTime value)
        {
            var dateMask = new BitVector32();
            dateMask[DateYearMask] = value.Year;
            dateMask[DateMonthMask] = value.Month;
            dateMask[DateDayMask] = value.Day;

            var initialData = (int)value.Kind;
            bool writeAdditionalData = value != value.Date;

            writeAdditionalData |= initialData != 0;
            dateMask[DateHasTimeOrKindMask] = writeAdditionalData ? 1 : 0;

            // Store 3 bytes of Date information
            int dateMaskData = dateMask.Data;
            _binaryWriter.Write((byte)dateMaskData);
            _binaryWriter.Write((byte)(dateMaskData >> 8));
            _binaryWriter.Write((byte)(dateMaskData >> 16));

            if (writeAdditionalData)
            {
                EncodeTimeSpan(value.TimeOfDay, true, initialData);
            }
        }

        /// <summary>
        ///     Use ReadOptimizedDouble() for reading.
        /// </summary>
        /// <param name="value"></param>
        public void WriteOptimized(double value)
        {
            var valueDouble = (Double)value;
            if (valueDouble == 0.0)
            {
                WriteSerializedType(SerializedType.ZeroDoubleType);
            }
            else if (valueDouble == 1.0)
            {
                WriteSerializedType(SerializedType.OneDoubleType);
            }
            else
            {
                WriteSerializedType(SerializedType.DoubleType);
                _binaryWriter.Write(valueDouble);
            }
        }

        /// <summary>
        ///     Writes a Decimal value into the stream using the fewest number of bytes possible.
        ///     Stored Size: 1 byte to 14 bytes (.Net is 16 bytes)
        ///     Restrictions: None
        ///     Use ReadOptimizedDecimal() for reading.
        /// </summary>
        /// <param name="value"> The Decimal value to store </param>
        public void WriteOptimized(Decimal value)
        {
            int[] data = Decimal.GetBits(value);
            var scale = (byte)(data[3] >> 16);
            byte flags = 0;

            if (scale != 0)
            {
                decimal normalized = Decimal.Truncate(value);

                if (normalized == value)
                {
                    data = Decimal.GetBits(normalized);
                    scale = 0;
                }
            }

            if ((data[3] & -2147483648) != 0)
            {
                flags |= 0x01;
            }

            if (scale != 0)
            {
                flags |= 0x02;
            }

            if (data[0] == 0)
            {
                flags |= 0x04;
            }
            else if (data[0] <= HighestOptimizable32BitValue && data[0] >= 0)
            {
                flags |= 0x20;
            }

            if (data[1] == 0)
            {
                flags |= 0x08;
            }
            else if (data[1] <= HighestOptimizable32BitValue && data[1] >= 0)
            {
                flags |= 0x40;
            }

            if (data[2] == 0)
            {
                flags |= 0x10;
            }
            else if (data[2] <= HighestOptimizable32BitValue && data[2] >= 0)
            {
                flags |= 0x80;
            }

            _binaryWriter.Write(flags);

            if (scale != 0)
            {
                _binaryWriter.Write(scale);
            }

            if ((flags & 0x04) == 0)
            {
                if ((flags & 0x20) != 0)
                {
                    _binaryWriter.Write7BitEncodedInt(data[0]);                    
                }
                else
                {
                    _binaryWriter.Write(data[0]);
                }
            }

            if ((flags & 0x08) == 0)
            {
                if ((flags & 0x40) != 0)
                {
                    _binaryWriter.Write7BitEncodedInt(data[1]);                    
                }
                else
                {
                    _binaryWriter.Write(data[1]);
                }
            }

            if ((flags & 0x10) == 0)
            {
                if ((flags & 0x80) != 0)
                {
                    _binaryWriter.Write7BitEncodedInt(data[2]);                    
                }
                else
                {
                    _binaryWriter.Write(data[2]);
                }
            }
        }

        /// <summary>
        ///     Writes a character array to the current stream and advances the current position
        ///     of the stream in accordance with the Encoding used and the specific characters
        ///     being written to the stream.
        ///     Use ReadRawChars(...) in SerializationReader.
        /// </summary>
        /// <param name="chars"></param>
        public void WriteRaw(char[] chars)
        {
            _binaryWriter.Write(chars);
        }

        /// <summary>
        ///     Writes a section of a character array to the current stream, and advances the
        ///     current position of the stream in accordance with the Encoding used and perhaps
        ///     the specific characters being written to the stream.
        ///     Use ReadRawChars(...) in SerializationReader.
        /// </summary>
        /// <param name="chars"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public void WriteRaw(char[] chars, int index, int count)
        {
            _binaryWriter.Write(chars, index, count);
        }

        /// <summary>
        ///     Writes a region of a byte array to the current stream.
        ///     Use ReadRawBytes(int count) in SerializationReader.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        public void WriteRaw(byte[] buffer, int index, int count)
        {
            _binaryWriter.Write(buffer, index, count);
        }

        /// <summary>
        ///     Writes a byte array to the current stream.
        ///     Use ReadRawBytes(int count) in SerializationReader.
        /// </summary>
        /// <param name="buffer"></param>
        public void WriteRaw(byte[] buffer)
        {
            _binaryWriter.Write(buffer);
        }

        /// <summary>
        ///     Use ReadByteArray() for reading.        
        /// </summary>                
        /// <param name="values"></param>
        public void WriteArray(byte[] values)
        {            
            WriteOptimized(values.LongLength);

            if (values.LongLength > 0)
            {
                _binaryWriter.Write(values);
            }            
        }

        /// <summary>
        ///     Use ReadCharArray() for reading.        
        /// </summary>
        /// <param name="values"> The Char[] to store. </param>
        public void WriteArray(char[] values)
        {            
            WriteOptimized(values.LongLength);

            if (values.LongLength > 0)
            {
                _binaryWriter.Write(values);
            }
        }

        /// <summary>
        ///     Use ReadObject() or ReadObjectTyped() for read.
        ///     Stores an object into the stream using the fewest number of bytes possible.
        ///     Stored Size: 1 byte upwards depending on type and/or content.
        ///     1 byte: null, DBNull.Value, Boolean
        ///     1 to 2 bytes: Int16, UInt16, Byte, SByte, Char,
        ///     1 to 4 bytes: Int32, UInt32, Single
        ///     1 to 8 bytes: DateTime, TimeSpan, Double, Int64, UInt64
        ///     1 or 16 bytes: Guid
        ///     1 plus content: string, object[], byte[], char[], BitArray, Type, ArrayList
        ///     Any other object be stored using a .Net Binary formatter but this should
        ///     only be allowed as a last resort:
        ///     Since this is effectively a different serialization session, there is a
        ///     possibility of the same shared object being serialized twice or, if the
        ///     object has a reference directly or indirectly back to the parent object,
        ///     there is a risk of looping which will throw an exception.
        ///     The type of object is checked with the most common types being checked first.
        ///     Each 'section' can be reordered to provide optimum speed but the check for
        ///     null should always be first and the default serialization always last.
        ///     Once the type is identified, a SerializedType byte is stored in the stream
        ///     followed by the data for the object (certain types/values may not require
        ///     storage of data as the SerializedType may imply the value).
        ///     For certain objects, if the value is within a certain range then optimized
        ///     storage may be used. If the value doesn't meet the required optimization
        ///     criteria then the value is stored directly.
        ///     The checks for optimization may be disabled by setting the OptimizeForSize
        ///     property to false in which case the value is stored directly. This could
        ///     result in a slightly larger stream but there will be a speed increate to
        ///     compensate.
        /// </summary>
        /// <param name="value"> The object to store. </param>
        public void WriteObject(object? value)
        {
            if (value is null)
            {
                WriteSerializedType(SerializedType.NullType);
                return;
            }

            if (value is UnknownObject unknownObject)
            {
                WriteSerializedType(SerializedType.OwnedDataSerializableType);
                WriteOptimizedOrNot(unknownObject.TypeString);
                _binaryWriter.Write(unknownObject.Data.LongLength);
                _binaryWriter.Write(unknownObject.Data);
                return;
            }

            if (IsOwnedDataSerializableAndRecreatable(value.GetType()))
            {
                WriteSerializedType(SerializedType.OwnedDataSerializableType);
                WriteOptimized(value.GetType());
                // it will store the block length
                long beginPosition = _baseStream.Position;
                _binaryWriter.Write((long)0);                
                ((IOwnedDataSerializable) value).SerializeOwnedData(this, null);
                long endPosition = _baseStream.Position;                
                _baseStream.Position = beginPosition;
                _binaryWriter.Write(endPosition - beginPosition - sizeof(long));
                _baseStream.Position = endPosition;
                return;
            }
            
            if (value is string valueString)
            {
                WriteOptimizedOrNot(valueString);
                return;
            }

            if (value == DBNull.Value)
            {
                WriteSerializedType(SerializedType.DbNullType);
                return;
            }            

            if (value is Decimal valueDecimal)
            {                
                if (valueDecimal == 0)
                {
                    WriteSerializedType(SerializedType.ZeroDecimalType);
                }
                else if (valueDecimal == 1)
                {
                    WriteSerializedType(SerializedType.OneDecimalType);
                }
                else
                {
                    WriteSerializedType(SerializedType.DecimalType);
                    WriteOptimized(valueDecimal);
                }
                return;
            }

            if (value is DateTime valueDateTime)
            {                
                if (valueDateTime == DateTime.MinValue)
                {
                    WriteSerializedType(SerializedType.MinDateTimeType);
                }
                else if (valueDateTime == DateTime.MaxValue)
                {
                    WriteSerializedType(SerializedType.MaxDateTimeType);
                }
                else if ((valueDateTime.Ticks%TimeSpan.TicksPerMillisecond) == 0)
                {
                    WriteSerializedType(SerializedType.OptimizedDateTimeType);
                    WriteOptimized(valueDateTime);
                }
                else
                {
                    WriteSerializedType(SerializedType.DateTimeType);
                    Write(valueDateTime);
                }
                return;
            }

            if (value is Guid valueGuid)
            {                
                if (valueGuid == Guid.Empty)
                {
                    WriteSerializedType(SerializedType.EmptyGuidType);
                }
                else
                {
                    WriteSerializedType(SerializedType.GuidType);
                    Write(valueGuid);
                }
                return;
            }

            if (value is Int64 valueInt64)
            {                
                switch (valueInt64)
                {
                    case 0:
                        WriteSerializedType(SerializedType.ZeroInt64Type);
                        return;
                    case -1:
                        WriteSerializedType(SerializedType.MinusOneInt64Type);
                        return;
                    case 1:
                        WriteSerializedType(SerializedType.OneInt64Type);
                        return;
                    default:
                        if (valueInt64 > 0)
                        {
                            if (valueInt64 <= HighestOptimizable64BitValue)
                            {
                                WriteSerializedType(SerializedType.OptimizedInt64Type);                                
                                WriteOptimized(valueInt64);
                                return;
                            }
                        }
                        else
                        {
                            long positiveInt64Value = -(valueInt64 + 1);

                            if (positiveInt64Value <= HighestOptimizable64BitValue)
                            {
                                WriteSerializedType(SerializedType.OptimizedInt64NegativeType);                                
                                WriteOptimized(positiveInt64Value);
                                return;
                            }
                        }
                        WriteSerializedType(SerializedType.Int64Type);
                        _binaryWriter.Write(valueInt64);
                        return;
                }
            }

            if (value is UInt64 valueUInt64)
            {                
                switch (valueUInt64)
                {
                    case 0:
                        WriteSerializedType(SerializedType.ZeroUInt64Type);
                        return;
                    case 1:
                        WriteSerializedType(SerializedType.OneUInt64Type);
                        return;
                    default:
                        if (valueUInt64 <= HighestOptimizable64BitValue)
                        {
                            WriteSerializedType(SerializedType.OptimizedUInt64Type);
                            WriteOptimized(valueUInt64);
                        }
                        else
                        {
                            WriteSerializedType(SerializedType.UInt64Type);
                            _binaryWriter.Write(valueUInt64);
                        }
                        return;
                }
            }

            if (value is TimeSpan valueTimeSpan)
            {                
                if (valueTimeSpan == TimeSpan.Zero)
                {
                    WriteSerializedType(SerializedType.ZeroTimeSpanType);
                }
                else if ((valueTimeSpan.Ticks%TimeSpan.TicksPerMillisecond) == 0)
                {
                    WriteSerializedType(SerializedType.OptimizedTimeSpanType);
                    WriteOptimized(valueTimeSpan);
                }
                else
                {
                    WriteSerializedType(SerializedType.TimeSpanType);
                    Write(valueTimeSpan);
                }
                return;
            }
            
            if (value is Array valueArray)
            {
                WriteArrayInternal(valueArray, valueArray.GetType().GetElementType() ?? typeof(object));
                return;
            }
            
            if (value is Type valueType)
            {
                WriteSerializedType(SerializedType.TypeType);
                WriteOptimized(valueType);
                return;
            }
            
            if (value is BitArray valueBitArray)
            {
                WriteSerializedType(SerializedType.BitArrayType);
                WriteOptimized(valueBitArray);
                return;
            }
            
            if (value is ArrayList valueArrayList)
            {
                WriteSerializedType(SerializedType.ArrayListType);
                WriteOptimizedObjectArray(valueArrayList.ToArray());
                return;
            }

            if (value is Enum)
            {
                Type enumType = value.GetType();
                Type underlyingType = Enum.GetUnderlyingType(enumType);

                switch (Type.GetTypeCode(underlyingType))
                {
                    case TypeCode.Int32:
                    case TypeCode.UInt32:
                        uint uint32Value = underlyingType == typeof (int) ? (uint) (int) value : (uint) value;

                        if (uint32Value <= HighestOptimizable32BitValue)
                        {
                            WriteSerializedType(SerializedType.OptimizedEnumType);
                            WriteOptimized(enumType);
                            WriteOptimized(uint32Value);
                        }
                        else
                        {
                            WriteSerializedType(SerializedType.EnumType);
                            WriteOptimized(enumType);
                            _binaryWriter.Write(uint32Value);
                        }
                        return;
                    case TypeCode.Int64:
                    case TypeCode.UInt64:
                        ulong uint64Value = underlyingType == typeof (long) ? (ulong) (long) value : (ulong) value;

                        if (uint64Value <= HighestOptimizable64BitValue)
                        {
                            WriteSerializedType(SerializedType.OptimizedEnumType);
                            WriteOptimized(enumType);
                            WriteOptimized(uint64Value);
                        }
                        else
                        {
                            WriteSerializedType(SerializedType.EnumType);
                            WriteOptimized(enumType);
                            _binaryWriter.Write(uint64Value);
                        }
                        return;
                    case TypeCode.Byte:
                        WriteSerializedType(SerializedType.EnumType);
                        WriteOptimized(enumType);
                        _binaryWriter.Write((byte) value);
                        return;
                    case TypeCode.SByte:
                        WriteSerializedType(SerializedType.EnumType);
                        WriteOptimized(enumType);
                        _binaryWriter.Write((sbyte) value);
                        return;

                    case TypeCode.Int16:
                        WriteSerializedType(SerializedType.EnumType);
                        WriteOptimized(enumType);
                        _binaryWriter.Write((short) value);
                        return;
                    default:
                        WriteSerializedType(SerializedType.EnumType);
                        WriteOptimized(enumType);
                        _binaryWriter.Write((ushort) value);
                        return;
                }
            }

            if (value is Char valueChar)
            {
                switch (valueChar)
                {
                    case (Char)0:
                        WriteSerializedType(SerializedType.ZeroCharType);
                        return;
                    case (Char)1:
                        WriteSerializedType(SerializedType.OneCharType);
                        return;
                    default:
                        WriteSerializedType(SerializedType.CharType);
                        _binaryWriter.Write(valueChar);
                        return;
                }
            }

            if (value is Boolean valueBool)
            {
                WriteSerializedType(valueBool ? SerializedType.BooleanTrueType : SerializedType.BooleanFalseType);
                return;
            }

            if (value is Double valueDouble)
            {
                if (valueDouble == 0.0)
                {
                    WriteSerializedType(SerializedType.ZeroDoubleType);
                }
                else if (valueDouble == 1.0)
                {
                    WriteSerializedType(SerializedType.OneDoubleType);
                }
                else
                {
                    WriteSerializedType(SerializedType.DoubleType);
                    _binaryWriter.Write(valueDouble);
                }
                return;
            }

            if (value is Single valueSingle)
            {
                if (valueSingle == 0.0)
                {
                    WriteSerializedType(SerializedType.ZeroSingleType);
                }
                else if (valueSingle == 1.0)
                {
                    WriteSerializedType(SerializedType.OneSingleType);
                }
                else
                {
                    WriteSerializedType(SerializedType.SingleType);
                    _binaryWriter.Write(valueSingle);
                }
                return;
            }

            if (value is SByte valueSByte)
            {
                switch (valueSByte)
                {
                    case 0:
                        WriteSerializedType(SerializedType.ZeroSByteType);
                        return;
                    case 1:
                        WriteSerializedType(SerializedType.OneSByteType);
                        return;
                    default:
                        WriteSerializedType(SerializedType.SByteType);
                        _binaryWriter.Write(valueSByte);
                        return;
                }
            }

            if (value is Byte valueByte)
            {
                switch (valueByte)
                {
                    case 0:
                        WriteSerializedType(SerializedType.ZeroByteType);
                        return;
                    case 1:
                        WriteSerializedType(SerializedType.OneByteType);
                        return;
                    default:
                        WriteSerializedType(SerializedType.ByteType);
                        _binaryWriter.Write(valueByte);
                        return;
                }
            }

            if (value is Int16 valueInt16)
            {
                switch (valueInt16)
                {
                    case 0:
                        WriteSerializedType(SerializedType.ZeroInt16Type);
                        return;
                    case -1:
                        WriteSerializedType(SerializedType.MinusOneInt16Type);
                        return;
                    case 1:
                        WriteSerializedType(SerializedType.OneInt16Type);
                        return;
                    default:
                        if (valueInt16 > 0)
                        {
                            if (valueInt16 <= HighestOptimizable16BitValue)
                            {
                                WriteSerializedType(SerializedType.OptimizedInt16Type);
                                WriteOptimized(valueInt16);
                                return;
                            }
                        }
                        else
                        {
                            int positiveInt16Value = (-(valueInt16 + 1));

                            if (positiveInt16Value <= HighestOptimizable16BitValue)
                            {
                                WriteSerializedType(SerializedType.OptimizedInt16NegativeType);
                                _binaryWriter.Write7BitEncodedInt(positiveInt16Value);                                
                                return;
                            }
                        }
                        WriteSerializedType(SerializedType.Int16Type);
                        _binaryWriter.Write(valueInt16);
                        return;
                }
            }

            if (value is UInt16 valueUInt16)
            {
                switch (valueUInt16)
                {
                    case 0:
                        WriteSerializedType(SerializedType.ZeroUInt16Type);
                        return;
                    case 1:
                        WriteSerializedType(SerializedType.OneUInt16Type);
                        return;
                    default:
                        if (valueUInt16 <= HighestOptimizable16BitValue)
                        {
                            WriteSerializedType(SerializedType.OptimizedUInt16Type);
                            WriteOptimized(valueUInt16);
                        }
                        else
                        {
                            WriteSerializedType(SerializedType.UInt16Type);
                            _binaryWriter.Write(valueUInt16);
                        }
                        return;
                }
            }

            if (value is Int32 valueInt32)
            {
                switch (valueInt32)
                {
                    case 0:
                        WriteSerializedType(SerializedType.ZeroInt32Type);
                        return;
                    case -1:
                        WriteSerializedType(SerializedType.MinusOneInt32Type);
                        return;
                    case 1:
                        WriteSerializedType(SerializedType.OneInt32Type);
                        return;
                    default:
                        if (valueInt32 > 0)
                        {
                            if (valueInt32 <= HighestOptimizable32BitValue)
                            {
                                WriteSerializedType(SerializedType.OptimizedInt32Type);
                                _binaryWriter.Write7BitEncodedInt(valueInt32);                                
                                return;
                            }
                        }
                        else
                        {
                            int positiveInt32Value = -(valueInt32 + 1);
                            if (positiveInt32Value <= HighestOptimizable32BitValue)
                            {
                                WriteSerializedType(SerializedType.OptimizedInt32NegativeType);
                                _binaryWriter.Write7BitEncodedInt(positiveInt32Value);                                
                                return;
                            }
                        }
                        WriteSerializedType(SerializedType.Int32Type);
                        _binaryWriter.Write(valueInt32);
                        return;
                }
            }

            if (value is UInt32 valueUInt32)
            {
                switch (valueUInt32)
                {
                    case 0:
                        WriteSerializedType(SerializedType.ZeroUInt32Type);
                        return;
                    case 1:
                        WriteSerializedType(SerializedType.OneUInt32Type);
                        return;
                    default:
                        if (valueUInt32 <= HighestOptimizable32BitValue)
                        {
                            WriteSerializedType(SerializedType.OptimizedUInt32Type);
                            WriteOptimized(valueUInt32);
                        }
                        else
                        {
                            WriteSerializedType(SerializedType.UInt32Type);
                            _binaryWriter.Write(valueUInt32);
                        }
                        return;
                }
            }

            WriteOtherType(value);
        }        

        /// <summary>
        ///     Use ReadObject() or ReadObjectTyped() for read.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void WriteObjectTyped<T>(T? value)
            where T : class
        {
            if (value is null)
            {
                WriteSerializedType(SerializedType.NullType);
                return;
            }

            if (IsOwnedDataSerializableAndRecreatable(value.GetType()))
            {
                WriteSerializedType(SerializedType.OwnedDataSerializableType);
                WriteOptimized(value.GetType());
                ((IOwnedDataSerializable)value).SerializeOwnedData(this, null);
                return;
            }
            
            if (value is string valueString)
            {
                WriteOptimizedOrNot(valueString);
                return;
            }
            
            if (value is Array valueArray)
            {
                WriteArrayInternal(valueArray, valueArray.GetType().GetElementType() ?? typeof(object));
                return;
            }
            
            if (value is Type valueType)
            {
                WriteSerializedType(SerializedType.TypeType);
                WriteOptimized(valueType);
                return;
            }
            
            if (value is BitArray valueBitArray)
            {
                WriteSerializedType(SerializedType.BitArrayType);
                WriteOptimized(valueBitArray);
                return;
            }
            
            if (value is ArrayList valueArrayList)
            {
                WriteSerializedType(SerializedType.ArrayListType);
                WriteOptimizedObjectArray(valueArrayList.ToArray());
                return;
            }

            WriteOtherType(value);
        }

        /// <summary>
        ///     Use ReadOwnedDataSerializable(...) for read.
        ///     Allows any object implementing IOwnedDataSerializable to serialize itself
        ///     into this SerializationWriter.
        ///     A context may also be used to give the object an indication of what data
        ///     to store.        
        /// </summary>
        /// <param name="target"> The IOwnedDataSerializable object to ask for owned data </param>
        /// <param name="context"> An arbtritrary object </param>
        public void WriteOwnedDataSerializable(IOwnedDataSerializable target, object? context)
        {
            target.SerializeOwnedData(this, context);
        }

        /// <summary>
        ///     Use ReadOwnedDataSerializable( Func<int,IOwnedDateSerializable?>, object?) for read.
        ///     Allows any object supply serialization typeId and implementing IOwnedDataSerializable to serialize itself
        ///     into this SerializationWriter.
        ///     A context may also be used to give the object an indication of what data
        ///     to store.        
        /// </summary>
        /// <param name="target"></param>
        /// <param name="typeId"></param>
        /// <param name="context"></param>
        public void WriteOwnedDataSerializable(IOwnedDataSerializable? target, int typeId, object? context)
        {
            if (target != null)
            {
                WriteSerializedType(SerializedType.OptimizedInt32Type);  // type code type )
                _binaryWriter.Write7BitEncodedInt(typeId);                
                target.SerializeOwnedData(this, context);
            }
            else
                WriteSerializedType(SerializedType.NullType); 
        }

        /// <summary>
        ///     Use ReadOwnedDataSerializable( Func<string,IOwnedDateSerializable?>, object?) for read.
        ///     Allows any object supply serialization typeStringId and implementing IOwnedDataSerializable to serialize itself
        ///     into this SerializationWriter.
        ///     A context may also be used to give the object an indication of what data
        ///     to store.        
        /// </summary>
        /// <param name="target"></param>
        /// <param name="typeStringId"></param>
        /// <param name="context"></param>
        /// <exception cref="ArgumentException"></exception>
        public void WriteOwnedDataSerializable(IOwnedDataSerializable? target, string typeStringId, object? context)
        {
            if (target != null )
            {
                if (!string.IsNullOrEmpty(typeStringId))
                {
                    Write(typeStringId);                                  // type Id
                    target.SerializeOwnedData(this, context);
                }
                else
                    throw new ArgumentException($"Parameter typeStringId has invalid value for object {target.GetType().Name}");
            }
            else
                WriteSerializedType(SerializedType.NullType);
        }

        /// <summary>
        ///     Use ReadOwnedDataSerializable( Func<Guid,IOwnedDateSerializable?>, object?) for read.
        ///     Allows any object supply serialization Guid and implementing IOwnedDataSerializable to serialize itself
        ///     into this SerializationWriter.
        ///     A context may also be used to give the object an indication of what data
        ///     to store.        
        /// </summary>
        /// <param name="target"></param>
        /// <param name="typeGuidId"></param>
        /// <param name="context"></param>
        /// <exception cref="ArgumentException"></exception>
        public void WriteOwnedDataSerializable(IOwnedDataSerializable? target, Guid typeGuidId, object? context)
        {
            if (target != null)
            {
                if (typeGuidId != Guid.Empty)
                {
                    WriteSerializedType(SerializedType.GuidType);           // type code type )
                    Write(typeGuidId);                                      // type Id
                    target.SerializeOwnedData(this, context);
                }
                else
                    throw new ArgumentException($"Parameter typeGuidId has invalid value for object {target.GetType().Name}");
            }
            else
                WriteSerializedType(SerializedType.NullType);
        }

        /// <summary>
        ///     Use ReadOwnedDataSerializable( Func<Type,IOwnedDateSerializable?>, object?) for read.
        ///     Allows any object supply serialization Guid and implementing IOwnedDataSerializable to serialize itself
        ///     into this SerializationWriter.
        ///     A context may also be used to give the object an indication of what data
        ///     to store.        
        /// </summary>
        /// <param name="target"></param>
        /// <param name="context"></param>
        /// <exception cref="ArgumentException"></exception>
        public void WriteOwnedDataSerializableWithType(IOwnedDataSerializable? target, object? context)
        {
            if (target != null)
            {
                var type = target.GetType();
                if (type != null)
                {
                    WriteSerializedType(SerializedType.TypeType);               // type code type )
                    WriteOptimized(type);                                       // type 
                    target.SerializeOwnedData(this, context);
                }
                else
                    throw new ArgumentException($"Fail to obtain type information for parameter target for serializing");
            }
            else
                WriteSerializedType(SerializedType.NullType);
        }



        /// <summary>
        ///     Use ReadOwnedDataSerializableAndRecreatable(...) for read.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="context"></param>
        public void WriteOwnedDataSerializableAndRecreatable<T>(T? value, object? context)
            where T : class, IOwnedDataSerializable
        {
            if (value is null)
            {
                WriteSerializedType(SerializedType.NullType);
                return;
            }

            WriteSerializedType(SerializedType.OwnedDataSerializableType);
            value.SerializeOwnedData(this, context);
        }        

        /// <summary>
        ///     User ReadNullable<T>() for reading.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        public void WriteNullable<T>(T? value)
            where T : struct
        {
            if (!value.HasValue)
            {
                WriteSerializedType(SerializedType.NullType);
            }
            else
            {
                WriteObject((object)value.Value);
            }
        }        

        ///// <summary>
        /////     Writes a System.Windows.Point value into the stream.
        ///// </summary>
        ///// <param name="value"> The System.Windows.Point value to store. </param>
        //public void Write(Point value)
        //{
        //    _binaryWriter.Write(value.X);
        //    _binaryWriter.Write(value.Y);
        //}

        ///// <summary>
        /////     Writes a System.Windows.Size value into the stream.
        ///// </summary>
        ///// <param name="value"> The System.Windows.Size value to store. </param>
        //public void Write(Size value)
        //{
        //    _binaryWriter.Write(value.Width);
        //    _binaryWriter.Write(value.Height);
        //}

        /// <summary>        
        ///     Use ReadArray() for reading.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        public void WriteArray<T>(T[]? values)
        {
            if (values is null)
            {
                WriteSerializedType(SerializedType.NullType);
            }
            else
            {
                WriteArrayInternal(values, typeof (T));
            }
        }

        /// <summary>
        ///     Use ReadArrayOfSingle() for reading.
        /// </summary>
        /// <param name="values"></param>
        public void WriteArrayOfSingle(float[] values)
        {            
            WriteOptimized(values.LongLength);

            foreach (float value in values)
            {
                _binaryWriter.Write(value);
            }
        }

        /// <summary>
        ///     Use ReadArrayOfDouble() for reading.
        /// </summary>
        /// <param name="values"></param>
        public void WriteArrayOfDouble(double[] values)
        {            
            WriteOptimized(values.LongLength);

            foreach (double value in values)
            {
                _binaryWriter.Write(value);
            }
        }

        /// <summary>
        ///     Use ReadArrayOfDecimal() for reading.
        ///     All elements are stored optimized.
        /// </summary>
        /// <param name="values"></param>
        public void WriteArrayOfDecimal(decimal[] values)
        {            
            WriteOptimized(values.LongLength);

            for (long i = 0; i < values.LongLength; i++)
            {
                WriteOptimized(values[i]);
            }
        }

        /// <summary>
        ///     use ReadArrayOfOwnedDataSerializable(...) for reading.
        ///     Writes array of same type not null objects.         
        /// </summary>        
        /// <param name="values"></param>
        /// <param name="context"></param>
        public void WriteArrayOfOwnedDataSerializable<T>(T[] values,
            object? context)
            where T : IOwnedDataSerializable
        {            
            WriteOptimized(values.LongLength);

            foreach (var v in values)
            {
                v.SerializeOwnedData(this, context);
            }
        }

        /// <summary>
        ///     use  T[] ReadArrayOfOwnedDataSerializable<T>(Func<int, IOwnedDataSerializable?> intTypeIdCreateFunc,
        ///         object? context) for reading array
        ///     Write array polymorphic objects to stream
        /// </summary>
        /// <param name="values">array of serializable objects</param>
        /// <param name="typeIdFunc"> return integer Type Id for stored object</param>
        /// <param name="context"></param>
        public void WriteArrayOfOwnedDataSerializable<T>(T[] values, Func<T, int> typeIdFunc, object? context)
            where T : IOwnedDataSerializable
        {
            Write(values.Length);
            foreach (var v in values)
            {
                WriteOwnedDataSerializable(v, typeIdFunc(v), context);
            }
        }

        /// <summary>
        ///     Use ReadNullableByteArray() for reading.
        /// </summary>
        /// <param name="values"></param>
        public void WriteNullableByteArray(byte[]? values)
        {
            if (values is null)
            {
                WriteSerializedType(SerializedType.NullType);
            }
            else
            {
                WriteSerializedType(SerializedType.ByteArrayType);

                WriteArray(values);
            }            
        }

        /// <summary>
        ///     use ReadList() for reading.
        ///     Writes a nullable generic List into the stream.
        ///     Objects can be of different types.
        /// </summary>
        /// <remarks>
        ///     The list type itself is not stored - it must be supplied
        ///     at deserialization time.
        ///     <para />
        ///     The list contents are stored as an array.
        /// </remarks>
        /// <typeparam name="T"> The list Type. </typeparam>
        /// <param name="value"> The generic List. </param>
        public void WriteList<T>(IList<T>? value)
        {
            if (value is null)
            {
                WriteSerializedType(SerializedType.NullType);
            }
            else
            {
                WriteArrayInternal(value.ToArray(), typeof (T));
            }
        }

        /// <summary>
        ///     use ReadListOfOwnedDataSerializable(...) for reading.
        ///     Writes list of same type not null objects.         
        /// </summary>        
        /// <param name="values"></param>
        /// <param name="context"></param>
        public void WriteListOfOwnedDataSerializable<T>(ICollection<T> values,
            object? context)
            where T : IOwnedDataSerializable
        {
            Write(values.Count);
            foreach (var v in values)
            {
                v.SerializeOwnedData(this, context);
            }
        }

        /// <summary>
        ///     use ReadListOfOwnedDataSerializable<T>(Func<int, IOwnedDataSerializable?> intTypeIdCreateFunc,
        ///         object? context) for reading
        ///     Writes list of polymorhic objects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="values"></param>
        /// <param name="typeIdFunc"> provide type identifiers for each element</param>
        /// <param name="context"></param>
        public void WriteListOfOwnedDataSerializable<T>(ICollection<T> values, Func<T, int> typeIdFunc,
            object? context)
            where T : IOwnedDataSerializable
        {
            Write(values.Count);
            foreach (var v in values)
            {
                WriteOwnedDataSerializable(v, typeIdFunc(v), context);
            }
        }



        /// <summary>
        ///     use ReadListOfStrings() for reading.
        ///     Writes list of strings.    
        /// </summary>
        /// <param name="values"></param>
        public void WriteListOfStrings(ICollection<string> values)
        {
            Write(values.Count);
            foreach (var v in values)
            {
                Write(v);
            }
        }

        /// <summary>
        ///     Use ReadDictionary() for reading.
        ///     Writes a generic Dictionary into the stream.
        /// </summary>
        /// <remarks>
        ///     The key and value types themselves are not stored - they must be
        ///     supplied at deserialization time.
        ///     <para />
        ///     An array of keys is stored followed by an array of values.
        /// </remarks>
        /// <typeparam name="TK"> The key Type. </typeparam>
        /// <typeparam name="TV"> The value Type. </typeparam>
        /// <param name="value"> The generic dictionary. </param>
        public void WriteDictionary<TK, TV>(Dictionary<TK, TV>? value)
            where TK : notnull
        {
            if (value is null)
            {
                WriteSerializedType(SerializedType.NullType);
            }
            else
            {
                WriteArrayInternal(value.Keys.ToArray(), typeof(TK));
                WriteArrayInternal(value.Values.ToArray(), typeof(TV));
            }            
        }

        /// <summary>
        ///     use ReadDictionaryOfOwnedDataSerializable(...) for reading.
        ///     Writes Dictionary of same type not null objects.         
        /// </summary>        
        /// <param name="values"></param>
        /// <param name="context"></param>
        public void WriteDictionaryOfOwnedDataSerializable<T>(Dictionary<string, T> values,
            object? context)
            where T : IOwnedDataSerializable
        {
            Write(values.Count);
            foreach (var kvp in values)
            {
                Write(kvp.Key);
                kvp.Value.SerializeOwnedData(this, context);
            }
        }

        //public void WriteCaseInsensitiveDictionary<T>(CaseInsensitiveDictionary<T> value)
        //    where T : class?
        //{
        //    Write(value.Count);
        //    foreach (var kvp in value)
        //    {
        //        Write(kvp.Key);
        //        WriteObject<T>(kvp.Value);
        //    }
        //}

        /// <summary>
        ///     Writes a BitArray value into the stream using the fewest number of bytes possible.
        ///     Stored Size: 1 byte upwards depending on data content
        ///     Notes:
        ///     A null BitArray takes 1 byte.
        ///     An empty BitArray takes 2 bytes.
        /// </summary>
        /// <param name="value"> The BitArray value to store. </param>
        public void Write(BitArray? value)
        {
            if (value is null)
            {
                WriteSerializedType(SerializedType.NullType);
            }
            else
            {
                WriteSerializedType(SerializedType.BitArrayType);
                WriteOptimized(value);
            }
        }

#endregion

#region internal functions

        internal const short OptimizationFailure16BitValue = 16384;

        // The int at which optimization fails because it takes more than 4 bytes
        internal const int OptimizationFailure32BitValue = 268435456; // 0x10000000

        // The long at which optimization fails because it takes more than 8 bytes
        internal const long OptimizationFailure64BitValue = 72057594037927936; // 0x0100000000000000

        /// <summary>
        ///     Section masks used for packing DateTime values
        /// </summary>
        internal static readonly BitVector32.Section DateYearMask = BitVector32.CreateSection(9999); //14 bits

        internal static readonly BitVector32.Section DateMonthMask = BitVector32.CreateSection(12, DateYearMask);
        // 4 bits

        internal static readonly BitVector32.Section DateDayMask = BitVector32.CreateSection(31, DateMonthMask);
        // 5 bits

        internal static readonly BitVector32.Section DateHasTimeOrKindMask = BitVector32.CreateSection(1, DateDayMask);
        // 1 bit  total= 3 bytes

        /// <summary>
        ///     Section masks used for packing TimeSpan values
        /// </summary>
        internal static readonly BitVector32.Section IsNegativeSection = BitVector32.CreateSection(1); //1 bit

        internal static readonly BitVector32.Section HasDaysSection = BitVector32.CreateSection(1, IsNegativeSection);
        //1 bit

        internal static readonly BitVector32.Section HasTimeSection = BitVector32.CreateSection(1, HasDaysSection);
        //1 bit

        internal static readonly BitVector32.Section HasSecondsSection = BitVector32.CreateSection(1, HasTimeSection);
        //1 bit

        internal static readonly BitVector32.Section HasMillisecondsSection = BitVector32.CreateSection(1,
            HasSecondsSection); //1 bit

        internal static readonly BitVector32.Section HoursSection = BitVector32.CreateSection(23, HasMillisecondsSection);
        // 5 bits

        internal static readonly BitVector32.Section MinutesSection = BitVector32.CreateSection(59, HoursSection);
        // 6 bits  total = 2 bytes

        internal static readonly BitVector32.Section SecondsSection = BitVector32.CreateSection(59, MinutesSection);
        // 6 bits total = 3 bytes

        internal static readonly BitVector32.Section MillisecondsSection = BitVector32.CreateSection(1024,
            SecondsSection); // 10 bits - total 31 bits = 4 bytes

#endregion

#region private functions

        /// <summary>
        ///     Checks whether instances of a Type can be created.
        /// </summary>
        /// <remarks>
        ///     A Value Type only needs to implement IOwnedDataSerializable.
        ///     A Reference Type needs to implement IOwnedDataSerializableAndRecreatable and provide a default constructor.
        /// </remarks>
        /// <param name="type"> The Type to check </param>
        /// <returns> true if the Type is recreatable; false otherwise. </returns>
        private static bool IsOwnedDataSerializableAndRecreatable(Type type)
        {
#if DEBUG && EXTDEBUG
            Console.WriteLine("IsOwnedDataSerializableAndRecreatable");
            var s = (!type.IsValueType) ? "not" : "";
            Console.WriteLine($"The type {type} is {s} ValueType");
            s = typeof(IOwnedDataSerializable).IsAssignableFrom(type) ? "True" : "False";
            Console.WriteLine($"typeof(IOwnedDataSerializable).IsAssignableFrom({type}) is {s}.");
#endif
            if (type.IsValueType) return typeof (IOwnedDataSerializable).IsAssignableFrom(type);
#if DEBUG && EXTDEBUG            
            // !!! Reflection requared!
            s = type.GetConstructor(Type.EmptyTypes) is not null ? "Constructor found." : "";

            Console.WriteLine($"type.GetConstructor(Type.EmptyTypes) {s}");
#endif
            return typeof (IOwnedDataSerializable).IsAssignableFrom(type) &&
                   type.GetConstructor(Type.EmptyTypes) is not null;
        }

        private void WriteOtherType(object value)
        {
            WriteSerializedType(SerializedType.OtherType);
            Type type = value.GetType();
            WriteOptimized(type);
            string s = JsonSerializer.Serialize(value, type, new JsonSerializerOptions
                {
                    NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals 
                });
            WriteOptimizedOrNot(s);
        }        

        /// <summary>
        ///     Writes a string value into the stream using the fewest number of bytes possible.
        ///     Stored Size: 1 byte upwards depending on string length
        ///     Notes:
        ///     Encodes null, Empty, 'Y', 'N', ' ' values as a single byte
        ///     Any other single char string is stored as two bytes
        ///     All other strings are stored in a string token list:
        ///     The TypeCode representing the current string token list is written first (1 byte),
        ///     followed by the string token itself (1-4 bytes)
        ///     When the current string list has reached 128 values then a new string list
        ///     is generated and that is used for generating future string tokens. This continues
        ///     until the maximum number (128) of string lists is in use, after which the string
        ///     lists are used in a round-robin fashion.
        ///     By doing this, more lists are created with fewer items which allows a smaller
        ///     token size to be used for more strings.
        ///     The first 16,384 strings will use a 1 byte token.
        ///     The next 2,097,152 strings will use a 2 byte token. (This should suffice for most uses!)
        ///     The next 268,435,456 strings will use a 3 byte token. (My, that is a lot!!)
        ///     The next 34,359,738,368 strings will use a 4 byte token. (only shown for completeness!!!)
        /// </summary>
        /// <param name="value"> The string to store. </param>
        private void WriteOptimizedOrNot(string? value)
        {
            if (value is null)
            {
                WriteSerializedType(SerializedType.NullType);
                return;
            }

            if (value.Length == 0)
            {
                WriteSerializedType(SerializedType.EmptyStringType);
                return;
            }

            if (value.Length == 1)
            {
                char singleChar = value[0];
                switch (singleChar)
                {
                    case ' ':
                        WriteSerializedType(SerializedType.SingleSpaceType);
                        return;
                    case 'Y':
                        WriteSerializedType(SerializedType.YStringType);
                        return;
                    case 'N':
                        WriteSerializedType(SerializedType.NStringType);
                        return;                    
                    default:
                        WriteSerializedType(SerializedType.SingleCharStringType);
                        _binaryWriter.Write(singleChar);
                        return;
                }
            }

            if (_stringsDictionary is null)
            {
                WriteSerializedType(SerializedType.StringDirect);
                _binaryWriter.Write(value);
            }
            else
            {
                int index;
                if (!_stringsDictionary.TryGetValue(value, out index))
                {
                    if (_stringsList is null) throw new InvalidOperationException();
                    index = _stringsList.Count;
                    _stringsDictionary.Add(value, index);
                    _stringsList.Add(value);
                }
                WriteSerializedType(SerializedType.OptimizedStringType);
                WriteOptimized((long)index);                
            }            
        }

        /// <summary>
        ///     Writes a TimeSpan value into the stream using the fewest number of bytes possible.
        ///     Stored Size: 2 bytes to 8 bytes (.Net is 8 bytes)
        ///     Notes:
        ///     hh:mm (time) are always stored together and take 2 bytes.
        ///     If seconds are present then 3 bytes unless (time) is not present in which case 2 bytes
        ///     since the seconds are stored in the minutes position.
        ///     If milliseconds are present then 4 bytes.
        ///     In addition, if days are present they will add 1 to 4 bytes to the above.
        /// </summary>
        /// <param name="value"> The TimeSpan value to store. Must not contain sub-millisecond data. </param>
        private void WriteOptimized(TimeSpan value)
        {
            EncodeTimeSpan(value, false, 0);
        }

        /// <summary>
        ///     Stores a non-null Type object into the stream.
        ///     Stored Size: Depends on the length of the Type's name.
        ///     If the type is a System type (mscorlib) then it is stored without assembly name information,
        ///     otherwise the Type's AssemblyQualifiedName is used.
        /// </summary>
        /// <param name="value"> The Type to store. Must not be null. </param>
        private void WriteOptimized(Type value)
        {
            WriteOptimizedOrNot(
                (value.AssemblyQualifiedName ?? "").IndexOf(", mscorlib,", StringComparison.InvariantCultureIgnoreCase) == -1
                    ? value.AssemblyQualifiedName ?? ""
                    : value.FullName ?? "");
        }

        /// <summary>
        ///     Writes an Int16[] into the stream using the fewest possible bytes.
        /// </summary>
        /// <param name="values"></param>
        private void WriteOptimizedArrayInternal(short[] values)
        {
            BitArray writeOptimizedFlags = AllFalseBitArray;
            if (values.LongLength < Int32.MaxValue)
            {
                int notOptimizable = 0;
                int notWorthOptimizingLimit = 1 + (int)(values.Length * (true ? 0.8f : 0.6f));

                for (int i = 0; i < values.Length && notOptimizable < notWorthOptimizingLimit; i++)
                {
                    if (values[i] < 0 || values[i] > HighestOptimizable16BitValue)
                    {
                        notOptimizable++;
                    }
                    else
                    {
                        if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray))
                        {
                            writeOptimizedFlags = new BitArray(values.Length);
                        }

                        writeOptimizedFlags[(int)i] = true;
                    }
                }

                if (notOptimizable == 0)
                {
                    writeOptimizedFlags = AllTrueBitArray;
                }
                else if (notOptimizable >= notWorthOptimizingLimit)
                {
                    writeOptimizedFlags = AllFalseBitArray;
                }
            }

            WriteArray(values, writeOptimizedFlags);
        }

        /// <summary>
        ///     Writes an Int32[] into the stream using the fewest possible bytes.
        /// </summary>
        /// <param name="values"></param>
        private void WriteOptimizedArrayInternal(int[] values)
        {
            BitArray writeOptimizedFlags = AllFalseBitArray;
            if (values.LongLength < Int32.MaxValue)
            {
                int notOptimizable = 0;
                int notWorthOptimizingLimit = 1 + (int)(values.Length * (true ? 0.8f : 0.6f));

                for (int i = 0; i < values.Length && notOptimizable < notWorthOptimizingLimit; i++)
                {
                    if (values[i] < 0 || values[i] > HighestOptimizable32BitValue)
                    {
                        notOptimizable++;
                    }
                    else
                    {
                        if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray))
                        {
                            writeOptimizedFlags = new BitArray(values.Length);
                        }

                        writeOptimizedFlags[(int)i] = true;
                    }
                }

                if (notOptimizable == 0)
                {
                    writeOptimizedFlags = AllTrueBitArray;
                }
                else if (notOptimizable >= notWorthOptimizingLimit)
                {
                    writeOptimizedFlags = AllFalseBitArray;
                }
            }

            WriteArray(values, writeOptimizedFlags);
        }

        /// <summary>
        ///     Writes an Int64[] into the stream using the fewest possible bytes.
        /// </summary>
        /// <param name="values"></param>
        private void WriteOptimizedArrayInternal(long[] values)
        {
            BitArray writeOptimizedFlags = AllFalseBitArray;
            if (values.LongLength < Int32.MaxValue)
            {
                int notOptimizable = 0;
                int notWorthOptimizingLimit = 1 + (int)(values.Length * (true ? 0.8f : 0.6f));

                for (int i = 0; i < values.Length && notOptimizable < notWorthOptimizingLimit; i++)
                {
                    if (values[i] < 0 || values[i] > HighestOptimizable64BitValue)
                    {
                        notOptimizable++;
                    }
                    else
                    {
                        if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray))
                        {
                            writeOptimizedFlags = new BitArray(values.Length);
                        }

                        writeOptimizedFlags[(int)i] = true;
                    }
                }

                if (notOptimizable == 0)
                {
                    writeOptimizedFlags = AllTrueBitArray;
                }
                else if (notOptimizable >= notWorthOptimizingLimit)
                {
                    writeOptimizedFlags = AllFalseBitArray;
                }
            }

            WriteArray(values, writeOptimizedFlags);
        }

        /// <summary>
        ///     Writes a UInt16[] into the stream using the fewest possible bytes.
        /// </summary>
        /// <param name="values"></param>
        private void WriteOptimizedArrayInternal(ushort[] values)
        {
            BitArray writeOptimizedFlags = AllFalseBitArray;
            if (values.LongLength < Int32.MaxValue)
            {
                int notOptimizable = 0;
                int notWorthOptimizingLimit = 1 + (int)(values.Length * (true ? 0.8f : 0.6f));

                for (int i = 0; i < values.Length && notOptimizable < notWorthOptimizingLimit; i++)
                {
                    if (values[i] > HighestOptimizable16BitValue)
                    {
                        notOptimizable++;
                    }
                    else
                    {
                        if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray))
                        {
                            writeOptimizedFlags = new BitArray(values.Length);
                        }

                        writeOptimizedFlags[(int)i] = true;
                    }
                }

                if (notOptimizable == 0)
                {
                    writeOptimizedFlags = AllTrueBitArray;
                }
                else if (notOptimizable >= notWorthOptimizingLimit)
                {
                    writeOptimizedFlags = AllFalseBitArray;
                }
            }

            WriteArray(values, writeOptimizedFlags);
        }

        /// <summary>
        ///     Writes a UInt32[] into the stream using the fewest possible bytes.
        /// </summary>
        /// <param name="values"></param>
        private void WriteOptimizedArrayInternal(uint[] values)
        {
            BitArray writeOptimizedFlags = AllFalseBitArray;
            if (values.LongLength < Int32.MaxValue)
            {
                int notOptimizable = 0;
                int notWorthOptimizingLimit = 1 + (int)(values.Length * (true ? 0.8f : 0.6f));

                for (int i = 0; i < values.Length && notOptimizable < notWorthOptimizingLimit; i++)
                {
                    if (values[i] > HighestOptimizable32BitValue)
                    {
                        notOptimizable++;
                    }
                    else
                    {
                        if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray))
                        {
                            writeOptimizedFlags = new BitArray(values.Length);
                        }

                        writeOptimizedFlags[(int)i] = true;
                    }
                }

                if (notOptimizable == 0)
                {
                    writeOptimizedFlags = AllTrueBitArray;
                }
                else if (notOptimizable >= notWorthOptimizingLimit)
                {
                    writeOptimizedFlags = AllFalseBitArray;
                }
            }

            WriteArray(values, writeOptimizedFlags);
        }

        /// <summary>
        ///     Writes a UInt64[] into the stream using the fewest possible bytes.
        /// </summary>
        /// <param name="values"></param>
        private void WriteOptimizedArrayInternal(ulong[] values)
        {
            BitArray writeOptimizedFlags = AllFalseBitArray;
            if (values.LongLength < Int32.MaxValue)
            {
                int notOptimizable = 0;
                int notWorthOptimizingLimit = 1 + (int)(values.Length * (true ? 0.8f : 0.6f));

                for (int i = 0; i < values.Length && notOptimizable < notWorthOptimizingLimit; i++)
                {
                    if (values[i] > HighestOptimizable64BitValue)
                    {
                        notOptimizable++;
                    }
                    else
                    {
                        if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray))
                        {
                            writeOptimizedFlags = new BitArray(values.Length);
                        }

                        writeOptimizedFlags[(int)i] = true;
                    }
                }

                if (notOptimizable == 0)
                {
                    writeOptimizedFlags = AllTrueBitArray;
                }
                else if (notOptimizable >= notWorthOptimizingLimit)
                {
                    writeOptimizedFlags = AllFalseBitArray;
                }
            }

            WriteArray(values, writeOptimizedFlags);
        }        

        /// <summary>
        ///     Writes a DateTime[] into the stream using the fewest possible bytes.        
        /// </summary>
        /// <param name="values"> The DateTime[] to store. </param>
        private void WriteOptimizedArrayInternal(DateTime[] values)
        {
            BitArray writeOptimizedFlags = AllFalseBitArray;
            if (values.LongLength < Int32.MaxValue)
            {
                int notOptimizable = 0;
                int notWorthOptimizingLimit = 1 + (int)(values.Length * (true ? 0.8f : 0.6f));

                for (int i = 0; (i < values.Length) && (notOptimizable < notWorthOptimizingLimit); i++)
                {
                    if (values[i].Ticks % TimeSpan.TicksPerMillisecond != 0)
                    {
                        notOptimizable++;
                    }
                    else
                    {
                        if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray))
                        {
                            writeOptimizedFlags = new BitArray(values.Length);
                        }

                        writeOptimizedFlags[(int)i] = true;
                    }
                }

                if (notOptimizable == 0)
                {
                    writeOptimizedFlags = AllTrueBitArray;
                }
                else if (notOptimizable >= notWorthOptimizingLimit)
                {
                    writeOptimizedFlags = AllFalseBitArray;
                }
            }

            WriteArray(values, writeOptimizedFlags);
        }

        /// <summary>
        ///     Writes a TimeSpan[] into the stream using the fewest possible bytes.        
        /// </summary>
        /// <param name="values"> The TimeSpan[] to store. </param>
        private void WriteOptimizedArrayInternal(TimeSpan[] values)
        {
            BitArray writeOptimizedFlags = AllFalseBitArray;
            if (values.LongLength < Int32.MaxValue)
            {
                int notOptimizable = 0;
                int notWorthOptimizingLimit = 1 + (int)(values.Length * (true ? 0.8f : 0.6f));

                for (int i = 0; i < values.Length && notOptimizable < notWorthOptimizingLimit; i++)
                {
                    if (values[i].Ticks % TimeSpan.TicksPerMillisecond != 0)
                    {
                        notOptimizable++;
                    }
                    else
                    {
                        if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray))
                        {
                            writeOptimizedFlags = new BitArray(values.Length);
                        }

                        writeOptimizedFlags[(int)i] = true;
                    }
                }

                if (notOptimizable == 0)
                {
                    writeOptimizedFlags = AllTrueBitArray;
                }
                else if (notOptimizable >= notWorthOptimizingLimit)
                {
                    writeOptimizedFlags = AllFalseBitArray;
                }
            }

            WriteArray(values, writeOptimizedFlags);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        private void WriteArrayInternal(Guid[] values)
        {            
            WriteOptimized(values.LongLength);

            foreach (Guid value in values)
            {
                Write(value);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="values"></param>
        private void WriteArrayInternal(bool[] values)
        {
            WriteOptimized(new BitArray(values));
        }

        /// <summary>
        /// /
        /// </summary>
        /// <param name="values"></param>
        private void WriteArrayInternal(sbyte[] values)
        {            
            WriteOptimized(values.LongLength);

            foreach (sbyte value in values)
            {
                _binaryWriter.Write(value);
            }
        }        

        /// <summary>
        ///     Encodes a TimeSpan into the fewest number of bytes.
        ///     Has been separated from the WriteOptimized(TimeSpan) method so that WriteOptimized(DateTime)
        ///     can also use this for .NET 2.0 DateTimeKind information.
        ///     By taking advantage of the fact that a DateTime's TimeOfDay portion will never use the IsNegative
        ///     and HasDays flags, we can use these 2 bits to store the DateTimeKind and, since DateTimeKind is
        ///     unlikely to be set without a Time, we need no additional bytes to support a .NET 2.0 DateTime.
        /// </summary>
        /// <param name="value"> The TimeSpan to store. </param>
        /// <param name="partOfDateTime"> True if the TimeSpan is the TimeOfDay from a DateTime; False if a real TimeSpan. </param>
        /// <param name="initialData"> The intial data for the BitVector32 - contains DateTimeKind or 0 </param>
        private void EncodeTimeSpan(TimeSpan value, bool partOfDateTime, int initialData)
        {
            var packedData = new BitVector32(initialData);
            int days;
            int hours = Math.Abs(value.Hours);
            int minutes = Math.Abs(value.Minutes);
            int seconds = Math.Abs(value.Seconds);
            int milliseconds = Math.Abs(value.Milliseconds);
            bool hasTime = hours != 0 || minutes != 0;
            int optionalBytes = 0;

            if (partOfDateTime)
            {
                days = 0;
            }
            else
            {
                days = Math.Abs(value.Days);
                packedData[IsNegativeSection] = value.Ticks < 0 ? 1 : 0;
                packedData[HasDaysSection] = days != 0 ? 1 : 0;
            }

            if (hasTime)
            {
                packedData[HasTimeSection] = 1;
                packedData[HoursSection] = hours;
                packedData[MinutesSection] = minutes;
            }

            if (seconds != 0)
            {
                packedData[HasSecondsSection] = 1;

                if (!hasTime && (milliseconds == 0))
                    // If only seconds are present then we can use the minutes slot to save a byte
                {
                    packedData[MinutesSection] = seconds;
                }
                else
                {
                    packedData[SecondsSection] = seconds;
                    optionalBytes++;
                }
            }

            if (milliseconds != 0)
            {
                packedData[HasMillisecondsSection] = 1;
                packedData[MillisecondsSection] = milliseconds;
                optionalBytes = 2;
            }

            int data = packedData.Data;
            _binaryWriter.Write((byte) data);
            _binaryWriter.Write((byte) (data >> 8)); // Always write minimum of two bytes

            if (optionalBytes > 0)
            {
                _binaryWriter.Write((byte) (data >> 16));
            }

            if (optionalBytes > 1)
            {
                _binaryWriter.Write((byte) (data >> 24));
            }

            if (days != 0)
            {
                _binaryWriter.Write7BitEncodedInt(days);                
            }
        }

        /// <summary>
        ///     Internal implementation to write a non, null DateTime[] using a BitArray to
        ///     determine which elements are optimizable.
        /// </summary>
        /// <param name="values"> The DateTime[] to store. </param>
        /// <param name="writeOptimizedFlags">
        ///     A BitArray indicating which of the elements which are optimizable; a reference to constant
        ///     FullyOptimizableValueArray if all the elements are optimizable; or null if none of the elements are optimizable.
        /// </param>
        private void WriteArray(DateTime[] values, BitArray writeOptimizedFlags)
        {
            WriteOptimizeFlags(writeOptimizedFlags);            
            WriteOptimized(values.LongLength);

            for (long i = 0; i < values.LongLength; i++)
            {
                if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray) ||
                    (!ReferenceEquals(writeOptimizedFlags, AllTrueBitArray) && !writeOptimizedFlags[(int)i]))
                {
                    Write(values[i]);
                }
                else
                {
                    DateTime valueDateTime = values[i];
                    if ((valueDateTime.Ticks%TimeSpan.TicksPerMillisecond) == 0)
                    {
                        WriteSerializedType(SerializedType.OptimizedDateTimeType);
                        WriteOptimized(valueDateTime);
                    }
                    else
                    {
                        WriteSerializedType(SerializedType.DateTimeType);
                        Write(valueDateTime);
                    }
                }
            }
        }

        /// <summary>
        ///     Internal implementation to write a non-null Int16[] using a BitArray to determine which elements are optimizable.
        /// </summary>
        /// <param name="values"> The Int16[] to store. </param>
        /// <param name="writeOptimizedFlags">
        ///     A BitArray indicating which of the elements which are optimizable; a reference to constant
        ///     FullyOptimizableValueArray if all the elements are optimizable; or null if none of the elements are optimizable.
        /// </param>
        private void WriteArray(short[] values, BitArray writeOptimizedFlags)
        {
            WriteOptimizeFlags(writeOptimizedFlags);            
            WriteOptimized(values.LongLength);

            for (long i = 0; i < values.LongLength; i++)
            {
                if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray) ||
                    (!ReferenceEquals(writeOptimizedFlags, AllTrueBitArray) && !writeOptimizedFlags[(int)i]))
                {
                    _binaryWriter.Write(values[i]);
                }
                else
                {
                    WriteOptimized(values[i]);
                }
            }
        }

        /// <summary>
        ///     Internal implementation to write a non-null Int32[] using a BitArray to determine which elements are optimizable.
        /// </summary>
        /// <param name="values"> The Int32[] to store. </param>
        /// <param name="writeOptimizedFlags">
        ///     A BitArray indicating which of the elements which are optimizable; a reference to constant
        ///     FullyOptimizableValueArray if all the elements are optimizable; or null if none of the elements are optimizable.
        /// </param>
        private void WriteArray(int[] values, BitArray writeOptimizedFlags)
        {
            WriteOptimizeFlags(writeOptimizedFlags);
            WriteOptimized(values.LongLength);

            for (long i = 0; i < values.LongLength; i++)
            {
                if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray) ||
                    (!ReferenceEquals(writeOptimizedFlags, AllTrueBitArray) && !writeOptimizedFlags[(int)i]))
                {
                    _binaryWriter.Write(values[i]);
                }
                else
                {
                    _binaryWriter.Write7BitEncodedInt(values[i]);                    
                }
            }
        }

        /// <summary>
        ///     Internal implementation to writes a non-null Int64[] using a BitArray to determine which elements are optimizable.
        /// </summary>
        /// <param name="values"> The Int64[] to store. </param>
        /// <param name="writeOptimizedFlags">
        ///     A BitArray indicating which of the elements which are optimizable; a reference to constant
        ///     FullyOptimizableValueArray if all the elements are optimizable; or null if none of the elements are optimizable.
        /// </param>
        private void WriteArray(long[] values, BitArray writeOptimizedFlags)
        {
            WriteOptimizeFlags(writeOptimizedFlags);
            WriteOptimized(values.LongLength);

            for (long i = 0; i < values.LongLength; i++)
            {
                if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray) ||
                    (!ReferenceEquals(writeOptimizedFlags, AllTrueBitArray) && !writeOptimizedFlags[(int)i]))
                {
                    _binaryWriter.Write(values[i]);
                }
                else
                {                    
                    WriteOptimized(values[i]);
                }
            }
        }

        /// <summary>
        ///     Internal implementation to write a non-null TimeSpan[] using a BitArray to determine which elements are
        ///     optimizable.
        /// </summary>
        /// <param name="values"> The TimeSpan[] to store. </param>
        /// <param name="writeOptimizedFlags">
        ///     A BitArray indicating which of the elements which are optimizable; a reference to constant
        ///     FullyOptimizableValueArray if all the elements are optimizable; or null if none of the elements are optimizable.
        /// </param>
        private void WriteArray(TimeSpan[] values, BitArray writeOptimizedFlags)
        {
            WriteOptimizeFlags(writeOptimizedFlags);            
            WriteOptimized(values.LongLength);

            for (long i = 0; i < values.LongLength; i++)
            {
                if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray) ||
                    (!ReferenceEquals(writeOptimizedFlags, AllTrueBitArray) && !writeOptimizedFlags[(int)i]))
                {
                    Write(values[i]);
                }
                else
                {
                    TimeSpan valueTimeSpan = values[i];
                    if ((valueTimeSpan.Ticks%TimeSpan.TicksPerMillisecond) == 0)
                    {
                        WriteSerializedType(SerializedType.OptimizedTimeSpanType);
                        WriteOptimized(valueTimeSpan);
                    }
                    else
                    {
                        WriteSerializedType(SerializedType.TimeSpanType);
                        Write(valueTimeSpan);
                    }
                }
            }
        }

        /// <summary>
        ///     Internal implementation to write a non-null UInt16[] using a BitArray to determine which elements are optimizable.
        /// </summary>
        /// <param name="values"> The UInt16[] to store. </param>
        /// <param name="writeOptimizedFlags">
        ///     A BitArray indicating which of the elements which are optimizable; a reference to constant
        ///     FullyOptimizableValueArray if all the elements are optimizable; or null if none of the elements are optimizable.
        /// </param>
        private void WriteArray(ushort[] values, BitArray writeOptimizedFlags)
        {
            WriteOptimizeFlags(writeOptimizedFlags);            
            WriteOptimized(values.LongLength);

            for (long i = 0; i < values.LongLength; i++)
            {
                if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray) ||
                    (!ReferenceEquals(writeOptimizedFlags, AllTrueBitArray) && !writeOptimizedFlags[(int)i]))
                {
                    _binaryWriter.Write(values[i]);
                }
                else
                {
                    WriteOptimized(values[i]);
                }
            }
        }

        /// <summary>
        ///     Internal implementation to write a non-null UInt32[] using a BitArray to determine which elements are optimizable.
        /// </summary>
        /// <param name="values"> The UInt32[] to store. </param>
        /// <param name="writeOptimizedFlags">
        ///     A BitArray indicating which of the elements which are optimizable; a reference to constant
        ///     FullyOptimizableValueArray if all the elements are optimizable; or null if none of the elements are optimizable.
        /// </param>
        private void WriteArray(uint[] values, BitArray writeOptimizedFlags)
        {
            WriteOptimizeFlags(writeOptimizedFlags);            
            WriteOptimized(values.LongLength);

            for (long i = 0; i < values.LongLength; i++)
            {
                if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray) ||
                    (!ReferenceEquals(writeOptimizedFlags, AllTrueBitArray) && !writeOptimizedFlags[(int)i]))
                {
                    _binaryWriter.Write(values[i]);
                }
                else
                {
                    WriteOptimized(values[i]);
                }
            }
        }        

        /// <summary>
        ///     Internal implementation to write a non-null UInt64[] using a BitArray to determine which elements are optimizable.
        /// </summary>
        /// <param name="values"> The UInt64[] to store. </param>
        /// <param name="writeOptimizedFlags">
        ///     A BitArray indicating which of the elements which are optimizable; a reference to constant
        ///     FullyOptimizableValueArray if all the elements are optimizable; or null if none of the elements are optimizable.
        /// </param>
        private void WriteArray(ulong[] values, BitArray writeOptimizedFlags)
        {
            WriteOptimizeFlags(writeOptimizedFlags);            
            WriteOptimized(values.LongLength);

            for (long i = 0; i < values.LongLength; i++)
            {
                if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray) ||
                    (!ReferenceEquals(writeOptimizedFlags, AllTrueBitArray) && !writeOptimizedFlags[(int)i]))
                {
                    _binaryWriter.Write(values[i]);
                }
                else
                {                    
                    WriteOptimized(values[i]);
                }
            }
        }

        /// <summary>
        ///     values is not null
        ///     Sequences of null values and sequences of DBNull.Values are stored with a flag and optimized count.
        ///     Other values are stored using WriteObject().
        ///     This routine is called by the Write(object[]), WriteOptimized(object[]) and Write(object[], object[])) methods.
        /// </summary>
        private void WriteOptimizedObjectArray(object?[] values)
        {            
            WriteOptimized(values.LongLength);
            long lastIndex = values.LongLength - 1;
            for (long i = 0; i < values.LongLength; i++)
            {
                object? value = values[i];
                if (i < lastIndex && (value is null ? values[i + 1] is null : value.Equals(values[i + 1])))
                {
                    long duplicates = 1;

                    if (value is null)
                    {
                        WriteSerializedType(SerializedType.NullSequenceType);

                        for (i++; i < lastIndex && values[i + 1] is null; i++)
                        {
                            duplicates++;
                        }
                    }
                    else if (value == DBNull.Value)
                    {
                        WriteSerializedType(SerializedType.DbNullSequenceType);

                        for (i++; i < lastIndex && values[i + 1] == DBNull.Value; i++)
                        {
                            duplicates++;
                        }
                    }
                    else
                    {
                        WriteSerializedType(SerializedType.DuplicateValueSequenceType);

                        for (i++; i < lastIndex && value.Equals(values[i + 1]); i++)
                        {
                            duplicates++;
                        }

                        WriteObject(value);
                    }

                    WriteOptimized(duplicates);
                }
                else
                {
                    WriteObject(value);
                }
            }
        }

        /// <summary>
        ///     Stores the specified SerializedType code into the stream.
        ///     By using a centralized method, it is possible to collect statistics for the
        ///     type of data being stored in DEBUG mode.
        ///     Use the DumpTypeUsage() method to show a list of used SerializedTypes and
        ///     the number of times each has been used. This method and the collection code
        ///     will be optimized out when compiling in Release mode.
        /// </summary>
        /// <param name="typeCode"> The SerializedType to store. </param>
        private void WriteSerializedType(SerializedType typeCode)
        {
            _binaryWriter.Write((byte) typeCode);
        }

        /// <summary>
        ///     Internal implementation to write a non-null array into the stream.
        ///     value is not null, elementType is not null
        /// </summary>
        private void WriteArrayInternal(Array values, Type elementType)
        {
            if (values.Length == 0)
            {
                if (elementType == typeof (object))
                {
                    WriteSerializedType(SerializedType.EmptyObjectArrayType);
                }
                else
                {
                    WriteSerializedType(SerializedType.EmptyTypedArrayType);
                    WriteOptimized(elementType);
                }
                return;
            }

            if (elementType == typeof (object))
            {
                WriteSerializedType(SerializedType.ObjectArrayType);
                WriteOptimizedObjectArray((object?[]) values);
                return;
            }

            if (IsOwnedDataSerializableAndRecreatable(elementType))
            {
                bool allObjectsSameType = true;
                foreach (object? v in values)
                {
                    if (v is null || v.GetType() != elementType)
                    {
                        allObjectsSameType = false;
                        break;
                    }
                }
                if (allObjectsSameType)
                {
                    WriteSerializedType(SerializedType.OwnedDataSerializableTypedArrayType);                    
                    WriteOptimized(values.LongLength);
                    for (long i = 0; i < values.LongLength; i++)
                    {
                        var ownedDataSerializable = values.GetValue(i) as IOwnedDataSerializable;
                        if (ownedDataSerializable is null) throw new InvalidOperationException();
                        ownedDataSerializable.SerializeOwnedData(this, null);                        
                    }
                }
                else
                {
                    WriteSerializedType(SerializedType.TypedArrayType);
                    WriteOptimizedObjectArray((object[])values);
                }
                return;
            }

            if (elementType == typeof (string))
            {
                WriteSerializedType(SerializedType.StringArrayType);
                WriteOptimizedObjectArray((object[]) values);
                return;
            }

            if (elementType == typeof (Int16))
            {
                WriteSerializedType(SerializedType.Int16ArrayType);
                WriteOptimizedArrayInternal((Int16[]) values);
                return;
            }

            if (elementType == typeof (Int32))
            {
                WriteSerializedType(SerializedType.Int32ArrayType);
                WriteOptimizedArrayInternal((Int32[]) values);
                return;
            }

            if (elementType == typeof (Int64))
            {
                WriteSerializedType(SerializedType.Int64ArrayType);
                WriteOptimizedArrayInternal((Int64[]) values);
                return;
            }

            if (elementType == typeof (UInt16))
            {
                WriteSerializedType(SerializedType.UInt16ArrayType);
                WriteOptimizedArrayInternal((UInt16[]) values);
                return;
            }

            if (elementType == typeof (UInt32))
            {
                WriteSerializedType(SerializedType.UInt32ArrayType);
                WriteOptimizedArrayInternal((UInt32[]) values);
                return;
            }

            if (elementType == typeof (UInt64))
            {
                WriteSerializedType(SerializedType.UInt64ArrayType);
                WriteOptimizedArrayInternal((UInt64[]) values);
                return;
            }

            if (elementType == typeof (Single))
            {
                WriteSerializedType(SerializedType.SingleArrayType);
                WriteArrayOfSingle((Single[]) values);
                return;
            }

            if (elementType == typeof (Double))
            {
                WriteSerializedType(SerializedType.DoubleArrayType);
                WriteArrayOfDouble((Double[]) values);
                return;
            }

            if (elementType == typeof (Decimal))
            {
                WriteSerializedType(SerializedType.DecimalArrayType);
                WriteArrayOfDecimal((Decimal[]) values);
                return;
            }

            if (elementType == typeof (DateTime))
            {
                WriteSerializedType(SerializedType.DateTimeArrayType);
                WriteOptimizedArrayInternal((DateTime[]) values);
                return;
            }

            if (elementType == typeof (TimeSpan))
            {
                WriteSerializedType(SerializedType.TimeSpanArrayType);
                WriteOptimizedArrayInternal((TimeSpan[]) values);
                return;
            }

            if (elementType == typeof (Guid))
            {
                WriteSerializedType(SerializedType.GuidArrayType);
                WriteArrayInternal((Guid[]) values);
                return;
            }

            if (elementType == typeof (Boolean))
            {
                WriteSerializedType(SerializedType.BooleanArrayType);
                WriteArrayInternal((bool[]) values);
                return;
            }

            if (elementType == typeof (SByte))
            {
                WriteSerializedType(SerializedType.SByteArrayType);
                WriteArrayInternal((SByte[]) values);
                return;
            }

            if (elementType == typeof (Byte))
            {
                WriteSerializedType(SerializedType.ByteArrayType);
                WriteArray((Byte[]) values);
                return;
            }

            if (elementType == typeof (Char))
            {
                WriteSerializedType(SerializedType.CharArrayType);
                WriteArray((Char[]) values);
                return;
            }

            WriteSerializedType(SerializedType.TypedArrayType);
            WriteOptimizedObjectArray((object[]) values);
        }

        /// <summary>
        ///     Writes the Optimize Flags.
        /// </summary>
        private void WriteOptimizeFlags(BitArray writeOptimizedFlags)
        {
            if (ReferenceEquals(writeOptimizedFlags, AllFalseBitArray))
            {
                WriteSerializedType(SerializedType.AllFalseOptimizeFlagsType);
            }
            else if (ReferenceEquals(writeOptimizedFlags, AllTrueBitArray))
            {
                WriteSerializedType(SerializedType.AllTrueOptimizeFlagsType);
            }
            else
            {
                WriteSerializedType(SerializedType.MiscOptimizeFlagsType);
                WriteOptimized(writeOptimizedFlags);
            }
        }

#endregion

#region private fields
        
        private static readonly BitArray AllFalseBitArray = new BitArray(0);
                
        private static readonly BitArray AllTrueBitArray = new BitArray(0);
        
        private readonly Stream _baseStream;

#if NET5_0_OR_GREATER
        private readonly BinaryWriter _binaryWriter;
#else
        private class BinaryWriterEx : BinaryWriter
        {
            public BinaryWriterEx(Stream output) : base(output)
            {
            }

            public BinaryWriterEx(Stream output, Encoding encoding) : base(output, encoding)
            {
            }

            public BinaryWriterEx(Stream output, Encoding encoding, bool leaveOpen) : base(output, encoding, leaveOpen)
            {
            }

            public new void Write7BitEncodedInt(int value)
            {
                uint uValue = (uint)value;

                // Write out an int 7 bits at a time. The high bit of the byte,
                // when on, tells reader to continue reading more bytes.
                //
                // Using the constants 0x7F and ~0x7F below offers smaller
                // codegen than using the constant 0x80.

                while (uValue > 0x7Fu)
                {
                    Write((byte)(uValue | ~0x7Fu));
                    uValue >>= 7;
                }

                Write((byte)uValue);
            }

            public void Write7BitEncodedInt64(long value)
            {
                ulong uValue = (ulong)value;

                // Write out an int 7 bits at a time. The high bit of the byte,
                // when on, tells reader to continue reading more bytes.
                //
                // Using the constants 0x7F and ~0x7F below offers smaller
                // codegen than using the constant 0x80.

                while (uValue > 0x7Fu)
                {
                    Write((byte)((uint)uValue | ~0x7Fu));
                    uValue >>= 7;
                }

                Write((byte)uValue);
            }
        }

        private readonly BinaryWriterEx _binaryWriter;

        #endif

        /// <summary>
        ///     (Position, IsShortBlock)
        /// </summary>
        private readonly Stack<(long, bool)> _blockBeginPositionsStack = new Stack<(long, bool)>();        

        private readonly long _stringsListPositionPlaceholder_Position;
        private Dictionary<string, int>? _stringsDictionary;
        private List<string>? _stringsList;

        #endregion

        private class Block : IDisposable
        {
            #region construction and destruction

            public Block(SerializationWriter serializationWriter, int version, bool isShortBlock = false)
            {
                _serializationWriter = serializationWriter;
                if (isShortBlock)
                    _serializationWriter.BeginShortBlock(version);
                else
                    _serializationWriter.BeginBlock(version);
            }

            public void Dispose()
            {
                _serializationWriter.EndBlock();
            }

            #endregion

            #region private fields

            private readonly SerializationWriter _serializationWriter;

            #endregion
        }
    }

    public class BlockSizeException : Exception
    {
        public BlockSizeException(long size = 0) : 
            base($"SortBlock too large. Current size is {size}.")
        {
        }
    }
}

///// <summary>
/////     Internal implementation to store a non-null UInt16[].
///// </summary>
///// <param name="values"> The UIn16[] to store. </param>
//private void WriteArray(ushort[] values)
//{
//    WriteOptimized(values.LongLength);

//    foreach (ushort value in values)
//    {
//        _binaryWriter.Write(value);
//    }
//}

///// <summary>
/////     Checks whether an optimization condition has been met and throw an exception if not.
/////     This method has been made conditional on THROW_IF_NOT_OPTIMIZABLE being set at compile time.
/////     By default, this isn't set but could be set explicitly if exceptions are required and
/////     the evaluation overhead is acceptable.
/////     If not set, then this method and all references to it are removed at compile time.
/////     Leave at the default for optimum usage.
///// </summary>
///// <param name="condition"> An expression evaluating to true if the optimization condition is met, false otherwise. </param>
///// <param name="message"> The message to include in the exception should the optimization condition not be met. </param>
//private static void CheckOptimizable(bool condition, string message)
//{
//    if (!condition)
//    {
//        throw new InvalidOperationException(message);
//    }
//}

///// <summary>
/////     Checks whether each element in an array is of the same type.
///// </summary>
///// <param name="values"> The array to check </param>
///// <param name="elementType"> The expected element type. </param>
///// <returns> </returns>
//private static bool ArrayElementsAreSameType(IEnumerable<object> values, Type elementType)
//{
//    foreach (object value in values)
//    {
//        if (value is not null && value.GetType() != elementType) return false;
//    }

//    return true;
//}