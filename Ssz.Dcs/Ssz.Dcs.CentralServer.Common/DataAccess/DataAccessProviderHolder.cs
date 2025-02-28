using Ssz.Utils;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.CentralServer.Common
{
    public class DataAccessProviderHolder : IObservableCollectionItem
    {
        public bool IsPriority { get; set; }

        /// <summary>
        ///     Substituted CentralServerAddress
        /// </summary>
        public string CentralServerAddress { get; set; } = null!;

        public string ObservableCollectionItemId => CentralServerAddress;

        /// <summary>
        ///     Used by the framework.
        /// </summary>
        public bool ObservableCollectionItemIsDeleted { get; set; }

        /// <summary>
        ///     Used by the framework.
        /// </summary>
        public bool ObservableCollectionItemIsAdded { get; set; }

        public void Initialize(CancellationToken cancellationToken)
        {
        }

        /// <summary>
        ///     Need implementation for updating.
        /// </summary>
        /// <param name="item"></param>
        public void ObservableCollectionItemUpdate(IObservableCollectionItem item)
        {
        }

        public void Close()
        {
        }

        public IDataAccessProvider DataAccessProvider { get; set; } = null!;
    }
}
