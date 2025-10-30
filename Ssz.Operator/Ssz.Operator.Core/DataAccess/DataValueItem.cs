using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.ControlsPlay;

using Ssz.Operator.Core.DataEngines;
using Ssz.Operator.Core.Drawings;
using Ssz.Operator.Core.Utils;
using Ssz.Utils;
using Ssz.Utils.DataAccess;

namespace Ssz.Operator.Core.DataAccess
{
    internal class DataValueItem : IDisposable, IValueSubscription
    {
        #region construction and destruction

        public DataValueItem(string dataSourceString,
            CaseInsensitiveOrderedDictionary<List<object?>>? globalVariables, IPlayWindowBase? playWindow,
            bool visualDesignMode)
        {
            _globalVariables = globalVariables;
            _playWindow = playWindow;
            _visualDesignMode = visualDesignMode;

            DataItemHelper.ParseDataSourceString(dataSourceString, out _dataSourceType,
                out _dataSourceIdString, out _defaultValueString);

            switch (_dataSourceType)
            {
                case DataSourceType.OpcVariable:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        DsDataAccessProvider.Instance.AddItem(_dataSourceIdString, this);
                    }
                    else
                    {
                        var v = DsProject.Instance.CsvDb.GetValue(DsProject.Instance.DataEngine.ElementIdsMapFileName,
                            _dataSourceIdString, 1);
                        var constAny = ElementIdsMap.TryGetConstValue(v);
                        if (constAny.HasValue)
                            _value = constAny.Value;
                        else
                            _value = new Any();
                    }
                    break;
                case DataSourceType.Constant:
                    if (_dataSourceIdString != @"")
                        _value = new Any(_dataSourceIdString);
                    else
                        _value = new Any();
                    break;
                case DataSourceType.ParamType:
                    if (_dataSourceIdString != @"")
                        _value = new Any(_dataSourceIdString);
                    else
                        _value = new Any();
                    break;
                case DataSourceType.AlarmUnacked:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        PlayDsProjectView.EventSourceModel.AddSubscription(EventSourceModel.AlarmsAny_SubscriptionType, _dataSourceIdString.Split(',')[0] + "," + EventSourceModelSubscriptionScope.Unacked, this);
                    }
                    else
                    {
                        _value = new Any();
                    }
                    break;
                case DataSourceType.AlarmCategory:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        PlayDsProjectView.EventSourceModel.AddSubscription(EventSourceModel.AlarmMaxCategoryId_SubscriptionType, _dataSourceIdString, this);
                    }
                    else
                    {
                        _value = new Any();
                    }
                    break;
                case DataSourceType.AlarmBrush:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        PlayDsProjectView.EventSourceModel.AddSubscription(DsEventSourceModel.AlarmBrush_SubscriptionType, _dataSourceIdString, this);
                    }
                    else
                    {
                        _value = new Any();
                    }
                    break;
                case DataSourceType.AlarmCondition:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        PlayDsProjectView.EventSourceModel.AddSubscription(EventSourceModel.AlarmConditionType_SubscriptionType, _dataSourceIdString, this);
                    }
                    else
                    {
                        _value = new Any();
                    }
                    break;
                case DataSourceType.PageExists:
                    CaseInsensitiveOrderedDictionary<DsPageDrawing> allDsPagesCache = DsProject.Instance.AllDsPagesCache;
                    if (allDsPagesCache.Count == 0) _value = new Any(true);
                    else
                        _value = new Any(allDsPagesCache.ContainsKey(_dataSourceIdString));
                    break;
                case DataSourceType.CsvDbFileExists:
                    _value = new Any(DsProject.Instance.CsvDb.FileExists(_dataSourceIdString));
                    break;
                case DataSourceType.GlobalVariable:
                    ParseVariableNameAndIndex();
                    break;
                case DataSourceType.WindowVariable:
                    ParseVariableNameAndIndex();
                    break;
                case DataSourceType.RootWindowNum:
                    if (_playWindow is null) _value = new Any(0);
                    else _value = new Any(_playWindow.RootWindowNum);
                    break;
                case DataSourceType.Random:
                    break;
                case DataSourceType.CurrentTimeSeconds:
                    break;
                case DataSourceType.TagNameToDisplay:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        var genericContainer = new GenericContainer();
                        genericContainer.ParentItem = DsProject.Instance;
                        genericContainer.DsConstantsCollection.Add(new DsConstant(DataEngineBase.TagConstant,
                            _dataSourceIdString));
                        _dataValueFrameworkElement = new DataValueFrameworkElement(genericContainer, null,
                            DsProject.Instance.DataEngine.TagNameToDisplayInfo);
                        _dataValueFrameworkElement.ValueChanged +=
                            o =>
                            {
                                Update(new ValueStatusTimestamp(new Any(o)));
                            };
                    }
                    else
                    {
                        _value = new Any();
                    }
                    break;
                case DataSourceType.TagDescription:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        var genericContainer = new GenericContainer();
                        genericContainer.ParentItem = DsProject.Instance;
                        genericContainer.DsConstantsCollection.Add(new DsConstant(DataEngineBase.TagConstant,
                            _dataSourceIdString));
                        _dataValueFrameworkElement = new DataValueFrameworkElement(genericContainer, null,
                            DsProject.Instance.DataEngine.TagDescInfo);
                        _dataValueFrameworkElement.ValueChanged +=
                            o =>
                            {
                                Update(new ValueStatusTimestamp(new Any(o)));
                            };
                    }
                    else
                    {
                        _value = new Any();
                    }

                    break;
                case DataSourceType.PageNameOfRootWindowWithNum:
                case DataSourceType.PageDescOfRootWindowWithNum:
                case DataSourceType.PageGroupOfRootWindowWithNum:
                    break;
                case DataSourceType.AlarmsCount:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        PlayDsProjectView.EventSourceModel.AddSubscription(EventSourceModel.AlarmsCount_SubscriptionType, _dataSourceIdString, this);
                    }
                    else
                    {
                        _value = new Any();
                    }
                    break;
                case DataSourceType.StatusCode:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        DsDataAccessProvider.Instance.AddItem(_dataSourceIdString, this);
                    }
                    else
                    {
                        _value = new Any(false);
                    }
                    break;
                case DataSourceType.BuzzerState:                    
                    break;
                case DataSourceType.BuzzerIsEnabled:
                    break;
                case DataSourceType.Passthrough:
                    if (!_visualDesignMode)
                    {
                        ValueUpdatedEvent.Reset();
                        try
                        {
                            DsDataAccessProvider.Instance.Passthrough(@"", _dataSourceIdString, ReadOnlyMemory<byte>.Empty, r =>
                            {
                                if (r.Length == 0)
                                {
                                    _value = new Any(@"");
                                }
                                else
                                {
                                    try
                                    {
                                        _value = new Any(Encoding.UTF8.GetString(r.ToArray()));
                                    }
                                    catch
                                    {
                                        _value = new Any(@"");
                                    }                                    
                                }
                                ValueUpdatedEvent.Set();
                            });
                        }
                        catch
                        {                            
                            _value = new Any(@"");
                            ValueUpdatedEvent.Reset();
                        }
                    }
                    else
                    {
                        _value = new Any(@"");
                    }
                    break;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Release and Dispose managed resources.
                switch (_dataSourceType)
                {
                    case DataSourceType.OpcVariable:
                        if (!_visualDesignMode) 
                            DsDataAccessProvider.Instance.RemoveItem(this);
                        break;
                    case DataSourceType.AlarmUnacked:
                        if (!_visualDesignMode)
                            PlayDsProjectView.EventSourceModel.RemoveSubscription(
                                _dataSourceIdString.Split(',')[0] + "," + EventSourceModelSubscriptionScope.Unacked,
                                this);
                        break;
                    case DataSourceType.AlarmCategory:
                        if (!_visualDesignMode)
                            PlayDsProjectView.EventSourceModel.RemoveSubscription(
                                _dataSourceIdString,
                                this);
                        break;
                    case DataSourceType.AlarmCondition:
                        if (!_visualDesignMode)
                            PlayDsProjectView.EventSourceModel.RemoveSubscription(
                                _dataSourceIdString,
                                this);
                        break;
                    case DataSourceType.AlarmBrush:
                        if (!_visualDesignMode)
                            PlayDsProjectView.EventSourceModel.RemoveSubscription(
                                _dataSourceIdString,
                                this);
                        break;
                    case DataSourceType.AlarmsCount:
                        if (!_visualDesignMode)
                            PlayDsProjectView.EventSourceModel.RemoveSubscription(
                                _dataSourceIdString,
                                this);
                        break;
                    case DataSourceType.StatusCode:
                        if (!_visualDesignMode)
                            DsDataAccessProvider.Instance.RemoveItem(this);
                        break;
                }

                if (_dataValueFrameworkElement is not null) _dataValueFrameworkElement.Dispose();

                if (_cancellationTokenSource is not null) _cancellationTokenSource.Cancel();
            }
            // Release unmanaged resources.
            // Set large fields to null.

            _cancellationTokenSource = null;
            _writtenValue = null;
            _globalVariables = null;
            _playWindow = null;
            _dataValueFrameworkElement = null;

            _disposed = true;
        }


        ~DataValueItem()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public string ElementId => _dataSourceIdString;

        public object? Value
        {
            get
            {
                switch (_dataSourceType)
                {
                    case DataSourceType.OpcVariable:
                        if (!_value.HasValue) 
                            return DependencyProperty.UnsetValue;
                        Any value;
                        if (_writtenValue.HasValue) 
                            value = _writtenValue.Value;
                        else 
                            value = _value.Value;
                        switch (value.ValueTypeCode)
                        {
                            case Ssz.Utils.Any.TypeCode.Empty:
                                return _defaultValueString;
                            case Ssz.Utils.Any.TypeCode.DBNull:
                                return _defaultValueString;                            
                            default:
                                return value.ValueAsObject();
                        }
                    case DataSourceType.Constant:
                        if (_value!.Value.ValueTypeCode == Ssz.Utils.Any.TypeCode.Empty) 
                            return _defaultValueString;
                        return _value.Value.ValueAsObject();
                    case DataSourceType.ParamType:
                        if (_value!.Value.ValueTypeCode == Ssz.Utils.Any.TypeCode.Empty) 
                            return _defaultValueString;
                        return _value.Value.ValueAsObject();
                    case DataSourceType.AlarmUnacked:
                        if (!_value.HasValue) 
                            return DependencyProperty.UnsetValue;
                        if (_value.Value.ValueTypeCode == Ssz.Utils.Any.TypeCode.Empty)
                            return ObsoleteAnyHelper.ConvertTo<bool>(_defaultValueString, false);
                        return _value.Value.ValueAsBoolean(false);
                    case DataSourceType.AlarmCategory:
                        if (!_value.HasValue) 
                            return DependencyProperty.UnsetValue;
                        if (_value.Value.ValueTypeCode == Ssz.Utils.Any.TypeCode.Empty)
                            return ObsoleteAnyHelper.ConvertTo<int>(_defaultValueString, false);
                        return _value.Value.ValueAsInt32(false);
                    case DataSourceType.AlarmBrush:
                        if (!_value.HasValue) 
                            return DependencyProperty.UnsetValue;
                        if (_value.Value.ValueTypeCode == Ssz.Utils.Any.TypeCode.Empty) 
                            return _defaultValueString;
                        return _value.Value.ValueAsObject();
                    case DataSourceType.AlarmCondition:
                        if (!_value.HasValue) 
                            return DependencyProperty.UnsetValue;
                        if (_value.Value.ValueTypeCode == Ssz.Utils.Any.TypeCode.Empty)
                            return ObsoleteAnyHelper.ConvertTo<int>(_defaultValueString, false);
                        return _value.Value.ValueAsInt32(false);
                    case DataSourceType.PageExists:
                        return _value!.Value.ValueAsObject();
                    case DataSourceType.CsvDbFileExists:
                        return _value!.Value.ValueAsObject();
                    case DataSourceType.GlobalVariable:
                        return GetVariableValue(_globalVariables);
                    case DataSourceType.WindowVariable:
                        return GetVariableValue(_playWindow is not null ? _playWindow.WindowVariables : null);
                    case DataSourceType.RootWindowNum:
                        return _value!.Value.ValueAsObject();
                    case DataSourceType.Random:
                        return _random.NextDouble();
                    case DataSourceType.CurrentTimeSeconds:
                        return DsProject.Instance.CurrentTimeSeconds;
                    case DataSourceType.TagNameToDisplay:
                        if (!_value.HasValue) 
                            return DependencyProperty.UnsetValue;
                        return _value.Value.ValueAsObject();
                    case DataSourceType.TagDescription:
                        if (!_value.HasValue) 
                            return DependencyProperty.UnsetValue;
                        return _value.Value.ValueAsObject();
                    case DataSourceType.PageNameOfRootWindowWithNum:
                    {
                        var dsPageDrawing =
                            PlayDsProjectView.GetDsPageDrawing(_playWindow as IPlayWindow, _dataSourceIdString);
                        if (dsPageDrawing is null) 
                                return @"";
                        return dsPageDrawing.Name;
                    }
                    case DataSourceType.PageDescOfRootWindowWithNum:
                    {
                        var dsPageDrawing =
                            PlayDsProjectView.GetDsPageDrawing(_playWindow as IPlayWindow, _dataSourceIdString);
                        if (dsPageDrawing is null) 
                                return @"";
                        return dsPageDrawing.Desc;
                    }
                    case DataSourceType.PageGroupOfRootWindowWithNum:
                    {
                        var dsPageDrawing =
                            PlayDsProjectView.GetDsPageDrawing(_playWindow as IPlayWindow, _dataSourceIdString);
                        if (dsPageDrawing is null) return @"";
                        return dsPageDrawing.Group;
                    }
                    case DataSourceType.AlarmsCount:
                        if (!_value.HasValue) 
                            return DependencyProperty.UnsetValue;
                        if (_value.Value.ValueTypeCode == Ssz.Utils.Any.TypeCode.Empty)
                            return ObsoleteAnyHelper.ConvertTo<uint>(_defaultValueString, false);
                        return _value.Value.ValueAsUInt32(false);
                    case DataSourceType.StatusCode:
                        if (!_value.HasValue) 
                            return DependencyProperty.UnsetValue;                        
                        Any value2 = _value.Value;
                        switch (value2.ValueTypeCode)
                        {
                            case Ssz.Utils.Any.TypeCode.Empty:
                                return _defaultValueString;
                            case Ssz.Utils.Any.TypeCode.DBNull:
                                return _defaultValueString;                            
                            default:
                                return value2.ValueAsObject();
                        }
                    case DataSourceType.BuzzerState:
                        return PlayDsProjectView.Buzzer.BuzzerState;
                    case DataSourceType.BuzzerIsEnabled:
                        return PlayDsProjectView.Buzzer.IsEnabled;
                    case DataSourceType.Passthrough:
                        if (!_value.HasValue)
                            return @"";
                        else
                            return _value.Value.ValueAsString(false);
                }

                throw new NotImplementedException();
            }
            set
            {
                switch (_dataSourceType)
                {
                    case DataSourceType.OpcVariable:
                        if (!_visualDesignMode)
                        {
                            if (_cancellationTokenSource is not null) 
                                _cancellationTokenSource.Cancel();

                            BeforeWriteValueEventArgs args = new()
                            {
                                ElementId = _dataSourceIdString,
                                NewValue = value,
                            };
                            PlayDsProjectView.OnBeforeWriteValue(this, args);
                            if (args.Cancel) 
                                return;

                            _writtenValue = new Any(value);
                            _writtenValueGuid = Guid.NewGuid();
                            _cancellationTokenSource = new CancellationTokenSource();
                            var token = _cancellationTokenSource.Token;
                            Task.Factory.StartNew(writtenValueGuid =>
                            {
                                Thread.Sleep(3000);
                                try
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        if (writtenValueGuid is null) throw new InvalidOperationException();
                                        if ((Guid) writtenValueGuid == _writtenValueGuid) _writtenValue = null;
                                    }, DispatcherPriority.Normal, token);
                                }
                                catch (Exception)
                                {
                                }
                            }, _writtenValueGuid, _cancellationTokenSource.Token);

                            var t = DsDataAccessProvider.Instance.WriteAsync(this,
                                new ValueStatusTimestamp(new Any(value), StatusCodes.Good, DateTime.UtcNow));
                        }

                        break;
                    case DataSourceType.GlobalVariable:
                        SetVariableValue(_globalVariables, value);
                        break;
                    case DataSourceType.WindowVariable:
                        SetVariableValue(_playWindow is not null ? _playWindow.WindowVariables : null, value);
                        break;
                    case DataSourceType.BuzzerState:
                        PlayDsProjectView.Buzzer.BuzzerState = new Any(value).ValueAs<BuzzerStateEnum>(false);
                        break;
                    case DataSourceType.BuzzerIsEnabled:
                        PlayDsProjectView.Buzzer.IsEnabled = new Any(value).ValueAs<bool>(false);
                        break;
                }
            }
        }

        public void Update(string mappedElementIdOrConst)
        {
        }        

        public ManualResetEvent ValueUpdatedEvent { get; } = new(true);

        public void Update(ValueStatusTimestamp valueStatusTimestamp)
        {
            switch (_dataSourceType)
            {
                case DataSourceType.StatusCode:
                    _value = new Any(valueStatusTimestamp.StatusCode);
                    break;
                default:                    
                    if (StatusCodes.IsBad(valueStatusTimestamp.StatusCode))
                        _value = new Any(DBNull.Value);
                    else if (StatusCodes.IsUncertain(valueStatusTimestamp.StatusCode))
                        _value = new Any();
                    else
                        _value = valueStatusTimestamp.Value;
                    break;
            }            

            ValueUpdatedEvent.Set();
        }

        #endregion

        #region private functions

        private void ParseVariableNameAndIndex()
        {
            _variableName = @"";
            string v = _dataSourceIdString.Trim();
            if (v.StartsWith("$(") && v.EndsWith(")"))
                ConstantsHelper.ParseVariableNameAndIndex(v.Substring(2, v.Length - 3), out _variableName,
                    out _variableIndex);
        }

        private object GetVariableValue(CaseInsensitiveOrderedDictionary<List<object?>>? variables)
        {
            if (variables is null || _variableName == @"") return _defaultValueString;
            List<object?>? variableValues;
            if (!variables.TryGetValue(_variableName, out variableValues)) return _defaultValueString;
            if (_variableIndex < 0 || _variableIndex >= variableValues.Count) return _defaultValueString;
            return variableValues[_variableIndex] ?? _defaultValueString;
        }

        private void SetVariableValue(CaseInsensitiveOrderedDictionary<List<object?>>? variables, object? value)
        {
            if (variables is null || _variableName == @"") return;
            List<object?>? variableValues;
            if (!variables.TryGetValue(_variableName, out variableValues))
            {
                variableValues = new List<object?> {null};
                variables.Add(_variableName, variableValues);
            }

            if (_variableIndex < 0 || _variableIndex >= 0xFFFF) return;
            if (_variableIndex >= variableValues.Count)
            {
                variableValues.Capacity = _variableIndex + 1;
                variableValues.AddRange(Enumerable.Repeat<object?>(null, _variableIndex + 1 - variableValues.Count));
            }

            variableValues[_variableIndex] = value;
        }

        #endregion

        #region private fields

        private bool _disposed;

        private static readonly Random _random = new();
        private readonly DataSourceType _dataSourceType;

        private readonly string _dataSourceIdString;

        private readonly string _defaultValueString;
        private CaseInsensitiveOrderedDictionary<List<object?>>? _globalVariables;

        private string _variableName = @"";
        private int _variableIndex;

        private IPlayWindowBase? _playWindow;
        private readonly bool _visualDesignMode;
        private Any? _value;
        private Any? _writtenValue;
        private Guid _writtenValueGuid;
        private CancellationTokenSource? _cancellationTokenSource;
        private DataValueFrameworkElement? _dataValueFrameworkElement;

        #endregion
    }
}