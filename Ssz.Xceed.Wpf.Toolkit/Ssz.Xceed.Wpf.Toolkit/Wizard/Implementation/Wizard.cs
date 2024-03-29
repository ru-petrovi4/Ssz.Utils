﻿/*************************************************************************************

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
using System.Windows.Interop;
using Ssz.Xceed.Wpf.Toolkit.Core;

namespace Ssz.Xceed.Wpf.Toolkit
{
    public class Wizard : ItemsControl
    {
        #region Properties

        public static readonly DependencyProperty BackButtonContentProperty =
            DependencyProperty.Register("BackButtonContent", typeof(object), typeof(Wizard),
                new UIPropertyMetadata("< Back"));

        public object BackButtonContent
        {
            get => GetValue(BackButtonContentProperty);
            set => SetValue(BackButtonContentProperty, value);
        }

        public static readonly DependencyProperty BackButtonVisibilityProperty =
            DependencyProperty.Register("BackButtonVisibility", typeof(Visibility), typeof(Wizard),
                new UIPropertyMetadata(Visibility.Visible));

        public Visibility BackButtonVisibility
        {
            get => (Visibility) GetValue(BackButtonVisibilityProperty);
            set => SetValue(BackButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty CanCancelProperty =
            DependencyProperty.Register("CanCancel", typeof(bool), typeof(Wizard), new UIPropertyMetadata(true));

        public bool CanCancel
        {
            get => (bool) GetValue(CanCancelProperty);
            set => SetValue(CanCancelProperty, value);
        }

        public static readonly DependencyProperty CancelButtonClosesWindowProperty =
            DependencyProperty.Register("CancelButtonClosesWindow", typeof(bool), typeof(Wizard),
                new UIPropertyMetadata(true));

        public bool CancelButtonClosesWindow
        {
            get => (bool) GetValue(CancelButtonClosesWindowProperty);
            set => SetValue(CancelButtonClosesWindowProperty, value);
        }

        public static readonly DependencyProperty CancelButtonContentProperty =
            DependencyProperty.Register("CancelButtonContent", typeof(object), typeof(Wizard),
                new UIPropertyMetadata("Cancel"));

        public object CancelButtonContent
        {
            get => GetValue(CancelButtonContentProperty);
            set => SetValue(CancelButtonContentProperty, value);
        }

        public static readonly DependencyProperty CancelButtonVisibilityProperty =
            DependencyProperty.Register("CancelButtonVisibility", typeof(Visibility), typeof(Wizard),
                new UIPropertyMetadata(Visibility.Visible));

        public Visibility CancelButtonVisibility
        {
            get => (Visibility) GetValue(CancelButtonVisibilityProperty);
            set => SetValue(CancelButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty CanFinishProperty =
            DependencyProperty.Register("CanFinish", typeof(bool), typeof(Wizard), new UIPropertyMetadata(false));

        public bool CanFinish
        {
            get => (bool) GetValue(CanFinishProperty);
            set => SetValue(CanFinishProperty, value);
        }

        public static readonly DependencyProperty CanHelpProperty =
            DependencyProperty.Register("CanHelp", typeof(bool), typeof(Wizard), new UIPropertyMetadata(true));

        public bool CanHelp
        {
            get => (bool) GetValue(CanHelpProperty);
            set => SetValue(CanHelpProperty, value);
        }

        public static readonly DependencyProperty CanSelectNextPageProperty =
            DependencyProperty.Register("CanSelectNextPage", typeof(bool), typeof(Wizard),
                new UIPropertyMetadata(true));

        public bool CanSelectNextPage
        {
            get => (bool) GetValue(CanSelectNextPageProperty);
            set => SetValue(CanSelectNextPageProperty, value);
        }

        public static readonly DependencyProperty CanSelectPreviousPageProperty =
            DependencyProperty.Register("CanSelectPreviousPage", typeof(bool), typeof(Wizard),
                new UIPropertyMetadata(true));

        public bool CanSelectPreviousPage
        {
            get => (bool) GetValue(CanSelectPreviousPageProperty);
            set => SetValue(CanSelectPreviousPageProperty, value);
        }

        #region CurrentPage

        public static readonly DependencyProperty CurrentPageProperty = DependencyProperty.Register("CurrentPage",
            typeof(WizardPage), typeof(Wizard), new UIPropertyMetadata(null, OnCurrentPageChanged));

        public WizardPage CurrentPage
        {
            get => (WizardPage) GetValue(CurrentPageProperty);
            set => SetValue(CurrentPageProperty, value);
        }

        private static void OnCurrentPageChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var wizard = o as Wizard;
            if (wizard is not null)
                wizard.OnCurrentPageChanged((WizardPage) e.OldValue, (WizardPage) e.NewValue);
        }

        protected virtual void OnCurrentPageChanged(WizardPage oldValue, WizardPage newValue)
        {
            RaiseRoutedEvent(PageChangedEvent);
        }

        #endregion //CurrentPage

        public static readonly DependencyProperty ExteriorPanelMinWidthProperty =
            DependencyProperty.Register("ExteriorPanelMinWidth", typeof(double), typeof(Wizard),
                new UIPropertyMetadata(165.0));

        public double ExteriorPanelMinWidth
        {
            get => (double) GetValue(ExteriorPanelMinWidthProperty);
            set => SetValue(ExteriorPanelMinWidthProperty, value);
        }


        public static readonly DependencyProperty FinishButtonClosesWindowProperty =
            DependencyProperty.Register("FinishButtonClosesWindow", typeof(bool), typeof(Wizard),
                new UIPropertyMetadata(true));

        public bool FinishButtonClosesWindow
        {
            get => (bool) GetValue(FinishButtonClosesWindowProperty);
            set => SetValue(FinishButtonClosesWindowProperty, value);
        }

        public static readonly DependencyProperty FinishButtonContentProperty =
            DependencyProperty.Register("FinishButtonContent", typeof(object), typeof(Wizard),
                new UIPropertyMetadata("Finish"));

        public object FinishButtonContent
        {
            get => GetValue(FinishButtonContentProperty);
            set => SetValue(FinishButtonContentProperty, value);
        }

        public static readonly DependencyProperty FinishButtonVisibilityProperty =
            DependencyProperty.Register("FinishButtonVisibility", typeof(Visibility), typeof(Wizard),
                new UIPropertyMetadata(Visibility.Visible));

        public Visibility FinishButtonVisibility
        {
            get => (Visibility) GetValue(FinishButtonVisibilityProperty);
            set => SetValue(FinishButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty HelpButtonContentProperty =
            DependencyProperty.Register("HelpButtonContent", typeof(object), typeof(Wizard),
                new UIPropertyMetadata("Help"));

        public object HelpButtonContent
        {
            get => GetValue(HelpButtonContentProperty);
            set => SetValue(HelpButtonContentProperty, value);
        }

        public static readonly DependencyProperty HelpButtonVisibilityProperty =
            DependencyProperty.Register("HelpButtonVisibility", typeof(Visibility), typeof(Wizard),
                new UIPropertyMetadata(Visibility.Visible));

        public Visibility HelpButtonVisibility
        {
            get => (Visibility) GetValue(HelpButtonVisibilityProperty);
            set => SetValue(HelpButtonVisibilityProperty, value);
        }

        public static readonly DependencyProperty NextButtonContentProperty =
            DependencyProperty.Register("NextButtonContent", typeof(object), typeof(Wizard),
                new UIPropertyMetadata("Next >"));

        public object NextButtonContent
        {
            get => GetValue(NextButtonContentProperty);
            set => SetValue(NextButtonContentProperty, value);
        }

        public static readonly DependencyProperty NextButtonVisibilityProperty =
            DependencyProperty.Register("NextButtonVisibility", typeof(Visibility), typeof(Wizard),
                new UIPropertyMetadata(Visibility.Visible));

        public Visibility NextButtonVisibility
        {
            get => (Visibility) GetValue(NextButtonVisibilityProperty);
            set => SetValue(NextButtonVisibilityProperty, value);
        }

        #endregion //Properties

        #region Constructors

        static Wizard()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(Wizard), new FrameworkPropertyMetadata(typeof(Wizard)));
        }

        public Wizard()
        {
            CommandBindings.Add(new CommandBinding(WizardCommands.Cancel, ExecuteCancelWizard, CanExecuteCancelWizard));
            CommandBindings.Add(new CommandBinding(WizardCommands.Finish, ExecuteFinishWizard, CanExecuteFinishWizard));
            CommandBindings.Add(new CommandBinding(WizardCommands.Help, ExecuteRequestHelp, CanExecuteRequestHelp));
            CommandBindings.Add(new CommandBinding(WizardCommands.NextPage, ExecuteSelectNextPage,
                CanExecuteSelectNextPage));
            CommandBindings.Add(new CommandBinding(WizardCommands.PreviousPage, ExecuteSelectPreviousPage,
                CanExecuteSelectPreviousPage));
        }

        #endregion //Constructors

        #region Base Class Overrides

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new WizardPage();
        }

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is WizardPage;
        }

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            if (Items.Count > 0 && CurrentPage is null)
                CurrentPage = Items[0] as WizardPage;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property.Name == "CanSelectNextPage" || e.Property.Name == "CanHelp" || e.Property.Name == "CanFinish"
                || e.Property.Name == "CanCancel" || e.Property.Name == "CanSelectPreviousPage")
                CommandManager.InvalidateRequerySuggested();
        }

        #endregion //Base Class Overrides

        #region Commands

        private void ExecuteCancelWizard(object sender, ExecutedRoutedEventArgs e)
        {
            RaiseRoutedEvent(CancelEvent);

            if (CancelButtonClosesWindow)
                CloseParentWindow(false);
        }

        private void CanExecuteCancelWizard(object sender, CanExecuteRoutedEventArgs e)
        {
            if (CurrentPage is not null)
            {
                if (CurrentPage.CanCancel.HasValue)
                    e.CanExecute = CurrentPage.CanCancel.Value;
                else
                    e.CanExecute = CanCancel;
            }
        }

        private void ExecuteFinishWizard(object sender, ExecutedRoutedEventArgs e)
        {
            RaiseRoutedEvent(FinishEvent);

            if (FinishButtonClosesWindow)
                CloseParentWindow(true);
        }

        private void CanExecuteFinishWizard(object sender, CanExecuteRoutedEventArgs e)
        {
            if (CurrentPage is not null)
            {
                if (CurrentPage.CanFinish.HasValue)
                    e.CanExecute = CurrentPage.CanFinish.Value;
                else
                    e.CanExecute = CanFinish;
            }
        }

        private void ExecuteRequestHelp(object sender, ExecutedRoutedEventArgs e)
        {
            RaiseRoutedEvent(HelpEvent);
        }

        private void CanExecuteRequestHelp(object sender, CanExecuteRoutedEventArgs e)
        {
            if (CurrentPage is not null)
            {
                if (CurrentPage.CanHelp.HasValue)
                    e.CanExecute = CurrentPage.CanHelp.Value;
                else
                    e.CanExecute = CanHelp;
            }
        }

        private void ExecuteSelectNextPage(object sender, ExecutedRoutedEventArgs e)
        {
            WizardPage nextPage = null;

            if (CurrentPage is not null)
            {
                var eventArgs = new CancelRoutedEventArgs(NextEvent);
                RaiseEvent(eventArgs);
                if (eventArgs.Cancel)
                    return;

                //check next page
                if (CurrentPage.NextPage is not null)
                {
                    nextPage = CurrentPage.NextPage;
                }
                else
                {
                    //no next page defined use index
                    var currentIndex = Items.IndexOf(CurrentPage);
                    var nextPageIndex = currentIndex + 1;
                    if (nextPageIndex < Items.Count)
                        nextPage = Items[nextPageIndex] as WizardPage;
                }
            }

            CurrentPage = nextPage;
        }

        private void CanExecuteSelectNextPage(object sender, CanExecuteRoutedEventArgs e)
        {
            if (CurrentPage is not null)
            {
                if (CurrentPage.CanSelectNextPage.HasValue) //check to see if page has overriden default behavior
                {
                    if (CurrentPage.CanSelectNextPage.Value)
                        e.CanExecute = NextPageExists();
                }
                else if (CanSelectNextPage)
                {
                    e.CanExecute = NextPageExists();
                }
            }
        }

        private void ExecuteSelectPreviousPage(object sender, ExecutedRoutedEventArgs e)
        {
            WizardPage previousPage = null;

            if (CurrentPage is not null)
            {
                var eventArgs = new CancelRoutedEventArgs(PreviousEvent);
                RaiseEvent(eventArgs);
                if (eventArgs.Cancel)
                    return;

                //check previous page
                if (CurrentPage.PreviousPage is not null)
                {
                    previousPage = CurrentPage.PreviousPage;
                }
                else
                {
                    //no previous page defined so use index
                    var currentIndex = Items.IndexOf(CurrentPage);
                    var previousPageIndex = currentIndex - 1;
                    if (previousPageIndex >= 0 && previousPageIndex < Items.Count)
                        previousPage = Items[previousPageIndex] as WizardPage;
                }
            }

            CurrentPage = previousPage;
        }

        private void CanExecuteSelectPreviousPage(object sender, CanExecuteRoutedEventArgs e)
        {
            if (CurrentPage is not null)
            {
                if (CurrentPage.CanSelectPreviousPage.HasValue) //check to see if page has overriden default behavior
                {
                    if (CurrentPage.CanSelectPreviousPage.Value)
                        e.CanExecute = PreviousPageExists();
                }
                else if (CanSelectPreviousPage)
                {
                    e.CanExecute = PreviousPageExists();
                }
            }
        }

        #endregion //Commands

        #region Events

        #region Cancel Event

        public static readonly RoutedEvent CancelEvent =
            EventManager.RegisterRoutedEvent("Cancel", RoutingStrategy.Bubble, typeof(EventHandler), typeof(Wizard));

        public event RoutedEventHandler Cancel
        {
            add => AddHandler(CancelEvent, value);
            remove => RemoveHandler(CancelEvent, value);
        }

        #endregion //Cancel Event

        #region PageChanged Event

        public static readonly RoutedEvent PageChangedEvent =
            EventManager.RegisterRoutedEvent("PageChanged", RoutingStrategy.Bubble, typeof(EventHandler),
                typeof(Wizard));

        public event RoutedEventHandler PageChanged
        {
            add => AddHandler(PageChangedEvent, value);
            remove => RemoveHandler(PageChangedEvent, value);
        }

        #endregion //PageChanged Event

        #region Finish Event

        public static readonly RoutedEvent FinishEvent =
            EventManager.RegisterRoutedEvent("Finish", RoutingStrategy.Bubble, typeof(EventHandler), typeof(Wizard));

        public event RoutedEventHandler Finish
        {
            add => AddHandler(FinishEvent, value);
            remove => RemoveHandler(FinishEvent, value);
        }

        #endregion //Finish Event

        #region Help Event

        public static readonly RoutedEvent HelpEvent =
            EventManager.RegisterRoutedEvent("Help", RoutingStrategy.Bubble, typeof(EventHandler), typeof(Wizard));

        public event RoutedEventHandler Help
        {
            add => AddHandler(HelpEvent, value);
            remove => RemoveHandler(HelpEvent, value);
        }

        #endregion //Help Event

        #region Next Event

        public delegate void NextRoutedEventHandler(object sender, CancelRoutedEventArgs e);

        /// <summary>
        ///     Identifies the Next routed event.
        /// </summary>
        public static readonly RoutedEvent NextEvent = EventManager.RegisterRoutedEvent("Next", RoutingStrategy.Bubble,
            typeof(NextRoutedEventHandler), typeof(Wizard));

        /// <summary>
        ///     Raised when WizardCommands.NextPage command is executed.
        ///     This cancellable event can prevent the command execution from continuing.
        /// </summary>
        public event NextRoutedEventHandler Next
        {
            add => AddHandler(NextEvent, value);
            remove => RemoveHandler(NextEvent, value);
        }

        #endregion //Next Event

        #region Previous Event

        public delegate void PreviousRoutedEventHandler(object sender, CancelRoutedEventArgs e);

        /// <summary>
        ///     Identifies the Previous routed event.
        /// </summary>
        public static readonly RoutedEvent PreviousEvent = EventManager.RegisterRoutedEvent("Previous",
            RoutingStrategy.Bubble, typeof(PreviousRoutedEventHandler), typeof(Wizard));

        /// <summary>
        ///     Raised when WizardCommands.PreviousPage command is executed.
        ///     This cancellable event can prevent the command execution from continuing.
        /// </summary>
        public event PreviousRoutedEventHandler Previous
        {
            add => AddHandler(PreviousEvent, value);
            remove => RemoveHandler(PreviousEvent, value);
        }

        #endregion //Previous Event

        #endregion //Events

        #region Methods

        private void CloseParentWindow(bool dialogResult)
        {
            var window = Window.GetWindow(this);
            if (window is not null)
            {
                //we can only set the DialogResult if the window was opened as modal with the ShowDialog() method. Otherwise an exception would occur
                if (ComponentDispatcher.IsThreadModal)
                    window.DialogResult = dialogResult;

                window.Close();
            }
        }

        private bool NextPageExists()
        {
            var exists = false;

            if (CurrentPage.NextPage is not null) //check to see if a next page has been specified
            {
                exists = true;
            }
            else
            {
                //lets use an index to find the next page
                var currentIndex = Items.IndexOf(CurrentPage);
                var nextPageIndex = currentIndex + 1;
                if (nextPageIndex < Items.Count)
                    exists = true;
            }

            return exists;
        }

        private bool PreviousPageExists()
        {
            var exists = false;

            if (CurrentPage.PreviousPage is not null) //check to see if a previous page has been specified
            {
                exists = true;
            }
            else
            {
                //lets use an index to find the next page
                var currentIndex = Items.IndexOf(CurrentPage);
                var previousPageIndex = currentIndex - 1;
                if (previousPageIndex >= 0 && previousPageIndex < Items.Count)
                    exists = true;
            }

            return exists;
        }

        private void RaiseRoutedEvent(RoutedEvent routedEvent)
        {
            var newEventArgs = new RoutedEventArgs(routedEvent, this);
            RaiseEvent(newEventArgs);
        }

        #endregion //Methods
    }
}