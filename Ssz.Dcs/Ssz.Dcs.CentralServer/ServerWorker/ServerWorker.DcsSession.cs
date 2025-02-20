using Ssz.Dcs.CentralServer.Common;
using Microsoft.Extensions.Logging;
using Ssz.DataAccessGrpc.ServerBase;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer
{
    public partial class ServerWorker : ServerWorkerBase
    {
        #region public functions

        public ObservableCollection<CentralServer.EngineSession> Dcs_EngineSessions { get; } = new();        

        #endregion

        #region private functions

        private void OnAddons_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    OnDcsCentralServerAddons_Added(e.NewItems!.OfType<DcsCentralServerAddon>());
                    OnDataAccessProviderGetter_Addons_Added(e.NewItems!.OfType<DataAccessProviderGetter_AddonBase>());
                    break;
                case NotifyCollectionChangedAction.Remove:
                    OnDcsCentralServerAddons_Removed(e.OldItems!.OfType<DcsCentralServerAddon>());
                    OnDataAccessProviderGetter_Addons_Removed(e.OldItems!.OfType<DataAccessProviderGetter_AddonBase>());
                    break;
                case NotifyCollectionChangedAction.Replace:
                    break;
                case NotifyCollectionChangedAction.Reset:
                    break;
            }
        }

        private void OnDcsCentralServerAddons_Added(IEnumerable<DcsCentralServerAddon> addedDcsCentralServerAddons)
        {
            foreach (var addedDcsCentralServerAddon in addedDcsCentralServerAddons)
            {
                addedDcsCentralServerAddon.CsvDb.CsvFileChanged += DcsCentralServerAddon_CsvDb_OnCsvFileChanged;
            }
        }        

        private void OnDcsCentralServerAddons_Removed(IEnumerable<DcsCentralServerAddon> removedDcsCentralServerAddons)
        {
            foreach (var removedDcsCentralServerAddon in removedDcsCentralServerAddons)
            {
                removedDcsCentralServerAddon.CsvDb.CsvFileChanged -= DcsCentralServerAddon_CsvDb_OnCsvFileChanged;
            }
        }

        private void DcsCentralServerAddon_CsvDb_OnCsvFileChanged(object? sender, CsvFileChangedEventArgs e)
        {
            if (String.Equals(e.CsvFileName, DcsCentralServerAddon.ClientsCsvFileName, StringComparison.InvariantCultureIgnoreCase))
                _utilityItemsDoWorkNeeded = true;
        }

        private void OnDataAccessProviderGetter_Addons_Added(IEnumerable<DataAccessProviderGetter_AddonBase> addedDataAccessProviderGetter_Addons)
        {
            foreach (var addedDataAccessProviderGetter_Addon in addedDataAccessProviderGetter_Addons)
            {
                var egineSession = new EngineSession(Guid.NewGuid().ToString(), addedDataAccessProviderGetter_Addon);
                Dcs_EngineSessions.Add(egineSession);
            }
        }

        private void OnDataAccessProviderGetter_Addons_Removed(IEnumerable<DataAccessProviderGetter_AddonBase> removedDataAccessProviderGetter_Addons)
        {
            foreach (var removedDataAccessProviderGetter_Addon in removedDataAccessProviderGetter_Addons)
            {
                for (int collectionIndex = Dcs_EngineSessions.Count - 1; collectionIndex >= 0; collectionIndex -= 1)
                {
                    var egineSession = Dcs_EngineSessions[collectionIndex];
                    if (ReferenceEquals(egineSession.DataAccessProviderGetter_Addon, removedDataAccessProviderGetter_Addon))
                    {
                        Dcs_EngineSessions.RemoveAt(collectionIndex);
                        egineSession.Dispose();
                        break;
                    }
                }                
            }
        }

        #endregion        
    }
}
