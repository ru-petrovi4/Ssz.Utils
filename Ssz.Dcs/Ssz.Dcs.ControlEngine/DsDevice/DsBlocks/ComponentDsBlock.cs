using Ssz.Utils;
using Ssz.Utils.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ssz.Dcs.ControlEngine
{
    public class ComponentDsBlock : DsBlockBase
    {
        #region construction and destruction

        static ComponentDsBlock()
        {            
            _paramInfos = new[]
            {
                new DsParamInfo { Name = @"DESC", Desc = @"DsBlock Description", IsConst = true },
            };
            _paramInfosVersion = 1;
        }

        public ComponentDsBlock(string blockTypeString, UInt16 blockType, string tag, DsModule parentModule, ComponentDsBlock? parentComponentDsBlock) :
            base(blockTypeString, blockType, tag, parentModule, parentComponentDsBlock)
        {            
        }

        /// <summary>
        ///     This method is invoked when the IDisposable.Dispose or Finalize actions are
        ///     requested.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                foreach (DsBlockBase childDsBlock in _childDsBlocks)
                {
                    childDsBlock.Dispose();
                }
                _childDsBlocks = DsBlocksEmptyArray;
                DsBlocksTempRuntimeData = DsBlocksTempRuntimeData.Empty;
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions        

        public override DsParamInfo[] ParamInfos => _paramInfos;

        public override UInt16 ParamInfosVersion => _paramInfosVersion;

        public bool IsTemplate { get; set; }

        /// <summary>
        ///     Tag of template Component DsBlock
        /// </summary>
        public string CreatedFromTemplate { get; set; } = @"";

        public ref DsParam DESC => ref Params[0];

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
                DsBlocksTempRuntimeData = new DsBlocksTempRuntimeData(_childDsBlocks, false);
            }
        }

        public DsBlocksTempRuntimeData DsBlocksTempRuntimeData { get; private set; } = DsBlocksTempRuntimeData.Empty;

        public DsParamAlias[] ParamAliases { get; set; } = DsParamAlias.ParamAliasesEmptyArray;

        public DsParamAlias[] JournalParamAliases { get; set; } = DsParamAlias.ParamAliasesEmptyArray;

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            base.SerializeOwnedData(writer, context);

            var serializationContext = context as DsModule.SerializationContext;

            if (serializationContext is not null && serializationContext.SaveState)
            {
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
            else
            {
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

                writer.Write(ParamAliases.Length);
                foreach (int index in Enumerable.Range(0, ParamAliases.Length))
                {
                    ParamAliases[index].SerializeOwnedData(writer, null);                    
                }

                writer.Write(JournalParamAliases.Length);
                foreach (int index in Enumerable.Range(0, JournalParamAliases.Length))
                {
                    JournalParamAliases[index].SerializeOwnedData(writer, null);
                }

                writer.Write(IsTemplate);
                writer.Write(CreatedFromTemplate);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedData(reader, context);

            var deserializationContext = context as DsModule.DeserializationContext;

            int childDsBlocksLength = reader.ReadInt32();
            if (deserializationContext is not null && deserializationContext.LoadState)
            {
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
                    DsBlockBase block = DsBlocksFactory.CreateDsBlock(blockType, tag, ParentModule, this);
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
            
                int paramsAliasesLength = reader.ReadInt32();
                ParamAliases = new DsParamAlias[paramsAliasesLength];
                foreach (int index in Enumerable.Range(0, paramsAliasesLength))
                {
                    ref var paramAlias = ref ParamAliases[index];
                    paramAlias.Connection = DsConnectionsFactory.CreateRefConnection(ParentModule, this);
                    paramAlias.DeserializeOwnedData(reader, null);
                }

                int journalParamsAliasesLength = reader.ReadInt32();
                JournalParamAliases = new DsParamAlias[journalParamsAliasesLength];
                foreach (int index in Enumerable.Range(0, journalParamsAliasesLength))
                {                    
                    ref var paramAlias = ref JournalParamAliases[index];
                    paramAlias.Connection = DsConnectionsFactory.CreateRefConnection(ParentModule, this);
                    paramAlias.DeserializeOwnedData(reader, null);
                }

                IsTemplate = reader.ReadBoolean();
                CreatedFromTemplate = reader.ReadString();
            }                
        }

        public override void Compute(uint dtMs)
        {
            if (IsTemplate) return;

            base.Compute(dtMs);

            foreach (var childDsBlock in _childDsBlocks)
            {
                childDsBlock.Compute(dtMs);
            }            
        }

        #endregion

        #region private fields        

        private static readonly DsParamInfo[] _paramInfos;

        private static readonly UInt16 _paramInfosVersion;

        private DsBlockBase[] _childDsBlocks = DsBlocksEmptyArray;        

        #endregion
    }
}
