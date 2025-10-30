using Microsoft.Extensions.Logging;
using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{    
    public class DsModulesTempRuntimeData
    {
        #region construction and destruction

        private DsModulesTempRuntimeData()
        {
        }

        /// <summary>
        ///     Logs with Error priority,
        /// </summary>
        /// <param name="childDsBlocks"></param>
        /// <param name="logger"></param>
        public DsModulesTempRuntimeData(DsModule[] modules, ILogger? logger = null)
        {
            if (logger is not null)
            {
                foreach (var module in modules)
                {                    
                    try
                    {
                        ModulesDictionary.Add(module.Name, module);                        
                    }
                    catch
                    {
                        logger.LogError("Duplicate Module Name in device.");
                    }
                    foreach (var childDsBlock in module.ChildDsBlocks)
                    {
                        try
                        {
                            ChildDsBlocksDictionary.Add(childDsBlock.TagName, childDsBlock);                            
                        }
                        catch
                        {
                            logger.LogError("Duplicate DsBlock Tag in device.");
                        }
                    }
                }
            }
            else
            {
                foreach (var module in modules)
                {
                    ModulesDictionary[module.Name] = module;
                    foreach (var childDsBlock in module.ChildDsBlocks)
                    {
                        ChildDsBlocksDictionary[childDsBlock.TagName] = childDsBlock;
                    }
                }
            }
        }

        #endregion

        #region public functions

        public static readonly DsModulesTempRuntimeData Empty = new DsModulesTempRuntimeData();

        /// <summary>
        ///     [Name, DsModule]
        /// </summary>
        public readonly CaseInsensitiveOrderedDictionary<DsModule> ModulesDictionary = new(1024);

        /// <summary>
        ///     [Tag, DsBlockBase]
        /// </summary>
        public readonly CaseInsensitiveOrderedDictionary<DsBlockBase> ChildDsBlocksDictionary = new(1024);        

        #endregion
    }
}
