using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;

namespace Ssz.Utils.Serialization
{
    /// <summary>
    ///     A SerializationReader instance is used to read stored values and objects from a byte array.
    ///     Once an instance is created, use the various methods to read the required data.
    ///     The data read MUST be exactly the same type and in the same order as it was written.
    ///     Disposing not disposes underlying stream.
    /// </summary>
    public class SerializationReader : IDisposable
    {
        #region construction and destruction

        /// <summary>
        ///     Creates a SerializationReader using a byte[] previous created by SerializationWriter
        ///     A MemoryStream is used to access the data without making a copy of it.
        /// </summary>
        /// <param name="data"> The byte[] containining serialized data. </param>
        public SerializationReader(byte[] data) : this(new MemoryStream(data))
        {
        }

        /// <summary>
        ///     Creates a SerializationReader around the specified stream.
        ///     baseStream.CanSeek must be true.
        /// </summary>
        /// <param name="baseStream"></param>
        public SerializationReader(Stream baseStream)
        {
            if (!baseStream.CanSeek) throw new ArgumentException("baseStream must be seekable.");

            _baseStream = baseStream;
            _binaryReader = new BinaryReaderEx(_baseStream);

            // Always read the first 4 bytes
            int version = _binaryReader.ReadInt32();

            switch (version)
            {
                case 0: // Obsolete version
                case 2: // Obsolete version
                case 4: // Obsolete version
                case 6:
                    _optimizedSize = false;
                    break;
                case 1: // Obsolete version
                case 3: // Obsolete version
                case 5: // Obsolete version
                case 7:
                    _optimizedSize = true;
                    long stringsListInfoPosition = _baseStream.Position;
                    long stringsListPositionDelta = _binaryReader.ReadInt64();
                    if (stringsListPositionDelta > 0)
                    {
                        long streamCurrentPosition = _baseStream.Position;
                        _baseStream.Seek(stringsListInfoPosition + stringsListPositionDelta, SeekOrigin.Begin);
                        int count = _binaryReader.ReadInt32();
                        _stringsList = new List<string>(count);
                        for (int i = 0; i < count; i++)
                        {
                            _stringsList.Add(_binaryReader.ReadString());
                        }
                        _streamEndPosition = _baseStream.Position;
                        _baseStream.Seek(streamCurrentPosition, SeekOrigin.Begin);
                    }
                    break;
                default:
                    _optimizedSize = false;
                    _binaryReader.ReadInt32();
                    _binaryReader.ReadInt32();
                    break;

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
                if (_stringsList is not null)
                {
                    _baseStream.Seek(_streamEndPosition, SeekOrigin.Begin);
                }
            }

            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~SerializationReader()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public bool Disposed { get; private set; }

        public Stream BaseStream { get { return _baseStream; } }

        public long SkippedBytesCount
        {
            get { return _skippedBytesCount; }
        }

        /// <summary>
        ///     Use only with memory streams.
        ///     Throws Exception, if not block beginning.
        /// </summary>
        public int GetBlockVersionWithoutChangingStreamPosition()
        {
            long originalPosition = _baseStream.Position;
            SerializedType typeCode = ReadSerializedType();
            if (typeCode == SerializedType.BlockBeginWithVersion)
            {
                // it will store either the block length or remain as 0 if allowUpdateHeader is false
                _binaryReader.ReadInt32();
                int version = ReadInt32OptimizedOrNot();
                _baseStream.Seek(originalPosition, SeekOrigin.Begin);
                return version;
            }
            if (typeCode == SerializedType.BlockBegin)
            {
                // it will store either the block length or remain as 0 if allowUpdateHeader is false                
                _baseStream.Seek(originalPosition, SeekOrigin.Begin);
                return -1;
            }
            throw new Exception("Stream error.");
        }

        /// <summary>
        ///     Use only with memory streams.
        ///     Throws Exception, if not block beginning.
        /// </summary>
        /// <returns>Returns block Version.</returns>
        public int BeginBlock()
        {
            SerializedType typeCode = ReadSerializedType();
            if (typeCode == SerializedType.BlockBeginWithVersion)
            {
                // it will store either the block length or remain as 0 if allowUpdateHeader is false
                int blockSize = _binaryReader.ReadInt32();
                // Store the ending position of the block if allowUpdateHeader true
                if (blockSize > 0) _blockEndingPositionsStack.Push(_baseStream.Position + blockSize);
                else _blockEndingPositionsStack.Push(0);
                int version = ReadInt32OptimizedOrNot();
                return version;
            }
            if (typeCode == SerializedType.BlockBegin)
            {
                // it will store either the block length or remain as 0 if allowUpdateHeader is false
                int blockSize = _binaryReader.ReadInt32();
                // Store the ending position of the block if allowUpdateHeader true
                if (blockSize > 0) _blockEndingPositionsStack.Push(_baseStream.Position + blockSize);
                else _blockEndingPositionsStack.Push(0);
                return -1;
            }
            throw new Exception("Stream error.");
        }

        /// <summary>
        ///     Use only with memory streams.
        /// </summary>
        public void EndBlock()
        {
            _blockEndingPositionsStack.Pop();
            SerializedType typeCode = ReadSerializedType();
            if (typeCode != SerializedType.BlockEnd) throw new Exception("Stream block has more data than expected");
        }

        /// <summary>
        ///     Use only with memory streams.
        /// </summary>
        public void ReadToBlockEnding(bool incrementSkippedBytesCount = true)
        {
            long blockEndingPosition = _blockEndingPositionsStack.Peek();
            if (blockEndingPosition > _baseStream.Position)
            {
                if (incrementSkippedBytesCount) _skippedBytesCount += blockEndingPosition - _baseStream.Position;
                _baseStream.Position = _blockEndingPositionsStack.Peek();
            }
        }

        public bool IsBlockEnding()
        {
            if (_blockEndingPositionsStack.Count == 0) 
                return false;
            long blockEndingPosition = _blockEndingPositionsStack.Peek();
            if (blockEndingPosition == 0) 
                return false;
            return _baseStream.Position >= blockEndingPosition;
        }

        /// <summary>
        ///     Use only with memory streams.
        /// </summary>
        /// <returns></returns>
        public Block EnterBlock()
        {
            return new Block(this);
        }

        public bool ReadBoolean()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadBoolean();
        }

        public sbyte ReadSByte()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadSByte();
        }

        public byte ReadByte()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadByte();
        }

        public char ReadChar()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadChar();
        }

        public short ReadInt16()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadInt16();
        }

        public ushort ReadUInt16()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadUInt16();
        }

        public int ReadInt32()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return ReadInt32OptimizedOrNot();
        }

        public uint ReadUInt32()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return ReadUInt32OptimizedOrNot();
        }

        public long ReadInt64()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadInt64();
        }

        public ulong ReadUInt64()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadUInt64();
        }        

        /// <summary>
        ///     Use Write(float value) for writing.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="BlockEndingException"></exception>
        public float ReadSingle()
        {
            if (IsBlockEnding()) 
                throw new BlockEndingException();

            return _binaryReader.ReadSingle();
        }

        /// <summary>
        ///     Use Write(double value) for writing.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="BlockEndingException"></exception>
        public double ReadDouble()
        {
            if (IsBlockEnding()) 
                throw new BlockEndingException();

            return _binaryReader.ReadDouble();
        }

        public decimal ReadDecimal()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadDecimal();
        }

        /// <summary>
        ///     Use Write(DateTimeOffset value) for writing.
        /// </summary>
        /// <returns> A DateTimeOffset value. </returns>
        public DateTimeOffset ReadDateTimeOffset()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            var ticks = _binaryReader.ReadInt64();
            var offsetTicks = _binaryReader.ReadInt64();
            return new DateTimeOffset(ticks, new TimeSpan(offsetTicks));
        }

        /// <summary>
        ///     Use Write(DateTime value) for writing.
        /// </summary>
        /// <returns> A DateTime value. </returns>
        public DateTime ReadDateTime()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return DateTime.FromBinary(_binaryReader.ReadInt64());
        }

        /// <summary>
        ///     Use Write(TimeSpan value) for writing.
        /// </summary>
        /// <returns> A TimeSpan value. </returns>
        public TimeSpan ReadTimeSpan()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return new TimeSpan(_binaryReader.ReadInt64());
        }

        /// <summary>
        ///     Returns a Guid value from the stream.
        /// </summary>
        /// <returns> A DateTime value. </returns>
        public Guid ReadGuid()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return new Guid(_binaryReader.ReadBytes(16));
        }

        /// <summary>
        ///     Use Write(string value) for writing.
        /// </summary>
        /// <returns> A string value. </returns>
        public string ReadString()
        {
            return ReadOptimizedOrNotString()!;
        }

        /// <summary>
        ///     Use WriteNullableString(string? value) for writing.
        /// </summary>
        /// <returns> A string value. </returns>
        public string? ReadNullableString()
        {
            return ReadOptimizedOrNotString();
        }

        /// <summary>
        ///     Returns an Int16 value from the stream that was stored optimized.
        /// </summary>
        /// <returns> An Int16 value. </returns>
        public short ReadOptimizedInt16()
        {
            return (short)_binaryReader.Read7BitEncodedInt();
        }

        /// <summary>
        ///     Returns a UInt16 value from the stream that was stored optimized.
        /// </summary>
        /// <returns> A UInt16 value. </returns>
        public ushort ReadOptimizedUInt16()
        {
            return (ushort)_binaryReader.Read7BitEncodedInt();
        }

        /// <summary>
        ///     Returns an Int32 value from the stream that was stored optimized.
        /// </summary>
        /// <returns> An Int32 value. </returns>
        public int ReadOptimizedInt32()
        {
            return _binaryReader.Read7BitEncodedInt();
        }

        /// <summary>
        ///     Returns a UInt32 value from the stream that was stored optimized.
        /// </summary>
        /// <returns> A UInt32 value. </returns>
        public uint ReadOptimizedUInt32()
        {
            return unchecked((uint)_binaryReader.Read7BitEncodedInt());
        }

        /// <summary>
        ///     Returns an Int64 value from the stream that was stored optimized.
        /// </summary>
        /// <returns> An Int64 value. </returns>
        public long ReadOptimizedInt64()
        {
            long result = 0;
            int bitShift = 0;

            while (true)
            {
                byte nextByte = _binaryReader.ReadByte();

                result |= ((long)nextByte & 0x7f) << bitShift;
                bitShift += 7;

                if ((nextByte & 0x80) == 0) return result;
            }
        }

        /// <summary>
        ///     Returns a UInt64 value from the stream that was stored optimized.
        /// </summary>
        /// <returns> A UInt64 value. </returns>
        public ulong ReadOptimizedUInt64()
        {
            ulong result = 0;
            int bitShift = 0;

            while (true)
            {
                byte nextByte = _binaryReader.ReadByte();

                result |= ((ulong)nextByte & 0x7f) << bitShift;
                bitShift += 7;

                if ((nextByte & 0x80) == 0) return result;
            }
        }

        /// <summary>
        ///     Use WriteObject(...) or WriteObjectTyped(...) for write.
        ///     Returns an object based on the SerializedType read next from the stream.
        /// </summary>
        /// <returns> An object instance. </returns>
        public object? ReadObject()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return ReadObjectInternal((SerializedType)_binaryReader.ReadByte());
        }

        /// <summary>
        ///     Use WriteObject(...) or WriteObjectTyped(...) for write.
        ///     Throws if saved object not correct type;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? ReadObjectTyped<T>()
            where T : notnull
        {
            if (IsBlockEnding()) throw new BlockEndingException();
            
            return (T?)ReadObjectInternal((SerializedType)_binaryReader.ReadByte(), typeof(T));
        }

        /// <summary>
        ///     Use WriteOwnedDataSerializable(...) for writing.
        ///     Allows an existing object, implementing IOwnedDataSerializable, to
        ///     retrieve its owned data from the stream.
        /// </summary>
        /// <param name="target"> Any IOwnedDataSerializable object. </param>
        /// <param name="context"> An optional, arbitrary object to allow context to be provided. </param>
        public void ReadOwnedDataSerializable(IOwnedDataSerializable target, object? context)
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            target.DeserializeOwnedData(this, context);
        }

        /// <summary>
        ///     Use WriteOwnedDataSerializableAndRecreatable(...) for writing.
        ///     Throws if saved object not correct type;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? ReadOwnedDataSerializableAndRecreatable<T>(object? context)
            where T : class, IOwnedDataSerializable, new()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            var serializedType = (SerializedType)_binaryReader.ReadByte();
            if (serializedType == SerializedType.NullType) 
                return null;

            var t = new T();
            t.DeserializeOwnedData(this, context);
            return t;
        }

        /// <summary>
        ///     Use WriteOwnedDataSerializableAndRecreatable(...) for writing.
        ///     Throws if saved object not correct type;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? ReadOwnedDataSerializableAndRecreatable<T>(Func<T> func, object? context)
            where T : class, IOwnedDataSerializable
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            var serializedType = (SerializedType)_binaryReader.ReadByte();
            if (serializedType == SerializedType.NullType)
                return null;

            var t = func();
            t.DeserializeOwnedData(this, context);
            return t;
        }

        /// <summary>
        ///     Use WriteNullable<T>() for reading.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? ReadNullable<T>()
            where T : struct
        {
            return (T?)ReadObject();
        }        

        ///// <summary>
        /////     Returns a System.Windows.Point value from the stream.
        ///// </summary>
        ///// <returns> A System.Windows.Point value. </returns>
        //public Point ReadPoint()
        //{
        //    if (IsBlockEnding()) throw new BlockEndingException();

        //    double x = _binaryReader.ReadDouble();
        //    double y = _binaryReader.ReadDouble();
        //    return new Point(x, y);
        //}

        ///// <summary>
        /////     Returns a System.Windows.Size value from the stream.
        ///// </summary>
        ///// <returns> A System.Windows.Size value. </returns>
        //public Size ReadSize()
        //{
        //    if (IsBlockEnding()) throw new BlockEndingException();

        //    double x = _binaryReader.ReadDouble();
        //    double y = _binaryReader.ReadDouble();
        //    return new Size(x, y);
        //}

        /// <summary>
        ///     Use WriteArray(...) for writing.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[]? ReadArray<T>()
        {
            if (IsBlockEnding()) 
                throw new BlockEndingException();

            SerializedType typeCode = ReadSerializedType();

            if (typeCode == SerializedType.NullType)
                return null;

            return (T[]?)ReadArrayInternal(typeCode, typeof(T));
        }

        /// <summary>
        ///     Use WriteArrayOfSingle(...) for writing.
        /// </summary>
        /// <returns> A Single[]. </returns>
        public float[] ReadArrayOfSingle()
        {
            var result = new float[ReadOptimizedInt32()];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ReadSingle();
            }

            return result;
        }

        /// <summary>
        ///     Use WriteArrayOfDouble(...) for writing.
        /// </summary>
        /// <returns> A Double[]. </returns>
        public double[] ReadArrayOfDouble()
        {
            var result = new double[ReadOptimizedInt32()];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ReadDouble();
            }

            return result;
        }

        /// <summary>
        ///     Use WriteArrayOfDecimal(...) for writing.
        /// </summary>
        /// <returns> A Decimal[]. </returns>
        private decimal[] ReadArrayOfDecimal()
        {
            var result = new decimal[ReadOptimizedInt32()];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ReadOptimizedDecimal();
            }

            return result;
        }        

        /// <summary>
        ///     Use WriteArrayOfOwnedDataSerializable(...) for writing.
        ///     Reads array of same objects.
        ///     func is constructor function.              
        /// </summary>        
        /// <param name="func"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public T[] ReadArrayOfOwnedDataSerializable<T>(Func<T> func,
            object? context)
            where T : IOwnedDataSerializable
        {
            if (IsBlockEnding())
                throw new BlockEndingException();

            int length = ReadInt32();
            var result = new T[length];
            for (int i = 0; i < length; i++)
            {
                var v = func();
                v.DeserializeOwnedData(this, context);
                result[i] = v;
            }
            return result;
        }

        /// <summary>
        ///     Use WriteNullableByteArray(...) for writing.
        /// </summary>
        /// <returns></returns>
        public byte[]? ReadNullableByteArray()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            SerializedType typeCode = ReadSerializedType();

            if (typeCode == SerializedType.NullType) 
                return null;

            return ReadByteArrayInternal();
        }

        /// <summary>
        ///     Use WriteList(...) for writing.
        ///     Returns a generic List populated with values from the stream or null.
        /// </summary>
        /// <typeparam name="T"> The list Type. </typeparam>
        /// <returns> A new generic List or null. </returns>
        public List<T>? ReadList<T>()
        {
            if (IsBlockEnding()) 
                throw new BlockEndingException();

            SerializedType typeCode = ReadSerializedType();

            if (typeCode == SerializedType.NullType) 
                return null;

            var arr = (IEnumerable?)ReadArrayInternal(typeCode, typeof(T));
            if (arr is null) 
                return new List<T>();
            else 
                return new List<T>(arr.OfType<T>());
        }

        /// <summary>
        ///     Use WriteListOfOwnedDataSerializable(...) for writing.
        ///     Reads list of same objects.
        ///     func is constructor function.              
        /// </summary>        
        /// <param name="func"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<T> ReadListOfOwnedDataSerializable<T>(Func<T> func,
            object? context)
            where T : IOwnedDataSerializable
        {
            if (IsBlockEnding())
                throw new BlockEndingException();

            int count = ReadInt32();
            var result = new List<T>(count);
            for (int i = 0; i < count; i++)
            {
                var v = func();
                v.DeserializeOwnedData(this, context);
                result.Add(v);
            }
            return result;
        }

        /// <summary>
        ///     Use WriteListOfStrings(...) for writing.
        /// </summary>
        /// <returns></returns>
        public List<string> ReadListOfStrings()
        {
            int count = ReadInt32();
            var result = new List<string>(count);
            for (int i = 0; i < count; i++)
            {                
                result.Add(ReadString());
            }
            return result;
        }

        /// <summary>
        ///     Use WriteDictionary(...) for writing.
        ///     Populates a pre-existing generic dictionary with keys and values from the stream.
        ///     This allows a generic dictionary to be created without using the default constructor.
        /// </summary>
        /// <typeparam name="TK"> The key Type. </typeparam>
        /// <typeparam name="TV"> The value Type. </typeparam>
        public Dictionary<TK, TV>? ReadDictionary<TK, TV>()
            where TK : notnull
        {
            if (IsBlockEnding()) 
                throw new BlockEndingException();

            SerializedType typeCode = ReadSerializedType();

            if (typeCode == SerializedType.NullType) 
                return null;

            var keys = (TK[]?)ReadArrayInternal(typeCode, typeof(TK));
            var values = (TV[]?)ReadArrayInternal(ReadSerializedType(), typeof(TV));

            if (keys is null || values is null) 
                throw new InvalidOperationException();

            Dictionary<TK, TV> dictionary = new();
            for (int i = 0; i < keys.Length; i++)
            {
                dictionary[keys[i]] = values[i];
            }
            return dictionary;
        }

        /// <summary>
        ///     Use WriteDictionaryOfOwnedDataSerializable(...) for writing.
        ///     Reads Dictionary of same objects.
        ///     func is constructor function.              
        /// </summary>        
        /// <param name="func"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public Dictionary<string, T> ReadDictionaryOfOwnedDataSerializable<T>(Func<T> func,
            object? context)
            where T : IOwnedDataSerializable
        {
            int count = ReadInt32();
            var result = new Dictionary<string, T>(count);
            for (int i = 0; i < count; i++)
            {
                string key = ReadString();
                var v = func();
                v.DeserializeOwnedData(this, context);
                result.Add(key, v);
            }
            return result;
        }

        /// <summary>
        ///     Returns a BitArray or null from the stream.
        /// </summary>
        /// <returns> A BitArray instance. </returns>
        public BitArray? ReadBitArray()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            if (ReadSerializedType() == SerializedType.NullType) return null;

            return ReadOptimizedBitArray();
        }

        public int PeekChar()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.PeekChar();
        }                                            

        public int ReadRawChars(char[] buffer, int index, int count)
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.Read(buffer, index, count);
        }

        public char[] ReadRawChars(int count)
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadChars(count);
        }

        public int ReadRawBytes(byte[] buffer, int index, int count)
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.Read(buffer, index, count);
        }

        public byte[] ReadRawBytes(int count)
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return _binaryReader.ReadBytes(count);
        }

        /// <summary>
        ///     Use Write(byte[] values) for writing.
        /// </summary>
        /// <returns></returns>
        public byte[] ReadByteArray()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return ReadByteArrayInternal();
        }

        public void SkipString()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            SerializedType typeCode = ReadSerializedType();

            switch (typeCode)
            {
                case SerializedType.NullType:
                    return;
                case SerializedType.EmptyStringType:
                    return;
                case SerializedType.SingleSpaceType:
                    return;
                case SerializedType.YStringType:
                    return;
                case SerializedType.NStringType:
                    return;
                case SerializedType.SingleCharStringType:
                    _binaryReader.ReadChar();
                    return;
                case SerializedType.StringDirect:                
                    // Length of the string in bytes, not chars
                    int stringLength = _binaryReader.Read7BitEncodedInt();
                    if (stringLength < 0)
                    {
                        throw new IOException();
                    }
                    if (stringLength == 0)
                    {
                        return;
                    }
                    _baseStream.Seek(stringLength, SeekOrigin.Current);
                    return;
                case SerializedType.OptimizedStringType:
                    ReadOptimizedInt32();
                    return;
                default:
                    throw new InvalidOperationException("Unrecognized TypeCode");
            }
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Returns a BitArray from the stream that was stored optimized.
        /// </summary>
        /// <returns> A BitArray instance. </returns>
        private BitArray ReadOptimizedBitArray()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            int length = ReadOptimizedInt32();
            if (length == 0) return new BitArray(0);

            return new BitArray(_binaryReader.ReadBytes((length + 7)/8)) {Length = length};
        }

        /// <summary>
        ///     Returns a DateTime value from the stream that was stored optimized.
        /// </summary>
        /// <returns> A DateTime value. </returns>
        private DateTime ReadOptimizedDateTime()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            // Read date information from first three bytes
            var dateMask = new BitVector32(_binaryReader.ReadByte() | (_binaryReader.ReadByte() << 8) | (_binaryReader.ReadByte() << 16));
            var result = new DateTime(
                dateMask[SerializationWriter.DateYearMask],
                dateMask[SerializationWriter.DateMonthMask],
                dateMask[SerializationWriter.DateDayMask]
                );

            if (dateMask[SerializationWriter.DateHasTimeOrKindMask] == 1)
            {
                byte initialByte = _binaryReader.ReadByte();
                var dateTimeKind = (DateTimeKind) (initialByte & 0x03);

                // Remove the IsNegative and HasDays flags which are never true for a DateTime
                initialByte &= 0xfc;
                if (dateTimeKind != DateTimeKind.Unspecified)
                {
                    result = DateTime.SpecifyKind(result, dateTimeKind);
                }

                if (initialByte == 0)
                {
                    // No need to call decodeTimeSpan if there is no time information
                    _binaryReader.ReadByte();
                }
                else
                {
                    result = result.Add(DecodeTimeSpan(initialByte));
                }
            }

            return result;
        }

        /// <summary>
        ///     Returns a Decimal value from the stream that was stored optimized.
        /// </summary>
        /// <returns> A Decimal value. </returns>
        private Decimal ReadOptimizedDecimal()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            byte flags = _binaryReader.ReadByte();
            int lo = 0;
            int mid = 0;
            int hi = 0;
            byte scale = 0;

            if ((flags & 0x02) != 0)
            {
                scale = _binaryReader.ReadByte();
            }

            if ((flags & 4) == 0)
            {
                lo = (flags & 32) != 0 ? ReadOptimizedInt32() : _binaryReader.ReadInt32();
            }

            if ((flags & 8) == 0)
            {
                mid = (flags & 64) != 0 ? ReadOptimizedInt32() : _binaryReader.ReadInt32();
            }

            if ((flags & 16) == 0)
            {
                hi = (flags & 128) != 0 ? ReadOptimizedInt32() : _binaryReader.ReadInt32();
            }

            return new decimal(lo, mid, hi, (flags & 0x01) != 0, scale);
        }

        /// <summary>
        ///     depends on _optimizedSize
        /// </summary>
        /// <returns></returns>
        private uint ReadUInt32OptimizedOrNot()
        {
            if (!_optimizedSize)
            {
                return _binaryReader.ReadUInt32();
            }
            else
            {
                return ReadOptimizedUInt32();
            }
        }

        /// <summary>
        ///     depends on _optimizedSize
        /// </summary>
        /// <returns></returns>
        private int ReadInt32OptimizedOrNot()
        {            
            if (!_optimizedSize)
            {
                return _binaryReader.ReadInt32();
            }
            else
            {
                return ReadOptimizedInt32();
            }
        }

        /// <summary>
        ///     depends on _optimizedSize
        /// </summary>
        /// <returns></returns>
        private double ReadDoubleOptimizedOrNot()
        {
            if (!_optimizedSize)
            {
                return _binaryReader.ReadDouble();
            }
            else
            {
                SerializedType typeCode = ReadSerializedType();
                switch (typeCode)
                {
                    case SerializedType.ZeroDoubleType:
                        return (Double)0;
                    case SerializedType.OneDoubleType:
                        return (Double)1;
                    case SerializedType.DoubleType:
                        return _binaryReader.ReadDouble();
                    default:
                        throw new InvalidOperationException("Unrecognized TypeCode");
                }
            }
        }

        /// <summary>        
        ///     Returns an object[] from the stream that was stored optimized.
        ///     The returned array will be typed according to the specified element type
        ///     and the resulting array can be cast to the expected type.
        ///     e.g.
        ///     string[] myStrings = (string[]) reader.ReadOptimizedObjectArray(typeof(string));
        ///     An exception will be thrown if any of the deserialized values cannot be
        ///     cast to the specified elementType.
        /// </summary>
        /// <param name="elementType"> The Type of the expected array elements. null will return a plain object[]. </param>
        /// <returns> An object[] instance. </returns>
        private object?[] ReadOptimizedObjectArray(Type elementType)
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            int length = ReadOptimizedInt32();
            var result =
                (object?[])
                    (elementType == typeof (object) ? new object?[length] : Array.CreateInstance(elementType, length));

            for (int i = 0; i < result.Length; i++)
            {
                var serializedType = (SerializedType) _binaryReader.ReadByte();

                switch (serializedType)
                {
                    case SerializedType.NullSequenceType:
                        i += ReadOptimizedInt32();
                        break;
                    case SerializedType.DuplicateValueSequenceType:
                        object? target = result[i] = ReadObject();
                        int duplicateValueCount = ReadOptimizedInt32();
                        while (duplicateValueCount-- > 0)
                        {
                            result[++i] = target;
                        }
                        break;
                    case SerializedType.DbNullSequenceType:
                        result[i] = DBNull.Value;
                        int duplicateDbNullCount = ReadOptimizedInt32();
                        while (duplicateDbNullCount-- > 0)
                        {
                            result[++i] = DBNull.Value;
                        }
                        break;
                    default:
                        result[i] = ReadObjectInternal(serializedType);
                        break;
                }
            }
            return result;
        }

        /// <summary>
        ///     Returns a string value from the stream that was stored optimized.
        /// </summary>
        /// <returns> A string value. </returns>
        private string? ReadOptimizedOrNotString()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            SerializedType typeCode = ReadSerializedType();

            switch (typeCode)
            {
                case SerializedType.NullType: // For compatibility
                    return null;
                case SerializedType.EmptyStringType:
                    return string.Empty;
                case SerializedType.SingleSpaceType:
                    return " ";
                case SerializedType.YStringType:
                    return "Y";
                case SerializedType.NStringType:
                    return "N";
                case SerializedType.SingleCharStringType:
                    return Char.ToString(ReadChar());
                case SerializedType.StringDirect:
                    return _binaryReader.ReadString();
                case SerializedType.OptimizedStringType:                    
                    if (_stringsList is null) throw new InvalidOperationException();
                    int index = ReadOptimizedInt32();
                    return _stringsList[index];
                default:
                    throw new InvalidOperationException("Unrecognized TypeCode");
            }
        }

        /// <summary>
        ///     Returns a TimeSpan value from the stream that was stored optimized.
        /// </summary>
        /// <returns> A TimeSpan value. </returns>
        private TimeSpan ReadOptimizedTimeSpan()
        {
            if (IsBlockEnding()) throw new BlockEndingException();

            return DecodeTimeSpan(ReadByte());
        }

        /// <summary>
        ///     Returns a Type from the stream.
        ///     Throws an exception if the Type cannot be found.
        /// </summary>
        /// <returns> A Type instance. </returns>
        private Type? ReadOptimizedType()
        {
            return GetType(ReadOptimizedOrNotString()!, true);
        }        

        private Type? GetType(string typeString, bool throwOnError)
        {
            string[] typeStringParts = typeString.Split(',');            
            return Type.GetType(typeStringParts[0], false) ?? 
                Type.GetType(typeStringParts[0] + "," + typeStringParts[1], throwOnError);   
        }

        /// <summary>
        ///     Returns a TimeSpan decoded from packed data.
        ///     This routine is called from ReadOptimizedDateTime() and ReadOptimizedTimeSpan().
        ///     <remarks>
        ///         This routine uses a parameter to allow ReadOptimizedDateTime() to 'peek' at the
        ///         next byte and extract the DateTimeKind from bits one and two (IsNegative and HasDays)
        ///         which are never set for a Time portion of a DateTime.
        ///     </remarks>
        /// </summary>
        /// <param name="initialByte"> The first of two always-present bytes. </param>
        /// <returns> A decoded TimeSpan </returns>
        private TimeSpan DecodeTimeSpan(byte initialByte)
        {
            var packedData = new BitVector32(initialByte | (ReadByte() << 8)); // Read first two bytes
            bool hasTime = packedData[SerializationWriter.HasTimeSection] == 1;
            bool hasSeconds = packedData[SerializationWriter.HasSecondsSection] == 1;
            bool hasMilliseconds = packedData[SerializationWriter.HasMillisecondsSection] == 1;
            long ticks = 0;

            if (hasMilliseconds)
            {
                packedData = new BitVector32(packedData.Data | (ReadByte() << 16) | (ReadByte() << 24));
            }
            else if (hasTime && hasSeconds)
            {
                packedData = new BitVector32(packedData.Data | (ReadByte() << 16));
            }

            if (hasTime)
            {
                ticks += packedData[SerializationWriter.HoursSection]*TimeSpan.TicksPerHour;
                ticks += packedData[SerializationWriter.MinutesSection]*TimeSpan.TicksPerMinute;
            }

            if (hasSeconds)
            {
                ticks += packedData[(!hasTime && !hasMilliseconds)
                    ? SerializationWriter.MinutesSection
                    : SerializationWriter.SecondsSection]*TimeSpan.TicksPerSecond;
            }

            if (hasMilliseconds)
            {
                ticks += packedData[SerializationWriter.MillisecondsSection]*TimeSpan.TicksPerMillisecond;
            }

            if (packedData[SerializationWriter.HasDaysSection] == 1)
            {
                ticks += ReadOptimizedInt32()*TimeSpan.TicksPerDay;
            }

            if (packedData[SerializationWriter.IsNegativeSection] == 1)
            {
                ticks = -ticks;
            }

            return new TimeSpan(ticks);
        }

        /// <summary>
        ///     Creates a BitArray representing which elements of a typed array
        ///     are serializable.
        /// </summary>
        /// <param name="serializedType"> The type of typed array. </param>
        /// <returns> A BitArray denoting which elements are serializable. </returns>
        private BitArray ReadTypedArrayOptimizeFlags(SerializedType serializedType)
        {
            switch (serializedType)
            {
                case SerializedType.AllFalseOptimizeFlagsType:
                    return AllFalseBitArray;
                case SerializedType.AllTrueOptimizeFlagsType:
                    return AllTrueBitArray;
                case SerializedType.MiscOptimizeFlagsType:
                    return ReadOptimizedBitArray();
                default:
                    throw new InvalidOperationException("Unexpected SerializedType: " + serializedType);
            }
        }

        /// <summary>
        ///     Returns an object based on supplied SerializedType.
        /// </summary>
        /// <param name="typeCode"></param>
        /// <param name="elementType"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private object? ReadObjectInternal(SerializedType typeCode, Type? objectType = null)
        {
            switch (typeCode)
            {
                case SerializedType.NullType:
                    return null;
                case SerializedType.Int32Type:
                    return ReadInt32();
                case SerializedType.EmptyStringType:
                    return string.Empty;
                case SerializedType.BooleanFalseType:
                    return false;
                case SerializedType.ZeroInt32Type:
                    return 0;
                case SerializedType.OptimizedInt32Type:
                    return ReadOptimizedInt32();
                case SerializedType.OptimizedInt32NegativeType:
                    return -ReadOptimizedInt32() - 1;
                case SerializedType.DecimalType:
                    return ReadOptimizedDecimal();
                case SerializedType.ZeroDecimalType:
                    return (Decimal) 0;
                case SerializedType.YStringType:
                    return "Y";
                case SerializedType.DateTimeType:
                    return ReadDateTime();
                case SerializedType.OptimizedDateTimeType:
                    return ReadOptimizedDateTime();
                case SerializedType.SingleCharStringType:
                    return Char.ToString(ReadChar());
                case SerializedType.SingleSpaceType:
                    return " ";
                case SerializedType.OneInt32Type:
                    return 1;
                case SerializedType.OptimizedInt16Type:
                    return ReadOptimizedInt16();
                case SerializedType.OptimizedInt16NegativeType:
                    return (short) (-ReadOptimizedInt16() - 1);
                case SerializedType.OneDecimalType:
                    return (Decimal) 1;
                case SerializedType.BooleanTrueType:
                    return true;
                case SerializedType.NStringType:
                    return "N";
                case SerializedType.DbNullType:
                    return DBNull.Value;
                case SerializedType.MinusOneInt32Type:
                    return -1;
                case SerializedType.MinusOneInt64Type:
                    return (Int64) (-1);
                case SerializedType.MinusOneInt16Type:
                    return (Int16) (-1);
                case SerializedType.MinDateTimeType:
                    return DateTime.MinValue;
                case SerializedType.GuidType:
                    return ReadGuid();
                case SerializedType.EmptyGuidType:
                    return Guid.Empty;
                case SerializedType.TimeSpanType:
                    return ReadTimeSpan();
                case SerializedType.MaxDateTimeType:
                    return DateTime.MaxValue;
                case SerializedType.ZeroTimeSpanType:
                    return TimeSpan.Zero;
                case SerializedType.OptimizedTimeSpanType:
                    return ReadOptimizedTimeSpan();
                case SerializedType.DoubleType:
                    return _binaryReader.ReadDouble();
                case SerializedType.ZeroDoubleType:
                    return (Double) 0;
                case SerializedType.Int64Type:
                    return ReadInt64();
                case SerializedType.ZeroInt64Type:
                    return (Int64) 0;
                case SerializedType.OptimizedInt64Type:
                    return ReadOptimizedInt64();
                case SerializedType.OptimizedInt64NegativeType:
                    return -ReadOptimizedInt64() - 1;
                case SerializedType.Int16Type:
                    return ReadInt16();
                case SerializedType.ZeroInt16Type:
                    return (Int16) 0;
                case SerializedType.SingleType:
                    return _binaryReader.ReadSingle();
                case SerializedType.ZeroSingleType:
                    return (Single) 0;
                case SerializedType.ByteType:
                    return ReadByte();
                case SerializedType.ZeroByteType:
                    return (Byte) 0;
                case SerializedType.OtherType:
                {
                    var type = ReadOptimizedType();
                    if (type is null) return null;
                    string s = ReadOptimizedOrNotString()!;
                    return JsonSerializer.Deserialize(s, type, new JsonSerializerOptions
                        {
                            NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals
                        });
                }
                case SerializedType.UInt16Type:
                    return ReadUInt16();
                case SerializedType.ZeroUInt16Type:
                    return (UInt16) 0;
                case SerializedType.UInt32Type:
                    return ReadUInt32();
                case SerializedType.ZeroUInt32Type:
                    return (UInt32) 0;
                case SerializedType.OptimizedUInt32Type:
                    return ReadOptimizedUInt32();
                case SerializedType.UInt64Type:
                    return ReadUInt64();
                case SerializedType.ZeroUInt64Type:
                    return (UInt64) 0;
                case SerializedType.OptimizedUInt64Type:
                    return ReadOptimizedUInt64();
                case SerializedType.CharType:
                    return ReadChar();
                case SerializedType.ZeroCharType:
                    return (Char) 0;
                case SerializedType.SByteType:
                    return ReadSByte();
                case SerializedType.ZeroSByteType:
                    return (SByte) 0;
                case SerializedType.OneByteType:
                    return (Byte) 1;
                case SerializedType.OneDoubleType:
                    return (Double) 1;
                case SerializedType.OneCharType:
                    return (Char) 1;
                case SerializedType.OneInt16Type:
                    return (Int16) 1;
                case SerializedType.OneInt64Type:
                    return (Int64) 1;
                case SerializedType.OneUInt16Type:
                    return (UInt16) 1;
                case SerializedType.OptimizedUInt16Type:
                    return ReadOptimizedUInt16();
                case SerializedType.OneUInt32Type:
                    return (UInt32) 1;
                case SerializedType.OneUInt64Type:
                    return (UInt64) 1;
                case SerializedType.OneSByteType:
                    return (SByte) 1;
                case SerializedType.OneSingleType:
                    return (Single) 1;
                case SerializedType.BitArrayType:
                    return ReadOptimizedBitArray();
                case SerializedType.TypeType:
                    return GetType(ReadOptimizedOrNotString()!, false);
                case SerializedType.ArrayListType:
                    return new ArrayList(ReadOptimizedObjectArray(typeof (object)));
                case SerializedType.OwnedDataSerializableType:
                {
                    var type = ReadOptimizedType();
                    if (type is null) throw new InvalidOperationException();
                    object? result = Activator.CreateInstance(type);
                    if (result is null) throw new InvalidOperationException();
                    ((IOwnedDataSerializable)result).DeserializeOwnedData(this, null);
                    return result;
                }
                case SerializedType.OptimizedEnumType:
                {
                    Type? enumType = ReadOptimizedType();
                    if (enumType is null) return null;
                    Type underlyingType = Enum.GetUnderlyingType(enumType);

                    if ((underlyingType == typeof (int)) || (underlyingType == typeof (uint)) ||
                        (underlyingType == typeof (long)) || (underlyingType == typeof (ulong)))
                    {
                        return Enum.ToObject(enumType, ReadOptimizedUInt64());
                    }

                    return Enum.ToObject(enumType, ReadUInt64());
                }
                case SerializedType.EnumType:
                {
                    Type? enumType = ReadOptimizedType();
                    if (enumType is null) return null;
                    Type underlyingType = Enum.GetUnderlyingType(enumType);

                    if (underlyingType == typeof (Int32)) return Enum.ToObject(enumType, ReadInt32());
                    if (underlyingType == typeof (Byte)) return Enum.ToObject(enumType, ReadByte());
                    if (underlyingType == typeof (Int16)) return Enum.ToObject(enumType, ReadInt16());
                    if (underlyingType == typeof (UInt32)) return Enum.ToObject(enumType, ReadUInt32());
                    if (underlyingType == typeof (Int64)) return Enum.ToObject(enumType, ReadInt64());
                    if (underlyingType == typeof (SByte)) return Enum.ToObject(enumType, ReadSByte());
                    if (underlyingType == typeof (UInt16)) return Enum.ToObject(enumType, ReadUInt16());

                    return Enum.ToObject(enumType, ReadUInt64());
                }
                case SerializedType.StringDirect:
                {
                    return _binaryReader.ReadString();
                }
                case SerializedType.OptimizedStringType:
                {
                    if (_stringsList is null) 
                            throw new InvalidOperationException();
                    int index = ReadOptimizedInt32();
                    return _stringsList[index];
                }
                default:
                {
                    object? result = ReadArrayInternal(typeCode, objectType?.GetElementType());
                    if (result is not null) 
                        return result;

                    throw new InvalidOperationException("Unrecognized TypeCode: " + typeCode);
                }
            }
        }

        /// <summary>
        ///     Returns an Int32[] from the stream.
        /// </summary>
        /// <returns> An Int32[] instance; or null. </returns>
        private int[]? ReadInt32Array()
        {
            SerializedType typeCode = ReadSerializedType();

            switch (typeCode)
            {
                case SerializedType.NullType:
                    return null;
                case SerializedType.EmptyTypedArrayType:
                    return new int[0];
                default:
                    BitArray readOptimizedFlags = ReadTypedArrayOptimizeFlags(typeCode);
                    var result = new int[ReadOptimizedInt32()];

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (ReferenceEquals(readOptimizedFlags, AllFalseBitArray) ||
                            (!ReferenceEquals(readOptimizedFlags, AllTrueBitArray) && !readOptimizedFlags[i]))
                        {
                            result[i] = _binaryReader.ReadInt32();
                        }
                        else
                        {
                            result[i] = ReadOptimizedInt32();
                        }
                    }

                    return result;
            }
        }

        /// <summary>
        ///     Returns an Int64[] from the stream.
        /// </summary>
        /// <returns> An Int64[] instance; or null. </returns>
        private long[]? ReadInt64Array()
        {
            SerializedType typeCode = ReadSerializedType();

            switch (typeCode)
            {
                case SerializedType.NullType:
                    return null;
                case SerializedType.EmptyTypedArrayType:
                    return new long[0];
                default:
                    BitArray readOptimizedFlags = ReadTypedArrayOptimizeFlags(typeCode);
                    var result = new long[ReadOptimizedInt64()];

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (ReferenceEquals(readOptimizedFlags, AllFalseBitArray) ||
                            (!ReferenceEquals(readOptimizedFlags, AllTrueBitArray) && !readOptimizedFlags[i]))
                        {
                            result[i] = _binaryReader.ReadInt64();
                        }
                        else
                        {
                            result[i] = ReadOptimizedInt64();
                        }
                    }

                    return result;
            }
        }

        /// <summary>
        ///     Returns a TimeSpan[] from the stream.
        /// </summary>
        /// <returns> A TimeSpan[] instance; or null. </returns>
        private TimeSpan[]? ReadTimeSpanArray()
        {
            SerializedType typeCode = ReadSerializedType();

            switch (typeCode)
            {
                case SerializedType.NullType:
                    return null;
                case SerializedType.EmptyTypedArrayType:
                    return new TimeSpan[0];
                default:
                    BitArray readOptimizedFlags = ReadTypedArrayOptimizeFlags(typeCode);
                    var result = new TimeSpan[ReadOptimizedInt32()];

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (ReferenceEquals(readOptimizedFlags, AllFalseBitArray) ||
                            (!ReferenceEquals(readOptimizedFlags, AllTrueBitArray) && !readOptimizedFlags[i]))
                        {
                            result[i] = ReadTimeSpan();
                        }
                        else
                        {
                            result[i] = ReadOptimizedTimeSpan();
                        }
                    }

                    return result;
            }
        }

        /// <summary>
        ///     Returns a UInt[] from the stream.
        /// </summary>
        /// <returns> A UInt[] instance; or null. </returns>
        private uint[]? ReadUInt32Array()
        {
            SerializedType typeCode = ReadSerializedType();

            switch (typeCode)
            {
                case SerializedType.NullType:
                    return null;
                case SerializedType.EmptyTypedArrayType:
                    return new uint[0];
                default:
                    BitArray readOptimizedFlags = ReadTypedArrayOptimizeFlags(typeCode);
                    var result = new uint[ReadOptimizedUInt32()];

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (ReferenceEquals(readOptimizedFlags, AllFalseBitArray) ||
                            (!ReferenceEquals(readOptimizedFlags, AllTrueBitArray) && !readOptimizedFlags[i]))
                        {
                            result[i] = _binaryReader.ReadUInt32();
                        }
                        else
                        {
                            result[i] = ReadOptimizedUInt32();
                        }
                    }

                    return result;
            }
        }

        /// <summary>
        ///     Returns a UInt64[] from the stream.
        /// </summary>
        /// <returns> A UInt64[] instance; or null. </returns>
        private ulong[]? ReadUInt64Array()
        {
            SerializedType typeCode = ReadSerializedType();

            switch (typeCode)
            {
                case SerializedType.NullType:
                    return null;
                case SerializedType.EmptyTypedArrayType:
                    return new ulong[0];
                default:
                    BitArray readOptimizedFlags = ReadTypedArrayOptimizeFlags(typeCode);
                    var result = new ulong[ReadOptimizedInt64()];

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (ReferenceEquals(readOptimizedFlags, AllFalseBitArray) ||
                            (!ReferenceEquals(readOptimizedFlags, AllTrueBitArray) && !readOptimizedFlags[i]))
                        {
                            result[i] = _binaryReader.ReadUInt64();
                        }
                        else
                        {
                            result[i] = ReadOptimizedUInt64();
                        }
                    }

                    return result;
            }
        }

        /// <summary>
        ///     Returns a DateTime[] from the stream.
        /// </summary>
        /// <returns> A DateTime[] instance; or null. </returns>
        private DateTime[]? ReadDateTimeArray()
        {
            SerializedType typeCode = ReadSerializedType();
            switch (typeCode)
            {
                case SerializedType.NullType:
                    return null;
                case SerializedType.EmptyTypedArrayType:
                    return new DateTime[0];
                default:
                    BitArray readOptimizedFlags = ReadTypedArrayOptimizeFlags(typeCode);
                    var result = new DateTime[ReadOptimizedInt32()];

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (ReferenceEquals(readOptimizedFlags, AllFalseBitArray) ||
                            (!ReferenceEquals(readOptimizedFlags, AllTrueBitArray) && !readOptimizedFlags[i]))
                        {
                            result[i] = ReadDateTime();
                        }
                        else
                        {
                            result[i] = ReadOptimizedDateTime();
                        }
                    }

                    return result;
            }
        }

        /// <summary>
        ///     Returns a UInt16[] from the stream.
        /// </summary>
        /// <returns> A UInt16[] instance; or null. </returns>
        private ushort[]? ReadUInt16Array()
        {
            SerializedType typeCode = ReadSerializedType();

            switch (typeCode)
            {
                case SerializedType.NullType:
                    return null;
                case SerializedType.EmptyTypedArrayType:
                    return new ushort[0];
                default:
                    BitArray readOptimizedFlags = ReadTypedArrayOptimizeFlags(typeCode);
                    var result = new ushort[ReadOptimizedUInt32()];

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (ReferenceEquals(readOptimizedFlags, AllFalseBitArray) ||
                            (!ReferenceEquals(readOptimizedFlags, AllTrueBitArray) && !readOptimizedFlags[i]))
                        {
                            result[i] = _binaryReader.ReadUInt16();
                        }
                        else
                        {
                            result[i] = ReadOptimizedUInt16();
                        }
                    }

                    return result;
            }
        }

        /// <summary>
        ///     Returns an Int16[] from the stream.
        /// </summary>
        /// <returns> An Int16[] instance; or null. </returns>
        private short[]? ReadInt16Array()
        {
            SerializedType t = ReadSerializedType();

            switch (t)
            {
                case SerializedType.NullType:
                    return null;
                case SerializedType.EmptyTypedArrayType:
                    return new short[0];
                default:
                    BitArray readOptimizedFlags = ReadTypedArrayOptimizeFlags(t);
                    var result = new short[ReadOptimizedInt32()];

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (ReferenceEquals(readOptimizedFlags, AllFalseBitArray) ||
                            (!ReferenceEquals(readOptimizedFlags, AllTrueBitArray) && !readOptimizedFlags[i]))
                        {
                            result[i] = _binaryReader.ReadInt16();
                        }
                        else
                        {
                            result[i] = ReadOptimizedInt16();
                        }
                    }

                    return result;
            }
        }

        /// <summary>
        ///     Determine whether the passed-in type code refers to an array type
        ///     and deserializes the array if it is.
        ///     elementType is not null
        ///     Returns null if not an array type.
        /// </summary>
        /// <param name="typeCode"> The SerializedType to check. </param>
        /// <param name="elementType"> The Type of array element; </param>
        /// <returns> </returns>
        private object? ReadArrayInternal(SerializedType typeCode, Type? elementType)
        {
            switch (typeCode)
            {
                case SerializedType.EmptyObjectArrayType:
                    return new object[0];
                case SerializedType.EmptyTypedArrayType:
                {
                    var deserializedElementType = ReadOptimizedType();
                    if (deserializedElementType is not null &&
                        elementType is not null &&
                        deserializedElementType != elementType)
                            throw new InvalidOperationException();
                    return Array.CreateInstance((deserializedElementType ?? elementType) ?? typeof(object), 0);
                }                    
                case SerializedType.ObjectArrayType:
                    return ReadOptimizedObjectArray(typeof (object));
                case SerializedType.AllTrueOptimizeFlagsType: // Obsolete
                case SerializedType.OwnedDataSerializableTypedArrayType:
                {
                    int length = ReadOptimizedInt32();
                    Array result = Array.CreateInstance(elementType!, length);
                    for (int i = 0; i < length; i++)
                    {
                        IOwnedDataSerializable? value = Activator.CreateInstance(elementType!) as IOwnedDataSerializable;
                        if (value is null) 
                            throw new InvalidOperationException();
                            ReadOwnedDataSerializable(value, null);
                        result.SetValue(value, i);
                    }
                    return result;
                }
                case SerializedType.StringArrayType:
                    return ReadOptimizedObjectArray(typeof (string));
                case SerializedType.Int16ArrayType:
                    return ReadInt16Array();
                case SerializedType.Int32ArrayType:
                    return ReadInt32Array();
                case SerializedType.Int64ArrayType:
                    return ReadInt64Array();
                case SerializedType.UInt16ArrayType:
                    return ReadUInt16Array();
                case SerializedType.UInt32ArrayType:
                    return ReadUInt32Array();
                case SerializedType.UInt64ArrayType:
                    return ReadUInt64Array();
                case SerializedType.SingleArrayType:
                    return ReadArrayOfSingle();
                case SerializedType.DoubleArrayType:
                    return ReadArrayOfDouble();
                case SerializedType.DecimalArrayType:
                    return ReadArrayOfDecimal();
                case SerializedType.DateTimeArrayType:
                    return ReadDateTimeArray();
                case SerializedType.TimeSpanArrayType:
                    return ReadTimeSpanArray();
                case SerializedType.GuidArrayType:
                    return ReadGuidArray();
                case SerializedType.BooleanArrayType:
                    return ReadBooleanArray();
                case SerializedType.SByteArrayType:
                    return ReadSByteArray();
                case SerializedType.ByteArrayType:
                    return ReadByteArrayInternal();
                case SerializedType.CharArrayType:
                    return ReadCharArray();
                case SerializedType.TypedArrayType:
                    return ReadOptimizedObjectArray(elementType ?? typeof(object));
            }

            return null;
        }

        /// <summary>
        ///     Returns the SerializedType read next from the stream.
        /// </summary>
        /// <returns> A SerializedType value. </returns>
        private SerializedType ReadSerializedType()
        {
            return (SerializedType) ReadByte();
        }

        /// <summary>
        ///     Internal implementation returning a Bool[].
        /// </summary>
        /// <returns> A Bool[]. </returns>
        private bool[] ReadBooleanArray()
        {
            BitArray bitArray = ReadOptimizedBitArray();
            var result = new bool[bitArray.Count];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = bitArray[i];
            }

            return result;
        }

        /// <summary>
        ///     Internal implementation returning a Byte[].
        /// </summary>
        /// <returns> A Byte[]. </returns>
        private byte[] ReadByteArrayInternal()
        {
            return _binaryReader.ReadBytes(ReadOptimizedInt32());
        }

        /// <summary>
        ///     Internal implementation returning a Char[].
        /// </summary>
        /// <returns> A Char[]. </returns>
        private char[] ReadCharArray()
        {
            return _binaryReader.ReadChars(ReadOptimizedInt32());
        }        

        /// <summary>
        ///     Internal implementation returning a Guid[].
        /// </summary>
        /// <returns> A Guid[]. </returns>
        private Guid[] ReadGuidArray()
        {
            var result = new Guid[ReadOptimizedInt32()];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ReadGuid();
            }

            return result;
        }

        /// <summary>
        ///     Internal implementation returning an SByte[].
        /// </summary>
        /// <returns> An SByte[]. </returns>
        private sbyte[] ReadSByteArray()
        {
            var result = new sbyte[ReadOptimizedInt32()];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = ReadSByte();
            }

            return result;
        }        

        #endregion

        #region private fields

        private static readonly BitArray AllFalseBitArray = new BitArray(0);

        private static readonly BitArray AllTrueBitArray = new BitArray(0);
                
        private readonly Stream _baseStream;

        private readonly bool _optimizedSize;
       
        private readonly BinaryReaderEx _binaryReader;
        
        private readonly Stack<long> _blockEndingPositionsStack = new Stack<long>();

        private long _skippedBytesCount;

        private readonly long _streamEndPosition;

        private List<string>? _stringsList;

        #endregion

        private class BinaryReaderEx : BinaryReader
        {
            public BinaryReaderEx(Stream input) : base(input)
            {
            }

            public BinaryReaderEx(Stream input, Encoding encoding) : base(input, encoding)
            {
            }

            public BinaryReaderEx(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
            {
            }

            public new int Read7BitEncodedInt()
            {
                return base.Read7BitEncodedInt();
            }
        }
    }

    /// <summary>
    ///     Reads to block ending when Disposing.
    /// </summary>
    public class Block : IDisposable
    {
        #region construction and destruction

        public Block(SerializationReader serializationReader)
        {
            _serializationReader = serializationReader;
            Version = _serializationReader.BeginBlock();
        }

        /// <summary>
        ///     Reads to block ending.
        /// </summary>
        public void Dispose()
        {
            _serializationReader.ReadToBlockEnding();
            _serializationReader.EndBlock();
        }

        #endregion

        #region public functions

        public readonly int Version;

        #endregion

        #region private fields

        private readonly SerializationReader _serializationReader;

        #endregion
    }

    public class BlockEndingException : Exception
    {
    }

    public class BlockUnsupportedVersionException : Exception
    {
    }
}

//public int Read()
//{
//    if (IsBlockEnding()) throw new BlockEndingException();

//    return _binaryReader.Read();
//}

//if (_baseStream.CanSeek)
//{
//    _baseStream.Seek(stringLength, SeekOrigin.Current);
//}
//else
//{
//    var mCharBytes = new byte[maxCharBytesSize];

//    do
//    {
//        long readLength = ((stringLength - currPos) > maxCharBytesSize)
//            ? maxCharBytesSize
//            : (stringLength - currPos);

//        long n = _baseStream.Read(mCharBytes, 0, (int)readLength);

//        if (n == 0)
//        {
//            throw new IOException();
//        }

//        if (currPos == 0 && n == stringLength)
//            return;

//        currPos += n;
//    } while (currPos < stringLength);
//}