using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using Ssz.Operator.Core.Commands.DsCommandOptions;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DataAccess;
using Ssz.Operator.Core.DsShapeViews;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core.Commands
{
    public class DsCommandView : FrameworkElement, IDisposable
    {
        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region construction and destruction

        public DsCommandView(Frame? frame, DsCommand dsCommand, DataValueViewModel dataViewModel)
        {
            var container = dsCommand.ParentItem.Find<IDsContainer>();
            _command = ConstantsHelper.ComputeValue(container, dsCommand.Command)!;

            if (IsEmpty) return;

            _frame = frame;

            DataContext = dataViewModel;

            switch (_command)
            {
                case CommandsManager.SetValueCommand:
                    var setValueDsCommandOptions =
                        (SetValueDsCommandOptions) (dsCommand.DsCommandOptions ??
                                                    throw new InvalidOperationException());
                    _valueBindingExpression = this.SetBindingOrConst(container,
                        ValueProperty, setValueDsCommandOptions.ValueInfo, BindingMode.TwoWay,
                        UpdateSourceTrigger.Explicit);
                    break;
                case CommandsManager.CommandsListCommand:
                    var commandsListDsCommandOptions =
                        (CommandsListDsCommandOptions) (dsCommand.DsCommandOptions ??
                                                        throw new InvalidOperationException());
                    _dsCommandViewsList = new List<DsCommandView>();
                    foreach (var ci in commandsListDsCommandOptions.DsCommandsList)
                    {
                        ci.ParentItem = dsCommand.ParentItem;
                        _dsCommandViewsList.Add(new DsCommandView(_frame, ci, dataViewModel));
                    }

                    break;
                default:
                    _dsCommandOptions = dsCommand.DsCommandOptions;
                    _parentItem = dsCommand.ParentItem;
                    break;
            }

            dsCommand.IsEnabledInfo.FallbackValue = false;
            this.SetBindingOrConst(container,
                IsEnabledProperty, dsCommand.IsEnabledInfo, BindingMode.OneWay, UpdateSourceTrigger.Default);
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
                if (!IsEmpty)
                {
                    if (_dsCommandViewsList is not null)
                    {
                        foreach (var dsCommandView in _dsCommandViewsList) dsCommandView.Dispose();
                        _dsCommandViewsList = null;
                    }

                    _valueBindingExpression = null;

                    _frame = null;
                }

            // Release unmanaged resources.
            // Set large fields to null.            
            Disposed = true;
        }


        ~DsCommandView()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(ButtonDsShapeViewBase),
                new PropertyMetadata(null));


        public bool IsEmpty => string.IsNullOrEmpty(_command);


        public async void DoCommand()
        {
            await DoCommandAsync();
        }

        public async Task DoCommandAsync()
        {
            if (IsEmpty || Disposed) return;

            if (!IsEnabled) return;

            switch (_command)
            {
                case CommandsManager.SetValueCommand:
                    var valueBindingExpression = _valueBindingExpression;
                    if (valueBindingExpression is not null)
                    {
                        var dataViewModel = DataContext as DataValueViewModel;
                        if (dataViewModel is not null) 
                            await dataViewModel.WaitAllDataValueItemsUpdated();
                        valueBindingExpression.UpdateTarget();
                        valueBindingExpression.UpdateSource();
                    }
                    break;
                case CommandsManager.CommandsListCommand:
                    if (_dsCommandViewsList is null) throw new InvalidOperationException();
                    var dsCommandViews = _dsCommandViewsList.ToArray();
                    foreach (var dsCommandView in dsCommandViews) 
                        await dsCommandView.DoCommandAsync();
                    break;
                default:
                    object? dsCommandOptionsClone = null;
                    if (_dsCommandOptions is not null)
                    {
                        dsCommandOptionsClone = (OwnedDataSerializableAndCloneable) _dsCommandOptions.Clone();
                        var itemClone = dsCommandOptionsClone as IDsItem;
                        if (itemClone is not null)
                        {
                            itemClone.ParentItem = _parentItem;
                            ItemHelper.ReplaceConstants(itemClone, _parentItem.Find<IDsContainer>());
                        }
                    }
                    await Task.Delay(100);
                    CommandsManager.NotifyCommand(_frame, _command, dsCommandOptionsClone);
                    //await Dispatcher.BeginInvoke(new Action(() =>
                    //{
                    //    CommandsManager.NotifyCommand(_frame, _command, dsCommandOptionsClone);
                    //}));
                    break;
            }
        }

        #endregion

        #region private fields

        private Frame? _frame;
        private readonly string _command;

        private BindingExpressionBase? _valueBindingExpression;
        private List<DsCommandView>? _dsCommandViewsList;
        private readonly OwnedDataSerializableAndCloneable? _dsCommandOptions;
        private readonly IDsItem? _parentItem;

        #endregion
    }
}