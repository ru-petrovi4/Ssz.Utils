/*************************************************************************************
   
   Toolkit for WPF

   Copyright (C) 2007-2018 Ssz.Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at https://xceed.com/xceed-toolkit-plus-for-wpf/

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Xml.Serialization;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [Serializable]
    public abstract class LayoutElement : DependencyObject, ILayoutElement
    {
        #region Constructors

        internal LayoutElement()
        {
        }

        #endregion

        #region Public Methods

#if TRACE
        public virtual void ConsoleDump(int tab)
        {
            Trace.Write(new string(' ', tab * 4));
            Trace.WriteLine(ToString());
        }
#endif

        #endregion

        #region Members

        [NonSerialized] private ILayoutContainer _parent;

        [NonSerialized] private ILayoutRoot _root;

        #endregion

        #region Properties

        #region Parent

        [XmlIgnore]
        public ILayoutContainer Parent
        {
            get => _parent;
            set
            {
                if (_parent != value)
                {
                    var oldValue = _parent;
                    var oldRoot = _root;
                    RaisePropertyChanging("Parent");
                    OnParentChanging(oldValue, value);
                    _parent = value;
                    OnParentChanged(oldValue, value);

                    _root = Root;
                    if (oldRoot != _root)
                        OnRootChanged(oldRoot, _root);

                    RaisePropertyChanged("Parent");

                    var root = Root as LayoutRoot;
                    if (root is not null)
                        root.FireLayoutUpdated();
                }
            }
        }

        #endregion

        #region Root

        public ILayoutRoot Root
        {
            get
            {
                var parent = Parent;

                while (parent is not null && !(parent is ILayoutRoot)) parent = parent.Parent;

                return parent as ILayoutRoot;
            }
        }

        #endregion

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Provides derived classes an opportunity to handle execute code before to the Parent property changes.
        /// </summary>
        protected virtual void OnParentChanging(ILayoutContainer oldValue, ILayoutContainer newValue)
        {
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the Parent property.
        /// </summary>
        protected virtual void OnParentChanged(ILayoutContainer oldValue, ILayoutContainer newValue)
        {
        }


        protected virtual void OnRootChanged(ILayoutRoot oldRoot, ILayoutRoot newRoot)
        {
            if (oldRoot is not null)
                ((LayoutRoot) oldRoot).OnLayoutElementRemoved(this);
            if (newRoot is not null)
                ((LayoutRoot) newRoot).OnLayoutElementAdded(this);
        }

        protected virtual void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged is not null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void RaisePropertyChanging(string propertyName)
        {
            if (PropertyChanging is not null)
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }

        #endregion

        #region Events

        [field: NonSerialized]
        [field: XmlIgnore]
        public event PropertyChangedEventHandler PropertyChanged;

        [field: NonSerialized]
        [field: XmlIgnore]
        public event PropertyChangingEventHandler PropertyChanging;

        #endregion
    }
}