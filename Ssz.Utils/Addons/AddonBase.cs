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

        public abstract Guid Guid { get; }

        public abstract string Name { get; }

        public abstract string Desc { get; }

        public abstract string Version { get; }

        public virtual bool IsDummy => false;

        public abstract (string, string)[] OptionsInfo { get; }

        public string DllFileFullName { get; internal set; } = @"";        
        
        public CsvDb CsvDb { get; internal set; } = null!;        

        /// <summary>
        ///     Unique ID for addon type and config.
        /// </summary>
        public string Id { get; internal set; } = null!;

        /// <summary>
        ///     Addon instance ID
        /// </summary>
        public string InstanceId { get; internal set; } = null!;

        public ILoggersSet LoggersSet { get; internal set; } = null!;

        public IConfiguration Configuration { get; internal set; } = null!;
        
        public IServiceProvider ServiceProvider { get; internal set; } = null!;

        bool IObservableCollectionItem.IsDeleted { get; set; }

        bool IObservableCollectionItem.IsAdded { get; set; }

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
