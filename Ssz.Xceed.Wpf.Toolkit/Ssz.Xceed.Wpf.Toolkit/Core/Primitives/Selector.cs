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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ssz.Xceed.Wpf.Toolkit.Core.Utilities;

namespace Ssz.Xceed.Wpf.Toolkit.Primitives
{
    public class Selector : ItemsControl, IWeakEventListener //should probably make this control an ICommandSource
    {
        #region Constructors

        public Selector()
        {
            SelectedItems = new ObservableCollection<object>();
            AddHandler(SelectedEvent, new RoutedEventHandler((s, args) => OnItemSelectionChangedCore(args, false)));
            AddHandler(UnSelectedEvent, new RoutedEventHandler((s, args) => OnItemSelectionChangedCore(args, true)));
            _selectedMemberPathValuesHelper = new ValueChangeHelper(OnSelectedMemberPathValuesChanged);
            _valueMemberPathValuesHelper = new ValueChangeHelper(OnValueMemberPathValuesChanged);
        }

        #endregion //Constructors

        #region IWeakEventListener Members

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType == typeof(CollectionChangedEventManager))
            {
                if (ReferenceEquals(_selectedItems, sender))
                {
                    OnSelectedItemsCollectionChanged(sender, (NotifyCollectionChangedEventArgs) e);
                    return true;
                }

                if (ReferenceEquals(ItemsCollection, sender))
                {
                    OnItemsSourceCollectionChanged(sender, (NotifyCollectionChangedEventArgs) e);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Members

        private bool _surpressItemSelectionChanged;
        private bool _ignoreSelectedItemChanged;
        private bool _ignoreSelectedValueChanged;
        private int _ignoreSelectedItemsCollectionChanged;
        private int _ignoreSelectedMemberPathValuesChanged;
        private IList _selectedItems;

        private readonly ValueChangeHelper _selectedMemberPathValuesHelper;
        private readonly ValueChangeHelper _valueMemberPathValuesHelper;

        #endregion //Members

        #region Properties

        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register("Command",
            typeof(ICommand), typeof(Selector), new PropertyMetadata((ICommand) null));

        [TypeConverter(typeof(CommandConverter))]
        public ICommand Command
        {
            get => (ICommand) GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        #region Delimiter

        public static readonly DependencyProperty DelimiterProperty = DependencyProperty.Register("Delimiter",
            typeof(string), typeof(Selector), new UIPropertyMetadata(",", OnDelimiterChanged));

        public string Delimiter
        {
            get => (string) GetValue(DelimiterProperty);
            set => SetValue(DelimiterProperty, value);
        }

        private static void OnDelimiterChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            ((Selector) o).UpdateSelectedValue();
        }

        #endregion

        #region SelectedItem property

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem",
            typeof(object), typeof(Selector), new UIPropertyMetadata(null, OnSelectedItemChanged));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
        {
            ((Selector) sender).OnSelectedItemChanged(args.OldValue, args.NewValue);
        }

        protected virtual void OnSelectedItemChanged(object oldValue, object newValue)
        {
            if (!IsInitialized || _ignoreSelectedItemChanged)
                return;

            _ignoreSelectedItemsCollectionChanged++;
            SelectedItems.Clear();
            if (newValue is not null) SelectedItems.Add(newValue);
            UpdateFromSelectedItems();
            _ignoreSelectedItemsCollectionChanged--;
        }

        #endregion

        #region SelectedItems Property

        public IList SelectedItems
        {
            get => _selectedItems;
            private set
            {
                if (value is null)
                    throw new ArgumentNullException("value");

                var oldCollection = _selectedItems as INotifyCollectionChanged;
                var newCollection = value as INotifyCollectionChanged;

                if (oldCollection is not null) CollectionChangedEventManager.RemoveListener(oldCollection, this);

                if (newCollection is not null) CollectionChangedEventManager.AddListener(newCollection, this);

                _selectedItems = value;
            }
        }

        #endregion SelectedItems


        #region SelectedItemsOverride property

        public static readonly DependencyProperty SelectedItemsOverrideProperty =
            DependencyProperty.Register("SelectedItemsOverride", typeof(IList), typeof(Selector),
                new UIPropertyMetadata(null, SelectedItemsOverrideChanged));

        public IList SelectedItemsOverride
        {
            get => (IList) GetValue(SelectedItemsOverrideProperty);
            set => SetValue(SelectedItemsOverrideProperty, value);
        }

        private static void SelectedItemsOverrideChanged(DependencyObject sender,
            DependencyPropertyChangedEventArgs args)
        {
            ((Selector) sender).OnSelectedItemsOverrideChanged((IList) args.OldValue, (IList) args.NewValue);
        }

        protected virtual void OnSelectedItemsOverrideChanged(IList oldValue, IList newValue)
        {
            if (!IsInitialized)
                return;

            SelectedItems = newValue is not null ? newValue : new ObservableCollection<object>();
            UpdateFromSelectedItems();
        }

        #endregion


        #region SelectedMemberPath Property

        public static readonly DependencyProperty SelectedMemberPathProperty =
            DependencyProperty.Register("SelectedMemberPath", typeof(string), typeof(Selector),
                new UIPropertyMetadata(null, OnSelectedMemberPathChanged));

        public string SelectedMemberPath
        {
            get => (string) GetValue(SelectedMemberPathProperty);
            set => SetValue(SelectedMemberPathProperty, value);
        }

        private static void OnSelectedMemberPathChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var sel = (Selector) o;
            sel.OnSelectedMemberPathChanged((string) e.OldValue, (string) e.NewValue);
        }

        protected virtual void OnSelectedMemberPathChanged(string oldValue, string newValue)
        {
            if (!IsInitialized)
                return;

            UpdateSelectedMemberPathValuesBindings();
        }

        #endregion //SelectedMemberPath

        #region SelectedValue

        public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register("SelectedValue",
            typeof(string), typeof(Selector),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnSelectedValueChanged));

        public string SelectedValue
        {
            get => (string) GetValue(SelectedValueProperty);
            set => SetValue(SelectedValueProperty, value);
        }

        private static void OnSelectedValueChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var selector = o as Selector;
            if (selector is not null)
                selector.OnSelectedValueChanged((string) e.OldValue, (string) e.NewValue);
        }

        protected virtual void OnSelectedValueChanged(string oldValue, string newValue)
        {
            if (!IsInitialized || _ignoreSelectedValueChanged)
                return;

            UpdateFromSelectedValue();
        }

        #endregion //SelectedValue

        #region ValueMemberPath

        public static readonly DependencyProperty ValueMemberPathProperty =
            DependencyProperty.Register("ValueMemberPath", typeof(string), typeof(Selector),
                new UIPropertyMetadata(OnValueMemberPathChanged));

        public string ValueMemberPath
        {
            get => (string) GetValue(ValueMemberPathProperty);
            set => SetValue(ValueMemberPathProperty, value);
        }

        private static void OnValueMemberPathChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            var sel = (Selector) o;
            sel.OnValueMemberPathChanged((string) e.OldValue, (string) e.NewValue);
        }

        protected virtual void OnValueMemberPathChanged(string oldValue, string newValue)
        {
            if (!IsInitialized)
                return;

            UpdateValueMemberPathValuesBindings();
        }

        #endregion

        #region ItemsCollection Property

        protected IEnumerable ItemsCollection => ItemsSource ?? (Items ?? (IEnumerable) new object[0]);

        #endregion

        #endregion //Properties

        #region Base Class Overrides

        protected override bool IsItemItsOwnContainerOverride(object item)
        {
            return item is SelectorItem;
        }

        protected override DependencyObject GetContainerForItemOverride()
        {
            return new SelectorItem();
        }

        protected override void PrepareContainerForItemOverride(DependencyObject element, object item)
        {
            base.PrepareContainerForItemOverride(element, item);

            _surpressItemSelectionChanged = true;
            var selectorItem = element as FrameworkElement;

            selectorItem.SetValue(SelectorItem.IsSelectedProperty, SelectedItems.Contains(item));

            _surpressItemSelectionChanged = false;
        }

        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);

            var oldCollection = oldValue as INotifyCollectionChanged;
            var newCollection = newValue as INotifyCollectionChanged;

            if (oldCollection is not null) CollectionChangedEventManager.RemoveListener(oldCollection, this);

            if (newCollection is not null) CollectionChangedEventManager.AddListener(newCollection, this);

            if (!IsInitialized)
                return;

            RemoveUnavailableSelectedItems();
            UpdateSelectedMemberPathValuesBindings();
            UpdateValueMemberPathValuesBindings();
        }

        // When a DataTemplate includes a CheckComboBox, some bindings are
        // not working, like SelectedValue.
        // We use a priority system to select the good items after initialization.
        public override void EndInit()
        {
            base.EndInit();

            if (SelectedItemsOverride is not null)
                OnSelectedItemsOverrideChanged(null, SelectedItemsOverride);
            else if (SelectedMemberPath is not null)
                OnSelectedMemberPathChanged(null, SelectedMemberPath);
            else if (SelectedValue is not null)
                OnSelectedValueChanged(null, SelectedValue);
            else if (SelectedItem is not null) OnSelectedItemChanged(null, SelectedItem);

            if (ValueMemberPath is not null) OnValueMemberPathChanged(null, ValueMemberPath);
        }

        #endregion //Base Class Overrides

        #region Events

        public static readonly RoutedEvent SelectedEvent = EventManager.RegisterRoutedEvent("SelectedEvent",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Selector));

        public static readonly RoutedEvent UnSelectedEvent = EventManager.RegisterRoutedEvent("UnSelectedEvent",
            RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(Selector));

        public static readonly RoutedEvent ItemSelectionChangedEvent =
            EventManager.RegisterRoutedEvent("ItemSelectionChanged", RoutingStrategy.Bubble,
                typeof(ItemSelectionChangedEventHandler), typeof(Selector));

        public event ItemSelectionChangedEventHandler ItemSelectionChanged
        {
            add => AddHandler(ItemSelectionChangedEvent, value);
            remove => RemoveHandler(ItemSelectionChangedEvent, value);
        }

        #endregion //Events

        #region Methods

        protected object GetItemValue(object item)
        {
            if (!string.IsNullOrEmpty(ValueMemberPath) && item is not null)
            {
                var property = item.GetType().GetProperty(ValueMemberPath);
                if (property is not null)
                    return property.GetValue(item, null);
            }

            return item;
        }

        protected object ResolveItemByValue(string value)
        {
            if (!string.IsNullOrEmpty(ValueMemberPath))
                foreach (var item in ItemsCollection)
                {
                    var property = item.GetType().GetProperty(ValueMemberPath);
                    if (property is not null)
                    {
                        var propertyValue = property.GetValue(item, null);
                        if (value == propertyValue.ToString())
                            return item;
                    }
                }

            return value;
        }

        private bool? GetSelectedMemberPathValue(object item)
        {
            var prop = GetSelectedMemberPathProperty(item);

            return prop is not null
                ? (bool) prop.GetValue(item, null)
                : (bool?) null;
        }

        private void SetSelectedMemberPathValue(object item, bool value)
        {
            var prop = GetSelectedMemberPathProperty(item);

            if (prop is not null) prop.SetValue(item, value, null);
        }

        private PropertyInfo GetSelectedMemberPathProperty(object item)
        {
            PropertyInfo propertyInfo = null;
            if (!string.IsNullOrEmpty(SelectedMemberPath) && item is not null)
            {
                var property = item.GetType().GetProperty(SelectedMemberPath);
                if (property is not null && property.PropertyType == typeof(bool)) propertyInfo = property;
            }

            return propertyInfo;
        }

        /// <summary>
        ///     When SelectedItems collection implements INotifyPropertyChanged, this is the callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSelectedItemsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (_ignoreSelectedItemsCollectionChanged > 0)
                return;

            // Keep it simple for now. Just update all
            UpdateFromSelectedItems();
        }

        private void OnItemSelectionChangedCore(RoutedEventArgs args, bool unselected)
        {
            var item = ItemContainerGenerator.ItemFromContainer((DependencyObject) args.OriginalSource);

            // When the item is it's own container, "UnsetValue" will be returned.
            if (item == DependencyProperty.UnsetValue) item = args.OriginalSource;

            if (unselected)
            {
                while (SelectedItems.Contains(item))
                    SelectedItems.Remove(item);
            }
            else
            {
                if (!SelectedItems.Contains(item))
                    SelectedItems.Add(item);
            }

            OnItemSelectionChanged(
                new ItemSelectionChangedEventArgs(ItemSelectionChangedEvent, this, item, !unselected));
        }

        /// <summary>
        ///     When the ItemsSource implements INotifyPropertyChanged, this is the change callback.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            RemoveUnavailableSelectedItems();
            UpdateSelectedMemberPathValuesBindings();
            UpdateValueMemberPathValuesBindings();
        }

        /// <summary>
        ///     This is called when any value of any item referenced by SelectedMemberPath
        ///     is modified. This may affect the SelectedItems collection.
        /// </summary>
        private void OnSelectedMemberPathValuesChanged()
        {
            if (_ignoreSelectedMemberPathValuesChanged > 0)
                return;

            UpdateFromSelectedMemberPathValues();
        }

        /// <summary>
        ///     This is called when any value of any item referenced by ValueMemberPath
        ///     is modified. This will affect the SelectedValue property
        /// </summary>
        private void OnValueMemberPathValuesChanged()
        {
            UpdateSelectedValue();
        }

        private void UpdateSelectedMemberPathValuesBindings()
        {
            _selectedMemberPathValuesHelper.UpdateValueSource(ItemsCollection, SelectedMemberPath);
        }

        private void UpdateValueMemberPathValuesBindings()
        {
            _valueMemberPathValuesHelper.UpdateValueSource(ItemsCollection, ValueMemberPath);
        }

        /// <summary>
        ///     This method will be called when the "IsSelected" property of an SelectorItem
        ///     has been modified.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnItemSelectionChanged(ItemSelectionChangedEventArgs args)
        {
            if (_surpressItemSelectionChanged)
                return;

            RaiseEvent(args);

            if (Command is not null)
                Command.Execute(args.Item);
        }

        /// <summary>
        ///     Updates the SelectedValue property based on what is present in the SelectedItems property.
        /// </summary>
        private void UpdateSelectedValue()
        {
#if VS2008
      string newValue =
 String.Join( Delimiter, SelectedItems.Cast<object>().Select( x => GetItemValue( x ).ToString() ).ToArray() );
#else
            var newValue = string.Join(Delimiter, SelectedItems.Cast<object>().Select(x => GetItemValue(x)));
#endif
            if (string.IsNullOrEmpty(SelectedValue) || !SelectedValue.Equals(newValue))
            {
                _ignoreSelectedValueChanged = true;
                SelectedValue = newValue;
                _ignoreSelectedValueChanged = false;
            }
        }

        /// <summary>
        ///     Updates the SelectedItem property based on what is present in the SelectedItems property.
        /// </summary>
        private void UpdateSelectedItem()
        {
            if (!SelectedItems.Contains(SelectedItem))
            {
                _ignoreSelectedItemChanged = true;
                SelectedItem = SelectedItems.Count > 0 ? SelectedItems[0] : null;
                _ignoreSelectedItemChanged = false;
            }
        }

        /// <summary>
        ///     Update the SelectedItems collection based on the values
        ///     refered to by the SelectedMemberPath property.
        /// </summary>
        private void UpdateFromSelectedMemberPathValues()
        {
            _ignoreSelectedItemsCollectionChanged++;
            foreach (var item in ItemsCollection)
            {
                var isSelected = GetSelectedMemberPathValue(item);
                if (isSelected is not null)
                {
                    if (isSelected.Value)
                    {
                        if (!SelectedItems.Contains(item)) SelectedItems.Add(item);
                    }
                    else
                    {
                        if (SelectedItems.Contains(item)) SelectedItems.Remove(item);
                    }
                }
            }

            _ignoreSelectedItemsCollectionChanged--;
            UpdateFromSelectedItems();
        }

        /// <summary>
        ///     Updates the following based on the content of SelectedItems:
        ///     - All SelectorItems "IsSelected" properties
        ///     - Values refered to by SelectedMemberPath
        ///     - SelectedItem property
        ///     - SelectedValue property
        ///     Refered to by the SelectedMemberPath property.
        /// </summary>
        private void UpdateFromSelectedItems()
        {
            foreach (var o in ItemsCollection)
            {
                var isSelected = SelectedItems.Contains(o);

                _ignoreSelectedMemberPathValuesChanged++;
                SetSelectedMemberPathValue(o, isSelected);
                _ignoreSelectedMemberPathValuesChanged--;

                var selectorItem = ItemContainerGenerator.ContainerFromItem(o) as SelectorItem;
                if (selectorItem is not null) selectorItem.IsSelected = isSelected;
            }

            UpdateSelectedItem();
            UpdateSelectedValue();
        }

        /// <summary>
        ///     Removes all items from SelectedItems that are no longer in ItemsSource.
        /// </summary>
        private void RemoveUnavailableSelectedItems()
        {
            _ignoreSelectedItemsCollectionChanged++;
            var hash = new HashSet<object>(ItemsCollection.Cast<object>());

            for (var i = 0; i < SelectedItems.Count; i++)
                if (!hash.Contains(SelectedItems[i]))
                {
                    SelectedItems.RemoveAt(i);
                    i--;
                }

            _ignoreSelectedItemsCollectionChanged--;

            UpdateSelectedItem();
            UpdateSelectedValue();
        }

        /// <summary>
        ///     Updates the SelectedItems collection based on the content of
        ///     the SelectedValue property.
        /// </summary>
        private void UpdateFromSelectedValue()
        {
            _ignoreSelectedItemsCollectionChanged++;
            // Just update the SelectedItems collection content 
            // and let the synchronization be made from UpdateFromSelectedItems();
            SelectedItems.Clear();

            if (!string.IsNullOrEmpty(SelectedValue))
            {
                var selectedValues = SelectedValue.Split(new[] {Delimiter}, StringSplitOptions.RemoveEmptyEntries)
                    .ToList();

                foreach (var item in ItemsCollection)
                {
                    var itemValue = GetItemValue(item);

                    var isSelected = itemValue is not null
                                     && selectedValues.Contains(itemValue.ToString());

                    if (isSelected) SelectedItems.Add(item);
                }
            }

            _ignoreSelectedItemsCollectionChanged--;

            UpdateFromSelectedItems();
        }

        #endregion //Methods
    }


    public delegate void ItemSelectionChangedEventHandler(object sender, ItemSelectionChangedEventArgs e);

    public class ItemSelectionChangedEventArgs : RoutedEventArgs
    {
        public ItemSelectionChangedEventArgs(RoutedEvent routedEvent, object source, object item, bool isSelected)
            : base(routedEvent, source)
        {
            Item = item;
            IsSelected = isSelected;
        }

        public bool IsSelected { get; }

        public object Item { get; }
    }
}