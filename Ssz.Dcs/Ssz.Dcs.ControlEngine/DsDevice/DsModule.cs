using Microsoft.Extensions.Logging;
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
    public class DsModule : IDisposable, IOwnedDataSerializable
    {
        public class SerializationContext
        {
            public static readonly int SerializationVersion = 1;

            public bool SaveState;
        }

        public class DeserializationContext
        {
            /// <summary>
            ///     Field is set by module.
            /// </summary>
            public int DeserializedVersion;

            public bool LoadState;

            /// <summary>
            ///     Field is set by module.
            /// </summary>
            public bool ChildDsBlocksGuidIsEqual;
        }

        //public enum ContextMode
        //{
        //    Config,
        //    State,
        //    Journal
        //}

        #region construction and destruction

        public DsModule(string name, DsDevice device)
        {
            Name = name;
            Device = device;            
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
                foreach (DsBlockBase childDsBlock in _childDsBlocks)
                {
                    childDsBlock.Dispose();
                }
                _childDsBlocks = DsBlockBase.DsBlocksEmptyArray;
                DsBlocksTempRuntimeData = DsBlocksTempRuntimeData.Empty;
            }

            Disposed = true;
        }        

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~DsModule()
        {
            Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>        
        public bool Disposed { get; private set; }

        #endregion

        #region public functions     

        public static readonly DsModule[] ModulesEmptyArray = new DsModule[0];

        public string Name { get; }

        /// <summary>
        ///     Defines ChildDsBlocks count.
        /// </summary>
        public Guid ChildDsBlocksGuid { get; private set; }

        /// <summary>
        ///     ChildDsBlocks ordered by compute order.
        /// </summary>
        public DsBlockBase[] ChildDsBlocks
        {
            get
            {
                return _childDsBlocks;
            }
            set
            {
                _childDsBlocks = value;                
                DsBlocksTempRuntimeData = new DsBlocksTempRuntimeData(_childDsBlocks, true);
                ChildDsBlocksGuid = Guid.NewGuid();
            }
        }

        public DsBlocksTempRuntimeData DsBlocksTempRuntimeData { get; private set; } = DsBlocksTempRuntimeData.Empty;

        public DsDevice Device { get; }

        /// <summary>
        ///     For saving state set context to SerializationContext { SaveState = true }
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {            
            using (writer.EnterBlock(SerializationContext.SerializationVersion))
            {
                writer.Write(ChildDsBlocksGuid);
                writer.Write(_childDsBlocks.Length);
                foreach (var childDsBlock in _childDsBlocks)
                {
                    writer.Write(childDsBlock.DsBlockType);
                    writer.Write(childDsBlock.TagName);    
                    using (writer.EnterBlock())
                    {
                        writer.WriteOwnedDataSerializable(childDsBlock, context);
                    }                    
                }
            }
        }

        /// <summary>
        ///     For loading state set context to DeserializationContext { LoadState = true }
        ///     Preconditions: if deserializes all data (config and state), module must be just created or clear.
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            var deserializationContext = context as DeserializationContext;
            using (Block b = reader.EnterBlock())
            {                
                if (deserializationContext is not null)
                    deserializationContext.DeserializedVersion = b.Version;
                switch (b.Version)
                {
                    case 1:
                        var childDsBlocksGuid = reader.ReadGuid();
                        int childDsBlocksLength = reader.ReadInt32();                        
                        if (deserializationContext is not null && deserializationContext.LoadState)
                        {
                            deserializationContext.ChildDsBlocksGuidIsEqual = childDsBlocksGuid == ChildDsBlocksGuid;
                            if (!deserializationContext.ChildDsBlocksGuidIsEqual)
                            {
                                foreach (int index in Enumerable.Range(0, childDsBlocksLength))
                                {
                                    UInt16 blockType = reader.ReadUInt16();
                                    string tag = reader.ReadString();
                                    DsBlockBase? block = DsBlocksTempRuntimeData.ChildDsBlocksDictionary.TryGetValue(tag);
                                    using (Block b2 = reader.EnterBlock())
                                    {
                                        if (block is not null && block.DsBlockType == blockType)
                                        {
                                            reader.ReadOwnedDataSerializable(block, context);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                foreach (int index in Enumerable.Range(0, childDsBlocksLength))
                                {
                                    UInt16 blockType = reader.ReadUInt16();
                                    reader.SkipString();
                                    DsBlockBase block = _childDsBlocks[index];
                                    using (Block b2 = reader.EnterBlock())
                                    {
                                        if (block.DsBlockType == blockType)
                                        {
                                            reader.ReadOwnedDataSerializable(block, context);
                                        }                                        
                                    }
                                }
                            }                            
                        }
                        else
                        {                            
                            var childDsBlocks = new DsBlockBase[childDsBlocksLength];
                            foreach (int index in Enumerable.Range(0, childDsBlocksLength))
                            {
                                UInt16 blockType = reader.ReadUInt16();
                                string tag = reader.ReadString();                                
                                DsBlockBase block = DsBlocksFactory.CreateDsBlock(blockType, tag, this, null);
                                using (Block b2 = reader.EnterBlock())
                                {
                                    if (block.DsBlockType == blockType)
                                    {
                                        reader.ReadOwnedDataSerializable(block, context);
                                    }                                    
                                }                                    
                                childDsBlocks[index] = block;
                            }
                            ChildDsBlocks = childDsBlocks;
                            ChildDsBlocksGuid = childDsBlocksGuid;
                        }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }            
        }        

        public void Compute(uint dtMs)
        {
            foreach (var childDsBlock in _childDsBlocks)
            {
                if (childDsBlock.Disposed) continue;
                childDsBlock.Compute(dtMs);
            }
        }

        /// <summary>
        ///     connection must belong to this module.
        ///     Returns true if valid connection and all parameters set to valid values, except ParamValueIndex.
        /// </summary>
        /// <param name="connection"></param>
        /// <param name="searchInParentContainers"></param>
        /// <returns></returns>
        public bool PrepareConnection(RefDsConnection connection, bool searchInParentContainers)
        {
            if (connection.DsBlockIndexInModule == IndexConstants.DsBlockIndexInModule_IncorrectDsBlockFullTagName) 
                return false;

            DsBlockBase? block;
            if (connection.DsBlockIndexInModule == IndexConstants.DsBlockIndexInModule_InitializationNeeded)
            {
                if (!searchInParentContainers && connection.ParentComponentDsBlock is not null)
                {
                    block = DsDeviceHelper.GetDsBlock(connection.DsBlockFullName, connection.ParentComponentDsBlock);
                }
                else
                {
                    block = DsDeviceHelper.GetDsBlock(connection.DsBlockFullName, connection.ParentModule, connection.ParentComponentDsBlock);
                }                
                if (block is null)
                {
                    connection.DsBlockIndexInModule = IndexConstants.DsBlockIndexInModule_IncorrectDsBlockFullTagName;
                    return false;
                }
                connection.DsBlockIndexInModule = block.DsBlockIndexInModule;
            }
            else
            {
                block = DsBlocksTempRuntimeData.DescendantDsBlocks[connection.DsBlockIndexInModule];
            }            

            if (connection.ParamIndex is null)
            {
                if (block.DsBlockType != connection.DsBlockType ||
                    block.ParamInfosVersion != connection.DsBlockParamInfosVersion)
                {
                    foreach (int index in Enumerable.Range(0, block.MajorConstParamInfos.Length))
                    {
                        if (block.MajorConstParamInfos[index].Name == connection.ParamName)
                        {
                            connection.ParamInfoType = 0;
                            connection.ParamInfoIndex = (byte)index;
                            connection.ParamIndex = index;
                            break;
                        }
                    }
                    if (connection.ParamIndex is null)
                    {
                        foreach (int index in Enumerable.Range(0, block.ConstParamInfos.Length))
                        {
                            if (block.ConstParamInfos[index].Name == connection.ParamName)
                            {
                                connection.ParamInfoType = 1;
                                connection.ParamInfoIndex = (byte)index;
                                connection.ParamIndex = block.MajorConstParamInfos.Length + index;
                                break;
                            }
                        }
                        if (connection.ParamIndex is null)
                        {
                            foreach (int index in Enumerable.Range(0, block.MajorParamInfos.Length))
                            {
                                if (block.MajorParamInfos[index].Name == connection.ParamName)
                                {
                                    connection.ParamInfoType = 2;
                                    connection.ParamInfoIndex = (byte)index;
                                    connection.ParamIndex = block.MajorConstParamInfos.Length + block.ConstParamInfos.Length + index;
                                    break;
                                }
                            }
                            if (connection.ParamIndex is null)
                            {
                                foreach (int index in Enumerable.Range(0, block.ParamInfos.Length))
                                {
                                    if (block.ParamInfos[index].Name == connection.ParamName)
                                    {
                                        connection.ParamInfoType = 3;
                                        connection.ParamInfoIndex = (byte)index;
                                        connection.ParamIndex = block.MajorConstParamInfos.Length + block.ConstParamInfos.Length + block.MajorParamInfos.Length + index;
                                        break;
                                    }
                                }
                                if (connection.ParamIndex is null)
                                {
                                    connection.ParamIndex = IndexConstants.ParamIndex_ParamDoesNotExist;
                                }
                            }
                        }
                    }
                }
                else
                {
                    switch (connection.ParamInfoType)
                    {
                        case 0:
                            connection.ParamIndex = connection.ParamInfoIndex;
                            break;
                        case 1:
                            connection.ParamIndex = block.MajorConstParamInfos.Length + connection.ParamInfoIndex;
                            break;
                        case 2:
                            connection.ParamIndex = block.MajorConstParamInfos.Length + block.ConstParamInfos.Length + connection.ParamInfoIndex;
                            break;
                        case 3:
                            connection.ParamIndex = block.MajorConstParamInfos.Length + block.ConstParamInfos.Length + block.MajorParamInfos.Length + connection.ParamInfoIndex;
                            break;
                        default:
                            connection.ParamIndex = IndexConstants.ParamIndex_ParamDoesNotExist;
                            break;
                    }
                }
            }
            int paramIndex = connection.ParamIndex.Value;
            if (paramIndex == IndexConstants.ParamIndex_ParamDoesNotExist)
            {
                connection.DsBlockIndexInModule = IndexConstants.DsBlockIndexInModule_IncorrectDsBlockFullTagName;
                return false;
            }
            return true;
        }

        /// <summary>
        ///     connection must belong to this module.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public Any GetParamValue(RefDsConnection connection)
        {
            if (!PrepareConnection(connection, true))
                return new Any();
            
            var block = DsBlocksTempRuntimeData.DescendantDsBlocks[connection.DsBlockIndexInModule];
            ref var param = ref block.Params[connection.ParamIndex!.Value];
            if (param.Values is not null)
            {
                if (connection.ParamValueIndex < param.Values.Length)
                    return param.Values[connection.ParamValueIndex];
                else
                    return new Any();
            }
            else
            {
                return param.Value;
            }
        }

        /// <summary>    
        ///     connection must belong to this module.
        ///     Returns Status Code (see Ssz.Utils.JobStatusCodes)
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public ResultInfo SetParamValue(RefDsConnection connection, Any value)
        {
            if (!PrepareConnection(connection, true))
                return new ResultInfo { StatusCode = JobStatusCodes.FailedPrecondition };

            var block = DsBlocksTempRuntimeData.DescendantDsBlocks[connection.DsBlockIndexInModule];
            ref var param = ref block.Params[connection.ParamIndex!.Value];
            
            if (param.Values is not null)
            {
                if (connection.ParamValueIndex < param.Values.Length)
                {
                    param.Values[connection.ParamValueIndex] = value;
                }                    
                else
                {
                    // _logger
                }
            }
            else
            {
                param.Value = value;
            }
            if (connection.IsRefToMajorParam())
                block.OnMajorParamsChanged();

            return ResultInfo.OkResultInfo;
        }

        #endregion        

        #region private fields        

        private DsBlockBase[] _childDsBlocks = DsBlockBase.DsBlocksEmptyArray;

        #endregion
    }
}
