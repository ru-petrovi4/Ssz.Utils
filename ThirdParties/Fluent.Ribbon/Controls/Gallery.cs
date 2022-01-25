﻿// ReSharper disable once CheckNamespace
namespace Fluent
{
    using System;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Windows.Markup;
    using Fluent.Extensions;
    using Fluent.Internal.KnownBoxes;

    /// <summary>
    /// Represents gallery control.
    /// Usually a gallery is hosted in context menu
    /// </summary>
    [ContentProperty(nameof(Items))]
    [TemplatePart(Name = "PART_DropDownButton", Type = typeof(DropDownButton))]
    public class Gallery : ListBox
    {
        #region Fields

        private ObservableCollection<GalleryGroupFilter>? filters;

        private DropDownButton? groupsMenuButton;

        #endregion

        #region Properties

        #region MinItemsInRow

        /// <summary>
        /// Min width of the Gallery
        /// </summary>
        public int MinItemsInRow
        {
            get { return (int)this.GetValue(MinItemsInRowProperty); }
            set { this.SetValue(MinItemsInRowProperty, value); }
        }

        /// <summary>Identifies the <see cref="MinItemsInRow"/> dependency property.</summary>
        public static readonly DependencyProperty MinItemsInRowProperty =
            DependencyProperty.Register(nameof(MinItemsInRow), typeof(int),
            typeof(Gallery), new PropertyMetadata(1));

        #endregion

        #region MaxItemsInRow

        /// <summary>
        /// Max width of the Gallery
        /// </summary>
        public int MaxItemsInRow
        {
            get { return (int)this.GetValue(MaxItemsInRowProperty); }
            set { this.SetValue(MaxItemsInRowProperty, value); }
        }

        /// <summary>Identifies the <see cref="MaxItemsInRow"/> dependency property.</summary>
        public static readonly DependencyProperty MaxItemsInRowProperty = DependencyProperty.Register(nameof(MaxItemsInRow), typeof(int), typeof(Gallery), new PropertyMetadata(IntBoxes.Zero));

        #endregion

        #region IsGrouped

        /// <summary>Identifies the <see cref="IsGrouped"/> dependency property.</summary>
        public static readonly DependencyProperty IsGroupedProperty = DependencyProperty.Register(nameof(IsGrouped), typeof(bool), typeof(Gallery), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>
        /// Gets or sets whether the inner gallery panel shows groups
        /// (Filter property still works as usual)
        /// </summary>
        public bool IsGrouped
        {
            get { return (bool)this.GetValue(IsGroupedProperty); }
            set { this.SetValue(IsGroupedProperty, BooleanBoxes.Box(value)); }
        }

        #endregion

        #region GroupBy

        /// <summary>
        /// Gets or sets name of property which
        /// will use to group items in the Gallery.
        /// </summary>
        public string? GroupBy
        {
            get { return (string?)this.GetValue(GroupByProperty); }
            set { this.SetValue(GroupByProperty, value); }
        }

        /// <summary>Identifies the <see cref="GroupBy"/> dependency property.</summary>
        public static readonly DependencyProperty GroupByProperty = DependencyProperty.Register(nameof(GroupBy), typeof(string), typeof(Gallery), new PropertyMetadata());

        #endregion

        #region GroupByAdvanced

        /// <summary>
        /// Gets or sets name of property which
        /// will use to group items in the Gallery.
        /// </summary>
        public Func<object, string>? GroupByAdvanced
        {
            get { return (Func<object, string>?)this.GetValue(GroupByAdvancedProperty); }
            set { this.SetValue(GroupByAdvancedProperty, value); }
        }

        /// <summary>Identifies the <see cref="GroupByAdvanced"/> dependency property.</summary>
        public static readonly DependencyProperty GroupByAdvancedProperty = DependencyProperty.Register(nameof(GroupByAdvanced), typeof(Func<object, string>), typeof(Gallery), new PropertyMetadata());

        #endregion

        #region Orientation

        /// <summary>
        /// Gets or sets orientation of gallery
        /// </summary>
        public Orientation Orientation
        {
            get { return (Orientation)this.GetValue(OrientationProperty); }
            set { this.SetValue(OrientationProperty, value); }
        }

        /// <summary>Identifies the <see cref="Orientation"/> dependency property.</summary>
        public static readonly DependencyProperty OrientationProperty =
            DependencyProperty.Register(nameof(Orientation), typeof(Orientation),
            typeof(Gallery), new PropertyMetadata(Orientation.Horizontal));

        #endregion

        #region ItemWidth

        /// <summary>
        /// Gets or sets item width
        /// </summary>
        public double ItemWidth
        {
            get { return (double)this.GetValue(ItemWidthProperty); }
            set { this.SetValue(ItemWidthProperty, value); }
        }

        /// <summary>Identifies the <see cref="ItemWidth"/> dependency property.</summary>
        public static readonly DependencyProperty ItemWidthProperty =
            DependencyProperty.Register(nameof(ItemWidth), typeof(double), typeof(Gallery), new PropertyMetadata(DoubleBoxes.NaN));

        /// <summary>
        /// Gets or sets item height
        /// </summary>
        public double ItemHeight
        {
            get { return (double)this.GetValue(ItemHeightProperty); }
            set { this.SetValue(ItemHeightProperty, value); }
        }

        /// <summary>Identifies the <see cref="ItemHeight"/> dependency property.</summary>
        public static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register(nameof(ItemHeight), typeof(double), typeof(Gallery), new PropertyMetadata(DoubleBoxes.NaN));

        #endregion

        #region Filters

        /// <summary>
        /// Gets collection of filters
        /// </summary>
        public ObservableCollection<GalleryGroupFilter> Filters
        {
            get
            {
                if (this.filters is null)
                {
                    this.filters = new ObservableCollection<GalleryGroupFilter>();
                    this.filters.CollectionChanged += this.OnFilterCollectionChanged;
                }

                return this.filters;
            }
        }

        // Handle toolbar items changes
        private void OnFilterCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.HasFilter = this.Filters.Count > 0;
            this.InvalidateProperty(SelectedFilterProperty);

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    for (var i = 0; i < e.NewItems?.Count; i++)
                    {
                        if (this.groupsMenuButton is not null)
                        {
                            var filter = (GalleryGroupFilter?)e.NewItems[i];
                            var menuItem = new MenuItem
                                           {
                                               Header = filter?.Title,
                                               Tag = filter
                                           };

                            if (ReferenceEquals(filter, this.SelectedFilter))
                            {
                                menuItem.IsChecked = true;
                            }

                            menuItem.Click += this.OnFilterMenuItemClick;
                            this.groupsMenuButton.Items.Insert(e.NewStartingIndex + i, menuItem);
                        }
                    }

                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var item in e.OldItems.NullSafe().OfType<GalleryGroupFilter>())
                    {
                        if (this.groupsMenuButton is not null)
                        {
                            var menuItem = this.GetFilterMenuItem(item);
                            if (menuItem is not null)
                            {
                                menuItem.Click -= this.OnFilterMenuItemClick;
                                this.groupsMenuButton.Items.Remove(menuItem);
                            }
                        }
                    }

                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (var item in e.OldItems.NullSafe().OfType<GalleryGroupFilter>())
                    {
                        if (this.groupsMenuButton is not null)
                        {
                            var menuItem = this.GetFilterMenuItem(item);

                            if (menuItem is not null)
                            {
                                menuItem.Click -= this.OnFilterMenuItemClick;
                                this.groupsMenuButton.Items.Remove(menuItem);
                            }
                        }
                    }

                    foreach (var item in e.NewItems.NullSafe().OfType<GalleryGroupFilter>())
                    {
                        if (this.groupsMenuButton is not null)
                        {
                            var filter = item;
                            var menuItem = new MenuItem
                                           {
                                               Header = filter.Title,
                                               Tag = filter
                                           };

                            if (ReferenceEquals(filter, this.SelectedFilter))
                            {
                                menuItem.IsChecked = true;
                            }

                            menuItem.Click += this.OnFilterMenuItemClick;
                            this.groupsMenuButton.Items.Add(menuItem);
                        }
                    }

                    break;
            }
        }

        /// <summary>
        /// Gets or sets selected filter
        /// </summary>
        public GalleryGroupFilter? SelectedFilter
        {
            get { return (GalleryGroupFilter?)this.GetValue(SelectedFilterProperty); }
            set { this.SetValue(SelectedFilterProperty, value); }
        }

        /// <summary>Identifies the <see cref="SelectedFilter"/> dependency property.</summary>
        public static readonly DependencyProperty SelectedFilterProperty =
            DependencyProperty.Register(nameof(SelectedFilter), typeof(GalleryGroupFilter),
            typeof(Gallery), new PropertyMetadata(null, OnSelectedFilterChanged, CoerceSelectedFilter));

        // Coerce selected filter
        private static object? CoerceSelectedFilter(DependencyObject d, object? basevalue)
        {
            var gallery = (Gallery)d;
            if (basevalue is null
                && gallery.Filters.Count > 0)
            {
                return gallery.Filters[0];
            }

            return basevalue;
        }

        // Handles filter property changed
        private static void OnSelectedFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var gallery = (Gallery)d;
            if (e.NewValue is GalleryGroupFilter filter)
            {
                gallery.SelectedFilterTitle = filter.Title;
                gallery.SelectedFilterGroups = filter.Groups;
            }
            else
            {
                gallery.SelectedFilterTitle = string.Empty;
                gallery.SelectedFilterGroups = null;
            }

            gallery.UpdateLayout();
        }

        /// <summary>
        /// Gets selected filter title
        /// </summary>
        public string? SelectedFilterTitle
        {
            get { return (string?)this.GetValue(SelectedFilterTitleProperty); }
            private set { this.SetValue(SelectedFilterTitlePropertyKey, value); }
        }

        private static readonly DependencyPropertyKey SelectedFilterTitlePropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(SelectedFilterTitle), typeof(string),
            typeof(Gallery), new PropertyMetadata());

        /// <summary>Identifies the <see cref="SelectedFilterTitle"/> dependency property.</summary>
        public static readonly DependencyProperty SelectedFilterTitleProperty = SelectedFilterTitlePropertyKey.DependencyProperty;

        /// <summary>
        /// Gets selected filter groups
        /// </summary>
        public string? SelectedFilterGroups
        {
            get { return (string?)this.GetValue(SelectedFilterGroupsProperty); }
            private set { this.SetValue(SelectedFilterGroupsPropertyKey, value); }
        }

        private static readonly DependencyPropertyKey SelectedFilterGroupsPropertyKey =
            DependencyProperty.RegisterReadOnly(nameof(SelectedFilterGroups), typeof(string),
            typeof(Gallery), new PropertyMetadata());

        /// <summary>Identifies the <see cref="SelectedFilterGroups"/> dependency property.</summary>
        public static readonly DependencyProperty SelectedFilterGroupsProperty = SelectedFilterGroupsPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets whether gallery has selected filter
        /// </summary>
        public bool HasFilter
        {
            get { return (bool)this.GetValue(HasFilterProperty); }
            private set { this.SetValue(HasFilterPropertyKey, BooleanBoxes.Box(value)); }
        }

        private static readonly DependencyPropertyKey HasFilterPropertyKey = DependencyProperty.RegisterReadOnly(nameof(HasFilter), typeof(bool), typeof(Gallery), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>Identifies the <see cref="HasFilter"/> dependency property.</summary>
        public static readonly DependencyProperty HasFilterProperty = HasFilterPropertyKey.DependencyProperty;

        private void OnFilterMenuItemClick(object sender, RoutedEventArgs e)
        {
            var senderItem = (MenuItem)sender;
            var item = this.GetFilterMenuItem(this.SelectedFilter);
            if (item is not null)
            {
                item.IsChecked = false;
            }

            senderItem.IsChecked = true;
            this.SelectedFilter = senderItem.Tag as GalleryGroupFilter;

            if (this.groupsMenuButton is not null)
            {
                this.groupsMenuButton.IsDropDownOpen = false;
            }

            e.Handled = true;
        }

        private MenuItem? GetFilterMenuItem(GalleryGroupFilter? filter)
        {
            if (filter is null)
            {
                return null;
            }

            return this.groupsMenuButton?.Items.Cast<MenuItem>().FirstOrDefault(item => (item is not null) && (item.Header.ToString() == filter.Title));
            /*foreach (MenuItem item in groupsMenuButton.Items)
            {
                if ((item!=null)&&(item.Header == filter.Title)) return item;
            }
            return null;*/
        }

        #endregion

        #region Selectable

        /// <summary>
        /// Gets or sets whether gallery items can be selected
        /// </summary>
        public bool Selectable
        {
            get { return (bool)this.GetValue(SelectableProperty); }
            set { this.SetValue(SelectableProperty, BooleanBoxes.Box(value)); }
        }

        /// <summary>Identifies the <see cref="Selectable"/> dependency property.</summary>
        public static readonly DependencyProperty SelectableProperty =
            DependencyProperty.Register(nameof(Selectable), typeof(bool),
            typeof(Gallery), new PropertyMetadata(BooleanBoxes.TrueBox, OnSelectableChanged));

        private static void OnSelectableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.CoerceValue(SelectedItemProperty);
        }

        #endregion

        #region IsLastItem

        /// <summary>
        /// Gets whether gallery is last item in ItemsControl
        /// </summary>
        public bool IsLastItem
        {
            get { return (bool)this.GetValue(IsLastItemProperty); }
            private set { this.SetValue(IsLastItemPropertyKey, BooleanBoxes.Box(value)); }
        }

        /// <summary>Identifies the <see cref="IsLastItem"/> dependency property.</summary>
        public static readonly DependencyPropertyKey IsLastItemPropertyKey = DependencyProperty.RegisterReadOnly(nameof(IsLastItem), typeof(bool), typeof(Gallery), new PropertyMetadata(BooleanBoxes.FalseBox));

        /// <summary>Identifies the <see cref="IsLastItem"/> dependency property.</summary>
        public static readonly DependencyProperty IsLastItemProperty = IsLastItemPropertyKey.DependencyProperty;

        #endregion

        #endregion

        #region Constructors

        /// <summary>
        /// Static constructor
        /// </summary>
        static Gallery()
        {
            var type = typeof(Gallery);
            DefaultStyleKeyProperty.OverrideMetadata(type, new FrameworkPropertyMetadata(typeof(Gallery)));
            SelectedItemProperty.OverrideMetadata(type, new FrameworkPropertyMetadata(null, CoerceSelectedItem));
            ContextMenuService.Attach(type);
        }

        // Coerce selected item
        private static object? CoerceSelectedItem(DependencyObject d, object? basevalue)
        {
            var gallery = (Gallery)d;

            if (gallery.Selectable == false)
            {
                var galleryItem = gallery.ItemContainerGenerator.ContainerOrContainerContentFromItem<GalleryItem>(basevalue);

                if (basevalue is not null
                    && galleryItem is not null)
                {
                    galleryItem.IsSelected = false;
                }

                return null;
            }

            return basevalue;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Gallery()
        {
            ContextMenuService.Coerce(this);
            this.Loaded += this.OnLoaded;
            this.Focusable = false;
            KeyboardNavigation.SetDirectionalNavigation(this, KeyboardNavigationMode.Continue);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.Parent is ItemsControl parent)
            {
                if (parent.Items.IndexOf(this) == parent.Items.Count - 1)
                {
                    this.IsLastItem = true;
                }
                else
                {
                    this.IsLastItem = false;
                }
            }
        }

        #endregion

        #region Overrides

        /// <inheritdoc />
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new GalleryItem();
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is GalleryItem;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (this.groupsMenuButton is not null)
            {
                foreach (var menuItem in this.groupsMenuButton.Items.Cast<MenuItem>())
                {
                    menuItem.Click -= this.OnFilterMenuItemClick;
                }
            }

            this.groupsMenuButton?.Items.Clear();

            this.groupsMenuButton = this.GetTemplateChild("PART_DropDownButton") as DropDownButton;

            if (this.groupsMenuButton is not null)
            {
                for (var i = 0; i < this.Filters.Count; i++)
                {
                    var item = new MenuItem
                    {
                        Header = this.Filters[i].Title,
                        Tag = this.Filters[i],
                        IsDefinitive = false
                    };

                    if (ReferenceEquals(this.Filters[i], this.SelectedFilter))
                    {
                        item.IsChecked = true;
                    }

                    item.Click += this.OnFilterMenuItemClick;
                    this.groupsMenuButton.Items.Add(item);
                }
            }

            base.OnApplyTemplate();
        }

        #endregion
    }
}