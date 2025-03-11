using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core.MultiValueConverters
{
    public class XamlConverter : ValueConverterBase
    {
        #region private fields

        #endregion

        #region construction and destruction

        public XamlConverter() :
            this(true, true)
        {
        }

        public XamlConverter(bool visualDesignMode, bool loadXamlContent)
        {
            VisualDesignMode = visualDesignMode;
            LoadXamlContent = loadXamlContent;

            DataSourceToUiStatements = new ObservableCollection<XamlStatement>();
            DataSourceToUiStatements.CollectionChanged += DataSourceToUiStatementsOnCollectionChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                DataSourceToUiStatements.CollectionChanged -= DataSourceToUiStatementsOnCollectionChanged;
                foreach (XamlStatement s in DataSourceToUiStatements) s.Dispose();
                DataSourceToUiStatements.Clear();
            }

            base.Dispose(disposing);
        }

        #endregion

        #region public functions

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
        // For XAML serialization of collections
        public ObservableCollection<XamlStatement> DataSourceToUiStatements { get; }

        [Searchable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool VisualDesignMode { get; }

        [Searchable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool LoadXamlContent { get; }

        public override async void Initialize()
        {
            foreach (XamlStatement statement in DataSourceToUiStatements)
            {
                await statement.CreateConstObjectAsync();
            }
        }

        public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values is null || values.Count == 0 || values.Any(v => Equals(v, AvaloniaProperty.UnsetValue)))
                return BindingOperations.DoNothing;

            if (DisableUpdatingTarget) 
                return BindingOperations.DoNothing;

            object? evaluatedValue;

            var firstTrue =
                DataSourceToUiStatements.FirstOrDefault(
                    s => ObsoleteAnyHelper.ConvertTo<bool>(s.Condition.Evaluate(values.ToArray(), null), false));
            if (firstTrue is not null)
                evaluatedValue = firstTrue.GetConstObject();
            else
                evaluatedValue = null;

            if (StringHelper.IsNullOrEmptyString(evaluatedValue)) 
                return NullOrEmptyValue;
            return evaluatedValue;
        }

        public override void ConvertBack(object? value, DataValueViewModel dataValueViewModel, object? parameter,
            CultureInfo culture)
        {            
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(2))
            {
                writer.WriteListOfOwnedDataSerializable(DataSourceToUiStatements, context);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 1:
                        try
                        {
                            var dataSourceToUiStatements = reader.ReadList<XamlStatement>();
                            DataSourceToUiStatements.Clear();
                            foreach (XamlStatement dataSourceToUiStatement in dataSourceToUiStatements)
                                DataSourceToUiStatements.Add(dataSourceToUiStatement);
                        }
                        catch (BlockEndingException)
                        {
                        }

                        break;
                    case 2:
                        try
                        {
                            var dataSourceToUiStatements =
                                reader.ReadListOfOwnedDataSerializable(() => new XamlStatement(LoadXamlContent),
                                    context);
                            DataSourceToUiStatements.Clear();
                            foreach (XamlStatement dataSourceToUiStatement in dataSourceToUiStatements)
                                DataSourceToUiStatements.Add(dataSourceToUiStatement);
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

        public override void FindConstants(HashSet<string> constants)
        {
            foreach (XamlStatement statement in DataSourceToUiStatements)
            {
                statement.FindConstants(constants);
            }
        }

        public override void ReplaceConstants(IDsContainer? container)
        {
            foreach (XamlStatement statement in DataSourceToUiStatements)
            {
                ItemHelper.ReplaceConstants(statement, container);
            }
        }

        public void GetUsedFileNames(HashSet<string> usedFileNames)
        {
            foreach (XamlStatement dataSourceToUiStatement in DataSourceToUiStatements)
            {
                dataSourceToUiStatement.GetUsedFileNames(usedFileNames);
            }
        }

        public override object Clone()
        {
            return this.CloneUsingSerialization(() => new XamlConverter(VisualDesignMode, LoadXamlContent));
        }

        #endregion

        #region private functions

        private void DataSourceToUiStatementsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    XamlStatement[] addedXamlStatements = (e.NewItems ?? throw new InvalidOperationException())
                        .OfType<XamlStatement>().ToArray();
                    foreach (XamlStatement addedXamlStatement in addedXamlStatements)
                        addedXamlStatement.ParentItem = this;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    XamlStatement[] removedXamlStatements = (e.OldItems ?? throw new InvalidOperationException())
                        .OfType<XamlStatement>().ToArray();
                    foreach (XamlStatement removedXamlStatement in removedXamlStatements)
                        removedXamlStatement.Dispose();
                    break;
            }
        }

        #endregion
    }
}