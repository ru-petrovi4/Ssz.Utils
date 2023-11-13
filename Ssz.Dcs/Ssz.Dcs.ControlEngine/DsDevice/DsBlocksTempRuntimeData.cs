using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public class DsBlocksTempRuntimeData
    {
        #region construction and destruction

        private DsBlocksTempRuntimeData()
        {
        }

        /// <summary>
        ///     Logs with Error priority,
        /// </summary>
        /// <param name="childDsBlocks"></param>
        /// <param name="logger"></param>
        public DsBlocksTempRuntimeData(DsBlockBase[] childDsBlocks, bool isModuleTempRuntimeData, ILogger? logger = null)
        {
            if (!isModuleTempRuntimeData) 
            {
                // Component DsBlock Cache
                if (logger is not null)
                {
                    foreach (var childDsBlock in childDsBlocks)
                    {
                        try
                        {
                            ChildDsBlocksDictionary.Add(childDsBlock.TagName, childDsBlock);                            
                            DescendantDsBlocks.Add(childDsBlock);
                            if (childDsBlock is ComponentDsBlock componentDsBlock)
                            {                                
                                DescendantDsBlocks.AddRange(componentDsBlock.DsBlocksTempRuntimeData.DescendantDsBlocks);
                            }
                        }
                        catch
                        {
                            logger.LogError("Duplicate block Tag in Component DsBlock.");
                        }
                    }
                }
                else
                {
                    foreach (var childDsBlock in childDsBlocks)
                    {
                        ChildDsBlocksDictionary[childDsBlock.TagName] = childDsBlock;
                        DescendantDsBlocks.Add(childDsBlock);
                        if (childDsBlock is ComponentDsBlock componentDsBlock)
                        {
                            DescendantDsBlocks.AddRange(componentDsBlock.DsBlocksTempRuntimeData.DescendantDsBlocks);
                        }
                    }
                }
            }
            else
            {
                // Module Cache
                if (logger is not null)
                {
                    foreach (var childDsBlock in childDsBlocks)
                    {
                        try
                        {
                            ChildDsBlocksDictionary.Add(childDsBlock.TagName, childDsBlock);
                            childDsBlock.DsBlockIndexInModule = (UInt16)DescendantDsBlocks.Count;
                            DescendantDsBlocks.Add(childDsBlock);
                            if (childDsBlock is ComponentDsBlock componentDsBlock)
                            {
                                foreach (var subDescendantDsBlock in componentDsBlock.DsBlocksTempRuntimeData.DescendantDsBlocks)
                                {
                                    subDescendantDsBlock.DsBlockIndexInModule = (UInt16)DescendantDsBlocks.Count;
                                    DescendantDsBlocks.Add(subDescendantDsBlock);
                                }
                            }
                        }
                        catch
                        {
                            logger.LogError("Duplicate block Tag in module.");
                        }
                    }
                }
                else
                {
                    foreach (var childDsBlock in childDsBlocks)
                    {
                        ChildDsBlocksDictionary[childDsBlock.TagName] = childDsBlock;
                        childDsBlock.DsBlockIndexInModule = (UInt16)DescendantDsBlocks.Count;
                        DescendantDsBlocks.Add(childDsBlock);
                        if (childDsBlock is ComponentDsBlock componentDsBlock)
                        {
                            foreach (var subDescendantDsBlock in componentDsBlock.DsBlocksTempRuntimeData.DescendantDsBlocks)
                            {
                                subDescendantDsBlock.DsBlockIndexInModule = (UInt16)DescendantDsBlocks.Count;
                                DescendantDsBlocks.Add(subDescendantDsBlock);
                            }
                        }
                    }
                }
            }            
        }

        #endregion

        #region public functions

        public static readonly DsBlocksTempRuntimeData Empty = new DsBlocksTempRuntimeData();

        /// <summary>
        ///     [Tag, DsBlockBase]
        /// </summary>
        public readonly CaseInsensitiveDictionary<DsBlockBase> ChildDsBlocksDictionary = new(1024);

        public readonly List<DsBlockBase> DescendantDsBlocks = new(1024);

        #endregion
    }
}
