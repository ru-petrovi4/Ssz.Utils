using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Ssz.Operator.Core.Addons
{
    public class AddonsCollection
    {
        #region private fields

        #endregion

        #region public functions

        [DisplayName(@" ")]
        //[Editor(typeof(AddonsCollectionTypeEditor), typeof(AddonsCollectionTypeEditor))]
        public ObservableCollection<AddonBase> ObservableCollection { get; } = new();

        public override string ToString()
        {
            return "";
        }

        public async void WriteAddonsOwenedDataToFiles()
        {            
            var existingProps = new List<Tuple<Guid, FileInfo>>();
            var addonsDirectoryInfo = new DirectoryInfo(DsProject.Instance.AddonsDirectoryFullName);
            if (!addonsDirectoryInfo.Exists) 
                return;
            foreach (FileInfo propsFileInfo in
                addonsDirectoryInfo.GetFiles("*.dsprops", SearchOption.TopDirectoryOnly))
            {
                var propsFileStream = await DsProject.GetStreamAsync(propsFileInfo.FullName);
                if (propsFileStream is not null)
                {
                    using (var reader = new SerializationReader(propsFileStream))
                    {
                        using (Block block = reader.EnterBlock())
                        {
                            switch (block.Version)
                            {
                                case 1:
                                    var addonGuid = reader.ReadGuid();
                                    existingProps.Add(Tuple.Create(addonGuid, propsFileInfo));
                                    break;
                            }
                        }
                    }

                    propsFileStream.Dispose();
                }
            }

            foreach (AddonBase addon in ObservableCollection)
            {
                var addonGuid = addon.Guid;
                foreach (FileInfo fi in existingProps.Where(kvp => kvp.Item1 == addonGuid).Select(kvp => kvp.Item2))
                    try
                    {
                        fi.Delete();
                    }
                    catch (Exception)
                    {
                    }

                try
                {                    
                    string fileFullName = Path.Combine(addonsDirectoryInfo.FullName,
                                          addon.Name + @".dsprops");
                    using (var memoryStream = new MemoryStream(1024 * 1024))
                    {
                        var isEmpty = false;
                        using (var writer = new SerializationWriter(memoryStream, true))
                        {
                            using (writer.EnterBlock(1))
                            {
                                writer.Write(addonGuid);

                                var beginPosition = memoryStream.Position;
                                addon.SerializeOwnedData(writer, null);
                                isEmpty = beginPosition == memoryStream.Position;
                            }
                        }

                        if (!isEmpty)
                            using (FileStream fileStream = File.Create(fileFullName))
                            {
                                memoryStream.WriteTo(fileStream);
                            }
                    }
                }
                catch (Exception ex)
                {
                    DsProject.LoggersSet.Logger.LogError(ex, Resources.AddonSerializationError);
                    if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.VisualDesignMode)
                        MessageBoxHelper.ShowError(Resources.AddonSerializationError + "\n" +
                                                   Resources.SeeErrorLogForDetails);
                }
            }
        }

        public async Task ReadAddonsOwnedDataFromFiles()
        {
            var addonsDirectoryInfo = new DirectoryInfo(DsProject.Instance.AddonsDirectoryFullName);
            if (!addonsDirectoryInfo.Exists) 
                return;            
            foreach (FileInfo propsFileInfo in
                addonsDirectoryInfo.GetFiles("*.dsprops", SearchOption.TopDirectoryOnly))
            {
                var propsFileStream = await DsProject.GetStreamAsync(propsFileInfo.FullName);
                if (propsFileStream is not null)
                {
                    using (var reader = new SerializationReader(propsFileStream))
                    {
                        using (Block block = reader.EnterBlock())
                        {
                            switch (block.Version)
                            {
                                case 1:
                                    var addonGuid = reader.ReadGuid();
                                    var addon =
                                        ObservableCollection.FirstOrDefault(p => p.Guid == addonGuid);
                                    if (addon is not null)
                                        try
                                        {
                                            reader.ReadOwnedData(addon, this);
                                        }
                                        catch (BlockUnsupportedVersionException ex)
                                        {
                                            DsProject.LoggersSet.Logger.LogError(ex,
                                                addon.Name + " " + Resources
                                                    .AddonDeserializationErrorUnsupportedVersion);
                                            if (DsProject.Instance.Mode ==
                                                DsProject.DsProjectModeEnum.VisualDesignMode)
                                                MessageBoxHelper.ShowError(addon.Name + "\n" +
                                                                            Resources
                                                                                .AddonDeserializationErrorUnsupportedVersion +
                                                                            "\n" + Resources.SeeErrorLogForDetails);
                                        }
                                        catch (Exception ex)
                                        {
                                            DsProject.LoggersSet.Logger.LogError(ex,
                                                addon.Name + " " + Resources.AddonDeserializationError);
                                            if (DsProject.Instance.Mode ==
                                                DsProject.DsProjectModeEnum.VisualDesignMode)
                                                MessageBoxHelper.ShowError(addon.Name + "\n" +
                                                                            Resources.AddonDeserializationError +
                                                                            "\n" + Resources.SeeErrorLogForDetails);
                                        }

                                    break;
                                default:
                                    DsProject.LoggersSet.Logger.LogError(propsFileInfo.FullName + " " +
                                                    Resources.AddonDeserializationErrorUnsupportedVersion);
                                    if (DsProject.Instance.Mode == DsProject.DsProjectModeEnum.VisualDesignMode)
                                        MessageBoxHelper.ShowError(propsFileInfo.FullName + "\n" +
                                                                    Resources
                                                                        .AddonDeserializationErrorUnsupportedFileVersion +
                                                                    "\n" + Resources.SeeErrorLogForDetails);
                                    break;
                            }
                        }
                    }

                    propsFileStream.Dispose();
                }
            }
        }

        #endregion
    }
}