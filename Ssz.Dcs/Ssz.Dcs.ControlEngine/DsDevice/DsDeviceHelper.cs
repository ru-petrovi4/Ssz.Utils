using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.ControlEngine
{
    public static class DsDeviceHelper
    {
        #region public functions

        public static string GetDsBlockFullNameWithParamFullName(string blockFullName, string paramName, byte paramValueIndex)
        {
            if (blockFullName == @"" || paramName == @"") return @"";
            var result = blockFullName + "." + paramName;
            if (paramValueIndex == IndexConstants.ParamValueIndex_IsNotArray)
                return result;
            else
                return result + "[" + paramValueIndex + 1 + "]";
        }

        /// <summary>
        ///     paramName is Upper-Case.
        /// </summary>
        /// <param name="blockFullNameWithParamFullName"></param>
        /// <param name="blockFullName"></param>
        /// <param name="paramName"></param>
        /// <param name="paramValueIndex"></param>
        public static void SplitDsBlockFullNameWithParamFullName(string blockFullNameWithParamFullName, out string blockFullName, out string paramName, out byte paramValueIndex)
        {
            blockFullName = @"";
            paramName = @"";
            paramValueIndex = IndexConstants.ParamValueIndex_IsNotArray;

            var index = blockFullNameWithParamFullName.LastIndexOf('.');
            if (index > 0 && index < blockFullNameWithParamFullName.Length - 1)
            {
                blockFullName = blockFullNameWithParamFullName.Substring(0, index);
                paramName = blockFullNameWithParamFullName.Substring(index + 1);                
                if (paramName.EndsWith(']'))
                {
                    index = paramName.IndexOf('[');
                    if (index > 0 && index < paramName.Length - 2)
                    {
                        paramValueIndex = (byte)(new Any(paramName.Substring(index + 1, paramName.Length - index - 2)).ValueAsInt32(false) - 1);
                        paramName = paramName.Substring(index).ToUpperInvariant();
                    }
                }
                else
                {
                    paramName = paramName.ToUpperInvariant();
                }
            }
        }

        /// <summary>
        ///     Searches only in componentDsBlock.
        /// </summary>
        /// <param name="blockFullName"></param>
        /// <param name="componentDsBlock"></param>
        /// <returns></returns>
        public static DsBlockBase? GetDsBlock(string blockFullName, ComponentDsBlock componentDsBlock)
        {
            return GetDsBlock(blockFullName.Split('.'), 0, componentDsBlock.DsBlocksTempRuntimeData.ChildDsBlocksDictionary);
        }

        /// <summary>
        ///     Searches in componentDsBlock and parent level containers.
        /// </summary>
        /// <param name="blockFullName"></param>
        /// <param name="module"></param>
        /// <param name="componentDsBlock"></param>
        /// <returns></returns>
        public static DsBlockBase? GetDsBlock(string blockFullName, DsModule module, ComponentDsBlock? componentDsBlock)
        {
            if (String.IsNullOrEmpty(blockFullName)) return null;
            if (componentDsBlock is not null)
            {
                var block = GetDsBlock(blockFullName.Split('.'), 0, componentDsBlock.DsBlocksTempRuntimeData.ChildDsBlocksDictionary);
                if (block is not null)
                    return block;
                return GetDsBlock(blockFullName, module, componentDsBlock.ParentComponentDsBlock);
            }
            else
            {
                return GetDsBlock(blockFullName.Split('.'), 0, module.DsBlocksTempRuntimeData.ChildDsBlocksDictionary);
            }
        }

        public static string GetDsBlockFullName(DsBlockBase block)
        {
            var parts = new List<string?> { block.TagName };
            var parentComponentDsBlock = block.ParentComponentDsBlock;
            while (parentComponentDsBlock is not null)
            {
                parts.Add(parentComponentDsBlock.TagName);
                parentComponentDsBlock = parentComponentDsBlock.ParentComponentDsBlock;
            }
            return String.Join(@".", ((IEnumerable<string?>)parts).Reverse());
        }

        #endregion

        #region private functions

        /// <summary>
        ///     partsStartIndex must be valid
        /// </summary>
        /// <param name="blockFullNameParts"></param>
        /// <param name="partsStartIndex"></param>
        /// <param name="childDsBlocksDictionary"></param>
        /// <returns></returns>
        private static DsBlockBase? GetDsBlock(string[] blockFullNameParts, int partsStartIndex, CaseInsensitiveOrderedDictionary<DsBlockBase> childDsBlocksDictionary)
        {
            var block = childDsBlocksDictionary.TryGetValue(blockFullNameParts[partsStartIndex]);
            if (block is null)
                return null;
            if (blockFullNameParts.Length - partsStartIndex == 1)
                return block;
            if (block is ComponentDsBlock componentDsBlock)
            {
                return GetDsBlock(blockFullNameParts, partsStartIndex + 1, componentDsBlock.DsBlocksTempRuntimeData.ChildDsBlocksDictionary);
            }
            else
            {
                return null;
            }
        }        

        #endregion
    }
}
