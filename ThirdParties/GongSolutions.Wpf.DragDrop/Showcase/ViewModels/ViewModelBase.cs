﻿using System.ComponentModel;
using System.Runtime.CompilerServices;


namespace Showcase.WPF.DragDrop.ViewModels
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /*[NotifyPropertyChangedInvocator]*/
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}