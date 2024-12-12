using System;
using System.Collections.Generic;
using System.Windows;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Operator.Core.DsShapes;
using Ssz.Utils;
using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.DsShapeViews
{
    public class CommandListenerDsShapeView : DsShapeViewBase
    {
        #region protected functions

        protected override void OnDsShapeChanged(string? propertyName)
        {
            base.OnDsShapeChanged(propertyName);

            var dsShape = DsShapeViewModel.DsShape as CommandListenerDsShape;
            if (dsShape is null) return;

            if (propertyName is null || propertyName == nameof(dsShape.CommandToListenDsCommand))
                if (!VisualDesignMode)
                {
                    if (_commandToListenDsCommandView is not null) _commandToListenDsCommandView.Dispose();
                    _commandToListenDsCommandView = new DsCommandView(Frame,
                        dsShape.CommandToListenDsCommand,
                        DsShapeViewModel);
                }

            if (propertyName is null || propertyName == nameof(dsShape.LoadedDsCommand))
                if (!VisualDesignMode)
                {
                    if (_loadedDsCommandView is not null) _loadedDsCommandView.Dispose();
                    _loadedDsCommandView = new DsCommandView(Frame,
                        dsShape.LoadedDsCommand,
                        DsShapeViewModel);
                }

            if (propertyName is null || propertyName == nameof(dsShape.EachSecondDsCommand))
                if (!VisualDesignMode)
                {
                    if (_eachSecondDsCommandView is not null)
                    {
                        if (!_eachSecondDsCommandView.IsEmpty)
                            DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEventEvent;
                        _eachSecondDsCommandView.Dispose();
                    }

                    _eachSecondDsCommandView = new DsCommandView(Frame,
                        dsShape.EachSecondDsCommand,
                        DsShapeViewModel);
                    if (!_eachSecondDsCommandView.IsEmpty)
                        DsProject.Instance.GlobalUITimerEvent += OnGlobalUITimerEventEvent;
                }

            if (propertyName is null ||
                propertyName == nameof(dsShape.ConditionalDsCommandsCollection))
                if (!VisualDesignMode)
                {
                    if (_conditionalDsCommandViewsCollection is not null)
                        foreach (DsCommandView dsCommandView in _conditionalDsCommandViewsCollection)
                        {
                            dsCommandView.IsEnabledChanged -= DsCommandViewOnIsEnabledChanged;
                            dsCommandView.Dispose();
                        }

                    _conditionalDsCommandViewsCollection = new List<DsCommandView>();

                    foreach (DsCommand dsCommand in dsShape.ConditionalDsCommandsCollection)
                    {
                        var dsCommandView = new DsCommandView(Frame,
                            dsCommand,
                            DsShapeViewModel);
                        dsCommandView.IsEnabledChanged += DsCommandViewOnIsEnabledChanged;
                        _conditionalDsCommandViewsCollection.Add(dsCommandView);
                    }
                }

            if (propertyName is null || propertyName == nameof(dsShape.UnloadedDsCommand))
                if (!VisualDesignMode)
                {
                    if (_unloadedDsCommandView is not null) _unloadedDsCommandView.Dispose();
                    _unloadedDsCommandView = new DsCommandView(Frame,
                        dsShape.UnloadedDsCommand,
                        DsShapeViewModel);
                }
        }

        #endregion

        #region construction and destruction

        public CommandListenerDsShapeView(CommandListenerDsShape dsShape, Frame? frame)
            : base(dsShape, frame)
        {
            if (!VisualDesignMode)
            {
                CommandsManager.AddCommandHandler(CommandsManagerOnGotCommand);

                Loaded += OnLoaded;
            }
        }


        protected override async void Dispose(bool disposing)
        {
            if (Disposed) return;
            if (disposing)
                if (!VisualDesignMode)
                {
                    CommandsManager.RemoveCommandHandler(CommandsManagerOnGotCommand);

                    if (_commandToListenDsCommandView is not null)
                    {
                        _commandToListenDsCommandView.Dispose();
                        _commandToListenDsCommandView = null;
                    }

                    if (_loadedDsCommandView is not null)
                    {
                        _loadedDsCommandView.Dispose();
                        _loadedDsCommandView = null;
                    }

                    if (_eachSecondDsCommandView is not null)
                    {
                        if (!_eachSecondDsCommandView.IsEmpty)
                            DsProject.Instance.GlobalUITimerEvent -= OnGlobalUITimerEventEvent;

                        _eachSecondDsCommandView.Dispose();
                        _eachSecondDsCommandView = null;
                    }

                    if (_conditionalDsCommandViewsCollection is not null)
                    {
                        foreach (DsCommandView dsCommandView in _conditionalDsCommandViewsCollection)
                        {
                            dsCommandView.IsEnabledChanged -= DsCommandViewOnIsEnabledChanged;
                            dsCommandView.Dispose();
                        }

                        _conditionalDsCommandViewsCollection.Clear();
                        _conditionalDsCommandViewsCollection = null;
                    }

                    if (_unloadedDsCommandView is not null)
                    {
                        await _unloadedDsCommandView.DoCommandAsync();

                        _unloadedDsCommandView.Dispose();
                        _unloadedDsCommandView = null;
                    }
                }

            // Release unmanaged resources.
            // Set large fields to null.            
            base.Dispose(disposing);
        }

        #endregion

        #region private functions

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (_loadedDsCommandView is not null) _loadedDsCommandView.DoCommand();

            if (_conditionalDsCommandViewsCollection is not null)
                foreach (DsCommandView dsCommandView in _conditionalDsCommandViewsCollection)
                    if (dsCommandView.IsEnabled)
                        dsCommandView.DoCommand();
        }

        private void CommandsManagerOnGotCommand(Command command)
        {
            if (Disposed) return;

            string commandToListen = ((CommandListenerDsShape) DsShapeViewModel.DsShape).CommandToListen;

            if (!StringHelper.CompareIgnoreCase(command.CommandString, commandToListen)) return;

            string commandToDo = ((CommandListenerDsShape) DsShapeViewModel.DsShape).CommandToListenDsCommand
                .Command;

            if (StringHelper.CompareIgnoreCase(commandToListen, commandToDo)) return;

            switch (((CommandListenerDsShape) DsShapeViewModel.DsShape).CommandToListenOptions)
            {
                case CommandListenerOptions.ListenOnlyInActiveDsPage:
                    var window = Frame is not null ? Frame.PlayWindow as Window : null;
                    if (window is null || !window.IsActive) return;

                    var playDrawingCanvas = TreeHelper.FindParent<PlayDrawingCanvas>(this);
                    if (playDrawingCanvas is null) return;
                    if (playDrawingCanvas.IsVisible)
                        if (_commandToListenDsCommandView is not null)
                            Dispatcher.BeginInvoke(new Action(_commandToListenDsCommandView.DoCommand));
                    return;
                default:
                    if (_commandToListenDsCommandView is not null)
                        Dispatcher.BeginInvoke(new Action(_commandToListenDsCommandView.DoCommand));
                    return;
            }
        }

        private void OnGlobalUITimerEventEvent(int phase)
        {
            if (Disposed) return;

            if (phase == 0 && _eachSecondDsCommandView is not null) _eachSecondDsCommandView.DoCommand();
        }

        private void DsCommandViewOnIsEnabledChanged(object? sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is not null && e.OldValue is bool && !(bool) e.OldValue && e.NewValue is bool && (bool) e.NewValue)
                ((DsCommandView) sender).DoCommand();
        }

        #endregion

        #region private fields

        private DsCommandView? _commandToListenDsCommandView;
        private DsCommandView? _loadedDsCommandView;
        private DsCommandView? _eachSecondDsCommandView;
        private List<DsCommandView>? _conditionalDsCommandViewsCollection;
        private DsCommandView? _unloadedDsCommandView;

        #endregion
    }
}