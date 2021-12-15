using Microsoft.Extensions.Logging.Abstractions;
using Ssz.Utils.DataAccess;
using Ssz.Utils.EventSourceModel;
using Ssz.Xi.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace TestWpfApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static readonly IDataAccessProvider DataAccessProvider = new XiDataAccessProvider(new NullLogger<XiDataAccessProvider>(), null);

        public static readonly EventSourceModel EventSourceModel = new EventSourceModel(DataAccessProvider);
    }
    
}
