using System.ComponentModel;
using Avalonia;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.CustomAttributes;


using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    public abstract class ButtonDsShapeBase : ControlDsShape
    {
        #region construction and destruction

        protected ButtonDsShapeBase()
            : this(true, true)
        {
        }

        protected ButtonDsShapeBase(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            BorderThickness = new Thickness(3);
            ClickDsCommand = new DsCommand(visualDesignMode);
            DoubleClickDsCommand = new DsCommand(visualDesignMode);
            RightClickDsCommand = new DsCommand(visualDesignMode);
            HoldDsCommand = new DsCommand(visualDesignMode);            
            HoldCommandDelayMs = 0;
            HoldCommandIntervalMs = 1000;
            PointerEnteredDsCommand = new DsCommand(visualDesignMode);
            PointerExitedDsCommand = new DsCommand(visualDesignMode);
            IsDefaultInfo = new BooleanDataBinding(visualDesignMode, loadXamlContent) {ConstValue = false};
            IsCancelInfo = new BooleanDataBinding(visualDesignMode, loadXamlContent) {ConstValue = false};
        }

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.AppearanceCategory)]
        [DsDisplayName(ResourceStrings.ControlDsShapeStyleInfo)]
        //[Editor(//typeof(DsUIElementPropertyTypeEditor<ButtonStyleInfoSupplier>),
            //typeof(DsUIElementPropertyTypeEditor<ButtonStyleInfoSupplier>))]
        //[PropertyOrder(101)]
        // For XAML serialization
        public override DsUIElementProperty StyleInfo
        {
            get => base.StyleInfo;
            set => base.StyleInfo = value;
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_ClickDsCommand)]
        //[PropertyOrder(0)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand ClickDsCommand
        {
            get => _clickDsCommand;
            set => SetValue(ref _clickDsCommand, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_DoubleClickDsCommand)]
        //[PropertyOrder(1)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand DoubleClickDsCommand
        {
            get => _doubleClickDsCommand;
            set => SetValue(ref _doubleClickDsCommand, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_RightClickDsCommand)]
        //[PropertyOrder(2)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand RightClickDsCommand
        {
            get => _rightClickDsCommand;
            set => SetValue(ref _rightClickDsCommand, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_HoldDsCommand)]
        //[PropertyOrder(3)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand HoldDsCommand
        {
            get => _holdDsCommand;
            set => SetValue(ref _holdDsCommand, value);
        }        

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_HoldCommandDelayMs)]
        [LocalizedDescription(ResourceStrings.ButtonDsShapeBase_HoldCommandDelayMsDescription)]
        //[PropertyOrder(4)]
        public int HoldCommandDelayMs
        {
            get => _holdCommandDelayMs;
            set => SetValue(ref _holdCommandDelayMs, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_HoldCommandIntervalMs)]
        [LocalizedDescription(ResourceStrings.ButtonDsShapeBase_HoldCommandIntervalMsDescription)]
        //[PropertyOrder(5)]
        public int HoldCommandIntervalMs
        {
            get => _holdCommandIntervalMs;
            set => SetValue(ref _holdCommandIntervalMs, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_MouseEnterDsCommand)]
        //[PropertyOrder(6)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand PointerEnteredDsCommand
        {
            get => _pointerEnteredDsCommand;
            set => SetValue(ref _pointerEnteredDsCommand, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_MouseLeaveDsCommand)]
        //[PropertyOrder(7)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand PointerExitedDsCommand
        {
            get => _pointerExitedDsCommand;
            set => SetValue(ref _pointerExitedDsCommand, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_IsDefaultInfo)]
        [LocalizedDescription(ResourceStrings.ButtonDsShapeBase_IsDefaultInfoDescription)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(CheckBoxEditor), typeof(CheckBoxEditor))]
        //[PropertyOrder(6)]
        [DefaultValue(typeof(BooleanDataBinding), @"False")] // For XAML serialization
        public BooleanDataBinding IsDefaultInfo
        {
            get => _isDefaultInfo;
            set => SetValue(ref _isDefaultInfo, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.ButtonDsShapeBase_IsCancelInfo)]
        [LocalizedDescription(ResourceStrings.ButtonDsShapeBase_IsCancelInfoDescription)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(CheckBoxEditor), typeof(CheckBoxEditor))]
        //[PropertyOrder(7)]
        [DefaultValue(typeof(BooleanDataBinding), @"False")] // For XAML serialization
        public BooleanDataBinding IsCancelInfo
        {
            get => _isCancelInfo;
            set => SetValue(ref _isCancelInfo, value);
        }

        public override string? GetStyleXamlString(IDsContainer? container)
        {
            return new ButtonStyleInfoSupplier().GetPropertyXamlString(base.StyleInfo, container);
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            base.SerializeOwnedData(writer, context);

            using (writer.EnterBlock(6))
            {
                writer.Write(ClickDsCommand, context);
                writer.Write(DoubleClickDsCommand, context);
                writer.Write(HoldDsCommand, context);
                writer.Write(HoldCommandDelayMs);
                writer.Write(HoldCommandIntervalMs);
                writer.Write(PointerEnteredDsCommand, context);
                writer.Write(PointerExitedDsCommand, context);
                writer.Write(IsDefaultInfo, context);
                writer.Write(IsCancelInfo, context);
                writer.Write(RightClickDsCommand, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedDataAsync(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                       
                    case 6:
                        try
                        {
                            reader.ReadOwnedData(ClickDsCommand, context);
                            reader.ReadOwnedData(DoubleClickDsCommand, context);
                            reader.ReadOwnedData(HoldDsCommand, context);
                            HoldCommandDelayMs = reader.ReadInt32();
                            HoldCommandIntervalMs = reader.ReadInt32();
                            reader.ReadOwnedData(PointerEnteredDsCommand, context);
                            reader.ReadOwnedData(PointerExitedDsCommand, context);
                            reader.ReadOwnedData(IsDefaultInfo, context);
                            reader.ReadOwnedData(IsCancelInfo, context);
                            reader.ReadOwnedData(RightClickDsCommand, context);
                        }
                        catch (BlockEndingException)
                        {
                        }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

        #region private fields
        
        private DsCommand _clickDsCommand = null!;
        private DsCommand _doubleClickDsCommand = null!;
        private DsCommand _rightClickDsCommand = null!;
        private DsCommand _holdDsCommand = null!;
        private int _holdCommandDelayMs;
        private int _holdCommandIntervalMs;
        private DsCommand _pointerEnteredDsCommand = null!;
        private DsCommand _pointerExitedDsCommand = null!;
        private BooleanDataBinding _isDefaultInfo = null!;
        private BooleanDataBinding _isCancelInfo = null!;

        #endregion
    }
}