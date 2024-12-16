using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    /// <summary>
    ///     Dcs DsBlock base class.
    /// </summary>
    public abstract partial class DsBlockBase : IOwnedDataSerializable, IDisposable
    {
        #region construction and destruction

        protected DsBlockBase(string blockTypeString, UInt16 blockType, string tagName, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock)
        {
            DsBlockTypeString = blockTypeString;
            DsBlockType = blockType;
            TagName = tagName;
            _parentModule = parentModule;
            _parentComponentDsBlock = parentComponentDsBlock;

            Params = new DsParam[ParamInfos.Length];
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
            if (Disposed) return;

            if (disposing)
            {
                foreach (int index in Enumerable.Range(0, Params.Length))
                {
                    Params[index].Dispose();
                }
            }

            _parentModule = null!;
            _parentComponentDsBlock = null;

            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~DsBlockBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>        
        public bool Disposed { get; private set; }

        #endregion

        #region public functions

        public static readonly DsBlockBase[] DsBlocksEmptyArray = new DsBlockBase[0];

        public static readonly DsBlockBase EmptyDsBlock = new EmptyDsBlock();

        public string DsBlockTypeString { get; }

        public UInt16 DsBlockType { get; }

        public string TagName { get; }

        /// <summary>
        ///     Data store for using by graphic tool.
        /// </summary>
        public string ShapeData { get; set; } = @"";        

        public DsModule ParentModule => _parentModule;

        /// <summary>
        ///     Index in module's descendant blocks array.
        ///     Is set by DsBlocksCache.
        /// </summary>
        public UInt16 DsBlockIndexInModule { get; internal set; } = IndexConstants.DsBlockIndexInModule_InitializationNeeded;

        public ComponentDsBlock? ParentComponentDsBlock => _parentComponentDsBlock;        

        public abstract DsParamInfo[] ParamInfos { get; }

        /// <summary>
        ///     If ParamInfo is added at the end of ParamInfos, there is no need to change this number. 
        /// </summary>
        public abstract UInt16 ParamInfosVersion { get; }

        public DsParam[] Params { get; }

        /// <summary>
        ///     Returns ParamIndex_ParamDoesNotExist or valid param index and valid paramArrayIndex. 
        ///     paramFullName is ParamName[ParamValueIndex]
        ///     paramFullName must be Upper-Case
        /// </summary>
        /// <param name="paramFullName"></param>
        /// <param name="paramValueIndex"></param>
        /// <returns></returns>
        public int GetParamIndex(string paramFullName, out byte paramValueIndex, out bool isMajor)
        {            
            paramValueIndex = IndexConstants.ParamValueIndex_IsNotArray;
            isMajor = false;
            string paramName = paramFullName;
            if (paramFullName.EndsWith(']'))
            {
                var index = paramFullName.IndexOf('[');
                if (index > 0 && index < paramFullName.Length - 2)
                {
                    paramName = paramFullName.Substring(0, index);
                    paramValueIndex = (byte)(new Any(paramFullName.Substring(index + 1, paramFullName.Length - index - 2)).ValueAsInt32(false) - 1);
                }
            }            
            
            foreach (int index in Enumerable.Range(0, ParamInfos.Length))
            {
                ref var paramInfo = ref ParamInfos[index];
                if (paramInfo.Name == paramName)
                {
                    int paramIndex = index;
                    if (paramInfo.IsArray) 
                    {
                        if (paramValueIndex == IndexConstants.ParamValueIndex_IsNotArray) 
                            return IndexConstants.ParamIndex_ParamDoesNotExist;
                    }
                    else
                    {
                        if (paramValueIndex != IndexConstants.ParamValueIndex_IsNotArray) 
                            return IndexConstants.ParamIndex_ParamDoesNotExist;
                    }
                    if (paramInfo.IsMajor)
                        isMajor = true;
                    return paramIndex;
                }
            }            
            return IndexConstants.ParamIndex_ParamDoesNotExist;
        }

        /// <summary>
        ///     For saving state set context to SerializationContext { SaveState = true }
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="context"></param>
        public virtual void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            var serializationContext = context as DsModule.SerializationContext;

            writer.Write(ParamInfosVersion);
            if (serializationContext is not null && serializationContext.SaveState)
            {
                writer.Write((byte)ParamInfos.Count(pi => !pi.IsConst));
                foreach (int index in Enumerable.Range(0, ParamInfos.Length))
                {
                    ref var paramInfo = ref ParamInfos[index];
                    if (!paramInfo.IsConst)                        
                        ParamSerializeOwnedData(writer, true,
                            ref paramInfo, 
                            ref Params[index]);                    
                }
            }
            else
            {
                writer.Write((byte)ParamInfos.Length);
                foreach (int index in Enumerable.Range(0, ParamInfos.Length))
                {
                    ref var paramInfo = ref ParamInfos[index];
                    ParamSerializeOwnedData(writer, false,
                        ref paramInfo,
                        ref Params[index]);
                }
                
                writer.Write(ShapeData);                
            }           
        }

        /// <summary>
        ///     For loading state set context to DeserializationContext { LoadState = true }
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="context"></param>
        public virtual void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            var deserializationContext = context as DsModule.DeserializationContext;

            UInt16 paramInfosVersion = reader.ReadUInt16();
            if (paramInfosVersion != ParamInfosVersion)
            {
                DeserializeOwnedDataObsolete(reader, context, DsBlockType, paramInfosVersion);
                return;
            }
            if (deserializationContext is not null && deserializationContext.LoadState)
            {
                bool majorParamsChanged = false;

                byte length = reader.ReadByte();

                foreach (int index in Enumerable.Range(0, length))
                {
                    ref var paramInfo = ref ParamInfos[index];
                    if (!paramInfo.IsConst && paramInfo.IsMajor)
                    {
                        majorParamsChanged = true;
                        ParamDeserializeOwnedData(reader, true,
                            ref paramInfo,
                            ref Params[index]);
                    }
                }

                if (majorParamsChanged) 
                    OnMajorParamsChanged();
                
                foreach (int index in Enumerable.Range(0, length))
                {
                    ref var paramInfo = ref ParamInfos[index];
                    if (!paramInfo.IsConst && !paramInfo.IsMajor)
                    {
                        ParamDeserializeOwnedData(reader, true,
                            ref paramInfo,
                            ref Params[index]);
                    }                        
                }
            }
            else
            {
                bool majorParamsChanged = false;

                byte length = reader.ReadByte();

                foreach (int index in Enumerable.Range(0, length))
                {
                    ref var paramInfo = ref ParamInfos[index];
                    if (paramInfo.IsMajor)
                    {
                        majorParamsChanged = true;
                        ParamDeserializeOwnedData(reader, true,
                            ref paramInfo,
                            ref Params[index]);
                    }
                }

                if (majorParamsChanged)
                    OnMajorParamsChanged();

                foreach (int index in Enumerable.Range(0, length))
                {
                    ref var paramInfo = ref ParamInfos[index];
                    if (!paramInfo.IsMajor)
                    {
                        ParamDeserializeOwnedData(reader, true,
                            ref paramInfo,
                            ref Params[index]);
                    }
                }

                ShapeData = reader.ReadString();
            }
        }

        public virtual void OnMajorParamsChanged()
        {
        }

        public virtual void Compute(uint dtMs)
        {
            bool majorParamsChanged = false;

            foreach (int index in Enumerable.Range(0, ParamInfos.Length))
            {
                ref var paramInfo = ref ParamInfos[index];
                if (paramInfo.IsMajor)
                {
                    if (ParamCompute(ref paramInfo,
                            ref Params[index]))
                        majorParamsChanged = true;
                }                    
            }            

            if (majorParamsChanged) 
                OnMajorParamsChanged();

            foreach (int index in Enumerable.Range(0, ParamInfos.Length))
            {
                ref var paramInfo = ref ParamInfos[index];
                if (!paramInfo.IsMajor)
                {
                    ParamCompute(ref paramInfo,
                        ref Params[index]);
                }
            }            
        }        

        #endregion

        #region protected functions

        protected void ParamSerializeOwnedData(SerializationWriter writer, bool saveState, ref DsParamInfo paramInfo, ref DsParam param)
        {
            if (saveState)
            {
                if (paramInfo.IsArray)
                {
                    byte valuesLength = (byte)param.Values.Length;
                    writer.Write(valuesLength);
                    foreach (int index in Enumerable.Range(0, valuesLength))
                    {
                        param.Values[index].SerializeOwnedData(writer, null);
                    }
                }
                else
                {
                    param.Value.SerializeOwnedData(writer, null);
                }
            }
            else
            {
                if (paramInfo.IsArray)
                {
                    byte valuesLength = (byte)param.Values.Length;
                    writer.Write(valuesLength);
                    foreach (int index in Enumerable.Range(0, valuesLength))
                    {
                        param.Values[index].SerializeOwnedData(writer, null);
                        var connection = param.Connections[index];
                        if (connection is null)
                        {
                            writer.Write((byte)0);
                        }                            
                        else
                        {
                            writer.Write((byte)connection.ConnectionType);
                            connection.SerializeOwnedData(writer, null);
                        }                            
                    }
                }
                else
                {
                    param.Value.SerializeOwnedData(writer, null);
                    var connection = param.Connection;
                    if (connection is null)
                    {
                        writer.Write((byte)0);
                    }
                    else
                    {
                        writer.Write((byte)connection.ConnectionType);
                        connection.SerializeOwnedData(writer, null);
                    }
                }
            }
        }

        protected void ParamDeserializeOwnedData(SerializationReader reader, bool loadState, ref DsParamInfo paramInfo, ref DsParam param)
        {
            if (loadState)
            {
                if (paramInfo.IsArray)
                {
                    byte valuesLength = reader.ReadByte();
                    foreach (int index in Enumerable.Range(0, valuesLength))
                    {
                        if (index < param.Values.Length)
                        {
                            param.Values[index].DeserializeOwnedData(reader, null);
                        }
                        else
                        {
                            new Any().DeserializeOwnedData(reader, null);
                        }
                    }
                }
                else
                {
                    param.Value.DeserializeOwnedData(reader, null);
                }
            }
            else
            {
                if (paramInfo.IsArray)
                {
                    byte valuesLength = reader.ReadByte();
                    foreach (int index in Enumerable.Range(0, valuesLength))
                    {
                        if (index < param.Values.Length)
                        {
                            param.Values[index].DeserializeOwnedData(reader, null);
                            byte connectionType = reader.ReadByte();
                            DsConnectionBase? connection;
                            if (connectionType == 0)
                            {
                                connection = null;
                            }
                            else
                            {
                                connection = DsConnectionsFactory.CreateConnection(connectionType, ParentModule, ParentComponentDsBlock);
                                connection!.DeserializeOwnedData(reader, null);
                            }
                            param.Connections[index] = connection;                            
                        }
                        else
                        {
                            new Any().DeserializeOwnedData(reader, null);
                            byte connectionType = reader.ReadByte();                            
                            if (connectionType == 0)
                            {
                            }
                            else
                            {
                                var connection = DsConnectionsFactory.CreateConnection(connectionType, ParentModule, ParentComponentDsBlock);
                                connection!.DeserializeOwnedData(reader, null);
                            }
                        }
                    }
                }
                else
                {
                    param.Value.DeserializeOwnedData(reader, null);
                    byte connectionType = reader.ReadByte();
                    DsConnectionBase? connection;
                    if (connectionType == 0)
                    {
                        connection = null;
                    }
                    else
                    {
                        connection = DsConnectionsFactory.CreateConnection(connectionType, ParentModule, ParentComponentDsBlock);
                        connection!.DeserializeOwnedData(reader, null);
                    }
                    param.Connection = connection;
                }
            }
        }

        /// <summary>
        ///     Returns true, if value changed.
        /// </summary>
        /// <param name="paramInfo"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        protected bool ParamCompute(ref DsParamInfo paramInfo, ref DsParam param)
        {
            bool valueChanged = false;

            if (paramInfo.IsArray)
            {
                var connections = param.Connections;
                foreach (int index in Enumerable.Range(0, connections.Length))
                {
                    var connection = connections[index];
                    if (connection is not null)
                    {
                        param.Values[index] = connection.GetValue();
                        valueChanged = true;
                    }
                }
            }
            else
            {
                var connection = param.Connection;
                if (connection is not null)
                {
                    param.Value = connection.GetValue();
                    valueChanged = true;
                }
            }

            return valueChanged;
        }

        #endregion        

        #region private fields

        private DsModule _parentModule;

        private ComponentDsBlock? _parentComponentDsBlock;

        #endregion
    }
}
