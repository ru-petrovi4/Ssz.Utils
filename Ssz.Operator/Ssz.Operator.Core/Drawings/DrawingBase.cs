using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Markup;
using Microsoft.Extensions.Logging;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors.DsConstantsCollection;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.MonitoredUndo;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
using GuidAndName = Ssz.Operator.Core.Utils.GuidAndName;
using OwnedDataSerializableAndCloneable = Ssz.Operator.Core.Utils.OwnedDataSerializableAndCloneable;

namespace Ssz.Operator.Core.Drawings
{
    [DsCategoryOrder(ResourceStrings.BasicCategory, 1)]
    [DsCategoryOrder(ResourceStrings.DataCategory, 2)]
    [DsCategoryOrder(ResourceStrings.AppearanceCategory, 3)]
    [DsCategoryOrder(ResourceStrings.DrawingCategory, 4)]
    [DsCategoryOrder(ResourceStrings.DsPageTypeCategory, 5)]
    [ContentProperty(@"DsShapesArray")] // For XAML serialization. Content property must be of type object or string.
    public abstract partial class DrawingBase : OwnedDataSerializableAndCloneable,
        INotifyPropertyChanged,
        IDsContainer,
        ISupportsUndo,
        IUsedAddonsInfo, IDisposable
    {
        #region construction and destruction

        protected DrawingBase(bool visualDesignMode, bool loadXamlContent)
        {
            VisualDesignMode = visualDesignMode;
            LoadXamlContent = loadXamlContent;

            Guid = Guid.NewGuid();
            _width = 800;
            _height = 600;
            _mark = 0;

            Settings = new Settings();

            ActuallyUsedAddonsInfo = new List<GuidAndName>();

            _dsShapes.CollectionChanged += DsShapesCollectionChanged;
            SystemDsShapes.CollectionChanged += DsShapesCollectionChanged;

            if (VisualDesignMode)
                DsConstantsCollection.CollectionChanged += DsConstantsCollectionChanged;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _dsShapes.CollectionChanged -= DsShapesCollectionChanged;
                SystemDsShapes.CollectionChanged -= DsShapesCollectionChanged;

                if (VisualDesignMode)
                    DsConstantsCollection.CollectionChanged -= DsConstantsCollectionChanged;

                foreach (DsShapeBase dsShape in _dsShapes) dsShape.Dispose();

                foreach (DsShapeBase dsShape in SystemDsShapes) dsShape.Dispose();

                _dsShapes.Clear();
                SystemDsShapes.Clear();

                ParentItem = null;
            }

            Disposed = true;
        }

        ~DrawingBase()
        {
            Dispose(false);
        }

        protected bool Disposed { get; private set; }

        #endregion

        public event PropertyChangedEventHandler? PropertyChanged;

        #region public functions

        public static void VerifyDsShapeName(DsShapeBase newDsShape, IList<DsShapeBase> existingDsShapes)
        {
            if (string.IsNullOrWhiteSpace(newDsShape.Name))
                newDsShape.Name = newDsShape.GetDsShapeTypeNameToDisplay();

            var exists = false;
            foreach (DsShapeBase dsShape in existingDsShapes)
                if (StringHelper.CompareIgnoreCase(dsShape.Name, newDsShape.Name))
                {
                    exists = true;
                    break;
                }

            if (exists)
            {
                int newDsShapeNameNumber;
                string newDsShapeNameBase = newDsShape.GetDsShapeNameBase(out newDsShapeNameNumber);
                var maxNumber = 1;
                foreach (DsShapeBase dsShape in existingDsShapes)
                {
                    int dsShapeNameNumber;
                    if (StringHelper.CompareIgnoreCase(dsShape.GetDsShapeNameBase(out dsShapeNameNumber),
                        newDsShapeNameBase))
                        if (dsShapeNameNumber > maxNumber)
                            maxNumber = dsShapeNameNumber;
                }

                maxNumber = Math.Max(maxNumber, newDsShapeNameNumber);
                maxNumber += 1;
                newDsShape.Name = DsShapeBase.GetDsShapeName(newDsShapeNameBase, maxNumber);
            }
        }

        public static string GetDrawingFilesDirectoryFullName(string drawingFileFullName)
        {
            if (string.IsNullOrEmpty(drawingFileFullName)) return "";
            return drawingFileFullName + @"_files";
        }

        public static bool IsDsPageFile(FileInfo fileInfo)
        {
            if (fileInfo is null) return false;
            return StringHelper.CompareIgnoreCase(fileInfo.Extension, DsProject.DsPageFileExtension);
        }

        public static bool IsDsShapeFile(FileInfo fileInfo)
        {
            if (fileInfo is null) return false;
            return StringHelper.CompareIgnoreCase(fileInfo.Extension, DsProject.DsShapeFileExtension);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public abstract bool IsFaceplate { get; }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DrawingGuid)]
        [ReadOnlyInEditor]
        [PropertyOrder(0)]
        public Guid Guid { get; set; }

        public virtual string Name
        {
            get => _name;
            set
            {
                if (Equals(value, _name)) return;

                _name = value;

                if (!string.IsNullOrEmpty(_fileFullName) && !string.IsNullOrEmpty(_name))
                    _fileFullName = Path.GetDirectoryName(_fileFullName) + @"\" + _name + DsProject.DsPageFileExtension;
            }
        }

        public virtual string Desc
        {
            get => _desc;
            set
            {
                if (SetValue(ref _desc, value)) OnDrawingHeaderChanged();
            }
        }

        public virtual string Group
        {
            get => _group;
            set
            {
                if (SetValue(ref _group, value)) OnDrawingPositionInTreeChanged();
            }
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DrawingWidth)]
        [PropertyOrder(4)]
        public virtual double Width
        {
            get => _width;
            set
            {
                if (value < 1) value = 1;
                else if (value > 0xFFFF) value = 0xFFFF;
                value = Math.Round(value, 1);
                SetValue(ref _width, value);
            }
        }

        [DsCategory(ResourceStrings.DrawingCategory)]
        [DsDisplayName(ResourceStrings.DrawingHeight)]
        [PropertyOrder(5)]
        public virtual double Height
        {
            get => _height;
            set
            {
                if (value < 1) value = 1;
                else if (value > 0xFFFF) value = 0xFFFF;
                value = Math.Round(value, 1);
                SetValue(ref _height, value);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public List<GuidAndName> ActuallyUsedAddonsInfo { get; private set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public DsShapeBase[] DsShapes
        {
            get => _dsShapes.ToArray();
            set
            {
                DsShapeBase[] dsShapesToRemove = _dsShapes.ToArray();
                if (dsShapesToRemove.Length != 0)
                {
                    for (var i = dsShapesToRemove.Length - 1; i >= 0; i--) _dsShapes.RemoveAt(i);

                    if (DsShapesRemoved is not null) DsShapesRemoved(dsShapesToRemove);
                }

                if (value.Length == 0) return;

                AddDsShapes(null, false, value);
            }
        }

        [Browsable(false)]
        public object? DsShapesArray
        {
            get => new ArrayList(_dsShapes);
            set
            {
                if (value is null) DsShapes = new DsShapeBase[0];
                else DsShapes = ((ArrayList) value).OfType<DsShapeBase>().ToArray();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public int DsShapesCount => _dsShapes.Count;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [field: Searchable(false)] // For XAML serialization
        public ObservableCollection<DsShapeBase> SystemDsShapes { get; } = new();

        [Browsable(false)]
        public int Mark
        {
            get => _mark;
            set
            {
                if (SetValue(ref _mark, value)) OnDrawingHeaderChanged();
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string FileFullName
        {
            get => _fileFullName;
            set
            {
                if (Equals(value, _fileFullName)) return;

                _fileFullName = value;

                _name = Path.GetFileNameWithoutExtension(_fileFullName) ?? "";

                OnDrawingPositionInTreeChanged();
            }
        }

        [Searchable(false)]
        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DrawingFilesDirectoryFullName)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string DrawingFilesDirectoryFullName => GetDrawingFilesDirectoryFullName(_fileFullName);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool DataChangedFromLastSave
        {
            get
            {
                if (!VisualDesignMode)
                    throw new Exception(
                        "Drawing is not in VisualDesignMode. Set VisualDesignMode=True while drawing creation to use this feature.");
                return _dataChangedFromLastSave.HasValue ? _dataChangedFromLastSave.Value : false;
            }
            private set
            {
                if (_dataChangedFromLastSave.HasValue && value == _dataChangedFromLastSave.Value) return;
                _dataChangedFromLastSave = value;
                if (DataChangedFromLastSaveEvent is not null) DataChangedFromLastSaveEvent();
            }
        }

        [DsCategory(ResourceStrings.DataCategory)]
        [DsDisplayName(ResourceStrings.DrawingDsConstantsCollection)]
        [LocalizedDescription(ResourceStrings.DsConstantsCollectionDescription)]
        [Editor(typeof(CollectionWithAddRemoveTypeEditor),
            typeof(CollectionWithAddRemoveTypeEditor))]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        [field: Searchable(false)]
        // For XAML serialization of collections
        public ObservableCollection<DsConstant> DsConstantsCollection { get; } = new();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public virtual DsConstant[]? HiddenDsConstantsCollection => null;

        [Browsable(false)]
        [DefaultValue(typeof(Settings), @"")] // For XAML serialization
        public Settings Settings { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool VisualDesignMode { get; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool LoadXamlContent { get; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public byte[]? PreviewImageBytes { get; protected set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)] 
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public IPlayWindowBase? PlayWindow { get; set; }

        [Browsable(false)] 
        public DateTime SerializationVersionDateTime { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public long BinDeserializationSkippedBytesCount { get; private set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool RefreshForPropertyGridIsDisabled { get; set; }        

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(this);
        }

        public void EndEditInPropertyGrid()
        {
        }

        public event Action? DrawingPositionInTreeChanged;

        public event Action<DrawingBase>? DrawingHeaderChanged;

        public event Action? DataChangedFromLastSaveEvent;

        public event Action? BeforeDrawingSaveEvent;

        public event Action<IEnumerable<DsShapeBase>>? DsShapesAdded;

        public event Action<IEnumerable<DsShapeBase>>? DsShapesRemoved;

        public event Action? DsShapesReodered;

        public void CheckDataChangedFromLastSave()
        {
            if (_dataChangedFromLastSave.HasValue && _dataChangedFromLastSave.Value) return;
            if (_fullBytes is null) return;
            byte[] newFullBytes = GetBytes(true);
            DataChangedFromLastSave = !BytesArrayEqualityComparer.Instance.Equals(_fullBytes, newFullBytes);
        }

        public void BeforeDrawingSave()
        {
            var beforeDrawingSaveEvent = BeforeDrawingSaveEvent;
            if (beforeDrawingSaveEvent is not null) beforeDrawingSaveEvent();

            ActuallyUsedAddonsInfo = AddonsManager.GetAddonsInfo(GetUsedAddonGuids());
            SerializationVersionDateTime = CurrentSerializationVersionDateTime;
        }

        public void AddDsShapes(int? atIndex, bool verifyDsShapeNames, params DsShapeBase[] newDsShapes)
        {
            if (atIndex.HasValue)
                foreach (DsShapeBase newDsShape in newDsShapes.Reverse())
                {
                    if (verifyDsShapeNames) VerifyDsShapeName(newDsShape, _dsShapes);

                    _dsShapes.Insert(atIndex.Value, newDsShape);
                }
            else
                foreach (DsShapeBase newDsShape in newDsShapes)
                {
                    if (verifyDsShapeNames) VerifyDsShapeName(newDsShape, _dsShapes);

                    _dsShapes.Add(newDsShape);
                }

            this.SetDsShapesIndexes();

            if (DsShapesAdded is not null) DsShapesAdded(newDsShapes);
        }

        public void RemoveDsShapes(params DsShapeBase[] dsShapes)
        {
            if (dsShapes.Length == 0) return;

            IOrderedEnumerable<int> indexesToRemove =
                dsShapes.Select(s => s.Index).Distinct().OrderByDescending(i => i);
            foreach (var index in indexesToRemove) _dsShapes.RemoveAt(index);

            this.SetDsShapesIndexes();

            if (DsShapesRemoved is not null) DsShapesRemoved(dsShapes);
        }

        public void ReorderDsShape(int oldIndex, int newIndex)
        {
            _dsShapes.Move(oldIndex, newIndex);

            this.SetDsShapesIndexes();

            if (DsShapesReodered is not null) DsShapesReodered();
        }

        public void SetUndoRoot(object? undoRoot)
        {
            _undoRoot = undoRoot;
        }

        public object? GetUndoRoot()
        {
            return _undoRoot;
        }

        public virtual void FindConstants(HashSet<string> constants)
        {
            // _dsShapes and _dsConstantsCollection have attribute [Searchable(false)]
            ConstantsHelper.FindConstantsInFields(this, constants);

            ConstantsHelper.FindConstants(DsConstantsCollection, constants);

            foreach (DsShapeBase dsShape in _dsShapes)
            {
                var complexDsShape = dsShape as ComplexDsShape;
                if (complexDsShape is not null)
                {
                    ConstantsHelper.FindConstants(complexDsShape.DsConstantsCollection, constants);
                    continue;
                }

                dsShape.FindConstants(constants);
            }
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            foreach (DsShapeBase dsShape in DsShapes) dsShape.ReplaceConstants(this);
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.OnPropertyGridRefreshInFields(this, container);
        }

        public virtual void ResizeHorizontalFromLeft(double horizontalChange)
        {
            foreach (DsShapeBase dsShape in _dsShapes.Concat(SystemDsShapes))
            {
                var point = dsShape.CenterInitialPositionNotRounded;

                point.X = point.X - horizontalChange;

                dsShape.CenterInitialPosition = point;
            }

            SetValue(ref _width, _width - horizontalChange, @"Width");
        }

        public virtual void ResizeVerticalFromTop(double verticalChange)
        {
            foreach (DsShapeBase dsShape in _dsShapes.Concat(SystemDsShapes))
            {
                var point = dsShape.CenterInitialPositionNotRounded;

                point.Y = point.Y - verticalChange;

                dsShape.CenterInitialPosition = point;
            }

            SetValue(ref _height, _height - verticalChange, @"Height");
        }

        public virtual void ResizeHorizontalFromRight(double horizontalChange)
        {
            SetValue(ref _width, _width + horizontalChange, @"Width");
        }

        public virtual void ResizeVerticalFromBottom(double verticalChange)
        {
            SetValue(ref _height, _height + verticalChange, @"Height");
        }

        public Rect GetBoundingRectOfAllDsShapes()
        {
            return DsShapeBase.GetBoundingRect(_dsShapes);
        }

        public virtual void DeleteUnusedFiles(bool showDebugWindow, HashSet<string>? exceptFileNames = null)
        {
            try
            {
                var existingFiles = new CaseInsensitiveOrderedDictionary<FileInfo>();
                if (Directory.Exists(DrawingFilesDirectoryFullName))
                    foreach (var f in Directory.GetFiles(DrawingFilesDirectoryFullName))
                    {
                        var fi = new FileInfo(f);
                        existingFiles.Add(fi.Name, fi);
                    }

                var usedFiles = new CaseInsensitiveHashSet();
                var dsPageDrawing = this as DsPageDrawing;
                if (dsPageDrawing is not null)
                    XamlHelper.GetUsedFileNames(dsPageDrawing.UnderlyingXaml.XamlWithRelativePaths, usedFiles);
                foreach (DsShapeBase dsShape in _dsShapes) dsShape.GetUsedFileNames(usedFiles);
                if (exceptFileNames is not null)
                    foreach (var exceptFileName in exceptFileNames)
                        usedFiles.Add(exceptFileName);

                foreach (string usedFile in usedFiles)
                {
                    if (string.IsNullOrEmpty(usedFile)) continue;
                    if (showDebugWindow)
                        if (!existingFiles.ContainsKey(usedFile))
                            DsProject.LoggersSet.UserFriendlyLogger.LogInformation(Name + DsProject.DsPageFileExtension + ": " + Resources.MissingFile + " '" +
                                                         usedFile + "'");
                    existingFiles.Remove(usedFile);
                }

                foreach (FileInfo fileInfo in existingFiles.Values) // TODO
                    try
                    {
                        fileInfo.Delete();
                    }
                    catch (Exception ex)
                    {
                        DsProject.LoggersSet.Logger.LogWarning(ex, @"");
                    }
            }
            catch (Exception ex)
            {
                DsProject.LoggersSet.Logger.LogError(ex, @"");
            }

            /*
            UnderlyingXaml = XamlHelper.OptimizeFileNames(UnderlyingXaml, FilesDir);
            foreach (var dsShape in _dsShapes)
            {
                dsShape.OptimizeFileNames();
            }*/
        }

        public void GetDsConstants(CaseInsensitiveOrderedDictionary<List<ExtendedDsConstant>> dsConstantsDictionary)
        {
            foreach (DsShapeBase dsShape in DsShapes)
            {
                dsShape.GetDsConstants(dsConstantsDictionary);
            }
        }

        public IEnumerable<Guid> GetUsedAddonGuids()
        {
            var addonGuid = AddonsManager.GetAddonGuidFromDsPageType(Guid);
            if (addonGuid != Guid.Empty) yield return addonGuid;

            foreach (DsShapeBase dsShape in _dsShapes)
            foreach (var guid in dsShape.GetUsedAddonGuids())
                yield return guid;
        }

        public abstract byte[] GetBytes(bool full);

        public void SetDataUnchanged()
        {
            if (!VisualDesignMode) return;

            _fullBytes = GetBytes(true);

            DataChangedFromLastSave = false;
        }

        public void SetDataChanged()
        {
            if (!VisualDesignMode) return;

            DataChangedFromLastSave = true;
        }

        public void SetDataChangedUnknown()
        {
            if (!VisualDesignMode) return;

            _dataChangedFromLastSave = null;
        }

        public abstract DrawingInfo GetDrawingInfo();

        public override bool Equals(object? obj)
        {
            var other = obj as DrawingBase;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return BytesArrayEqualityComparer.Instance.Equals(GetBytes(true), other.GetBytes(true));
        }

        public override string ToString()
        {   
            return Name;
        }

        #endregion

        #region internal functions

        internal virtual void SetDrawingInfo(DrawingInfo drawingInfo)
        {
            FileFullName = drawingInfo.FileInfo.FullName;
            SerializationVersionDateTime = drawingInfo.SerializationVersionDateTime;
            ActuallyUsedAddonsInfo = drawingInfo.ActuallyUsedAddonsInfo;
            Guid = drawingInfo.Guid;

            DsConstantsCollection.Clear();
            foreach (DsConstant dsConstant in drawingInfo.DsConstantsCollection) 
                DsConstantsCollection.Add(dsConstant);
            
            Desc = drawingInfo.Desc;
            Group = drawingInfo.Group;
            PreviewImageBytes = drawingInfo.PreviewImageBytes;
            Mark = drawingInfo.Mark;
        }

        #endregion

        #region protected functions

#if XP
        protected bool SetValue<T>(ref T field, T value)
        {
            string propertyName = (new StackTrace()).GetFrame(1).GetMethod().Name.Substring(4);
#else
        protected bool SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
#endif
            if (Equals(value, field)) return false;
            if (VisualDesignMode) DefaultChangeFactory.Instance.OnChanging(this, propertyName, field, value);
            var item = field as IDsItem;
            if (item is not null) item.ParentItem = null;
            field = value;
            item = field as IDsItem;
            if (item is not null) item.ParentItem = this;
            OnPropertyChanged(propertyName);
            SetDataChanged();
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged is not null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

#if XP
        protected void OnPropertyChangedAuto()
        {
            string propertyName = (new StackTrace()).GetFrame(1).GetMethod().Name.Substring(4);
#else
        protected void OnPropertyChangedAuto([CallerMemberName] string propertyName = "")
        {
#endif
            if (PropertyChanged is not null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }


        protected void OnDrawingPositionInTreeChanged()
        {
            SetDataChanged();

            var drawingPositionInTreeChanged = DrawingPositionInTreeChanged;
            if (drawingPositionInTreeChanged is not null) drawingPositionInTreeChanged();
        }


        protected void OnDrawingHeaderChanged()
        {
            var drawingHeaderChanged = DrawingHeaderChanged;
            if (drawingHeaderChanged is not null) drawingHeaderChanged(this);
        }

        #endregion

        #region private functions

        private void DsShapesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            var isUndoingOrRedoing = false;
            if (VisualDesignMode)
                isUndoingOrRedoing =
                    DefaultChangeFactory.Instance.OnCollectionChanged(this, "DsShapes", _dsShapes, e);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    DsShapeBase[] addedDsShapes = (e.NewItems ?? throw new InvalidOperationException())
                        .OfType<DsShapeBase>().ToArray();
                    if (VisualDesignMode)
                    {
                        foreach (DsShapeBase addedDsShape in addedDsShapes)
                            if (!ReferenceEquals(addedDsShape.ParentItem, this))
                                addedDsShape.ParentItem = this;
                    }
                    else
                    {
                        foreach (DsShapeBase addedDsShape in addedDsShapes) addedDsShape.ParentItem = this;
                    }

                    if (isUndoingOrRedoing)
                    {
                        this.SetDsShapesIndexes();
                        if (DsShapesAdded is not null) DsShapesAdded(addedDsShapes);
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    DsShapeBase[] removedDsShapes = (e.OldItems ?? throw new InvalidOperationException())
                        .OfType<DsShapeBase>().ToArray();
                    foreach (DsShapeBase removedDsShape in removedDsShapes) removedDsShape.ParentItem = null;
                    if (isUndoingOrRedoing)
                    {
                        this.SetDsShapesIndexes();
                        if (DsShapesRemoved is not null) DsShapesRemoved(removedDsShapes);
                    }

                    break;
                case NotifyCollectionChangedAction.Move:
                    if (isUndoingOrRedoing)
                    {
                        this.SetDsShapesIndexes();
                        if (DsShapesReodered is not null) DsShapesReodered();
                    }

                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        private void DsConstantsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, @"DsConstantsCollection",
                DsConstantsCollection, e);
        }

        #endregion

        #region private fields

        private string _desc = @"";
        private string _group = @"";
        private double _width;
        private double _height;
        private int _mark;

        private string _name = @"";
        private string _fileFullName = @"";
        private bool? _dataChangedFromLastSave;
        private byte[]? _fullBytes;

        [Searchable(false)] 
        private object? _undoRoot;

        [Searchable(false)] 
        private readonly ObservableCollection<DsShapeBase> _dsShapes = new();        

        #endregion
    }
}