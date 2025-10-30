using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public interface IPlayWindowBase
    {
        bool IsRootWindow { get; }

        int RootWindowNum { get; }

        CaseInsensitiveOrderedDictionary<List<object?>> WindowVariables { get; }
    }

    public interface IPlayWindow : IPlayWindowBase
    {
        IPlayWindow? ParentWindow { get; }

        PlayControlWrapper PlayControlWrapper { get; }

        /// <summary>
        ///     Frame with empty name
        /// </summary>
        Frame MainFrame { get; }

        string WindowCategory { get; }

        double Left { get; }

        double Top { get; }

        double Width { get; }

        double Height { get; }

        double ActualWidth { get; }

        double ActualHeight { get; }

        WindowState WindowState { get; set; }

        WindowStyle WindowStyle { get; set; }

        bool IsActive { get; }

        bool Activate();

        void Close();

        event EventHandler Activated;

        event CancelEventHandler Closing;

        event EventHandler Closed;
    }
}