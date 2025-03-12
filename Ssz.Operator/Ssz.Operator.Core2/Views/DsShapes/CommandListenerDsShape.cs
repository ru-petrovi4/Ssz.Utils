using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.CustomAttributes;

using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils.MonitoredUndo;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;

namespace Ssz.Operator.Core.DsShapes
{
    public class CommandListenerDsShape : DsShapeBase
    {
        #region private functions

        private void OnConditionalDsCommandsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DefaultChangeFactory.Instance.OnCollectionChanged(this, nameof(ConditionalDsCommandsCollection),
                ConditionalDsCommandsCollection, e);

            OnPropertyChanged(nameof(ConditionalDsCommandsCollection));
        }

        #endregion

        #region construction and destruction

        public CommandListenerDsShape() // For XAML serialization
            : this(true, true)
        {
        }

        public CommandListenerDsShape(bool visualDesignMode, bool loadXamlContent)
            : base(visualDesignMode, loadXamlContent)
        {
            WidthInitial = 30;
            HeightInitial = 30;

            CommandToListenOptions = CommandListenerOptions.ListenOnlyInActiveDsPage;
            CommandToListenDsCommand = new DsCommand(visualDesignMode);
            LoadedDsCommand = new DsCommand(visualDesignMode);
            EachSecondDsCommand = new DsCommand(visualDesignMode);
            if (visualDesignMode)
                ConditionalDsCommandsCollection.CollectionChanged += OnConditionalDsCommandsCollectionChanged;
            UnloadedDsCommand = new DsCommand(visualDesignMode);
        }

        #endregion

        #region public functions

        public const string DsShapeTypeNameToDisplay = "CommandDoer";
        public static readonly Guid DsShapeTypeGuid = new(@"AF5035F0-DABE-4FE4-AC6D-66C6F969406E");

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override DoubleDataBinding OpacityInfo
        {
            get => base.OpacityInfo;
            set => base.OpacityInfo = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override BooleanDataBinding IsVisibleInfo
        {
            get => base.IsVisibleInfo;
            set => base.IsVisibleInfo = value;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override BooleanDataBinding IsEnabledInfo
        {
            get => base.IsEnabledInfo;
            set => base.IsEnabledInfo = value;
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.CommandListenerDsShape_CommandToListen)]
        [LocalizedDescription(ResourceStrings.CommandListenerDsShape_CommandToListenDescription)]
        //[ItemsSource(typeof(CommandToListenItemsSource), true)]
        //[PropertyOrder(1)]
        public string CommandToListen
        {
            get => _commandToListen;
            set => SetValue(ref _commandToListen, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.CommandListenerDsShape_CommandToListenOptions)]
        [LocalizedDescription(ResourceStrings.CommandListenerDsShape_CommandToListenOptionsDescription)]
        //[PropertyOrder(2)]
        public CommandListenerOptions CommandToListenOptions
        {
            get => _commandToListenOptions;
            set => SetValue(ref _commandToListenOptions, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.CommandListenerDsShape_CommandToListenDsCommand)]
        [LocalizedDescription(ResourceStrings.CommandListenerDsShape_CommandToListenDsCommandDescription)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        //[PropertyOrder(3)]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand CommandToListenDsCommand
        {
            get => _commandToListenDsCommand;
            set => SetValue(ref _commandToListenDsCommand, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.CommandListenerDsShape_LoadedDsCommand)]
        [LocalizedDescription(ResourceStrings.CommandListenerDsShape_LoadedDsCommandDescription)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        //[PropertyOrder(4)]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand LoadedDsCommand
        {
            get => _loadedDsCommand;
            set => SetValue(ref _loadedDsCommand, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.CommandListenerDsShape_EachSecondDsCommand)]
        [LocalizedDescription(ResourceStrings.CommandListenerDsShape_EachSecondDsCommandDescription)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        //[PropertyOrder(5)]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand EachSecondDsCommand
        {
            get => _eachSecondDsCommand;
            set => SetValue(ref _eachSecondDsCommand, value);
        }

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.CommandListenerDsShape_ConditionalDsCommandsCollection)]
        [LocalizedDescription(ResourceStrings.CommandListenerDsShape_ConditionalDsCommandsCollectionDescription)]
        //[Editor(//typeof(MiscTypeCloneableObjectsCollectionTypeEditor),
            //typeof(MiscTypeCloneableObjectsCollectionTypeEditor))]
        //[NewItemTypes(typeof(DsCommand))]
        //[PropertyOrder(6)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        // For XAML serialization of collections
        public ObservableCollection<DsCommand> ConditionalDsCommandsCollection { get; } = new();

        [DsCategory(ResourceStrings.BehaviourCategory)]
        [DsDisplayName(ResourceStrings.CommandListenerDsShape_UnloadedDsCommand)]
        [LocalizedDescription(ResourceStrings.CommandListenerDsShape_UnloadedDsCommandDescription)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"DsCommandString")]
        //[IsValueEditorEnabledPropertyPath(@"IsEmpty")]
        //[Editor(typeof(TextBlockEditor), typeof(TextBlockEditor))]
        //[PropertyOrder(7)]
        [DefaultValue(typeof(DsCommand), @"")] // For XAML serialization
        public DsCommand UnloadedDsCommand
        {
            get => _unloadedDsCommand;
            set => SetValue(ref _unloadedDsCommand, value);
        }

        public override Guid GetDsShapeTypeGuid()
        {
            return DsShapeTypeGuid;
        }

        public override string GetDsShapeTypeNameToDisplay()
        {
            return DsShapeTypeNameToDisplay;
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            base.SerializeOwnedData(writer, context);

            using (writer.EnterBlock(4))
            {
                writer.Write(CommandToListen);
                writer.Write((int) CommandToListenOptions);
                writer.Write(CommandToListenDsCommand, context);
                writer.Write(LoadedDsCommand, context);
                writer.Write(EachSecondDsCommand, context);
                writer.Write(ConditionalDsCommandsCollection.ToList());
                writer.Write(UnloadedDsCommand, context);
            }
        }

        public override void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            base.DeserializeOwnedDataAsync(reader, context);

            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 4:
                        CommandToListen = reader.ReadString();
                        CommandToListenOptions = (CommandListenerOptions) reader.ReadInt32();
                        reader.ReadOwnedData(CommandToListenDsCommand, context);
                        reader.ReadOwnedData(LoadedDsCommand, context);
                        reader.ReadOwnedData(EachSecondDsCommand, context);
                        List<DsCommand> conditionalDsCommandsCollection = reader.ReadList<DsCommand>();
                        ConditionalDsCommandsCollection.Clear();
                        foreach (DsCommand dsCommand in conditionalDsCommandsCollection)
                            ConditionalDsCommandsCollection.Add(dsCommand);
                        reader.ReadOwnedData(UnloadedDsCommand, context);
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public override void RefreshForPropertyGrid(IDsContainer? container)
        {
            base.RefreshForPropertyGrid(container);

            foreach (DsCommand dsCommand in ConditionalDsCommandsCollection)
                ItemHelper.RefreshForPropertyGrid(dsCommand, container);

            OnPropertyChanged(nameof(ConditionalDsCommandsCollection));
        }

        #endregion

        #region private fields

        private string _commandToListen = @"";
        private CommandListenerOptions _commandToListenOptions;
        private DsCommand _commandToListenDsCommand = null!;

        private DsCommand _loadedDsCommand = null!;
        private DsCommand _eachSecondDsCommand = null!;

        private DsCommand _unloadedDsCommand = null!;

        #endregion
    }

    public enum CommandListenerOptions
    {
        ListenAlways = 0,
        ListenOnlyInActiveDsPage = 1
    }

    //public class CommandToListenItemsSource : IItemsSource
    //{
    //    #region public functions

    //    public ItemCollection GetValues()
    //    {
    //        var commands = new ItemCollection();
    //        commands.Add("Ack");
    //        return commands;
    //    }

    //    #endregion
    //}
}