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
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Ssz.Utils.Wpf;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid;
using Ssz.Xceed.Wpf.Toolkit.PropertyGrid.Implementation.Editors;

namespace Ssz.Xceed.Wpf.Toolkit
{
    [TemplatePart(Name = PART_NewItemTypesComboBox, Type = typeof(ComboBox))]
    [TemplatePart(Name = PART_PropertyGrid, Type = typeof(PropertyGrid.PropertyGrid))]
    public class CollectionControl : Control
    {
        private const string PART_NewItemTypesComboBox = "PART_NewItemTypesComboBox";
        private const string PART_PropertyGrid = "PART_PropertyGrid";

        private readonly DispatcherTimer _dispatcherTimer;

        #region Override Methods

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (_newItemTypesComboBox is not null)
                _newItemTypesComboBox.Loaded -= NewItemTypesComboBox_Loaded;

            _newItemTypesComboBox = GetTemplateChild(PART_NewItemTypesComboBox) as ComboBox;

            if (_newItemTypesComboBox is not null)
                _newItemTypesComboBox.Loaded += NewItemTypesComboBox_Loaded;


            if (_objectPropertyGrid is not null)
                _objectPropertyGrid.PropertyValueChanged -= ObjectPropertyGridOnPropertyValueChanged;

            _objectPropertyGrid = GetTemplateChild(PART_PropertyGrid) as PropertyGrid.PropertyGrid;

            if (_objectPropertyGrid is not null)
                _objectPropertyGrid.PropertyValueChanged += ObjectPropertyGridOnPropertyValueChanged;
        }

        #endregion

        private void RefreshForPropertyGrid()
        {
            var item = _objectPropertyGrid.SelectedObject as IPropertyGridItem;
            if (item is null || item.RefreshForPropertyGridIsDisabled) return;

            foreach (var child in TreeHelper.FindChilds<IPropertyGridItem>(_objectPropertyGrid))
                child.RefreshForPropertyGrid();

            item.RefreshForPropertyGrid();
        }

        #region Private Members

        private ComboBox _newItemTypesComboBox;
        private PropertyGrid.PropertyGrid _objectPropertyGrid;

        #endregion

        #region Properties

        #region IsReadOnly Property

        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register("IsReadOnly",
            typeof(bool), typeof(CollectionControl), new UIPropertyMetadata(false));

        public bool IsReadOnly
        {
            get => (bool) GetValue(IsReadOnlyProperty);
            set => SetValue(IsReadOnlyProperty, value);
        }

        #endregion //Items

        #region Items Property

        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register("Items",
            typeof(ReferenceEqualityList<object>), typeof(CollectionControl), new UIPropertyMetadata(null));

        public ReferenceEqualityList<object> Items
        {
            get => (ReferenceEqualityList<object>) GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        #endregion //Items

        #region ItemsSource Property

        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register("ItemsSource",
            typeof(IList), typeof(CollectionControl), new UIPropertyMetadata(null, OnItemsSourceChanged));

        public IList ItemsSource
        {
            get => (IList) GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var collectionControl = (CollectionControl) d;
            if (collectionControl is not null)
                collectionControl.OnItemSourceChanged((IList) e.OldValue, (IList) e.NewValue);
        }

        public void OnItemSourceChanged(IList oldValue, IList newValue)
        {
            // VP
            var items = new ReferenceEqualityList<object>();
            // End VP
            if (newValue is not null)
                foreach (var item in newValue)
                    items.Add(CreateClone(item));
            Items = items;

            SelectedItem = Items.FirstOrDefault();
        }

        #endregion //ItemsSource

        #region ItemsSourceType Property

        public static readonly DependencyProperty ItemsSourceTypeProperty =
            DependencyProperty.Register("ItemsSourceType", typeof(Type), typeof(CollectionControl),
                new UIPropertyMetadata(null));

        public Type ItemsSourceType
        {
            get => (Type) GetValue(ItemsSourceTypeProperty);
            set => SetValue(ItemsSourceTypeProperty, value);
        }

        #endregion //ItemsSourceType

        #region NewItemType Property

        public static readonly DependencyProperty NewItemTypesProperty = DependencyProperty.Register("NewItemTypes",
            typeof(IList), typeof(CollectionControl), new UIPropertyMetadata(null));

        public IList<Type> NewItemTypes
        {
            get => (IList<Type>) GetValue(NewItemTypesProperty);
            set => SetValue(NewItemTypesProperty, value);
        }

        #endregion //NewItemType

        #region SelectedItem Property

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register("SelectedItem",
            typeof(object), typeof(CollectionControl), new UIPropertyMetadata(SelectedItemChanged));

        private static void SelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var objectPropertyGrid = ((CollectionControl) d)._objectPropertyGrid;
            if (objectPropertyGrid is not null)
            {
                if (objectPropertyGrid.SelectedObject is not null)
                {
                    var cancelEventArgs = new CancelEventArgs();

                    objectPropertyGrid.EndEditInPropertyGrid();

                    ((CollectionControl) d).RefreshForPropertyGrid();
                }

                objectPropertyGrid.SelectedObject = e.NewValue;
            }
        }

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        #endregion //SelectedItem

        #endregion //Properties

        #region Constructors

        static CollectionControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CollectionControl),
                new FrameworkPropertyMetadata(typeof(CollectionControl)));
        }

        public CollectionControl()
        {
            Items = new ReferenceEqualityList<object>();
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, AddNew, CanAddNew));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, Delete, CanDelete));
            CommandBindings.Add(new CommandBinding(ComponentCommands.MoveDown, MoveDown, CanMoveDown));
            CommandBindings.Add(new CommandBinding(ComponentCommands.MoveUp, MoveUp, CanMoveUp));

            _dispatcherTimer = new DispatcherTimer(new TimeSpan(0, 0, 2), DispatcherPriority.Background,
                (sender, e) => RefreshForPropertyGrid(),
                Dispatcher);

            _dispatcherTimer.Start();
        }

        #endregion //Constructors

        #region Events

        #region ItemDeleting Event

        public delegate void ItemDeletingRoutedEventHandler(object sender, ItemDeletingEventArgs e);

        public static readonly RoutedEvent ItemDeletingEvent = EventManager.RegisterRoutedEvent("ItemDeleting",
            RoutingStrategy.Bubble, typeof(ItemDeletingRoutedEventHandler), typeof(CollectionControl));

        public event ItemDeletingRoutedEventHandler ItemDeleting
        {
            add => AddHandler(ItemDeletingEvent, value);
            remove => RemoveHandler(ItemDeletingEvent, value);
        }

        #endregion //ItemDeleting Event

        #region ItemDeleted Event

        public delegate void ItemDeletedRoutedEventHandler(object sender, ItemEventArgs e);

        public static readonly RoutedEvent ItemDeletedEvent = EventManager.RegisterRoutedEvent("ItemDeleted",
            RoutingStrategy.Bubble, typeof(ItemDeletedRoutedEventHandler), typeof(CollectionControl));

        public event ItemDeletedRoutedEventHandler ItemDeleted
        {
            add => AddHandler(ItemDeletedEvent, value);
            remove => RemoveHandler(ItemDeletedEvent, value);
        }

        #endregion //ItemDeleted Event

        #region ItemAdding Event

        public delegate void ItemAddingRoutedEventHandler(object sender, ItemAddingEventArgs e);

        public static readonly RoutedEvent ItemAddingEvent = EventManager.RegisterRoutedEvent("ItemAdding",
            RoutingStrategy.Bubble, typeof(ItemAddingRoutedEventHandler), typeof(CollectionControl));

        public event ItemAddingRoutedEventHandler ItemAdding
        {
            add => AddHandler(ItemAddingEvent, value);
            remove => RemoveHandler(ItemAddingEvent, value);
        }

        #endregion //ItemAdding Event

        #region ItemAdded Event

        public delegate void ItemAddedRoutedEventHandler(object sender, ItemEventArgs e);

        public static readonly RoutedEvent ItemAddedEvent = EventManager.RegisterRoutedEvent("ItemAdded",
            RoutingStrategy.Bubble, typeof(ItemAddedRoutedEventHandler), typeof(CollectionControl));

        public event ItemAddedRoutedEventHandler ItemAdded
        {
            add => AddHandler(ItemAddedEvent, value);
            remove => RemoveHandler(ItemAddedEvent, value);
        }

        #endregion //ItemAdded Event

        #endregion

        #region EventHandlers

        private void NewItemTypesComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (_newItemTypesComboBox is not null)
                _newItemTypesComboBox.SelectedIndex = 0;
        }

        private void ObjectPropertyGridOnPropertyValueChanged(object sender, PropertyValueChangedEventArgs e)
        {
            if (_objectPropertyGrid is not null)
            {
                if (_objectPropertyGridUpdating) return;
                _objectPropertyGridUpdating = true;
                _objectPropertyGrid.Update();
                _objectPropertyGridUpdating = false;
            }
        }

        private bool _objectPropertyGridUpdating;

        #endregion

        #region Commands

        private void AddNew(object sender, ExecutedRoutedEventArgs e)
        {
            var newItem = CreateNewItem((Type) e.Parameter);

            var eventArgs = new ItemAddingEventArgs(ItemAddingEvent, newItem);
            RaiseEvent(eventArgs);
            if (eventArgs.Cancel)
                return;
            newItem = eventArgs.Item;

            var items = Items;
            Items = null;
            items.Add(newItem);
            Items = items;

            RaiseEvent(new ItemEventArgs(ItemAddedEvent, newItem));

            SelectedItem = newItem;
        }

        private void CanAddNew(object sender, CanExecuteRoutedEventArgs e)
        {
            var t = e.Parameter as Type;
            if (t is not null && t.GetConstructor(Type.EmptyTypes) is not null && !IsReadOnly)
                e.CanExecute = true;
        }

        private void Delete(object sender, ExecutedRoutedEventArgs e)
        {
            var eventArgs = new ItemDeletingEventArgs(ItemDeletingEvent, e.Parameter);
            RaiseEvent(eventArgs);
            if (eventArgs.Cancel)
                return;

            var items = Items;
            Items = null;
            items.Remove(e.Parameter);
            Items = items;

            RaiseEvent(new ItemEventArgs(ItemDeletedEvent, e.Parameter));
        }

        private void CanDelete(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = e.Parameter is not null && !IsReadOnly;
        }

        private void MoveDown(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedItem = e.Parameter;

            var items = Items;
            Items = null;
            var index = items.IndexOf(selectedItem);
            items.RemoveAt(index);
            items.Insert(++index, selectedItem);
            Items = items;

            SelectedItem = selectedItem;
        }

        private void CanMoveDown(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter is not null && Items.IndexOf(e.Parameter) < Items.Count - 1 && !IsReadOnly)
                e.CanExecute = true;
        }

        private void MoveUp(object sender, ExecutedRoutedEventArgs e)
        {
            var selectedItem = e.Parameter;

            var items = Items;
            Items = null;
            var index = items.IndexOf(selectedItem);
            items.RemoveAt(index);
            items.Insert(--index, selectedItem);
            Items = items;

            SelectedItem = selectedItem;
        }

        private void CanMoveUp(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Parameter is not null && Items.IndexOf(e.Parameter) > 0 && !IsReadOnly)
                e.CanExecute = true;
        }

        #endregion //Commands

        #region Methods

        private static void CopyValues(object source, object destination)
        {
            var currentType = source.GetType();

            while (currentType is not null)
            {
                var myObjectFields =
                    currentType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                foreach (var fi in myObjectFields) fi.SetValue(destination, fi.GetValue(source));

                currentType = currentType.BaseType;
            }
        }

        private object CreateClone(object source)
        {
            // VP
            var cloneableSource = source as ICloneable;
            if (cloneableSource is not null) return cloneableSource.Clone();
            // End VP
            object clone = null;

            var type = source.GetType();
            clone = Activator.CreateInstance(type);
            CopyValues(source, clone);

            return clone;
        }

        private IList CreateItemsSource()
        {
            IList list = null;

            if (ItemsSourceType is not null)
            {
                var constructor = ItemsSourceType.GetConstructor(Type.EmptyTypes);
                list = (IList) constructor.Invoke(null);
            }

            return list;
        }

        private object CreateNewItem(Type type)
        {
            return Activator.CreateInstance(type);
        }

        // VP. Returns true if collection changed
        public bool PersistChanges()
        {
            _dispatcherTimer.Stop();

            _objectPropertyGrid.EndEditInPropertyGrid();

            RefreshForPropertyGrid();

            var originalList = ComputeItemsSource();
            if (originalList is null)
                return false;

            if (originalList.OfType<object>().SequenceEqual(Items)) return false;
            //the easiest way to persist changes to the source is to just clear the source list and then add all items to it.
            originalList.Clear();

            if (originalList.IsFixedSize)
                for (var i = 0; i < Items.Count; ++i)
                    originalList[i] = Items[i];
            else
                foreach (var item in Items)
                    originalList.Add(item);

            return true;
        }

        private IList ComputeItemsSource()
        {
            if (ItemsSource is null)
                ItemsSource = CreateItemsSource();

            return ItemsSource;
        }

        #endregion //Methods
    }

    /// <summary>
    ///     A generic List that would only use object's reference,
    ///     ignoring any <see cref="IEquatable{T}" /> or <see cref="object.Equals(object)" />  overrides.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class ReferenceEqualityList<T> : IList<T>, IList, IReadOnlyList<T>
    {
        #region internal functions

        internal static IList<T> Synchronized(ReferenceEqualityList<T> list)
        {
            return new SynchronizedList(list);
        }

        #endregion

        [Serializable]
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private readonly ReferenceEqualityList<T> list;
            private int index;
            private readonly int version;

            internal Enumerator(ReferenceEqualityList<T> list)
            {
                this.list = list;
                index = 0;
                version = list._version;
                Current = default;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                var localList = list;

                if (version == localList._version && (uint) index < (uint) localList.Count)
                {
                    Current = localList._items[index];
                    index++;
                    return true;
                }

                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (version != list._version) throw new InvalidOperationException();

                index = list.Count + 1;
                Current = default;
                return false;
            }

            public T Current { get; private set; }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == list.Count + 1) throw new InvalidOperationException();
                    return Current;
                }
            }

            void IEnumerator.Reset()
            {
                if (version != list._version) throw new InvalidOperationException();

                index = 0;
                Current = default;
            }
        }

        [Serializable]
        internal class SynchronizedList : IList<T>
        {
            #region construction and destruction

            internal SynchronizedList(ReferenceEqualityList<T> list)
            {
                _list = list;
                _root = ((ICollection) list).SyncRoot;
            }

            #endregion

            #region public functions

            public int Count
            {
                get
                {
                    lock (_root)
                    {
                        return _list.Count;
                    }
                }
            }

            public bool IsReadOnly => ((ICollection<T>) _list).IsReadOnly;

            public T this[int index]
            {
                get
                {
                    lock (_root)
                    {
                        return _list[index];
                    }
                }
                set
                {
                    lock (_root)
                    {
                        _list[index] = value;
                    }
                }
            }

            public void Add(T item)
            {
                lock (_root)
                {
                    _list.Add(item);
                }
            }

            public void Clear()
            {
                lock (_root)
                {
                    _list.Clear();
                }
            }

            public bool Contains(T item)
            {
                lock (_root)
                {
                    return _list.Contains(item);
                }
            }

            public void CopyTo(T[] array, int arrayIndex)
            {
                lock (_root)
                {
                    _list.CopyTo(array, arrayIndex);
                }
            }

            public bool Remove(T item)
            {
                lock (_root)
                {
                    return _list.Remove(item);
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                lock (_root)
                {
                    return _list.GetEnumerator();
                }
            }

            IEnumerator<T> IEnumerable<T>.GetEnumerator()
            {
                lock (_root)
                {
                    return ((IEnumerable<T>) _list).GetEnumerator();
                }
            }

            public int IndexOf(T item)
            {
                lock (_root)
                {
                    return _list.IndexOf(item);
                }
            }

            public void Insert(int index, T item)
            {
                lock (_root)
                {
                    _list.Insert(index, item);
                }
            }

            public void RemoveAt(int index)
            {
                lock (_root)
                {
                    _list.RemoveAt(index);
                }
            }

            #endregion

            #region private fields

            private readonly ReferenceEqualityList<T> _list;
            private readonly object _root;

            #endregion
        }
        // Constructs a List. The list is initially empty and has a capacity
        // of zero. Upon adding the first element to the list the capacity is
        // increased to 16, and then increased in multiples of two as required.

        #region construction and destruction

        public ReferenceEqualityList()
        {
            _items = _emptyArray;
        }

        // Constructs a List with a given initial capacity. The list is
        // initially empty, but will have room for the given number of elements
        // before any reallocations are required.
        // 
        public ReferenceEqualityList(int capacity)
        {
            if (capacity < 0) throw new ArgumentException("capacity");

            if (capacity == 0)
                _items = _emptyArray;
            else
                _items = new T[capacity];
        }

        // Constructs a List, copying the contents of the given collection. The
        // size and capacity of the new list will both be equal to the size of the
        // given collection.
        // 
        public ReferenceEqualityList(IEnumerable<T> collection)
        {
            if (collection is null)
                throw new ArgumentNullException("collection");

            var c = collection as ICollection<T>;
            if (c is not null)
            {
                var count = c.Count;
                if (count == 0)
                {
                    _items = _emptyArray;
                }
                else
                {
                    _items = new T[count];
                    c.CopyTo(_items, 0);
                    Count = count;
                }
            }
            else
            {
                Count = 0;
                _items = _emptyArray;
                // This enumerable could be empty.  Let Add allocate a new array, if needed.
                // Note it will also go to _defaultCapacity first, not 1, then 2, etc.

                using (var en = collection.GetEnumerator())
                {
                    while (en.MoveNext()) Add(en.Current);
                }
            }
        }

        #endregion

        // Gets and sets the capacity of this list.  The capacity is the size of
        // the internal array used to hold items.  When set, the internal 
        // array of the list is reallocated to the given capacity.
        // 

        #region public functions

        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value < Count) throw new ArgumentOutOfRangeException();

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        var newItems = new T[value];
                        if (Count > 0) Array.Copy(_items, 0, newItems, 0, Count);
                        _items = newItems;
                    }
                    else
                    {
                        _items = _emptyArray;
                    }
                }
            }
        }

        // Read-only property describing how many elements are in the List.
        [field: ContractPublicPropertyName("Count")]
        public int Count { get; private set; }

        bool IList.IsFixedSize => false;


        // Is this List read-only?
        bool ICollection<T>.IsReadOnly => false;

        bool IList.IsReadOnly => false;

        // Is this List synchronized (thread-safe)?
        bool ICollection.IsSynchronized => false;

        // Synchronization root for this object.
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot is null) Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);
                return _syncRoot;
            }
        }

        // Sets or Gets the element at the given index.
        // 
        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint) index >= (uint) Count) throw new ArgumentOutOfRangeException();

                return _items[index];
            }

            set
            {
                if ((uint) index >= (uint) Count) throw new ArgumentOutOfRangeException();

                _items[index] = value;
                _version++;
            }
        }

        object IList.this[int index]
        {
            get => this[index];
            set
            {
                try
                {
                    this[index] = (T) value;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException();
                }
            }
        }

        // Adds the given object to the end of this list. The size of the list is
        // increased by one. If required, the capacity of the list is doubled
        // before adding the new element.
        //
        public void Add(T item)
        {
            if (Count == _items.Length) EnsureCapacity(Count + 1);
            _items[Count++] = item;
            _version++;
        }

        int IList.Add(object item)
        {
            try
            {
                Add((T) item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException();
            }

            return Count - 1;
        }


        // Adds the elements of the given collection to the end of this list. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.
        //
        public void AddRange(IEnumerable<T> collection)
        {
            InsertRange(Count, collection);
        }

        public ReadOnlyCollection<T> AsReadOnly()
        {
            return new(this);
        }

        // Searches a section of the list for a given element using a binary search
        // algorithm. Elements of the list are compared to the search value using
        // the given IComparer interface. If comparer is null, elements of
        // the list are compared to the search value using the IComparable
        // interface, which in that case must be implemented by all elements of the
        // list and the given search value. This method assumes that the given
        // section of the list is already sorted; if this is not the case, the
        // result will be incorrect.
        //
        // The method returns the index of the given value in the list. If the
        // list does not contain the given value, the method returns a negative
        // integer. The bitwise complement operator (~) can be applied to a
        // negative result to produce the index of the first element (if any) that
        // is larger than the given search value. This is also the index at which
        // the search value should be inserted into the list in order for the list
        // to remain sorted.
        // 
        // The method uses the Array.BinarySearch method to perform the
        // search.
        // 
        public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (Count - index < count)
                throw new ArgumentException();

            return Array.BinarySearch(_items, index, count, item, comparer);
        }

        public int BinarySearch(T item)
        {
            return BinarySearch(0, Count, item, null);
        }

        public int BinarySearch(T item, IComparer<T> comparer)
        {
            return BinarySearch(0, Count, item, comparer);
        }


        // Clears the contents of List.
        public void Clear()
        {
            if (Count > 0)
            {
                Array.Clear(_items, 0, Count);
                // Don't need to doc this but we clear the elements so that the gc can reclaim the references.
                Count = 0;
            }

            _version++;
        }

        // Contains returns true if the specified element is in the List.
        // It does a linear, O(n) search.  Equality is determined by calling
        // item.Equals().
        //
        public bool Contains(T item)
        {
            if (item is null)
            {
                for (var i = 0; i < Count; i++)
                    if (_items[i] is null)
                        return true;
                return false;
            }

            var c = EqualityComparer<T>.Default;
            for (var i = 0; i < Count; i++)
                if (c.Equals(_items[i], item))
                    return true;
            return false;
        }

        bool IList.Contains(object item)
        {
            if (IsCompatibleObject(item)) return Contains((T) item);
            return false;
        }

        public ReferenceEqualityList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter is null) throw new ArgumentNullException("converter");
            // @


            var list = new ReferenceEqualityList<TOutput>(Count);
            for (var i = 0; i < Count; i++) list._items[i] = converter(_items[i]);
            list.Count = Count;
            return list;
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
        public void CopyTo(T[] array)
        {
            CopyTo(array, 0);
        }

        // Copies this List into array, which must be of a 
        // compatible array type.  
        //
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if (array is not null && array.Rank != 1) throw new ArgumentException();


            try
            {
                // Array.Copy will check for NULL.
                Array.Copy(_items, 0, array, arrayIndex, Count);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException();
            }
        }

        // Copies a section of this list to the given array at the given index.
        // 
        // The method uses the Array.Copy method to copy the elements.
        // 
        public void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (Count - index < count) throw new ArgumentException();


            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, 0, array, arrayIndex, Count);
        }

        // Ensures that the capacity of this list is at least the given minimum
        // value. If the currect capacity of the list is less than min, the
        // capacity is increased to twice the current capacity or to min,
        // whichever is larger.

        public bool Exists(Predicate<T> match)
        {
            return FindIndex(match) != -1;
        }

        public T Find(Predicate<T> match)
        {
            if (match is null) throw new ArgumentNullException("match");


            for (var i = 0; i < Count; i++)
                if (match(_items[i]))
                    return _items[i];
            return default;
        }

        public List<T> FindAll(Predicate<T> match)
        {
            if (match is null) throw new ArgumentNullException("match");


            var list = new List<T>();
            for (var i = 0; i < Count; i++)
                if (match(_items[i]))
                    list.Add(_items[i]);
            return list;
        }

        public int FindIndex(Predicate<T> match)
        {
            return FindIndex(0, Count, match);
        }

        public int FindIndex(int startIndex, Predicate<T> match)
        {
            return FindIndex(startIndex, Count - startIndex, match);
        }

        public int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if ((uint) startIndex > (uint) Count) throw new ArgumentOutOfRangeException("startIndex");

            if (count < 0 || startIndex > Count - count) throw new ArgumentOutOfRangeException();

            if (match is null) throw new ArgumentNullException("match");

            var endIndex = startIndex + count;
            for (var i = startIndex; i < endIndex; i++)
                if (match(_items[i]))
                    return i;
            return -1;
        }

        public T FindLast(Predicate<T> match)
        {
            if (match is null) throw new ArgumentNullException("match");


            for (var i = Count - 1; i >= 0; i--)
                if (match(_items[i]))
                    return _items[i];
            return default;
        }

        public int FindLastIndex(Predicate<T> match)
        {
            return FindLastIndex(Count - 1, Count, match);
        }

        public int FindLastIndex(int startIndex, Predicate<T> match)
        {
            return FindLastIndex(startIndex, startIndex + 1, match);
        }

        public int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match is null) throw new ArgumentNullException("match");

            if (Count == 0)
            {
                // Special case for 0 length List
                if (startIndex != -1) throw new ArgumentOutOfRangeException("startIndex");
            }
            else
            {
                // Make sure we're not out of range            
                if ((uint) startIndex >= (uint) Count) throw new ArgumentOutOfRangeException("startIndex");
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0) throw new ArgumentOutOfRangeException();

            var endIndex = startIndex - count;
            for (var i = startIndex; i > endIndex; i--)
                if (match(_items[i]))
                    return i;
            return -1;
        }

        public void ForEach(Action<T> action)
        {
            if (action is null) throw new ArgumentNullException("action");


            var version = _version;

            for (var i = 0; i < Count; i++)
            {
                if (version != _version) break;
                action(_items[i]);
            }

            if (version != _version)
                throw new InvalidOperationException();
        }

        // Returns an enumerator for this list with the given
        // permission for removal of elements. If modifications made to the list 
        // while an enumeration is in progress, the MoveNext and 
        // GetObject methods of the enumerator will throw an exception.
        //
        public Enumerator GetEnumerator()
        {
            return new(this);
        }

        /// <internalonly />
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public ReferenceEqualityList<T> GetRange(int index, int count)
        {
            if (index < 0) throw new ArgumentOutOfRangeException("index");

            if (count < 0) throw new ArgumentOutOfRangeException("count");

            if (Count - index < count) throw new ArgumentException();

            var list = new ReferenceEqualityList<T>(count);
            Array.Copy(_items, index, list._items, 0, count);
            list.Count = count;
            return list;
        }

        /// <summary>
        ///     Returns the index of the first occurrence of a given value in a range of
        ///     this list. The list is searched forwards from beginning to end.
        ///     The elements of the list are compared to the given value using the
        ///     Object.ReferenceEquals method.
        /// </summary>
        public int IndexOf(T item)
        {
            return IndexOf(_items, item, 0, Count);
        }

        int IList.IndexOf(object item)
        {
            if (IsCompatibleObject(item)) return IndexOf((T) item);
            return -1;
        }


        /// <summary>
        ///     Returns the index of the first occurrence of a given value in a range of
        ///     this list. The list is searched forwards, starting at index
        ///     index and ending at count number of elements. The
        ///     elements of the list are compared to the given value using the
        ///     Object.ReferenceEquals method.
        /// </summary>
        public int IndexOf(T item, int index)
        {
            if (index > Count)
                throw new ArgumentOutOfRangeException("index");

            return IndexOf(_items, item, index, Count - index);
        }

        /// <summary>
        ///     Returns the index of the first occurrence of a given value in a range of
        ///     this list. The list is searched forwards, starting at index
        ///     index and upto count number of elements. The
        ///     elements of the list are compared to the given value using the
        ///     Object.ReferenceEquals method.
        /// </summary>
        public int IndexOf(T item, int index, int count)
        {
            if (index > Count)
                throw new ArgumentOutOfRangeException("index");

            if (count < 0 || index > Count - count) throw new ArgumentOutOfRangeException();

            return IndexOf(_items, item, index, count);
        }

        // Inserts an element into this list at a given index. The size of the list
        // is increased by one. If required, the capacity of the list is doubled
        // before inserting the new element.
        // 
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint) index > (uint) Count) throw new ArgumentOutOfRangeException("index");

            if (Count == _items.Length) EnsureCapacity(Count + 1);
            if (index < Count) Array.Copy(_items, index, _items, index + 1, Count - index);
            _items[index] = item;
            Count++;
            _version++;
        }

        void IList.Insert(int index, object item)
        {
            try
            {
                Insert(index, (T) item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException();
            }
        }

        // Inserts the elements of the given collection at a given index. If
        // required, the capacity of the list is increased to twice the previous
        // capacity or the new size, whichever is larger.  Ranges may be added
        // to the end of the list by setting index to the List's size.
        //
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection is null) throw new ArgumentNullException("collection");

            if ((uint) index > (uint) Count) throw new ArgumentOutOfRangeException("index");


            var c = collection as ICollection<T>;
            if (c is not null)
            {
                // if collection is ICollection<T>
                var count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(Count + count);
                    if (index < Count) Array.Copy(_items, index, _items, index + count, Count - index);

                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (this == c)
                    {
                        // Copy first part of _items to insert location
                        Array.Copy(_items, 0, _items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(_items, index + count, _items, index * 2, Count - index);
                    }
                    else
                    {
                        var itemsToInsert = new T[count];
                        c.CopyTo(itemsToInsert, 0);
                        itemsToInsert.CopyTo(_items, index);
                    }

                    Count += count;
                }
            }
            else
            {
                using (var en = collection.GetEnumerator())
                {
                    while (en.MoveNext()) Insert(index++, en.Current);
                }
            }

            _version++;
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at the end 
        // and ending at the first element in the list. The elements of the list 
        // are compared to the given value using the Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item)
        {
            if (Count == 0)
                // Special case for empty list
                return -1;
            return LastIndexOf(item, Count - 1, Count);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and ending at the first element in the list. The 
        // elements of the list are compared to the given value using the 
        // Object.Equals method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index)
        {
            if (index >= Count)
                throw new ArgumentOutOfRangeException("index");

            return LastIndexOf(item, index, index + 1);
        }

        // Returns the index of the last occurrence of a given value in a range of
        // this list. The list is searched backwards, starting at index
        // index and upto count elements. The elements of
        // the list are compared to the given value using the Object.Equals
        // method.
        // 
        // This method uses the Array.LastIndexOf method to perform the
        // search.
        // 
        public int LastIndexOf(T item, int index, int count)
        {
            if (Count != 0 && index < 0) throw new ArgumentOutOfRangeException("index");

            if (Count != 0 && count < 0) throw new ArgumentOutOfRangeException("count");

            if (Count == 0)
                // Special case for empty list
                return -1;

            if (index >= Count) throw new ArgumentOutOfRangeException("index");

            if (count > index + 1) throw new ArgumentOutOfRangeException();

            return Array.LastIndexOf(_items, item, index, count);
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public bool Remove(T item)
        {
            var index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        void IList.Remove(object item)
        {
            if (IsCompatibleObject(item)) Remove((T) item);
        }

        // This method removes all items which matches the predicate.
        // The complexity is O(n).   
        public int RemoveAll(Predicate<T> match)
        {
            if (match is null) throw new ArgumentNullException("match");

            var freeIndex = 0; // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < Count && !match(_items[freeIndex])) freeIndex++;
            if (freeIndex >= Count) return 0;

            var current = freeIndex + 1;
            while (current < Count)
            {
                // Find the first item which needs to be kept.
                while (current < Count && match(_items[current])) current++;

                if (current < Count)
                    // copy item to the free slot.
                    _items[freeIndex++] = _items[current++];
            }

            Array.Clear(_items, freeIndex, Count - freeIndex);
            var result = Count - freeIndex;
            Count = freeIndex;
            _version++;
            return result;
        }

        // Removes the element at the given index. The size of the list is
        // decreased by one.
        // 
        public void RemoveAt(int index)
        {
            if ((uint) index >= (uint) Count) throw new ArgumentOutOfRangeException();

            Count--;
            if (index < Count) Array.Copy(_items, index + 1, _items, index, Count - index);
            _items[Count] = default;
            _version++;
        }

        // Removes a range of elements from this list.
        // 
        public void RemoveRange(int index, int count)
        {
            if (index < 0) throw new ArgumentOutOfRangeException("index");

            if (count < 0) throw new ArgumentOutOfRangeException("count");

            if (Count - index < count)
                throw new ArgumentException();


            if (count > 0)
            {
                var i = Count;
                Count -= count;
                if (index < Count) Array.Copy(_items, index + count, _items, index, Count - index);
                Array.Clear(_items, Count, count);
                _version++;
            }
        }

        // Reverses the elements in this list.
        public void Reverse()
        {
            Reverse(0, Count);
        }

        // Reverses the elements in a range of this list. Following a call to this
        // method, an element in the range given by index and count
        // which was previously located at index i will now be located at
        // index index + (index + count - i - 1).
        // 
        // This method uses the Array.Reverse method to reverse the
        // elements.
        // 
        public void Reverse(int index, int count)
        {
            if (index < 0) throw new ArgumentOutOfRangeException("index");

            if (count < 0) throw new ArgumentOutOfRangeException("count");

            if (Count - index < count)
                throw new ArgumentException();

            Array.Reverse(_items, index, count);
            _version++;
        }

        // Sorts the elements in this list.  Uses the default comparer and 
        // Array.Sort.
        public void Sort()
        {
            Sort(0, Count, null);
        }

        // Sorts the elements in this list.  Uses Array.Sort with the
        // provided comparer.
        public void Sort(IComparer<T> comparer)
        {
            Sort(0, Count, comparer);
        }

        // Sorts the elements in a section of this list. The sort compares the
        // elements to each other using the given IComparer interface. If
        // comparer is null, the elements are compared to each other using
        // the IComparable interface, which in that case must be implemented by all
        // elements of the list.
        // 
        // This method uses the Array.Sort method to sort the elements.
        // 
        public void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0) throw new ArgumentOutOfRangeException("index");

            if (count < 0) throw new ArgumentOutOfRangeException("count");

            if (Count - index < count)
                throw new ArgumentException();


            Array.Sort(_items, index, count, comparer);
            _version++;
        }

        /*
        public void Sort(Comparison<T> comparison) {
            if( comparison is null) {
                throw new ArgumentNullException("comparison");
            }
            
 
            if( _size > 0) {
                IComparer<T> comparer = new Array.FunctorComparer<T>(comparison);
                Array.Sort(_items, 0, _size, comparer);
            }
        }*/

        // ToArray returns a new Object array containing the contents of the List.
        // This requires copying the List, which is an O(n) operation.
        public T[] ToArray()
        {
            var array = new T[Count];
            Array.Copy(_items, 0, array, 0, Count);
            return array;
        }

        // Sets the capacity of this list to the size of the list. This method can
        // be used to minimize a list's memory overhead once it is known that no
        // new elements will be added to the list. To completely clear a list and
        // release all memory referenced by the list, execute the following
        // statements:
        // 
        // list.Clear();
        // list.TrimExcess();
        // 
        public void TrimExcess()
        {
            var threshold = (int) (_items.Length * 0.9);
            if (Count < threshold) Capacity = Count;
        }

        public bool TrueForAll(Predicate<T> match)
        {
            if (match is null) throw new ArgumentNullException("match");


            for (var i = 0; i < Count; i++)
                if (!match(_items[i]))
                    return false;
            return true;
        }

        #endregion

        #region private functions

        private static bool IsCompatibleObject(object value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>. 
            return value is T || value is null && default(T) is null;
        }

        private static int IndexOf(T[] array, T value, int startIndex, int count)
        {
            if (array is null)
                throw new ArgumentNullException("array");
            var lb = array.GetLowerBound(0);
            if (startIndex < lb || startIndex > array.Length + lb)
                throw new ArgumentOutOfRangeException("startIndex");
            if (count < 0 || count > array.Length - startIndex + lb)
                throw new ArgumentOutOfRangeException("count");
            if (array.Rank != 1)
                throw new RankException();

            var endIndex = startIndex + count;

            if (ReferenceEquals(value, null))
            {
                for (var i = startIndex; i < endIndex; i++)
                    if (ReferenceEquals(array[i], null))
                        return i;
            }
            else
            {
                for (var i = startIndex; i < endIndex; i++)
                    if (ReferenceEquals(array[i], value))
                        return i;
            }

            // Return one less than the lower bound of the array.  This way, 
            // for arrays with a lower bound of -1 we will not return -1 when the 
            // item was not found.  And for SZArrays (the vast majority), -1 still
            // works for them. 
            return lb - 1;
        }

        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                var newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                var elementSize = typeof(T).IsValueType ? Marshal.SizeOf(typeof(T)) : Marshal.SizeOf(typeof(IntPtr));
                var maxArrayLength = 2147483591 / elementSize; // max element count
                if ((uint) newCapacity > maxArrayLength) newCapacity = maxArrayLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        #endregion

        #region private fields

        private static readonly T[] _emptyArray = new T[0];
        private T[] _items;
        private int _version;
        [NonSerialized] private object _syncRoot;
        private const int _defaultCapacity = 4;

        #endregion
    }
}