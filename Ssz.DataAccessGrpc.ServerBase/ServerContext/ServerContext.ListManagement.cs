using System;
using System.Collections.Generic;
using Grpc.Core;
using Ssz.Utils.DataAccess;
using Ssz.Utils;
using System.Threading.Tasks;

namespace Ssz.DataAccessGrpc.ServerBase
{
    /// <summary>
    /// 
    /// </summary>
    public partial class ServerContext        
    {
        #region internal functions

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listClientAlias"></param>
        /// <param name="listType"></param>
        /// <param name="listParams"></param>
        /// <returns></returns>
        public Utils.DataAccess.AliasResult DefineList(uint listClientAlias, uint listType, CaseInsensitiveOrderedDictionary<string?> listParams)
        {
            ServerListRoot serverList = ServerWorker.NewServerList(this, listClientAlias, listType, listParams);
            
            uint listServerAlias = _listsManager.Add(serverList);

            var aliasResult = new Utils.DataAccess.AliasResult();
            aliasResult.StatusCode = (uint)StatusCode.OK;
            aliasResult.ClientAlias = listClientAlias;
            aliasResult.ServerAlias = listServerAlias;
            return aliasResult;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listServerAliases"></param>
        /// <returns></returns>
        public List<Utils.DataAccess.AliasResult> DeleteLists(List<uint> listServerAliases)
        {
            var resultsList = new List<Utils.DataAccess.AliasResult>();

            // null means to delete all lists, so put all lists into listIds
            if (listServerAliases.Count == 0)
            {
                // Remove each list from the endpoints to which it is assigned
                foreach (ServerListRoot serverList in _listsManager)
                {
                    serverList.Dispose();
                }                
                _listsManager.Clear();
            }
            else
            {
                foreach (uint listServerAlias in listServerAliases)
                {
                    ServerListRoot? serverList;
                    if (_listsManager.TryGetValue(listServerAlias, out serverList))
                    {
                        serverList.Dispose();
                        _listsManager.Remove(listServerAlias);
                    }
                    else
                    {                        
                        resultsList.Add(new Utils.DataAccess.AliasResult
                            {
                                StatusCode = (uint)StatusCode.InvalidArgument,
                                ClientAlias = 0,
                                ServerAlias = listServerAlias
                            }
                            );
                    }
                }
            }

            return resultsList;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="listServerAlias"></param>
        /// <param name="itemsToAdd"></param>
        /// <returns></returns>
        public async Task<List<Utils.DataAccess.AliasResult>> AddItemsToListAsync(uint listServerAlias, List<Utils.DataAccess.ListItemInfo> itemsToAdd)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            return await serverList.AddItemsToListAsync(itemsToAdd);
        }

        /// <summary>
        ///     Returns failed AliasResultMessages only.
        /// </summary>
        /// <param name="listServerAlias"></param>
        /// <param name="serverAliasesToRemove"></param>
        /// <returns></returns>
        /// <exception cref="RpcException"></exception>
        public async Task<List<Utils.DataAccess.AliasResult>> RemoveItemsFromListAsync(uint listServerAlias, List<uint> serverAliasesToRemove)
        {
            ServerListRoot? serverList;

            if (!_listsManager.TryGetValue(listServerAlias, out serverList))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Incorrect listServerAlias."));
            }

            return await serverList.RemoveItemsFromListAsync(serverAliasesToRemove);
        }        

        #endregion
    }
}