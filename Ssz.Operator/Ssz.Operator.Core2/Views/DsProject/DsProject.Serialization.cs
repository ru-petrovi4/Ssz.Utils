using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomExceptions;

using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace Ssz.Operator.Core
{
    public partial class DsProject
    {
        #region private fields

        private int _deserializedVersion;

        #endregion

        #region public functions

        public static readonly int CurrentSerializationVersion = 31;

        [Browsable(false)] public long BinDeserializationSkippedBytesCount { get; private set; }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            if (ReferenceEquals(context, SerializationContext.ShortBytes) ||
                ReferenceEquals(context, SerializationContext.FullBytes))
            {
                writer.Write(DesiredAdditionalAddonsInfo);

                foreach (var addon in AddonsCollection.ObservableCollection) writer.Write(addon, context);

                writer.Write(DefaultDsPageSize);
                writer.Write((int) DsPageStretchMode);
                writer.Write((int) DsPageHorizontalAlignment);
                writer.Write((int) DsPageVerticalAlignment);
                writer.WriteObject(DsPageBackground);
                writer.Write(ConditionalDsCommandsCollection.ToList());
                writer.Write(PlayWindowClassOptionsCollection.ToList());
                writer.Write(RootWindowProps, context);
                writer.Write(_dataEngineGuidAndName, context);
                writer.WriteNullableOwnedData(DataEngine, context);
                writer.Write((int) DrawingsStoreMode);

                writer.Write(DsConstantsCollection.OrderBy(gpi => gpi.Name).ToList());

                writer.Write(Desc);

                writer.Write(DefaultServerAddress);

                writer.Write(OperatorUICultureName);
               
                writer.Write(Settings, context);

                writer.Write(TryConvertXamlToDsShapes);

                writer.Write(DefaultSystemNameToConnect);

                return;
            }

            using (writer.EnterBlock(CurrentSerializationVersion))
            {
                writer.Write(DesiredAdditionalAddonsInfo);

                AddonsCollection.WriteAddonsOwenedDataToFiles();

                _actuallyUsedAddonsInfo = AddonsHelper.GetAddonsInfo(GetUsedAddonGuids());
                writer.Write(_actuallyUsedAddonsInfo);

                writer.Write(DefaultDsPageSize);
                writer.Write((int) DsPageStretchMode);
                writer.Write((int) DsPageHorizontalAlignment);
                writer.Write((int) DsPageVerticalAlignment);
                writer.WriteObject(DsPageBackground);
                writer.Write(ConditionalDsCommandsCollection.ToList());
                writer.Write(PlayWindowClassOptionsCollection.ToList());
                writer.Write(RootWindowProps, context);
                writer.Write(_dataEngineGuidAndName, context);
                writer.WriteNullableOwnedData(DataEngine, context);
                writer.Write((int) DrawingsStoreMode);

                writer.Write(DsConstantsCollection.OrderBy(gpi => gpi.Name).ToList());

                writer.Write(Desc);

                writer.Write(DefaultServerAddress);

                writer.Write(OperatorUICultureName);
                
                writer.Write(Settings, context);

                writer.Write(TryConvertXamlToDsShapes);

                writer.Write(DefaultSystemNameToConnect);
            }
        }

        public async Task DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            var skippedBytesCountBefore = reader.SkippedBytesCount;

            using (Block block = reader.EnterBlock())
            {
                _deserializedVersion = block.Version;
                if (_deserializedVersion == CurrentSerializationVersion - 1 ||
                    _deserializedVersion == CurrentSerializationVersion)
                {
                    try
                    {
                        DesiredAdditionalAddonsInfo = reader.ReadList<GuidAndName>();
                        // Refresh addons display names, if possible
                        if (Mode != DsProjectModeEnum.BrowserPlayMode)
                            if (_desiredAdditionalAddonsInfo is not null)
                                foreach (GuidAndName guidAndName in _desiredAdditionalAddonsInfo)
                                {
                                    var latestNameToDisplay = AddonsHelper.GetAddonName(guidAndName.Guid);
                                    if (!string.IsNullOrWhiteSpace(latestNameToDisplay))
                                        guidAndName.Name = latestNameToDisplay;
                                }

                        await AddonsCollection.ReadAddonsOwnedDataFromFilesAsync();

                        _actuallyUsedAddonsInfo = reader.ReadList<GuidAndName>();
                        // Refresh addons display names, if possible
                        if (Mode != DsProjectModeEnum.BrowserPlayMode)
                            foreach (GuidAndName guidAndName in _actuallyUsedAddonsInfo)
                            {
                                var latestNameToDisplay = AddonsHelper.GetAddonName(guidAndName.Guid);
                                if (!string.IsNullOrWhiteSpace(latestNameToDisplay))
                                    guidAndName.Name = latestNameToDisplay;
                            }

                        string[] unSupportedAddonsNameToDisplays =
                            AddonsHelper.GetNotInAddonsCollection(_actuallyUsedAddonsInfo);
                        if (unSupportedAddonsNameToDisplays.Length > 0 && Review)
                        {
                            string message = Resources.DsProjectFileOpenErrorAddonIsNotFound + @" ";
                            foreach (string unSupportedAddonNameToDisplay in unSupportedAddonsNameToDisplays)
                                message += unSupportedAddonNameToDisplay + @"; ";
                            IsReadOnly = true;
                            MessageBoxHelper.ShowWarning(message);
                        }

                        unSupportedAddonsNameToDisplays =
                            AddonsHelper.GetNotInAddonsCollection(_desiredAdditionalAddonsInfo);
                        if (unSupportedAddonsNameToDisplays.Length > 0)
                        {
                            string message = Resources.DsProjectFileOpenWarningAddonIsNotFound + @" ";
                            foreach (string unSupportedAddonNameToDisplay in unSupportedAddonsNameToDisplays)
                                message += unSupportedAddonNameToDisplay + @"; ";
                            if (Review || Mode == DsProjectModeEnum.VisualDesignMode)
                                MessageBoxHelper.ShowWarning(message);
                            DsProject.LoggersSet.Logger.LogCritical(message);
                        }

                        DefaultDsPageSize = reader.ReadSize();
                        DsPageStretchMode = (DsPageStretchMode) reader.ReadInt32();
                        DsPageHorizontalAlignment = (DsPageHorizontalAlignment) reader.ReadInt32();
                        DsPageVerticalAlignment = (DsPageVerticalAlignment) reader.ReadInt32();
                        DsPageBackground = reader.ReadObject<SolidDsBrush>();

                        List<DsCommand> conditionalDsCommandsCollection = reader.ReadList<DsCommand>();
                        ConditionalDsCommandsCollection.Clear();
                        foreach (DsCommand dsCommand in conditionalDsCommandsCollection)
                            ConditionalDsCommandsCollection.Add(dsCommand);

                        List<PlayWindowClassOptions> playWindowClassOptionssCollection = reader.ReadList<PlayWindowClassOptions>();
                        PlayWindowClassOptionsCollection.Clear();
                        foreach (PlayWindowClassOptions playWindowClassOptions in playWindowClassOptionssCollection)
                            PlayWindowClassOptionsCollection.Add(playWindowClassOptions);

                        reader.ReadOwnedData(RootWindowProps, context);
                        var dataEngineInfo = new GuidAndName();
                        reader.ReadOwnedData(dataEngineInfo, context);
                        DataEngineGuidAndName = dataEngineInfo;
                        reader.ReadNullableOwnedData(DataEngine, context);
                        DrawingsStoreMode = (DrawingsStoreModeEnum) reader.ReadInt32();

                        List<DsConstant> dsConstantsCollection = reader.ReadList<DsConstant>();
                        DsConstantsCollection.Clear();
                        foreach (DsConstant dsConstant in dsConstantsCollection) DsConstantsCollection.Add(dsConstant);

                        Desc = reader.ReadString();

                        DefaultServerAddress = reader.ReadString();

                        OperatorUICultureName = reader.ReadString();
                        
                        reader.ReadOwnedData(Settings, context);

                        TryConvertXamlToDsShapes = reader.ReadBoolean();

                        DefaultSystemNameToConnect = reader.ReadString();

                        if (_deserializedVersion < CurrentSerializationVersion)
                        {
                            DefaultServerAddress = "https://localhost:60060/";
                            DefaultSystemNameToConnect = @"PLATFORM";
                        }
                    }
                    catch (BlockEndingException)
                    {
                    }
                    catch (BlockUnsupportedVersionException)
                    {
                        string message = Resources.DsProjectFileOpenErrorFileWasSavedInNewerVersion;
                        throw new ShowMessageException(message);
                    }
                }
                else if (_deserializedVersion < CurrentSerializationVersion - 1)
                {
                    //DeserializeOwnedDataObsolete(reader, context);
                    throw new ShowMessageException("Unsupported old file format.");
                }
                else
                {
                    string message = Resources.DsProjectFileOpenErrorFileWasSavedInNewerVersion + @" (" +
                                     _deserializedVersion + ")";
                    throw new ShowMessageException(message);
                }
            }

            BinDeserializationSkippedBytesCount = reader.SkippedBytesCount - skippedBytesCountBefore;
        }

        #endregion
    }
}