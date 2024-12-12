using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.Utils;
using Ssz.DataAccessGrpc.Client;
using Ssz.Utils;
using Ssz.Utils.DataAccess;

namespace Ssz.Operator.Core.DataAccess
{
    public partial class DsDataAccessProvider : GrpcDataAccessProvider
    {
        #region construction and destruction

        public DsDataAccessProvider(ILogger<GrpcDataAccessProvider> logger) :
            base(logger, DsProject.LoggersSet.UserFriendlyLogger)
        {            
        }

        #endregion

        #region public functions

        public static IServiceProvider? ServiceProvider { get; set; }

        public static DsDataAccessProvider Instance { get; private set; } = null!;

        public const int MaximumPollRateMs = 60000;

        public const int MinimumPollRateMs = 300;

        public static async Task StaticInitialize(            
            ElementIdsMap? elementIdsMap,
            string serverAddress,
            string clientApplicationName,
            string systemNameToConnect,
            CaseInsensitiveDictionary<string?> contextParams,
            IDispatcher? callbackDispatcher)
        {
            if (Instance is not null)
                await Instance.DisposeAsync();

            string clientWorkstationName;
            try
            {
                clientWorkstationName = Environment.MachineName;
            }
            catch (Exception)
            {
                clientWorkstationName = "localhost";
            }

            if (ServiceProvider is null)
            {                
                Instance = new DsDataAccessProvider(NullLogger<GrpcDataAccessProvider>.Instance);
            }
            else
            {
                Instance = new DsDataAccessProvider(ServiceProvider.GetRequiredService<ILogger<GrpcDataAccessProvider>>());
            }

            string userName = DsProject.Instance.UserName;
            if (string.IsNullOrEmpty(userName))
                try
                {
                    userName = Environment.UserName;
                }
                catch
                {
                }
            if (!string.IsNullOrEmpty(userName))
                contextParams[@"UserName"] = userName;

            string? windowsUserName = null;
            try
            {
                windowsUserName = Environment.UserName;
            }
            catch
            {
            }
            if (!string.IsNullOrEmpty(windowsUserName))
                contextParams[@"WindowsUserName"] = windowsUserName;

            if (PlayDsProjectView.EventSourceModel.IsInitialized)
                PlayDsProjectView.EventSourceModel.Close();
            PlayDsProjectView.EventSourceModel.Initialize(Instance);

            Instance.Initialize(
                elementIdsMap,
                serverAddress,
                clientApplicationName,
                clientWorkstationName,
                systemNameToConnect,
                contextParams,
                new DataAccessProviderOptions
                {
                    UnsubscribeValueListItemsFromServer = false
                },
                callbackDispatcher);
        }

        public static async Task StaticDisposeAsync()
        {
            await Instance.DisposeAsync();

            if (PlayDsProjectView.EventSourceModel.IsInitialized)
                PlayDsProjectView.EventSourceModel.Close();
        }        

        #endregion

        #region protected functions        

        protected override async Task UnsubscribeAsync(bool clearClientSubscriptions)
        {
            var genericDataEngine = DsProject.Instance.DataEngine;
            genericDataEngine.ClearCache();            

            await base.UnsubscribeAsync(clearClientSubscriptions);
        }

        /// <summary>
        ///     On loop in working thread.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected override async Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            await base.DoWorkAsync(nowUtc, cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested) return;
            var callbackDispatcher = CallbackDispatcher;
            if (callbackDispatcher is not null &&
                nowUtc - LastValueSubscriptionsUpdatedDateTimeUtc >= TimeSpan.FromMilliseconds(1000))
                try
                {
                    callbackDispatcher.BeginInvoke(ct =>
                    {
                        RaiseValueSubscriptionsUpdated();
                    });
                }
                catch (Exception)
                {
                }
        }

        #endregion
    }
}