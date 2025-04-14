using System.Diagnostics.CodeAnalysis;
using Avalonia;

namespace Ssz.Operator.Core.ControlsPlay
{
    public class Frame
    {
        public Frame(IPlayWindow playWindow, string frameName)
        {
            PlayWindow = playWindow;
            FrameName = frameName;
        }

        public IPlayWindow PlayWindow { get; }

        public string FrameName { get; }
    }
}