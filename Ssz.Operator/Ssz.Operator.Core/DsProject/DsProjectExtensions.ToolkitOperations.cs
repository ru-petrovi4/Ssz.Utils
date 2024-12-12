using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Properties;

using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core
{
    public static partial class DsProjectExtensions
    {
        [Serializable]
        public class CreateDsPagesToolkitOperationOptions : OwnedDataSerializableAndCloneable
        {
            #region construction and destruction

            public CreateDsPagesToolkitOperationOptions()
            {
                TemplateDsPageDrawingFileName = "";
                TryCreateDsPagesWithSizeOfImage = true;
            }

            #endregion

            #region public functions

            [DsDisplayName(ResourceStrings.CreateDsPagesToolkitOperationOptions_TemplateDsPageDrawingFileName)]
            [LocalizedDescription(ResourceStrings
                .CreateDsPagesToolkitOperationOptions_TemplateDsPageDrawingFileName_Description)]
            [Editor(typeof(FileNameTypeEditor), typeof(FileNameTypeEditor))]
            public string TemplateDsPageDrawingFileName { get; set; }

            [DsDisplayName(ResourceStrings.CreateDsPagesToolkitOperationOptions_TryCreateDsPagesWithSizeOfImage)]
            [LocalizedDescription(ResourceStrings
                .CreateDsPagesToolkitOperationOptions_TryCreateDsPagesWithSizeOfImage_Description)]
            public bool TryCreateDsPagesWithSizeOfImage { get; set; }

            public override string ToString()
            {
                return "Create DsPages";
            }

            #endregion
        }

        [Serializable]
        public class UpdateDsPagesToolkitOperationOptions : OwnedDataSerializableAndCloneable
        {
            #region construction and destruction

            public UpdateDsPagesToolkitOperationOptions()
            {
                TryUpdateDsPagesWithSizeOfImage = false;
            }

            #endregion

            #region public functions

            [DsDisplayName(ResourceStrings.UpdateDsPagesToolkitOperationOptionsTryUpdateDsPagesWithSizeOfImage)]
            [LocalizedDescription(ResourceStrings
                .UpdateDsPagesToolkitOperationOptionsTryUpdateDsPagesWithSizeOfImageDescription)]
            public bool TryUpdateDsPagesWithSizeOfImage { get; set; }

            public override string ToString()
            {
                return "Update DsPages";
            }

            #endregion
        }

        [Serializable]
        public class UpdateComplexDsShapesToolkitOperationOptions : OwnedDataSerializableAndCloneable
        {
            #region construction and destruction

            public UpdateComplexDsShapesToolkitOperationOptions()
            {
                ComplexDsShapeNames = "";
                UpdateOnAllDsPages = false;
                ResetSizeToOriginal = false;
                ResetCenterRelativePositionToOriginal = false;
                MoveCenterPointOfDsShapes = new Point(0, 0);
            }

            #endregion

            #region public functions

            [DsDisplayName(ResourceStrings.UpdateComplexDsShapesToolkitOperationOptions_ComplexDsShapeNames)]
            [LocalizedDescription(ResourceStrings
                .UpdateComplexDsShapesToolkitOperationOptions_ComplexDsShapeNamesDescription)]
            [Editor(typeof(ComplexDsShapeNameTypeEditor), typeof(ComplexDsShapeNameTypeEditor))]
            [PropertyOrder(1)]
            public string ComplexDsShapeNames { get; set; }

            [DsDisplayName(ResourceStrings.UpdateComplexDsShapesToolkitOperationOptions_UpdateOnAllDsPages)]
            [PropertyOrder(2)]
            public bool UpdateOnAllDsPages { get; set; }

            [DsDisplayName(ResourceStrings.UpdateComplexDsShapesToolkitOperationOptions_ResetSizeToOriginal)]
            [LocalizedDescription(ResourceStrings
                .UpdateComplexDsShapesToolkitOperationOptions_ResetSizeToOriginalDescription)]
            [PropertyOrder(3)]
            public bool ResetSizeToOriginal { get; set; }

            [DsDisplayName(ResourceStrings
                .UpdateComplexDsShapesToolkitOperationOptions_ResetCenterRelativePositionToOriginal)]
            [LocalizedDescription(ResourceStrings
                .UpdateComplexDsShapesToolkitOperationOptions_ResetCenterRelativePositionToOriginalDescription)]
            [PropertyOrder(4)]
            public bool ResetCenterRelativePositionToOriginal { get; set; }

            [DsDisplayName(ResourceStrings.UpdateComplexDsShapesToolkitOperationOptions_MoveCenterPointOfDsShapes)]
            [LocalizedDescription(ResourceStrings
                .UpdateComplexDsShapesToolkitOperationOptions_MoveCenterPointOfDsShapesDescription)]
            [PropertyOrder(5)]
            public Point MoveCenterPointOfDsShapes { get; set; }

            public override string ToString()
            {
                return Resources.UpdateComplexDsShapesToolkitOperationOptions;
            }

            #endregion
        }

        #region public functions

        public static async Task<ToolkitOperationResult> CreateDsPagesToolkitOperationAsync(this DsProject dsProject,
            string[] imageFilesFullNames,
            CreateDsPagesToolkitOperationOptions toolkitOperationOptions, IProgressInfo progressInfo)
        {
            if (!dsProject.IsInitialized) return ToolkitOperationResult.Cancelled;

            var result = ToolkitOperationResult.Done;

            var existingTemplateDsPageDrawingFileInfo =
                DsProject.Instance.GetExistingDsPageFileInfoOrNull(toolkitOperationOptions.TemplateDsPageDrawingFileName);
            if (existingTemplateDsPageDrawingFileInfo is null &&
                !string.IsNullOrWhiteSpace(toolkitOperationOptions.TemplateDsPageDrawingFileName))
            {
                if (progressInfo is not null)
                    await progressInfo.DebugInfoAsync("Template dsPage doesn't exsist " +
                                                      toolkitOperationOptions.TemplateDsPageDrawingFileName);
                return ToolkitOperationResult.DoneWithErrors;
            }

            DsPageDrawing? templateDsPageDrawing = null;
            if (existingTemplateDsPageDrawingFileInfo is not null)
            {
                templateDsPageDrawing = DsProject.ReadDrawing(existingTemplateDsPageDrawingFileInfo, false,
                    true) as DsPageDrawing;
                if (templateDsPageDrawing is null)
                {
                    if (progressInfo is not null)
                        await progressInfo.DebugInfoAsync("Cannot read template dsPage " +
                                                          existingTemplateDsPageDrawingFileInfo.Name);
                    return ToolkitOperationResult.DoneWithErrors;
                }
            }

            var cancellationToken =
                progressInfo is not null ? progressInfo.GetCancellationToken() : new CancellationToken();
            var i = 0;
            var count = imageFilesFullNames.Length;
            foreach (string imageFileFullName in imageFilesFullNames)
            {
                i += 1;
                if (cancellationToken.IsCancellationRequested) return ToolkitOperationResult.Cancelled;
                if (progressInfo is not null) await progressInfo.RefreshProgressBarAsync(i, count);

                try
                {
                    var imageFileInfo = new FileInfo(imageFileFullName);
                    string dsPageName = Path.GetFileNameWithoutExtension(imageFileInfo.Name);
                    if (dsProject.AllDsPagesCache.ContainsKey(dsPageName))
                    {
                        if (progressInfo is not null)
                            await progressInfo.DebugInfoAsync(@"Image file skipped, dsPage already exists: " +
                                                              imageFileInfo.Name);
                        DsProject.LoggersSet.Logger.LogWarning(@"Image file skipped, dsPage already exists: " + imageFileInfo.Name);
                        continue;
                    }

                    DsPageDrawing dsPageDrawing = dsProject.NewDsPageDrawingObject(dsPageName, false);

                    Size? contentOriginalSize;
                    dsPageDrawing.UnderlyingXaml.Xaml = XamlHelper.GetXamlWithAbsolutePaths(imageFileInfo,
                        Stretch.Fill,
                        out contentOriginalSize);

                    if (templateDsPageDrawing is not null)
                    {
                        dsPageDrawing.Width = templateDsPageDrawing.Width;
                        dsPageDrawing.Height = templateDsPageDrawing.Height;
                        dsPageDrawing.DsPageTypeGuid = templateDsPageDrawing.DsPageTypeGuid;
                        dsPageDrawing.DsPageTypeObject = templateDsPageDrawing.DsPageTypeObject;
                    }
                    else
                    {
                        dsPageDrawing.Width = DsProject.Instance.DefaultDsPageSize.Width;
                        dsPageDrawing.Height = DsProject.Instance.DefaultDsPageSize.Height;
                    }

                    if (toolkitOperationOptions.TryCreateDsPagesWithSizeOfImage && contentOriginalSize is not null)
                    {
                        dsPageDrawing.Width = contentOriginalSize.Value.Width;
                        dsPageDrawing.Height = contentOriginalSize.Value.Height;
                    }

                    dsProject.SaveUnconditionally(dsPageDrawing, DsProject.IfFileExistsActions.CreateBackup);
                }
                catch (Exception ex)
                {
                    result = ToolkitOperationResult.DoneWithErrors;
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                }
            }

            dsProject.OnDsPageDrawingsListChanged();

            return result;
        }

        public static async Task<ToolkitOperationResult> UpdateDsPagesToolkitOperationAsync(this DsProject dsProject,
            string[] imageFilesFullNames,
            UpdateDsPagesToolkitOperationOptions toolkitOperationOptions, IProgressInfo? progressInfo)
        {
            if (!dsProject.IsInitialized) return ToolkitOperationResult.Cancelled;

            var result = ToolkitOperationResult.Done;

            var cancellationToken =
                progressInfo is not null ? progressInfo.GetCancellationToken() : new CancellationToken();
            var i = 0;
            var count = imageFilesFullNames.Length;
            foreach (string imageFileFullName in imageFilesFullNames)
            {
                i += 1;
                if (cancellationToken.IsCancellationRequested) return ToolkitOperationResult.Cancelled;
                if (progressInfo is not null) await progressInfo.RefreshProgressBarAsync(i, count);

                try
                {
                    var imageFileInfo = new FileInfo(imageFileFullName);
                    var drawingFileInfo = new FileInfo(
                        dsProject.DsPagesDirectoryInfo!.FullName + @"\" +
                        Path.GetFileNameWithoutExtension(imageFileInfo.Name) +
                        DsProject.DsPageFileExtension);

                    if (!drawingFileInfo.Exists)
                    {
                        if (progressInfo is not null)
                            await progressInfo.DebugInfoAsync(@"Image file skipped, dsPage does not exist: " +
                                                              imageFileInfo.Name);
                        DsProject.LoggersSet.Logger.LogWarning(@"Image file skipped, dsPage does not exist: " + imageFileInfo.Name);
                        continue;
                    }

                    var dsPageDrawing = DsProject.ReadDrawing(drawingFileInfo, false, true) as DsPageDrawing;

                    if (dsPageDrawing is null)
                    {
                        if (progressInfo is not null)
                            await progressInfo.DebugInfoAsync(@"Cannot read drawing: " + drawingFileInfo.Name);
                        DsProject.LoggersSet.Logger.LogWarning(@"Cannot read drawing: " + drawingFileInfo.Name);
                        continue;
                    }

                    Size? contentOriginalSize;
                    var underlyingXamlWithAbsolutePaths =
                        XamlHelper.GetXamlWithAbsolutePaths(imageFileInfo, Stretch.Fill,
                            out contentOriginalSize);
                    dsPageDrawing.UnderlyingXaml.Xaml = underlyingXamlWithAbsolutePaths;
                    dsPageDrawing.DeleteUnusedFiles(false);

                    if (toolkitOperationOptions.TryUpdateDsPagesWithSizeOfImage && contentOriginalSize is not null)
                    {
                        dsPageDrawing.Width = contentOriginalSize.Value.Width;
                        dsPageDrawing.Height = contentOriginalSize.Value.Height;
                    }

                    dsProject.SaveUnconditionally(dsPageDrawing, DsProject.IfFileExistsActions.CreateBackup);
                }
                catch (Exception ex)
                {
                    result = ToolkitOperationResult.DoneWithErrors;
                    DsProject.LoggersSet.Logger.LogError(ex, @"");
                }
            }

            return result;
        }

        #endregion
    }
}