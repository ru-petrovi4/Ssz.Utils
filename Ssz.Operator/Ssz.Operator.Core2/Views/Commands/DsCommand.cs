using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Markup;
using Ssz.Operator.Core.Addons;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.CustomAttributes;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.DsShapes;
using Ssz.Operator.Core.Properties;
//using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
using Ssz.Utils.MonitoredUndo;
//using Ssz.Utils.Wpf;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Attributes;
//using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Editors;
using OwnedDataSerializableAndCloneable = Ssz.Operator.Core.Utils.OwnedDataSerializableAndCloneable;
using Microsoft.Extensions.Logging;

namespace Ssz.Operator.Core.Commands
{
    [TypeConverter(typeof(DsCommandTypeConverter))]
    //[ValueSerializer(typeof(DsCommandValueSerializer))]
    //[ContentProperty(@"DsCommandOptionsForXaml")]
    // For XAML serialization. Content property must be of type object or string.
    public class DsCommand :
        ViewModelBase, IOwnedDataSerializable,
        IDsItem, ISupportsUndo,
        IUsedAddonsInfo, ICloneable, IDisposable
    {
        #region construction and destruction

        public DsCommand() :
            this(true)
        {
        }

        public DsCommand(bool visualDesignMode)
        {
            _visualDesignMode = visualDesignMode;
            _command = "";
            _dsCommandOptions = null;
            _isEnabledInfo = new BooleanDataBinding(visualDesignMode, true) {ConstValue = true};
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
                IsEnabledInfo.Dispose();

                _dsCommandOptions = null;

                ParentItem = null;
            }

            Disposed = true;
        }

        ~DsCommand()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsCommandCommand)]
        //[ItemsSource(typeof(DsCommandCommandsItemsSource), true)]
        //[PropertyOrder(1)]
        public string Command
        {
            get => _command;
            set
            {
                if (string.IsNullOrWhiteSpace(value)) value = "";
                if (Equals(value, _command)) return;
                OnChanging(_command, value, @"Command");

                if (!string.IsNullOrWhiteSpace(_command) && DsCommandOptions is not null)
                    _dsCommandOptionsDictionary[_command] = DsCommandOptions;

                _command = value;

                if (!string.IsNullOrWhiteSpace(_command))
                {
                    OwnedDataSerializableAndCloneable? existingDsCommandOptions;
                    if (_dsCommandOptionsDictionary.TryGetValue(_command, out existingDsCommandOptions))
                    {
                        DsCommandOptions = existingDsCommandOptions;
                        _dsCommandOptionsDictionary.Remove(_command);
                    }
                    else
                    {
                        DsCommandOptions = CommandsManager.NewDsCommandOptionsObject(_command);
                    }
                }
                else
                {
                    DsCommandOptions = null;
                }

                OnPropertyChangedAuto();

                OnPropertyChanged(@"IsEmpty");
            }
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsCommandDsCommandOptions)]
        //[Editor(typeof(CloneableObjectTypeEditor), typeof(CloneableObjectTypeEditor))]
        //[PropertyOrder(2)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public OwnedDataSerializableAndCloneable? DsCommandOptions
        {
            get => _dsCommandOptions;
            set
            {
                if (Equals(value, _dsCommandOptions)) return;
                OnChanging(_dsCommandOptions, value, @"DsCommandOptions");
                var item = _dsCommandOptions as IDsItem;
                if (item is not null) item.ParentItem = null;
                _dsCommandOptions = value;
                item = _dsCommandOptions as IDsItem;
                if (item is not null) item.ParentItem = this;
                OnPropertyChangedAuto();
            }
        }

        [Browsable(false)]
        [DefaultValue(null)] // For XAML serialization
        public object? DsCommandOptionsForXaml
        {
            get => DsCommandOptions;
            set => DsCommandOptions = value as OwnedDataSerializableAndCloneable;
        }

        [Searchable(false)]
        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsCommandCommandUrl)]
        //[ReadOnlyInEditor]
        //[PropertyOrder(3)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string CommandUrl
        {
            get => _commandUrl;
            set => SetValue(ref _commandUrl, value);
        }


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public string DsCommandString
        {
            get => _dsCommandString;
            set => SetValue(ref _dsCommandString, value);
        }

        [DsCategory(ResourceStrings.BasicCategory)]
        [DsDisplayName(ResourceStrings.DsCommandIsEnabledInfo)]
        //[ExpandableObject]
        //[ValuePropertyPath(@"ConstValue")]
        //[IsValueEditorEnabledPropertyPath(@"IsConst")]
        //[Editor(typeof(CheckBoxEditor), typeof(CheckBoxEditor))]
        //[PropertyOrder(4)]
        [DefaultValue(typeof(BooleanDataBinding), "True")] // For XAML serialization
        public BooleanDataBinding IsEnabledInfo
        {
            get => _isEnabledInfo;
            set
            {
                if (Equals(value, _isEnabledInfo)) return;
                _isEnabledInfo = value;
                if (_visualDesignMode)
                    _isEnabledInfo.PropertyChanged += (sender, e) => OnPropertyChanged(@"IsEnabledInfo");
                OnPropertyChanged(@"IsEnabledInfo");
            }
        }


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool IsEmpty => string.IsNullOrEmpty(Command);

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [field: Searchable(false)] // For XAML serialization        
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization      
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(4))
            {
                writer.Write(Command);
                writer.WriteNullableOwnedData(DsCommandOptions, context);
                writer.Write(IsEnabledInfo, context);
            }
        }

        public void DeserializeOwnedDataAsync(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 4:
                    {
                        Command = reader.ReadString();
                        reader.ReadNullableOwnedData(DsCommandOptions, context);
                        reader.ReadOwnedData(IsEnabledInfo, context);

                        if (!string.IsNullOrEmpty(Command) && DsCommandOptions is null)
                            DsProject.LoggersSet.Logger.LogError("Command not found, probably addon is missing. Command: " + Command);
                    }
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            var constantsHolder = DsCommandOptions as IConstantsHolder;
            if (constantsHolder is not null) 
                constantsHolder.FindConstants(constants);
            IsEnabledInfo.FindConstants(constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            ItemHelper.ReplaceConstants(DsCommandOptions as IDsItem, container);
            ItemHelper.ReplaceConstants(IsEnabledInfo, container);
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
            ItemHelper.RefreshForPropertyGrid(DsCommandOptions as IDsItem, container);
            ItemHelper.RefreshForPropertyGrid(IsEnabledInfo, container);

            CommandUrl = DsCommandValueSerializer.Instance.ConvertToString(this) ?? "";

            string? childWindowInfo = null;
            var childWindowInfoDsCommandOptions = DsCommandOptions as IChildWindowInfoDsCommandOptions;
            if (childWindowInfoDsCommandOptions is not null &&
                !string.IsNullOrEmpty(childWindowInfoDsCommandOptions.ChildWindowInfo))
                childWindowInfo = Command + @": " + childWindowInfoDsCommandOptions.ChildWindowInfo;

            if (!string.IsNullOrEmpty(childWindowInfo))
            {
                DsCommandString = childWindowInfo!;

                var complexDsShape = container as ComplexDsShape;
                if (complexDsShape is not null) complexDsShape.ChildWindowInfo = childWindowInfo!;
            }
            else
            {
                string dsCommandString = Command;
                string dsCommandOptionsString = DsCommandOptions is not null ? DsCommandOptions.ToString() : "";
                if (!string.IsNullOrEmpty(dsCommandOptionsString)) dsCommandString += @": " + dsCommandOptionsString;
                DsCommandString = dsCommandString;
            }
        }

        public object? GetUndoRoot()
        {
            var drawing = ParentItem.Find<DrawingBase>();
            if (drawing is not null) return drawing.GetUndoRoot();
            return null;
        }

        public object Clone()
        {
            return this.CloneUsingSerialization(() => new DsCommand(_visualDesignMode));
        }

        public override bool Equals(object? obj)
        {
            var other = obj as DsCommand;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public override string ToString()
        {
            return DsCommandString;
        }

        public IEnumerable<Guid> GetUsedAddonGuids()
        {
            var additionalAddon = AddonsHelper.GetAdditionalAddon(Command);
            if (additionalAddon is not null) yield return additionalAddon.Guid;
        }

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        protected bool Equals(DsCommand other)
        {
            return Command == other.Command &&
                   Equals(DsCommandOptions, other.DsCommandOptions) &&
                   Equals(IsEnabledInfo, other.IsEnabledInfo);
        }

        protected void OnChanging(object? oldValue, object? newValue, string propertyName)
        {
            if (_visualDesignMode) DefaultChangeFactory.Instance.OnChanging(this, propertyName, oldValue, newValue);
        }

        #endregion

        #region private fields

        private readonly bool _visualDesignMode;


        private string _command;
        private BooleanDataBinding _isEnabledInfo = null!;
        private OwnedDataSerializableAndCloneable? _dsCommandOptions;

        [Searchable(false)] private string _dsCommandString = @"";

        [Searchable(false)] private string _commandUrl = @"";

        private readonly Ssz.Utils.CaseInsensitiveDictionary<OwnedDataSerializableAndCloneable>
            _dsCommandOptionsDictionary =
                new();

        #endregion
    }

    //public class DsCommandCommandsItemsSource : IItemsSource
    //{
    //    #region public functions

    //    public ItemCollection GetValues()
    //    {
    //        var commands = new ItemCollection();
    //        foreach (string command in CommandsManager.GetAvailableCommands()) commands.Add(command);
    //        return commands;
    //    }

    //    #endregion
    //}
}

/*
var clone = (DsCommand)this.CloneUsingSerialization();
            clone.ParentItem = ParentItem;
            clone.DsCommandOptions */