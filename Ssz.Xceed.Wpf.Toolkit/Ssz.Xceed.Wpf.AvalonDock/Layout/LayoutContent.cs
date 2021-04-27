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
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using Ssz.Xceed.Wpf.AvalonDock.Controls;

namespace Ssz.Xceed.Wpf.AvalonDock.Layout
{
    [ContentProperty("Content")]
    [Serializable]
    public abstract class LayoutContent : LayoutElement, IXmlSerializable, ILayoutElementForFloatingWindow,
        IComparable<LayoutContent>, ILayoutPreviousContainer
    {
        #region Constructors

        internal LayoutContent()
        {
        }

        #endregion

        #region Properties

        #region Title

        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string),
            typeof(LayoutContent), new UIPropertyMetadata(null, OnTitlePropertyChanged, CoerceTitleValue));

        public string Title
        {
            get => (string) GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        private static object CoerceTitleValue(DependencyObject obj, object value)
        {
            var lc = (LayoutContent) obj;
            if ((string) value != lc.Title) lc.RaisePropertyChanging(TitleProperty.Name);
            return value;
        }

        private static void OnTitlePropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            ((LayoutContent) obj).RaisePropertyChanged(TitleProperty.Name);
        }

        #endregion //Title

        #region Content

        [NonSerialized] private object _content;

        [XmlIgnore]
        public object Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    RaisePropertyChanging("Content");
                    _content = value;
                    RaisePropertyChanged("Content");

                    if (ContentId == null)
                    {
                        var contentAsControl = _content as FrameworkElement;
                        if (contentAsControl != null && !string.IsNullOrWhiteSpace(contentAsControl.Name))
                            SetCurrentValue(ContentIdProperty, contentAsControl.Name);
                    }
                }
            }
        }

        #endregion

        #region ContentId

        public static readonly DependencyProperty ContentIdProperty = DependencyProperty.Register("ContentId",
            typeof(string), typeof(LayoutContent), new UIPropertyMetadata(null, OnContentIdPropertyChanged));

        public string ContentId
        {
            get => (string) GetValue(ContentIdProperty);
            set => SetValue(ContentIdProperty, value);
        }

        private static void OnContentIdPropertyChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var layoutContent = obj as LayoutContent;
            if (layoutContent != null)
                layoutContent.OnContentIdPropertyChanged((string) args.OldValue, (string) args.NewValue);
        }

        private void OnContentIdPropertyChanged(string oldValue, string newValue)
        {
            if (oldValue != newValue) RaisePropertyChanged("ContentId");
        }

        #endregion //ContentId

        #region IsSelected

        private bool _isSelected;

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    var oldValue = _isSelected;
                    RaisePropertyChanging("IsSelected");
                    _isSelected = value;
                    var parentSelector = Parent as ILayoutContentSelector;
                    if (parentSelector != null)
                        parentSelector.SelectedContentIndex = _isSelected ? parentSelector.IndexOf(this) : -1;
                    OnIsSelectedChanged(oldValue, value);
                    RaisePropertyChanged("IsSelected");
                    LayoutAnchorableTabItem.CancelMouseLeave();
                }
            }
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the IsSelected property.
        /// </summary>
        protected virtual void OnIsSelectedChanged(bool oldValue, bool newValue)
        {
            if (IsSelectedChanged != null)
                IsSelectedChanged(this, EventArgs.Empty);
        }

        public event EventHandler IsSelectedChanged;

        #endregion

        #region IsActive

        [field: NonSerialized] private bool _isActive;

        [XmlIgnore]
        public bool IsActive
        {
            get => _isActive;
            set
            {
                if (_isActive != value)
                {
                    RaisePropertyChanging("IsActive");
                    var oldValue = _isActive;

                    _isActive = value;

                    var root = Root;
                    if (root != null && _isActive)
                        root.ActiveContent = this;

                    if (_isActive)
                        IsSelected = true;

                    OnIsActiveChanged(oldValue, value);
                    RaisePropertyChanged("IsActive");
                }
            }
        }

        /// <summary>
        ///     Provides derived classes an opportunity to handle changes to the IsActive property.
        /// </summary>
        protected virtual void OnIsActiveChanged(bool oldValue, bool newValue)
        {
            if (newValue)
                LastActivationTimeStamp = DateTime.Now;

            if (IsActiveChanged != null)
                IsActiveChanged(this, EventArgs.Empty);
        }

        public event EventHandler IsActiveChanged;

        #endregion

        #region IsLastFocusedDocument

        private bool _isLastFocusedDocument;

        public bool IsLastFocusedDocument
        {
            get => _isLastFocusedDocument;
            internal set
            {
                if (_isLastFocusedDocument != value)
                {
                    RaisePropertyChanging("IsLastFocusedDocument");
                    _isLastFocusedDocument = value;
                    RaisePropertyChanged("IsLastFocusedDocument");
                }
            }
        }

        #endregion

        #region PreviousContainer

        [field: NonSerialized] private ILayoutContainer _previousContainer;

        [XmlIgnore]
        ILayoutContainer ILayoutPreviousContainer.PreviousContainer
        {
            get => _previousContainer;
            set
            {
                if (_previousContainer != value)
                {
                    _previousContainer = value;
                    RaisePropertyChanged("PreviousContainer");

                    var paneSerializable = _previousContainer as ILayoutPaneSerializable;
                    if (paneSerializable != null &&
                        paneSerializable.Id == null)
                        paneSerializable.Id = Guid.NewGuid().ToString();
                }
            }
        }

        protected ILayoutContainer PreviousContainer
        {
            get => ((ILayoutPreviousContainer) this).PreviousContainer;
            set => ((ILayoutPreviousContainer) this).PreviousContainer = value;
        }

        [XmlIgnore] string ILayoutPreviousContainer.PreviousContainerId { get; set; }

        protected string PreviousContainerId
        {
            get => ((ILayoutPreviousContainer) this).PreviousContainerId;
            set => ((ILayoutPreviousContainer) this).PreviousContainerId = value;
        }

        #endregion

        #region PreviousContainerIndex

        [field: NonSerialized] private int _previousContainerIndex = -1;

        [XmlIgnore]
        public int PreviousContainerIndex
        {
            get => _previousContainerIndex;
            set
            {
                if (_previousContainerIndex != value)
                {
                    _previousContainerIndex = value;
                    RaisePropertyChanged("PreviousContainerIndex");
                }
            }
        }

        #endregion

        #region LastActivationTimeStamp

        private DateTime? _lastActivationTimeStamp;

        public DateTime? LastActivationTimeStamp
        {
            get => _lastActivationTimeStamp;
            set
            {
                if (_lastActivationTimeStamp != value)
                {
                    _lastActivationTimeStamp = value;
                    RaisePropertyChanged("LastActivationTimeStamp");
                }
            }
        }

        #endregion

        #region FloatingWidth

        private double _floatingWidth;

        public double FloatingWidth
        {
            get => _floatingWidth;
            set
            {
                if (_floatingWidth != value)
                {
                    RaisePropertyChanging("FloatingWidth");
                    _floatingWidth = value;
                    RaisePropertyChanged("FloatingWidth");
                }
            }
        }

        #endregion

        #region FloatingHeight

        private double _floatingHeight;

        public double FloatingHeight
        {
            get => _floatingHeight;
            set
            {
                if (_floatingHeight != value)
                {
                    RaisePropertyChanging("FloatingHeight");
                    _floatingHeight = value;
                    RaisePropertyChanged("FloatingHeight");
                }
            }
        }

        #endregion

        #region FloatingLeft

        private double _floatingLeft;

        public double FloatingLeft
        {
            get => _floatingLeft;
            set
            {
                if (_floatingLeft != value)
                {
                    RaisePropertyChanging("FloatingLeft");
                    _floatingLeft = value;
                    RaisePropertyChanged("FloatingLeft");
                }
            }
        }

        #endregion

        #region FloatingTop

        private double _floatingTop;

        public double FloatingTop
        {
            get => _floatingTop;
            set
            {
                if (_floatingTop != value)
                {
                    RaisePropertyChanging("FloatingTop");
                    _floatingTop = value;
                    RaisePropertyChanged("FloatingTop");
                }
            }
        }

        #endregion

        #region IsMaximized

        private bool _isMaximized;

        public bool IsMaximized
        {
            get => _isMaximized;
            set
            {
                if (_isMaximized != value)
                {
                    RaisePropertyChanging("IsMaximized");
                    _isMaximized = value;
                    RaisePropertyChanged("IsMaximized");
                }
            }
        }

        #endregion

        #region ToolTip

        private object _toolTip;

        public object ToolTip
        {
            get => _toolTip;
            set
            {
                if (_toolTip != value)
                {
                    _toolTip = value;
                    RaisePropertyChanged("ToolTip");
                }
            }
        }

        #endregion

        #region IsFloating

        public bool IsFloating => this.FindParent<LayoutFloatingWindow>() != null;

        #endregion

        #region IconSource

        private ImageSource _iconSource;

        public ImageSource IconSource
        {
            get => _iconSource;
            set
            {
                if (_iconSource != value)
                {
                    _iconSource = value;
                    RaisePropertyChanged("IconSource");
                }
            }
        }

        #endregion

        #region CanClose

        internal bool _canClose = true;

        public bool CanClose
        {
            get => _canClose;
            set
            {
                if (_canClose != value)
                {
                    _canClose = value;
                    RaisePropertyChanged("CanClose");
                }
            }
        }

        #endregion

        #region CanFloat

        private bool _canFloat = true;

        public bool CanFloat
        {
            get => _canFloat;
            set
            {
                if (_canFloat != value)
                {
                    _canFloat = value;
                    RaisePropertyChanged("CanFloat");
                }
            }
        }

        #endregion

        #region IsEnabled

        private bool _isEnabled = true;

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    RaisePropertyChanged("IsEnabled");
                }
            }
        }

        #endregion

        #endregion

        #region Overrides

        protected override void OnParentChanging(ILayoutContainer oldValue, ILayoutContainer newValue)
        {
            var root = Root;

            if (oldValue != null)
                IsSelected = false;

            //if (root != null && _isActive && newValue == null)
            //    root.ActiveContent = null;

            base.OnParentChanging(oldValue, newValue);
        }

        protected override void OnParentChanged(ILayoutContainer oldValue, ILayoutContainer newValue)
        {
            if (IsSelected && Parent != null && Parent is ILayoutContentSelector)
            {
                var parentSelector = Parent as ILayoutContentSelector;
                parentSelector.SelectedContentIndex = parentSelector.IndexOf(this);
            }

            //var root = Root;
            //if (root != null && _isActive)
            //    root.ActiveContent = this;

            base.OnParentChanged(oldValue, newValue);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Close the content
        /// </summary>
        /// <remarks>
        ///     Please note that usually the anchorable is only hidden (not closed). By default when user click the X button
        ///     it only hides the content.
        /// </remarks>
        public abstract void Close();

        public XmlSchema GetSchema()
        {
            return null;
        }

        public virtual void ReadXml(XmlReader reader)
        {
            if (reader.MoveToAttribute("Title"))
                Title = reader.Value;
            //if (reader.MoveToAttribute("IconSource"))
            //    IconSource = new Uri(reader.Value, UriKind.RelativeOrAbsolute);

            if (reader.MoveToAttribute("IsSelected"))
                IsSelected = bool.Parse(reader.Value);
            if (reader.MoveToAttribute("ContentId"))
                ContentId = reader.Value;
            if (reader.MoveToAttribute("IsLastFocusedDocument"))
                IsLastFocusedDocument = bool.Parse(reader.Value);
            if (reader.MoveToAttribute("PreviousContainerId"))
                PreviousContainerId = reader.Value;
            if (reader.MoveToAttribute("PreviousContainerIndex"))
                PreviousContainerIndex = int.Parse(reader.Value);

            if (reader.MoveToAttribute("FloatingLeft"))
                FloatingLeft = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            if (reader.MoveToAttribute("FloatingTop"))
                FloatingTop = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            if (reader.MoveToAttribute("FloatingWidth"))
                FloatingWidth = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            if (reader.MoveToAttribute("FloatingHeight"))
                FloatingHeight = double.Parse(reader.Value, CultureInfo.InvariantCulture);
            if (reader.MoveToAttribute("IsMaximized"))
                IsMaximized = bool.Parse(reader.Value);
            if (reader.MoveToAttribute("CanClose"))
                CanClose = bool.Parse(reader.Value);
            if (reader.MoveToAttribute("CanFloat"))
                CanFloat = bool.Parse(reader.Value);
            if (reader.MoveToAttribute("LastActivationTimeStamp"))
                LastActivationTimeStamp = DateTime.Parse(reader.Value, CultureInfo.InvariantCulture);

            reader.Read();
        }

        public virtual void WriteXml(XmlWriter writer)
        {
            if (!string.IsNullOrWhiteSpace(Title))
                writer.WriteAttributeString("Title", Title);

            //if (IconSource != null)
            //    writer.WriteAttributeString("IconSource", IconSource.ToString());

            if (IsSelected)
                writer.WriteAttributeString("IsSelected", IsSelected.ToString());

            if (IsLastFocusedDocument)
                writer.WriteAttributeString("IsLastFocusedDocument", IsLastFocusedDocument.ToString());

            if (!string.IsNullOrWhiteSpace(ContentId))
                writer.WriteAttributeString("ContentId", ContentId);


            if (ToolTip != null && ToolTip is string)
                if (!string.IsNullOrWhiteSpace((string) ToolTip))
                    writer.WriteAttributeString("ToolTip", (string) ToolTip);

            if (FloatingLeft != 0.0)
                writer.WriteAttributeString("FloatingLeft", FloatingLeft.ToString(CultureInfo.InvariantCulture));
            if (FloatingTop != 0.0)
                writer.WriteAttributeString("FloatingTop", FloatingTop.ToString(CultureInfo.InvariantCulture));
            if (FloatingWidth != 0.0)
                writer.WriteAttributeString("FloatingWidth", FloatingWidth.ToString(CultureInfo.InvariantCulture));
            if (FloatingHeight != 0.0)
                writer.WriteAttributeString("FloatingHeight", FloatingHeight.ToString(CultureInfo.InvariantCulture));

            if (IsMaximized)
                writer.WriteAttributeString("IsMaximized", IsMaximized.ToString());
            if (!CanClose)
                writer.WriteAttributeString("CanClose", CanClose.ToString());
            if (!CanFloat)
                writer.WriteAttributeString("CanFloat", CanFloat.ToString());


            if (LastActivationTimeStamp != null)
                writer.WriteAttributeString("LastActivationTimeStamp",
                    LastActivationTimeStamp.Value.ToString(CultureInfo.InvariantCulture));

            if (_previousContainer != null)
            {
                var paneSerializable = _previousContainer as ILayoutPaneSerializable;
                if (paneSerializable != null)
                {
                    writer.WriteAttributeString("PreviousContainerId", paneSerializable.Id);
                    writer.WriteAttributeString("PreviousContainerIndex", _previousContainerIndex.ToString());
                }
            }
        }

        public int CompareTo(LayoutContent other)
        {
            var contentAsComparable = Content as IComparable;
            if (contentAsComparable != null) return contentAsComparable.CompareTo(other.Content);

            return string.Compare(Title, other.Title);
        }

        /// <summary>
        ///     Float the content in a popup window
        /// </summary>
        public void Float()
        {
            if (PreviousContainer != null &&
                PreviousContainer.FindParent<LayoutFloatingWindow>() != null)
            {
                var currentContainer = Parent as ILayoutPane;
                var currentContainerIndex = (currentContainer as ILayoutGroup).IndexOfChild(this);
                var previousContainerAsLayoutGroup = PreviousContainer as ILayoutGroup;

                if (PreviousContainerIndex < previousContainerAsLayoutGroup.ChildrenCount)
                    previousContainerAsLayoutGroup.InsertChildAt(PreviousContainerIndex, this);
                else
                    previousContainerAsLayoutGroup.InsertChildAt(previousContainerAsLayoutGroup.ChildrenCount, this);

                PreviousContainer = currentContainer;
                PreviousContainerIndex = currentContainerIndex;

                IsSelected = true;
                IsActive = true;

                Root.CollectGarbage();
            }
            else
            {
                Root.Manager.StartDraggingFloatingWindowForContent(this, false);

                IsSelected = true;
                IsActive = true;
            }
        }

        /// <summary>
        ///     Dock the content as document
        /// </summary>
        public void DockAsDocument()
        {
            var root = Root as LayoutRoot;
            if (root == null)
                throw new InvalidOperationException();
            if (Parent is LayoutDocumentPane)
                return;

            if (PreviousContainer is LayoutDocumentPane)
            {
                Dock();
                return;
            }

            LayoutDocumentPane newParentPane;
            if (root.LastFocusedDocument != null)
                newParentPane = root.LastFocusedDocument.Parent as LayoutDocumentPane;
            else
                newParentPane = root.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();

            if (newParentPane != null)
            {
                newParentPane.Children.Add(this);
                root.CollectGarbage();
            }

            IsSelected = true;
            IsActive = true;
        }

        /// <summary>
        ///     Re-dock the content to its previous container
        /// </summary>
        public void Dock()
        {
            if (PreviousContainer != null)
            {
                var currentContainer = Parent;
                var currentContainerIndex = currentContainer is ILayoutGroup
                    ? (currentContainer as ILayoutGroup).IndexOfChild(this)
                    : -1;
                var previousContainerAsLayoutGroup = PreviousContainer as ILayoutGroup;

                if (PreviousContainerIndex < previousContainerAsLayoutGroup.ChildrenCount)
                    previousContainerAsLayoutGroup.InsertChildAt(PreviousContainerIndex, this);
                else
                    previousContainerAsLayoutGroup.InsertChildAt(previousContainerAsLayoutGroup.ChildrenCount, this);

                if (currentContainerIndex > -1)
                {
                    PreviousContainer = currentContainer;
                    PreviousContainerIndex = currentContainerIndex;
                }
                else
                {
                    PreviousContainer = null;
                    PreviousContainerIndex = 0;
                }

                IsSelected = true;
                IsActive = true;
            }
            else
            {
                InternalDock();
            }


            Root.CollectGarbage();
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Test if the content can be closed
        /// </summary>
        /// <returns></returns>
        internal bool TestCanClose()
        {
            var args = new CancelEventArgs();

            OnClosing(args);

            if (args.Cancel)
                return false;

            return true;
        }

        internal void CloseInternal()
        {
            var root = Root;
            var parentAsContainer = Parent;
            parentAsContainer.RemoveChild(this);
            if (root != null)
                root.CollectGarbage();

            OnClosed();
        }

        protected virtual void OnClosed()
        {
            if (Closed != null)
                Closed(this, EventArgs.Empty);
        }

        protected virtual void OnClosing(CancelEventArgs args)
        {
            if (Closing != null)
                Closing(this, args);
        }

        protected virtual void InternalDock()
        {
        }

        #endregion

        #region Events

        /// <summary>
        ///     Event fired when the content is closed (i.e. removed definitely from the layout)
        /// </summary>
        public event EventHandler Closed;

        /// <summary>
        ///     Event fired when the content is about to be closed (i.e. removed definitely from the layout)
        /// </summary>
        /// <remarks>
        ///     Please note that LayoutAnchorable also can be hidden. Usually user hide anchorables when click the 'X' button. To
        ///     completely close
        ///     an anchorable the user should click the 'Close' menu item from the context menu. When an LayoutAnchorable is hidden
        ///     its visibility changes to false and
        ///     IsHidden property is set to true.
        ///     Hanlde the Hiding event for the LayoutAnchorable to cancel the hide operation.
        /// </remarks>
        public event EventHandler<CancelEventArgs> Closing;

        #endregion
    }
}