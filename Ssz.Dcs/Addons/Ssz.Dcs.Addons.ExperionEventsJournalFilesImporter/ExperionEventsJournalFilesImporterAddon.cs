using Ssz.Dcs.CentralServer.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ssz.Utils;
using Ssz.Utils.Addons;
using Ssz.Utils.DataAccess;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ssz.Dcs.Addons.ExperionEventsJournalFilesImporter
{
    [Export(typeof(AddonBase))]
    public class ExperionEventsJournalFilesImporterAddon : DataAccessProviderGetter_AddonBase
    {
        public static readonly string JournalFiles_Path_OptionName = @"%(JournalFiles_Path)";

        public static readonly string JournalFiles_Type_OptionName = @"%(JournalFiles_Type)";

        public static readonly string JournalFiles_DateTimeFormat_OptionName = @"%(JournalFiles_DateTimeFormat)";
        public static readonly string JournalFiles_DateTimeFormat_OptionDefaultValue = @"dd.MM.yyyy H:mm:ss.FFFFF";

        public static readonly string JournalFiles_SourceTimeZone_OptionName = @"%(JournalFiles_SourceTimeZone)";
        public static readonly string JournalFiles_SourceTimeZone_OptionDefaultValue = @"3";

        public static readonly string RptFiles_Encoding_OptionName = @"%(RptFiles_Encoding)";

        public static readonly string ScanPeriodSeconds_OptionName = @"%(ScanPeriodSeconds)";

        public static readonly string MaxProcessedTimeUtc_VariableName = @"%(MaxProcessedTimeUtc)";

        public static readonly string JournalFilesDeleteScanPeriodSeconds_OptionName = @"%(JournalFilesDeleteScanPeriodSeconds)";        

        public const string ExperionEventsSource = @"EPKS";

        public static readonly Guid AddonGuid = new Guid(@"A270F881-405B-42C7-971F-50C40A5B6202");

        public static readonly string AddonIdentifier = @"ExperionEventsJournalFilesImporter";

        public override Guid Guid => AddonGuid;

        public override string Identifier => AddonIdentifier;

        public override string Desc => Properties.Resources.ExperionEventsJournalFilesImporterAddon_Desc;

        public override string Version => "1.0";
        
        public override (string, string, string)[] OptionsInfo => new (string, string, string)[]
        {
            (JournalFiles_Path_OptionName, Properties.Resources.JournalFiles_Path_Option, @""),
            (JournalFiles_Type_OptionName, Properties.Resources.JournalFiles_Type_Option, @"RPT"),
            (JournalFiles_DateTimeFormat_OptionName, Properties.Resources.JournalFiles_DateTimeFormat_Option, 
                JournalFiles_DateTimeFormat_OptionDefaultValue),
            (JournalFiles_SourceTimeZone_OptionName, Properties.Resources.JournalFiles_SourceTimeZone_Option, 
                JournalFiles_SourceTimeZone_OptionDefaultValue),
            (RptFiles_Encoding_OptionName, Properties.Resources.RptFiles_Encoding_Option, @"windows-1251"),
            (ScanPeriodSeconds_OptionName, Properties.Resources.ScanPeriodSeconds_Option, @"3600"),
            (DataAccessProviderGetter_CommonEventMessageFieldsToAdd_OptionName, Properties.Resources.CommonEventMessageFieldsToAdd_Option, @"UnitIdentifier=MLSP"),
            (JournalFilesDeleteScanPeriodSeconds_OptionName, Properties.Resources.JournalFilesDeleteScanPeriodSeconds_Option, @"3600")
        };
        
        public override void Initialize(CancellationToken cancellationToken)
        {
            var dataAccessProvider = ActivatorUtilities.CreateInstance<ExperionEventsJournalFiles_DataAccessProvider>(ServiceProvider, this);

            var elementIdsMap = ActivatorUtilities.CreateInstance<ElementIdsMap>(ServiceProvider);
            elementIdsMap.Initialize(CsvDb.GetData(ElementIdsMap.StandardMapFileName), CsvDb.GetData(ElementIdsMap.StandardTagsFileName), CsvDb);
            elementIdsMap.CommonEventMessageFieldsToAdd[@"EventsSource"] = ExperionEventsSource;
            elementIdsMap.CommonEventMessageFieldsToAdd[@"SourceAddonInstanceId"] = InstanceId;            
            foreach (var kvp in NameValueCollectionHelper.Parse(OptionsSubstituted.TryGetValue(DataAccessProviderGetter_CommonEventMessageFieldsToAdd_OptionName)))
            {
                elementIdsMap.CommonEventMessageFieldsToAdd[kvp.Key] = kvp.Value;
            }

            dataAccessProvider.Initialize(elementIdsMap,                
                @"",
                @"Ssz.Dcs.Addons.ExperionEventsJournalFilesImporter",
                Environment.MachineName,
                @"",
                new CaseInsensitiveDictionary<string?>(),
                new DataAccessProviderOptions(),
                Dispatcher);

            DataAccessProvider = dataAccessProvider;

            base.Initialize(cancellationToken);
        }

        public override void Close()
        {
            if (DataAccessProvider is not null)
            {
                var t = DataAccessProvider.CloseAsync();
                DataAccessProvider = null;
            }

            base.Close();
        }
    }
}