using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Media;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsCommon.Converters;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Properties;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using Ssz.Utils.MonitoredUndo;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    //[DsCategoryOrder(ResourceStrings.BasicCategory, 1)]
    //[DsCategoryOrder(ResourceStrings.PrototypeDsShapeDrawingCategory, 2)]
    //[DsCategoryOrder(ResourceStrings.MainCategory, 3)]
    //[DsCategoryOrder(ResourceStrings.DataCategory, 4)]
    //[DsCategoryOrder(ResourceStrings.AppearanceCategory, 5)]
    //[DsCategoryOrder(ResourceStrings.BehaviourCategory, 6)]
    //[DsCategoryOrder(ResourceStrings.ToolTipCategory, 7)]
    //[DsCategoryOrder(ResourceStrings.GeometryCategory, 8)]
    //[DsCategoryOrder(ResourceStrings.GeometryAdvancedCategory, 9)]
    //[DsCategoryOrder(ResourceStrings.Geometry3DCategory, 10)]
    public abstract class DsShapeBase : IOwnedDataSerializable,
        INotifyPropertyChanged,
        IDsItem, ISupportsUndo,
        IUsedAddonsInfo,
        ICloneable
    {
        #region construction and destruction

        protected DsShapeBase(bool visualDesignMode, bool loadXamlContent)
        {
            VisualDesignMode = visualDesignMode;
            LoadXamlContent = loadXamlContent;

            CenterInitialPosition = new Point(0, 0);
            CenterDeltaPositionXInfo = new DoubleDataBinding(visualDesignMode, loadXamlContent) {ConstValue = 0.0};
            CenterDeltaPositionYInfo = new DoubleDataBinding(visualDesignMode, loadXamlContent) {ConstValue = 0.0};
            CenterFinalPosition = new Point(0, 0);
            CenterRelativePosition = new Point(0.5, 0.5);
            ResizeMode = DsShapeResizeMode.WidthAndHeight;
            WidthInitial = 20;
            WidthDeltaInfo = new DoubleDataBinding(visualDesignMode, loadXamlContent) {ConstValue = 0.0};
            WidthFinal = 0;
            HeightInitial = 20;
            HeightDeltaInfo = new DoubleDataBinding(visualDesignMode, loadXamlContent) {ConstValue = 0.0};
            HeightFinal = 0;
            AngleInitial = 0.0;
            AngleDeltaInfo = new DoubleDataBinding(visualDesignMode, loadXamlContent) {ConstValue = 0.0};
            AngleFinal = 0.0;
            IsFlipped = false;
            IsVisibleInfo = new BooleanDataBinding(visualDesignMode, loadXamlContent) {ConstValue = true};
            IsEnabledInfo = new BooleanDataBinding(visualDesignMode, loadXamlContent) {ConstValue = true};
            IsLocked = false;
            OpacityInfo = new DoubleDataBinding(visualDesignMode, loadXamlContent) {ConstValue = 1.0};
            RotationX = 0.0;
            RotationY = 0.0;
            RotationZ = 0.0;
            FieldOfView = 45.0;
        }

        protected DsShapeBase(bool isEmpty)
        {
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
                foreach (FieldInfo field in ObjectHelper.GetAllFields(this))
                    if (typeof(IDisposable).IsAssignableFrom(field.FieldType))
                    {
                        var disposable = field.GetValue(this) as IDisposable;
                        if (disposable is not null) disposable.Dispose();
                    }

                PropertyChanged = delegate { };

                ParentItem = null;
            }

            Disposed = true;
        }


        ~DsShapeBase()
        {
            Dispose(false);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool Disposed { get; private set; }

        #endregion

        #region public functions

        public const double MinWidth = 1;
        public const double MinHeight = 1;

        public override string ToString()
        {
            return GetDsShapeTypeNameToDisplay();
        }

        public abstract string GetDsShapeTypeNameToDisplay();

        public abstract Guid GetDsShapeTypeGuid();

        public virtual void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            if (ReferenceEquals(context, SerializationContext.IndexFile)) return;

            if (ReferenceEquals(context, SerializationContext.ShortBytes))
            {
                var t = 5.0;
                var notTransformedRect = GetNotTransformedRect();
                writer.Write((int) (Math.Round(notTransformedRect.Right / t, 0) * t));
                writer.Write((int) (Math.Round(notTransformedRect.Bottom / t, 0) * t));
                writer.Write((int) (Math.Round(notTransformedRect.Width / t, 0) * t));
                writer.Write((int) (Math.Round(notTransformedRect.Height / t, 0) * t));
                if (!CenterDeltaPositionXInfo.IsConst)
                {
                    writer.Write(CenterDeltaPositionXInfo, context);
                    writer.Write((int) (Math.Round(CenterFinalPositionNotRounded.X / t, 0) * t));
                }

                if (!CenterDeltaPositionYInfo.IsConst)
                {
                    writer.Write(CenterDeltaPositionYInfo, context);
                    writer.Write((int) (Math.Round(CenterFinalPositionNotRounded.Y / t, 0) * t));
                }

                writer.Write((int) (Math.Round(CenterRelativePosition.X, 2) * 100));
                writer.Write((int) (Math.Round(CenterRelativePosition.Y, 2) * 100));
                writer.Write((int) ResizeMode);
                if (!WidthDeltaInfo.IsConst)
                {
                    writer.Write(WidthDeltaInfo, context);
                    writer.Write((int) (Math.Round(WidthFinalNotRounded / t, 0) * t));
                }

                if (!HeightDeltaInfo.IsConst)
                {
                    writer.Write(HeightDeltaInfo, context);
                    writer.Write((int) (Math.Round(HeightFinalNotRounded / t, 0) * t));
                }

                writer.Write((int) (Math.Round(AngleInitialNotRounded / t, 0) * t));
                if (!AngleDeltaInfo.IsConst)
                {
                    writer.Write(AngleDeltaInfo, context);
                    writer.Write((int) (Math.Round(AngleFinalNotRounded / t, 0) * t));
                }

                writer.Write(IsFlipped);
                writer.Write((int) (Math.Round(RotationX / t, 0) * t));
                writer.Write((int) (Math.Round(RotationY / t, 0) * t));
                writer.Write((int) (Math.Round(RotationZ / t, 0) * t));
                writer.Write((int) (Math.Round(FieldOfView / t, 0) * t));
                writer.Write(IsVisibleInfo, context);
                writer.Write(IsEnabledInfo, context);
                writer.Write(IsLocked);
                writer.Write(OpacityInfo, context);
                writer.Write(Settings, context);
                return;
            }

            using (writer.EnterBlock(4))
            {
                writer.Write(Name);
                writer.Write(Desc);
                writer.Write(CenterInitialPositionNotRounded);
                writer.Write(CenterDeltaPositionXInfo, context);
                writer.Write(CenterDeltaPositionYInfo, context);
                writer.Write(CenterFinalPositionNotRounded);
                writer.Write(CenterRelativePosition);
                writer.Write((int) ResizeMode);
                writer.Write(WidthInitialNotRounded);
                writer.Write(WidthDeltaInfo, context);
                writer.Write(WidthFinalNotRounded);
                writer.Write(HeightInitialNotRounded);
                writer.Write(HeightDeltaInfo, context);
                writer.Write(HeightFinalNotRounded);
                writer.Write(AngleInitialNotRounded);
                writer.Write(AngleDeltaInfo, context);
                writer.Write(AngleFinalNotRounded);
                writer.Write(IsFlipped);
                writer.Write(RotationX);
                writer.Write(RotationY);
                writer.Write(RotationZ);
                writer.Write(FieldOfView);
                writer.Write(IsVisibleInfo, context);
                writer.Write(IsEnabledInfo, context);
                writer.Write(IsLocked);
                writer.Write(OpacityInfo, context);
                writer.Write(Settings, context);
            }
        }

        public virtual void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            if (ReferenceEquals(context, SerializationContext.IndexFile)) return;

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 4:
                        Name = reader.ReadString();
                        Desc = reader.ReadString();
                        CenterInitialPosition = reader.ReadPoint();
                        reader.ReadOwnedData(CenterDeltaPositionXInfo, context);
                        reader.ReadOwnedData(CenterDeltaPositionYInfo, context);
                        CenterFinalPosition = reader.ReadPoint();
                        CenterRelativePosition = reader.ReadPoint();
                        ResizeMode = (DsShapeResizeMode) reader.ReadInt32();
                        WidthInitial = reader.ReadDouble();
                        reader.ReadOwnedData(WidthDeltaInfo, context);
                        WidthFinal = reader.ReadDouble();
                        HeightInitial = reader.ReadDouble();
                        reader.ReadOwnedData(HeightDeltaInfo, context);
                        HeightFinal = reader.ReadDouble();
                        AngleInitial = reader.ReadDouble();
                        reader.ReadOwnedData(AngleDeltaInfo, context);
                        AngleFinal = reader.ReadDouble();
                        IsFlipped = reader.ReadBoolean();
                        RotationX = reader.ReadDouble();
                        RotationY = reader.ReadDouble();
                        RotationZ = reader.ReadDouble();
                        FieldOfView = reader.ReadDouble();
                        reader.ReadOwnedData(IsVisibleInfo, context);
                        reader.ReadOwnedData(IsEnabledInfo, context);
                        IsLocked = reader.ReadBoolean();
                        reader.ReadOwnedData(OpacityInfo, context);
                        reader.ReadOwnedData(Settings, context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public virtual void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstantsInFields(this, constants);
        }

        public virtual void ReplaceConstants(IDsContainer? container)
        {
            IEnumerable<FieldInfo> fields = ObjectHelper.GetAllFields(this);
            foreach (FieldInfo field in fields)
                if (typeof(IDsItem).IsAssignableFrom(field.FieldType))
                    ItemHelper.ReplaceConstants(field.GetValue(this) as IDsItem, container);
        }

        public virtual void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.OnPropertyGridRefreshInFields(this, container);
        }

        public virtual void GetUsedFileNames(HashSet<string> usedFileNames)
        {
            IEnumerable<FieldInfo> fields = ObjectHelper.GetAllFields(this);
            foreach (FieldInfo field in fields)
                if (typeof(XamlDataBinding).IsAssignableFrom(field.FieldType))
                {
                    var xamlDataBinding = field.GetValue(this) as XamlDataBinding;
                    if (xamlDataBinding is not null) xamlDataBinding.GetUsedFileNames(usedFileNames);
                }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsShapeName)]
        //[PropertyOrder(1)]
        public string Name
        {
            get => _name;
            set
            {
                value = value.Replace('/', '_');
                SetValue(ref _name, value);
            }
        }

        /*
        [DsCategory(ResourceStrings.BasicCategory), DsDisplayName(ResourceStrings.DsShapeDesc)]
        //[PropertyOrder(2)]*/
        [Browsable(false)]
        [DefaultValue(@"")] // For XAML serialization
        public string Desc
        {
            get => _desc;
            set => SetValue(ref _desc, value);
        }

        [DsCategory(ResourceStrings.GeometryCategory)]
        [DsDisplayName(ResourceStrings.DsShapeCenterInitialPosition)]
        //[PropertyOrder(0)]
        public Point CenterInitialPosition
        {
            get => new(Math.Round(_centerInitialPosition.X, 1), Math.Round(_centerInitialPosition.Y, 1));
            set
            {
                ResetGeometryCache();

                if (SetValue(ref _centerInitialPosition, value)) 
                    OnPropertyChanged(nameof(CenterInitialPositionAdvanced));
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public Point CenterInitialPositionNotRounded => _centerInitialPosition;

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeCenterInitialPositionAdvanced)]
        //[PropertyOrder(1)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public Point CenterInitialPositionAdvanced
        {
            get => CenterInitialPosition;
            set => CenterInitialPosition = value;
        }

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeCenterDeltaPositionXInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(2)]
        [DefaultValue(typeof(DoubleDataBinding), "0.0")] // For XAML serialization
        public DoubleDataBinding CenterDeltaPositionXInfo
        {
            get => _centerDeltaPositionXInfo;
            set => SetValue(ref _centerDeltaPositionXInfo, value);
        }

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeCenterDeltaPositionYInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(3)]
        [DefaultValue(typeof(DoubleDataBinding), "0.0")] // For XAML serialization
        public DoubleDataBinding CenterDeltaPositionYInfo
        {
            get => _centerDeltaPositionYInfo;
            set => SetValue(ref _centerDeltaPositionYInfo, value);
        }

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeCenterFinalPosition)]
        //[PropertyOrder(4)]
        [DefaultValue(typeof(Point), "0, 0")] // For XAML serialization
        public Point CenterFinalPosition
        {
            get => new(Math.Round(_centerFinalPosition.X, 1), Math.Round(_centerFinalPosition.Y, 1));
            set => SetValue(ref _centerFinalPosition, value);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public Point CenterFinalPositionNotRounded => _centerFinalPosition;

        [DsCategory(ResourceStrings.GeometryCategory)]
        [DsDisplayName(ResourceStrings.DsShapeCenterRelativePosition)]
        //[PropertyOrder(1)]
        public Point CenterRelativePosition
        {
            get => _centerRelativePosition;
            set
            {
                ResetGeometryCache();

                SetValue(ref _centerRelativePosition, new Point(Math.Round(value.X, 6), Math.Round(value.Y, 6)));
            }
        }

        [DsCategory(ResourceStrings.GeometryCategory)]
        [DsDisplayName(ResourceStrings.DsShape_ResizeMode)]
        [LocalizedDescription(ResourceStrings.DsShape_ResizeModeDescription)]
        //[PropertyOrder(2)]
        public DsShapeResizeMode ResizeMode
        {
            get => _resizeMode;
            set => SetValue(ref _resizeMode, value);
        }

        [DsCategory(ResourceStrings.GeometryCategory)]
        [DsDisplayName(ResourceStrings.DsShapeWidthInitial)]
        //[PropertyOrder(3)]
        public virtual double WidthInitial
        {
            get => Math.Round(_widthInitial, 1);
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException();

                ResetGeometryCache();

                if (SetValue(ref _widthInitial, value)) 
                    OnPropertyChanged(nameof(WidthInitialAdvanced));
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double WidthInitialNotRounded => _widthInitial;

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeWidthInitialAdvanced)]
        //[PropertyOrder(5)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double WidthInitialAdvanced
        {
            get => WidthInitial;
            set => WidthInitial = value;
        }

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeWidthDeltaInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(6)]
        [DefaultValue(typeof(DoubleDataBinding), "0.0")] // For XAML serialization
        public DoubleDataBinding WidthDeltaInfo
        {
            get => _widthDeltaInfo;
            set => SetValue(ref _widthDeltaInfo, value);
        }

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeWidthFinal)]
        //[PropertyOrder(7)]
        [DefaultValue(0.0)] // For XAML serialization
        public double WidthFinal
        {
            get => Math.Round(_widthFinal, 1);
            set
            {
                if (value < 0) value = 0;

                SetValue(ref _widthFinal, value);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double WidthFinalNotRounded => _widthFinal;

        [DsCategory(ResourceStrings.GeometryCategory)]
        [DsDisplayName(ResourceStrings.DsShapeHeightInitial)]
        //[PropertyOrder(4)]
        public virtual double HeightInitial
        {
            get => Math.Round(_heightInitial, 1);
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException();

                ResetGeometryCache();

                if (SetValue(ref _heightInitial, value)) 
                    OnPropertyChanged(nameof(HeightInitialAdvanced));
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double HeightInitialNotRounded => _heightInitial;

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeHeightInitialAdvanced)]
        //[PropertyOrder(8)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double HeightInitialAdvanced
        {
            get => HeightInitial;
            set => HeightInitial = value;
        }

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeHeightDeltaInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(9)]
        [DefaultValue(typeof(DoubleDataBinding), "0.0")] // For XAML serialization
        public DoubleDataBinding HeightDeltaInfo
        {
            get => _heightDeltaInfo;
            set => SetValue(ref _heightDeltaInfo, value);
        }

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeHeightFinal)]
        //[PropertyOrder(10)]
        [DefaultValue(0.0)] // For XAML serialization
        public double HeightFinal
        {
            get => Math.Round(_heightFinal, 1);
            set
            {
                if (value < 0) value = 0;

                SetValue(ref _heightFinal, value);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double HeightFinalNotRounded => _heightFinal;

        public double GetMinDeltaWidth()
        {
            return MinWidth - _widthInitial;
        }

        public double GetMinDeltaHeight()
        {
            return MinHeight - _heightInitial;
        }

        [DsCategory(ResourceStrings.GeometryCategory)]
        [DsDisplayName(ResourceStrings.DsShapeAngleInitial)]
        //[PropertyOrder(5)]
        [DefaultValue(0.0)] // For XAML serialization
        public double AngleInitial
        {
            get => Math.Round(_angleInitial, 1);
            set
            {
                if (value <= -360.0 || value >= 360.0) value = value % 360.0;

                ResetGeometryCache();

                if (SetValue(ref _angleInitial, value)) 
                    OnPropertyChanged(nameof(AngleInitialAdvanced));
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double AngleInitialNotRounded => _angleInitial;

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeAngleInitialAdvanced)]
        //[PropertyOrder(11)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double AngleInitialAdvanced
        {
            get => AngleInitial;
            set => AngleInitial = value;
        }

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeAngleDeltaInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(12)]
        [DefaultValue(typeof(DoubleDataBinding), "0.0")] // For XAML serialization
        public DoubleDataBinding AngleDeltaInfo
        {
            get => _angleDeltaInfo;
            set => SetValue(ref _angleDeltaInfo, value);
        }

        [DsCategory(ResourceStrings.GeometryAdvancedCategory)]
        [DsDisplayName(ResourceStrings.DsShapeAngleFinal)]
        //[PropertyOrder(13)]
        [DefaultValue(0.0)] // For XAML serialization
        public double AngleFinal
        {
            get => Math.Round(_angleFinal, 1);
            set => SetValue(ref _angleFinal, value);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double AngleFinalNotRounded => _angleFinal;

        [DsCategory(ResourceStrings.GeometryCategory)]
        [DsDisplayName(ResourceStrings.DsShapeIsFlipped)]
        //[PropertyOrder(6)]
        [DefaultValue(false)] // For XAML serialization
        public bool IsFlipped
        {
            get => _isFlipped;
            set
            {
                ResetGeometryCache();

                SetValue(ref _isFlipped, value);
            }
        }

        [DsCategory(ResourceStrings.Geometry3DCategory)]
        [DsDisplayName(ResourceStrings.DsShape_RotationX)]
        //[PropertyOrder(6)]
        [DefaultValue(0.0)] // For XAML serialization
        public double RotationX
        {
            get => Math.Round(_rotationX, 1);
            set => SetValue(ref _rotationX, value);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double RotationXNotRounded => _rotationX;

        [DsCategory(ResourceStrings.Geometry3DCategory)]
        [DsDisplayName(ResourceStrings.DsShape_RotationY)]
        //[PropertyOrder(7)]
        [DefaultValue(0.0)] // For XAML serialization
        public double RotationY
        {
            get => Math.Round(_rotationY, 1);
            set => SetValue(ref _rotationY, value);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double RotationYNotRounded => _rotationY;

        [DsCategory(ResourceStrings.Geometry3DCategory)]
        [DsDisplayName(ResourceStrings.DsShape_RotationZ)]
        //[PropertyOrder(8)]
        [DefaultValue(0.0)] // For XAML serialization
        public double RotationZ
        {
            get => Math.Round(_rotationZ, 1);
            set => SetValue(ref _rotationZ, value);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double RotationZNotRounded => _rotationZ;

        [DsCategory(ResourceStrings.Geometry3DCategory)]
        [DsDisplayName(ResourceStrings.DsShape_FieldOfView)]
        //[PropertyOrder(9)]
        [DefaultValue(45.0)] // For XAML serialization
        public double FieldOfView
        {
            get => Math.Round(_fieldOfView, 1);
            set
            {
                if (value < 0.5) value = 0.5;
                else if (value > 179.9) value = 179.9;

                SetValue(ref _fieldOfView, value);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double FieldOfViewNotRounded => _fieldOfView;

        [Browsable(false)]
        [DefaultValue(typeof(Settings), @"")] // For XAML serialization
        public Settings Settings { get; set; } = new();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public int Index
        {
            get => _index;
            set
            {
                if (Equals(value, _index)) return;
                _index = value;
                OnPropertyChangedAuto();
            }
        }

        [Browsable(false)]
        [DefaultValue(false)] // For XAML serialization
        public bool IsLocked
        {
            get => _isLocked;
            set => SetValue(ref _isLocked, value);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public object? TagObject { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(Container);
        }

        public void EndEditInPropertyGrid()
        {
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool SelectWhenShow { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool FirstSelectWhenShow { get; set; }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsShapeOpacityInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(TextBoxEditor), typeof(TextBoxEditor))]
        //[PropertyOrder(4)]
        [DefaultValue(typeof(DoubleDataBinding), @"1.0")] // For XAML serialization
        public virtual DoubleDataBinding OpacityInfo
        {
            get => _opacityInfo;
            set => SetValue(ref _opacityInfo, value);
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsShapeIsVisibleInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(CheckBoxEditor), typeof(CheckBoxEditor))]
        //[PropertyOrder(5)]
        [DefaultValue(typeof(BooleanDataBinding), "True")] // For XAML serialization
        public virtual BooleanDataBinding IsVisibleInfo
        {
            get => _isVisibleInfo;
            set => SetValue(ref _isVisibleInfo, value);
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsShapeIsEnabledInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(CheckBoxEditor), typeof(CheckBoxEditor))]
        //[PropertyOrder(6)]
        [DefaultValue(typeof(BooleanDataBinding), "True")] // For XAML serialization
        public virtual BooleanDataBinding IsEnabledInfo
        {
            get => _isEnabledInfo;
            set => SetValue(ref _isEnabledInfo, value);
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double LeftNotTransformed
        {
            get
            {
                return
                    (double)
                    LeftTopConverter.Instance.Convert(
                        new object[]
                        {
                            _centerInitialPosition.X, 0.0d, 0.0d,
                            CenterRelativePosition.X,
                            _widthInitial, 0.0d, 0.0d
                        }, typeof(double), null, CultureInfo.InvariantCulture)!;
            }
            set
            {
                CenterInitialPosition = _centerInitialPosition.WithX(value + _widthInitial * CenterRelativePosition.X);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public double TopNotTransformed
        {
            get
            {
                return
                    (double)
                    LeftTopConverter.Instance.Convert(
                        new object[]
                        {
                            _centerInitialPosition.Y, 0.0d, 0.0d, CenterRelativePosition.Y,
                            _heightInitial, 0.0d, 0.0d
                        }, typeof(double), null, CultureInfo.InvariantCulture)!;
            }
            set
            {
                CenterInitialPosition = _centerInitialPosition.WithY(value + _heightInitial * CenterRelativePosition.Y);
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [field: Searchable(false)] // For XAML serialization        
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public IPlayWindowBase? PlayWindow
        {
            get
            {
                var drawingBase = ParentItem.Find<DrawingBase>();
                if (drawingBase is null) return null;
                return drawingBase.PlayWindow;
            }
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public virtual IDsContainer? Container => ParentItem as IDsContainer;

        public Rect GetNotTransformedRect()
        {
            return new(LeftNotTransformed,
                TopNotTransformed,
                _widthInitial,
                _heightInitial);
        }

        public void SetNotTransformedRect(Rect newRect)
        {
            WidthInitial = newRect.Width;
            HeightInitial = newRect.Height;
            LeftNotTransformed = newRect.Left;
            TopNotTransformed = newRect.Top;
        }

        public Rect GetBoundingRect()
        {
            if (!_boundingRect.HasValue)
            {
                if (!IsFlipped && AngleInitial == 0.0)
                {
                    _boundingRect = GetNotTransformedRect();
                }
                else
                {
                    var center = _centerInitialPosition;
                    var rect = new Rect(
                        LeftNotTransformed - center.X,
                        TopNotTransformed - center.Y,
                        _widthInitial,
                        _heightInitial);
                    var matrix = GetTransformGroup().Value;

                    var topLeft = matrix.Transform(rect.TopLeft);
                    var topRight = matrix.Transform(rect.TopRight);
                    var bottomLeft = matrix.Transform(rect.BottomLeft);
                    var bottomRight = matrix.Transform(rect.BottomRight);

                    _boundingRect = new Rect(
                        new Point(
                            Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X)) + center.X,
                            Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y)) + center.Y
                        ),
                        new Point(
                            Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X)) + center.X,
                            Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y)) + center.Y
                        )
                    );
                }
            }

            return _boundingRect.Value;
        }

        public void SetBoundingRect(Rect newRect)
        {
            var origCenter = _centerInitialPosition;
            var newCenter = new Point(newRect.X + newRect.Width * CenterRelativePosition.X,
                newRect.Y + newRect.Height * CenterRelativePosition.Y);

            if (!IsFlipped && AngleInitial == 0.0)
            {
                WidthInitial = newRect.Width;
                HeightInitial = newRect.Height;
            }
            else
            {
                var a = AngleInitial % 90;
                if (a == 0.0)
                {
                    var rect = newRect.Translate(new Vector(-origCenter.X, -origCenter.Y));
                    var matrix = GetInverseTransformGroup().Value;

                    var topLeft = matrix.Transform(rect.TopLeft);
                    var topRight = matrix.Transform(rect.TopRight);
                    var bottomLeft = matrix.Transform(rect.BottomLeft);
                    var bottomRight = matrix.Transform(rect.BottomRight);

                    newRect = new Rect(
                        new Point(
                            Math.Min(Math.Min(topLeft.X, topRight.X), Math.Min(bottomLeft.X, bottomRight.X)) + origCenter.X,
                            Math.Min(Math.Min(topLeft.Y, topRight.Y), Math.Min(bottomLeft.Y, bottomRight.Y)) + origCenter.Y
                        ),
                        new Point(
                            Math.Max(Math.Max(topLeft.X, topRight.X), Math.Max(bottomLeft.X, bottomRight.X)) + origCenter.X,
                            Math.Max(Math.Max(topLeft.Y, topRight.Y), Math.Max(bottomLeft.Y, bottomRight.Y)) + origCenter.Y
                        )
                    );
                    
                    WidthInitial = newRect.Width;
                    HeightInitial = newRect.Height;
                }
            }

            CenterInitialPosition = newCenter;
        }

        public void Transform(double scaleX, double scaleY)
        {
            switch (ResizeMode)
            {
                case DsShapeResizeMode.WidthAndHeight:
                    if (AngleInitial % 180.0 == 0)
                    {
                        WidthInitial = scaleX * _widthInitial;
                        WidthFinal = scaleX * _widthFinal;
                        HeightInitial = scaleY * _heightInitial;
                        HeightFinal = scaleY * _heightFinal;
                    }
                    else if ((AngleInitial - 90.0) % 180.0 == 0)
                    {
                        WidthInitial = scaleY * _widthInitial;
                        WidthFinal = scaleY * _widthFinal;
                        HeightInitial = scaleX * _heightInitial;
                        HeightFinal = scaleX * _heightFinal;
                    }
                    else
                    {
                        var radians = -Math.PI * _angleInitial / 180;

                        WidthInitial = Math.Pow(Math.Pow(scaleX * _widthInitial * Math.Cos(radians), 2.0)
                                                + Math.Pow(scaleX * _widthInitial * Math.Sin(radians), 2.0), 0.5);
                        WidthFinal = Math.Pow(Math.Pow(scaleX * _widthFinal * Math.Cos(radians), 2.0)
                                              + Math.Pow(scaleX * _widthFinal * Math.Sin(radians), 2.0), 0.5);
                        HeightInitial = Math.Pow(Math.Pow(scaleX * _heightInitial * Math.Sin(radians), 2.0)
                                                 + Math.Pow(scaleX * _heightInitial * Math.Cos(radians), 2.0), 0.5);
                        HeightFinal = Math.Pow(Math.Pow(scaleX * _heightFinal * Math.Sin(radians), 2.0)
                                               + Math.Pow(scaleX * _heightFinal * Math.Cos(radians), 2.0), 0.5);
                    }

                    break;
                case DsShapeResizeMode.WidthOnly:
                    if (AngleInitial % 180.0 == 0)
                    {
                        WidthInitial = scaleX * _widthInitial;
                        WidthFinal = scaleX * _widthFinal;
                    }
                    else if ((AngleInitial - 90.0) % 180.0 == 0)
                    {
                        WidthInitial = scaleY * _widthInitial;
                        WidthFinal = scaleY * _widthFinal;
                    }
                    else
                    {
                        var radians = -Math.PI * _angleInitial / 180;

                        WidthInitial = Math.Pow(Math.Pow(scaleX * _widthInitial * Math.Cos(radians), 2.0)
                                                + Math.Pow(scaleX * _widthInitial * Math.Sin(radians), 2.0), 0.5);
                        WidthFinal = Math.Pow(Math.Pow(scaleX * _widthFinal * Math.Cos(radians), 2.0)
                                              + Math.Pow(scaleX * _widthFinal * Math.Sin(radians), 2.0), 0.5);
                    }

                    break;
                case DsShapeResizeMode.HeightOnly:
                    if (AngleInitial % 180.0 == 0)
                    {
                        HeightInitial = scaleY * _heightInitial;
                        HeightFinal = scaleY * _heightFinal;
                    }
                    else if ((AngleInitial - 90.0) % 180.0 == 0)
                    {
                        HeightInitial = scaleX * _heightInitial;
                        HeightFinal = scaleX * _heightFinal;
                    }
                    else
                    {
                        var radians = -Math.PI * _angleInitial / 180;

                        HeightInitial = Math.Pow(Math.Pow(scaleX * _heightInitial * Math.Sin(radians), 2.0)
                                                 + Math.Pow(scaleX * _heightInitial * Math.Cos(radians), 2.0), 0.5);
                        HeightFinal = Math.Pow(Math.Pow(scaleX * _heightFinal * Math.Sin(radians), 2.0)
                                               + Math.Pow(scaleX * _heightFinal * Math.Cos(radians), 2.0), 0.5);
                    }

                    break;
                case DsShapeResizeMode.KeepAspectRatio:
                    if (AngleInitial % 180.0 == 0)
                    {
                        WidthInitial = scaleY * _widthInitial;
                        WidthFinal = scaleY * _widthFinal;
                        HeightInitial = scaleY * _heightInitial;
                        HeightFinal = scaleY * _heightFinal;
                    }
                    else if ((AngleInitial - 90.0) % 180.0 == 0)
                    {
                        WidthInitial = scaleX * _widthInitial;
                        WidthFinal = scaleX * _widthFinal;
                        HeightInitial = scaleX * _heightInitial;
                        HeightFinal = scaleX * _heightFinal;
                    }
                    else
                    {
                        var widthHeightRatio = _widthInitial / _heightInitial;
                        var radians = -Math.PI * _angleInitial / 180;

                        HeightInitial = Math.Pow(Math.Pow(scaleX * _heightInitial * Math.Sin(radians), 2.0)
                                                 + Math.Pow(scaleX * _heightInitial * Math.Cos(radians), 2.0), 0.5);
                        HeightFinal = Math.Pow(Math.Pow(scaleX * _heightFinal * Math.Sin(radians), 2.0)
                                               + Math.Pow(scaleX * _heightFinal * Math.Cos(radians), 2.0), 0.5);
                        var newWidthInitial = _heightInitial * widthHeightRatio;
                        var k = newWidthInitial / WidthInitial;
                        WidthInitial = newWidthInitial;
                        WidthFinal = _widthFinal * k;
                    }

                    break;
            }

            CenterInitialPosition = new Point(scaleX * _centerInitialPosition.X, scaleY * _centerInitialPosition.Y);
            CenterFinalPosition = new Point(scaleX * _centerFinalPosition.X, scaleY * _centerFinalPosition.Y);
        }


        public virtual void GetDsConstants(
            CaseInsensitiveDictionary<List<ExtendedDsConstant>> dsConstantsDictionary)
        {
        }

        public virtual IEnumerable<Guid> GetUsedAddonGuids()
        {
            var additionalAddon = AddonsHelper.GetAdditionalAddon(GetType());
            if (additionalAddon is not null) yield return additionalAddon.Guid;

            IEnumerable<FieldInfo> fields = ObjectHelper.GetAllFields(this);
            foreach (FieldInfo field in fields)
                if (typeof(IUsedAddonsInfo).IsAssignableFrom(field.FieldType))
                {
                    var usedAddonsInfo = field.GetValue(this) as IUsedAddonsInfo;
                    if (usedAddonsInfo is not null)
                        foreach (var guid in usedAddonsInfo.GetUsedAddonGuids())
                            yield return guid;
                }
        }

        public static Rect GetBoundingRect(IList<DsShapeBase> dsShapes)
        {
            if (dsShapes.Count == 0) return new Rect();

            var rect = dsShapes[0].GetBoundingRect();
            foreach (DsShapeBase dsShape in dsShapes)
            {
                if (dsShape == dsShapes[0]) continue;
                rect.Union(dsShape.GetBoundingRect());
            }

            return rect;
        }

        public TransformGroup GetTransformGroup()
        {
            if (_transformGroup is null)
            {
                _transformGroup = new TransformGroup();
                var scaleTranform = new ScaleTransform
                {
                    ScaleX = IsFlipped ? -1 : 1
                };
                var rotateTransform = new RotateTransform
                {
                    Angle = AngleInitialNotRounded
                };
                _transformGroup.Children.Add(scaleTranform);
                _transformGroup.Children.Add(rotateTransform);
            }

            return _transformGroup;
        }

        public Transform GetInverseTransformGroup()
        {
            if (_inverseTransformGroup is null)
            {
                var matrix = GetTransformGroup().Value;
                if (matrix.HasInverse)
                    _inverseTransformGroup = new MatrixTransform(matrix.Invert());
                else
                    _inverseTransformGroup = new TransformGroup();
            }

            return _inverseTransformGroup;
        }

        public static string GetDsShapeName(string dsShapeNameBase, int dsShapeNameNumber)
        {
            if (dsShapeNameNumber > 1)
                return dsShapeNameBase + @"-" + dsShapeNameNumber.ToString(CultureInfo.InvariantCulture);
            return dsShapeNameBase;
        }

        public object? GetUndoRoot()
        {
            var drawing = ParentItem.Find<DrawingBase>();
            if (drawing is not null) return drawing.GetUndoRoot();
            return null;
        }

        public virtual object Clone()
        {
            return this.CloneUsingSerialization();
        }

        #endregion

        #region protected functions

        protected void ResetGeometryCache()
        {
            _boundingRect = null;
            _transformGroup = null;
            _inverseTransformGroup = null;
        }

        protected bool SetValue<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (Equals(value, field)) return false;
            if (VisualDesignMode)
            {
                DefaultChangeFactory.Instance.OnChanging(this, propertyName, field, value);

                var valueDataBinding = field as IValueDataBinding;
                if (valueDataBinding is not null) valueDataBinding.ClearPropertyChangedEvent();
            }

            var item = field as IDsItem;
            if (item is not null) item.ParentItem = null;
            field = value;
            item = field as IDsItem;
            if (item is not null) item.ParentItem = this;
            if (VisualDesignMode)
            {
                var valueDataBinding = field as IValueDataBinding;
                if (valueDataBinding is not null)
                    valueDataBinding.PropertyChanged += (s, a) =>
                    {
                        if (a is not null && a.PropertyName == @"ConstValue")
                            OnPropertyChanged(propertyName);
                    };
            }

            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged is not null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChangedAuto([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged is not null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool VisualDesignMode { get; }
        protected bool LoadXamlContent { get; }

        #endregion

        #region private fields

        private string _name = @"";
        private string _desc = @"";

        private Point _centerInitialPosition;
        private DoubleDataBinding _centerDeltaPositionXInfo = null!;
        private DoubleDataBinding _centerDeltaPositionYInfo = null!;
        private Point _centerFinalPosition;
        private Point _centerRelativePosition;
        private DsShapeResizeMode _resizeMode;
        private double _widthInitial;
        private DoubleDataBinding _widthDeltaInfo = null!;
        private double _widthFinal;
        private double _heightInitial;
        private DoubleDataBinding _heightDeltaInfo = null!;
        private double _heightFinal;
        private double _angleInitial;
        private DoubleDataBinding _angleDeltaInfo = null!;
        private double _angleFinal;
        private bool _isFlipped;
        private double _rotationX;
        private double _rotationY;
        private double _rotationZ;
        private double _fieldOfView;
        private int _index;
        private bool _isLocked;
        private BooleanDataBinding _isVisibleInfo = null!;
        private BooleanDataBinding _isEnabledInfo = null!;
        private DoubleDataBinding _opacityInfo = null!;
        private Rect? _boundingRect;
        private TransformGroup? _transformGroup;
        private Transform? _inverseTransformGroup;

        #endregion
    }

    public enum DsShapeResizeMode
    {
        WidthAndHeight = 0,
        WidthOnly,
        HeightOnly,
        KeepAspectRatio,
        NoResize
    }
}