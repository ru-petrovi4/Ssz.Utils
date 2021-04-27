/*************************************************************************************

   Extended WPF Toolkit

   Copyright (C) 2007-2013 Xceed Software Inc.

   This program is provided to you under the terms of the Microsoft Public
   License (Ms-PL) as published at http://wpftoolkit.codeplex.com/license 

   For more features, controls, and fast professional support,
   pick up the Plus Edition at http://xceed.com/wpf_toolkit

   Stay informed: follow @datagrid on Twitter or Like http://facebook.com/datagrids

  ***********************************************************************************/

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class WizardPage : ContentControl
    {
        #region Overrides

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property.Name == "CanSelectNextPage" || e.Property.Name == "CanHelp" || e.Property.Name == "CanFinish"
                || e.Property.Name == "CanCancel" || e.Property.Name == "CanSelectPreviousPage")
                CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Properties

        public static readonly DependencyProperty BackButtonVisibilityProperty =
            DependencyProperty.Register("BackButtonVisibility", typeof(WizardPageButtonVisibility), typeof(WizardPage),
                new UIPropertyMetadata(WizardPageButtonVisibility.Inherit));

        public WizardPageButtonVisibility BackButtonVisibility
        {
            get => (WizardPageButtonVisibility) GetValue(BackButtonVisibilityProperty);
            set => SetValue(BackButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty CanCancelProperty =
            DependencyProperty.Register("CanCancel", typeof(bool?), typeof(WizardPage), new UIPropertyMetadata(null));

        public bool? CanCancel
        {
            get => (bool?) GetValue(CanCancelProperty);
            set => SetValue(CanCancelProperty, value);
        }

        public static readonly DependencyProperty CancelButtonVisibilityProperty =
            DependencyProperty.Register("CancelButtonVisibility", typeof(WizardPageButtonVisibility),
                typeof(WizardPage), new UIPropertyMetadata(WizardPageButtonVisibility.Inherit));

        public WizardPageButtonVisibility CancelButtonVisibility
        {
            get => (WizardPageButtonVisibility) GetValue(CancelButtonVisibilityProperty);
            set => SetValue(CancelButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty CanFinishProperty =
            DependencyProperty.Register("CanFinish", typeof(bool?), typeof(WizardPage), new UIPropertyMetadata(null));

        public bool? CanFinish
        {
            get => (bool?) GetValue(CanFinishProperty);
            set => SetValue(CanFinishProperty, value);
        }

        public static readonly DependencyProperty CanHelpProperty =
            DependencyProperty.Register("CanHelp", typeof(bool?), typeof(WizardPage), new UIPropertyMetadata(null));

        public bool? CanHelp
        {
            get => (bool?) GetValue(CanHelpProperty);
            set => SetValue(CanHelpProperty, value);
        }

        public static readonly DependencyProperty CanSelectNextPageProperty =
            DependencyProperty.Register("CanSelectNextPage", typeof(bool?), typeof(WizardPage),
                new UIPropertyMetadata(null));

        public bool? CanSelectNextPage
        {
            get => (bool?) GetValue(CanSelectNextPageProperty);
            set => SetValue(CanSelectNextPageProperty, value);
        }

        public static readonly DependencyProperty CanSelectPreviousPageProperty =
            DependencyProperty.Register("CanSelectPreviousPage", typeof(bool?), typeof(WizardPage),
                new UIPropertyMetadata(null));

        public bool? CanSelectPreviousPage
        {
            get => (bool?) GetValue(CanSelectPreviousPageProperty);
            set => SetValue(CanSelectPreviousPageProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(WizardPage));

        public string Description
        {
            get => (string) GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty ExteriorPanelBackgroundProperty =
            DependencyProperty.Register("ExteriorPanelBackground", typeof(Brush), typeof(WizardPage),
                new UIPropertyMetadata(null));

        public Brush ExteriorPanelBackground
        {
            get => (Brush) GetValue(ExteriorPanelBackgroundProperty);
            set => SetValue(ExteriorPanelBackgroundProperty, value);
        }

        public static readonly DependencyProperty ExteriorPanelContentProperty =
            DependencyProperty.Register("ExteriorPanelContent", typeof(object), typeof(WizardPage),
                new UIPropertyMetadata(null));

        public object ExteriorPanelContent
        {
            get => GetValue(ExteriorPanelContentProperty);
            set => SetValue(ExteriorPanelContentProperty, value);
        }

        public static readonly DependencyProperty FinishButtonVisibilityProperty =
            DependencyProperty.Register("FinishButtonVisibility", typeof(WizardPageButtonVisibility),
                typeof(WizardPage), new UIPropertyMetadata(WizardPageButtonVisibility.Inherit));

        public WizardPageButtonVisibility FinishButtonVisibility
        {
            get => (WizardPageButtonVisibility) GetValue(FinishButtonVisibilityProperty);
            set => SetValue(FinishButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty HeaderBackgroundProperty =
            DependencyProperty.Register("HeaderBackground", typeof(Brush), typeof(WizardPage),
                new UIPropertyMetadata(Brushes.White));

        public Brush HeaderBackground
        {
            get => (Brush) GetValue(HeaderBackgroundProperty);
            set => SetValue(HeaderBackgroundProperty, value);
        }

        public static readonly DependencyProperty HeaderImageProperty = DependencyProperty.Register("HeaderImage",
            typeof(ImageSource), typeof(WizardPage), new UIPropertyMetadata(null));

        public ImageSource HeaderImage
        {
            get => (ImageSource) GetValue(HeaderImageProperty);
            set => SetValue(HeaderImageProperty, value);
        }

        public static readonly DependencyProperty HelpButtonVisibilityProperty =
            DependencyProperty.Register("HelpButtonVisibility", typeof(WizardPageButtonVisibility), typeof(WizardPage),
                new UIPropertyMetadata(WizardPageButtonVisibility.Inherit));

        public WizardPageButtonVisibility HelpButtonVisibility
        {
            get => (WizardPageButtonVisibility) GetValue(HelpButtonVisibilityProperty);
            set => SetValue(HelpButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty NextButtonVisibilityProperty =
            DependencyProperty.Register("NextButtonVisibility", typeof(WizardPageButtonVisibility), typeof(WizardPage),
                new UIPropertyMetadata(WizardPageButtonVisibility.Inherit));

        public WizardPageButtonVisibility NextButtonVisibility
        {
            get => (WizardPageButtonVisibility) GetValue(NextButtonVisibilityProperty);
            set => SetValue(NextButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty NextPageProperty = DependencyProperty.Register("NextPage",
            typeof(WizardPage), typeof(WizardPage), new UIPropertyMetadata(null));

        public WizardPage NextPage
        {
            get => (WizardPage) GetValue(NextPageProperty);
            set => SetValue(NextPageProperty, value);
        }

        public static readonly DependencyProperty PageTypeProperty = DependencyProperty.Register("PageType",
            typeof(WizardPageType), typeof(WizardPage), new UIPropertyMetadata(WizardPageType.Exterior));

        public WizardPageType PageType
        {
            get => (WizardPageType) GetValue(PageTypeProperty);
            set => SetValue(PageTypeProperty, value);
        }

        public static readonly DependencyProperty PreviousPageProperty = DependencyProperty.Register("PreviousPage",
            typeof(WizardPage), typeof(WizardPage), new UIPropertyMetadata(null));

        public WizardPage PreviousPage
        {
            get => (WizardPage) GetValue(PreviousPageProperty);
            set => SetValue(PreviousPageProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(WizardPage));

        public string Title
        {
            get => (string) GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        #endregion //Properties

        #region Constructors

        static WizardPage()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WizardPage),
                new FrameworkPropertyMetadata(typeof(WizardPage)));
        }

        public WizardPage()
        {
            Loaded += WizardPage_Loaded;
            Unloaded += WizardPage_Unloaded;
        }

        private void WizardPage_Unloaded(object sender, RoutedEventArgs e)
        {
            RaiseEvent(new RoutedEventArgs(LeaveEvent, this));
        }

        private void WizardPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (IsVisible) RaiseEvent(new RoutedEventArgs(EnterEvent, this));
        }

        #endregion //Constructors

        #region Events

        #region Enter Event

        public static readonly RoutedEvent EnterEvent = EventManager.RegisterRoutedEvent("Enter",
            RoutingStrategy.Bubble, typeof(EventHandler), typeof(WizardPage));

        public event RoutedEventHandler Enter
        {
            add => AddHandler(EnterEvent, value);
            remove => RemoveHandler(EnterEvent, value);
        }

        #endregion //Enter Event

        #region Leave Event

        public static readonly RoutedEvent LeaveEvent = EventManager.RegisterRoutedEvent("Leave",
            RoutingStrategy.Bubble, typeof(EventHandler), typeof(WizardPage));

        public event RoutedEventHandler Leave
        {
            add => AddHandler(LeaveEvent, value);
            remove => RemoveHandler(LeaveEvent, value);
        }

        #endregion //Leave Event

        #endregion //Events
    }
}