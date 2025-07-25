﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Dispatcher;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Utils.Addons
{
    public abstract class AddonBase : IObservableCollectionItem, IDispatcherObject
    {
        #region public functions

        /// <summary>
        ///     Is addon contains resource monitoring data.
        /// </summary>
        public const string ParamName_IsResourceMonitoringAddon = @"IsResourceMonitoringAddon";

        /// <summary>
        ///     Addon options .csv file name.
        /// </summary>
        public const string OptionsCsvFileName = @"options.csv";

        /// <summary>
        ///     Addon runtime variables .csv file name.
        /// </summary>
        public const string VariablesCsvFileName = @"variables.csv";        

        /// <summary>
        ///     Addon GUID, never changes.
        ///     Does not change when version changes.
        ///     Thread-safe.
        /// </summary>
        public abstract Guid Guid { get; }

        /// <summary>
        ///     Cannot contain periods.
        ///     Does not change when version changes.
        ///     Thread-safe.
        /// </summary>
        public abstract string Identifier { get; }

        /// <summary>
        ///     Thread-safe.
        /// </summary>
        public abstract string Desc { get; }

        /// <summary>
        ///     Thread-safe.
        /// </summary>
        public abstract string Version { get; }        

        /// <summary>
        ///     Thread-safe.
        /// </summary>
        public virtual bool IsMultiInstance => false;

        /// <summary>
        ///     Thread-safe.
        /// </summary>
        public virtual bool IsAlwaysSwitchedOn => false;

        /// <summary>
        ///     Thread-safe.
        /// </summary>
        public virtual bool IsSwitchedOnByDefault => false;

        /// <summary>
        ///     Option names cannot contain periods.
        ///     (Option Name, Option Description, Option Default Value)
        ///     Thread-safe. 
        /// </summary>
        /// <remarks>
        ///     Use OptionsSubstituted.TryGetValue(EventsToleranceSeconds_OptionName) to extract values.
        /// </remarks>
        public abstract (string, string, string)[] OptionsInfo { get; }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        public string DllFileFullName { get; internal set; } = @"";        
        
        public CsvDb CsvDb { get; internal set; } = null!;

        /// <summary>
        ///     Dispatcher associated with this addon.        
        /// </summary>
        public IDispatcher? Dispatcher => CsvDb.Dispatcher;

        /// <summary>
        ///     Do not changes after addon creation.
        ///     All values substituted. 
        ///     E.g value 'appsettings.json:DataAccessClient_ContextParams'.
        ///     Gets value from appsettings.json:AddonsOptions:_Addon_Identifier_:DataAccessClient_ContextParams
        ///     Thread-safe.
        /// </summary>
        public CaseInsensitiveDictionary<string?> OptionsSubstituted { get; internal set; } = null!;

        /// <summary>
        ///     Unique ID for addon type and options.
        ///     Contains Addon.Identifier, Addon.InstanceId, Addon.OptionsSubstituted.
        ///     Thread-safe.
        /// </summary>
        public string ObservableCollectionItemId { get; internal set; } = null!;

        /// <summary>
        ///     Addon instance ID.
        ///     Defines config directory name. 
        ///     If empty, config directory is not used.
        ///     Thread-safe.
        /// </summary>
        public string InstanceId { get; internal set; } = null!;        

        /// <summary>
        ///     Thread-safe.
        /// </summary>
        public IConfiguration Configuration { get; internal set; } = null!;

        /// <summary>
        ///     Thread-safe.
        /// </summary>
        public IServiceProvider ServiceProvider { get; internal set; } = null!;

        bool IObservableCollectionItem.ObservableCollectionItemIsDeleted { get; set; }

        bool IObservableCollectionItem.ObservableCollectionItemIsAdded { get; set; }

        public bool IsInitialized { get; private set; }

        /// <summary>
        ///     Last successful work time.
        /// </summary>
        public DateTime? LastWorkTimeUtc { get; set; }

        public event EventHandler? Initialized;

        public event EventHandler? Closed;

        public virtual Task<AddonStatus> GetAddonStatusAsync()
        {
            return Task.FromResult(new AddonStatus
            {
                AddonGuid = Guid,
                AddonIdentifier = Identifier,
                AddonDesc = Desc,
                AddonInstanceId = InstanceId,
                LastWorkTimeUtc = LastWorkTimeUtc,
                StateCode = AddonStateCodes.STATE_OPERATIONAL
            });
        }

        /// <summary>
        ///     When overridden call this base class method after your code.
        /// </summary>
        public virtual void Initialize(CancellationToken cancellationToken)
        {
            IsInitialized = true;

            Initialized?.Invoke(this, EventArgs.Empty);
        }        

        /// <summary>
        ///     When overridden call this base class method before your code.
        /// </summary>
        public virtual void Close()
        {
            Closed?.Invoke(this, EventArgs.Empty);

            IsInitialized = false;
        }

        public virtual Task DoWorkAsync(DateTime nowUtc, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        void IObservableCollectionItem.ObservableCollectionItemUpdate(IObservableCollectionItem item)
        {
        }        

        public virtual string GetAddonTestInfo()
        {
            return @"";
        }

        public virtual Task AddonTestAsync(string options, ILoggersSet loggersSet)
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
