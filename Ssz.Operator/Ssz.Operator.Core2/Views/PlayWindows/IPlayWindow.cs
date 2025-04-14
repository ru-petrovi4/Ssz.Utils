using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Ssz.Operator.Core.ControlsPlay;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    public interface IPlayWindowBase
    {
        bool IsRootWindow { get; }

        int RootWindowNum { get; }

        CaseInsensitiveDictionary<List<object?>> WindowVariables { get; }
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

        PixelPoint Position { get; set; }

        double Width { get; }

        double Height { get; }

        Rect Bounds { get; }        

        WindowState WindowState { get; set; }

        //WindowStyle WindowStyle { get; set; }

        bool IsActive { get; }

        void Activate();

        void Close();

        event EventHandler? Activated;

        event EventHandler<WindowClosingEventArgs>? Closing;

        event EventHandler? Closed;
    }
}