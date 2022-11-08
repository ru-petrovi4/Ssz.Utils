using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Utils.Addons
{
    public abstract class AddonBase : IObservableCollectionItem
    {
        #region public functions

        /// <summary>
        ///     Addon options .csv file name.
        /// </summary>
        public const string OptionsCsvFileName = @"Options.csv";

        /// <summary>
        ///     Addon runtime variables .csv file name.
        /// </summary>
        public const string VariablesCsvFileName = @"Variables.csv";

        /// <summary>
        ///     Addons list .csv file name.
        /// </summary>
        public const string AddonsCsvFileName = @"Addons.csv";

        /// <summary>
        ///     Available addons info .csv file name.
        /// </summary>
        public const string AddonsAvailableCsvFileName = @"AddonsAvailable.csv";

        /// <summary>
        ///     Thread-safe
        /// </summary>
        public abstract Guid Guid { get; }

        /// <summary>
        ///     Cannot contain periods.
        ///     Thread-safe
        /// </summary>
        public abstract string Identifier { get; }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        public abstract string Desc { get; }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        public abstract string Version { get; }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        public virtual bool IsDummy => false;

        /// <summary>
        ///     Options cannot contain periods.
        ///     Thread-safe
        /// </summary>
        public abstract (string, string)[] OptionsInfo { get; }

        /// <summary>
        ///     Thread-safe
        /// </summary>
        public string DllFileFullName { get; internal set; } = @"";        
        
        public CsvDb CsvDb { get; internal set; } = null!;

        public CaseInsensitiveDictionary<string?> OptionsThreadSafe { get; internal set; } = null!;

        /// <summary>
        ///     Unique ID for addon type and options.
        ///     Thread-safe
        /// </summary>
        public string ObservableCollectionItem_Id { get; internal set; } = null!;

        /// <summary>
        ///     Addon instance ID
        ///     Thread-safe
        /// </summary>
        public string InstanceId { get; internal set; } = null!;

        /// <summary>
        ///     Thread-safe
        /// </summary>
        public ILoggersSet LoggersSet { get; internal set; } = null!;

        /// <summary>
        ///     Thread-safe
        /// </summary>
        public IConfiguration Configuration { get; internal set; } = null!;

        /// <summary>
        ///     Thread-safe
        /// </summary>
        public IServiceProvider ServiceProvider { get; internal set; } = null!;

        bool IObservableCollectionItem.ObservableCollectionItem_IsDeleted { get; set; }

        bool IObservableCollectionItem.ObservableCollectionItem_IsAdded { get; set; }

        public event EventHandler? Initialized;

        public event EventHandler? Closed;

        /// <summary>
        ///     When overridden call this base class method after your code.
        /// </summary>
        public virtual void Initialize()
        {
            Initialized?.Invoke(this, EventArgs.Empty);
        }

        void IObservableCollectionItem.Update(IObservableCollectionItem item)
        {            
        }

        /// <summary>
        ///     When overridden call this base class method before your code.
        /// </summary>
        public virtual void Close()
        {
            Closed?.Invoke(this, EventArgs.Empty);
        }

        #endregion        
    }
}
