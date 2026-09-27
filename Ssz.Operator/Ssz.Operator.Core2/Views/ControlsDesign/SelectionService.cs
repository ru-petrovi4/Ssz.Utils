using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;

namespace Ssz.Operator.Core.ControlsDesign
{
    /// <summary>
    ///     Keeps what is selected in a list or on a drawing, and works out what a click means when Ctrl
    ///     or Shift is held.
    ///     <para>
    ///         Ported from the WPF editor. There <see cref="UpdateSelection" /> read the keyboard itself
    ///         through Keyboard.Modifiers; Avalonia has no such ambient state, so the caller passes the
    ///         modifiers of the event it is handling.
    ///     </para>
    /// </summary>
    public class SelectionService<T> : IDisposable
        where T : class, ISelectable
    {
        #region construction and destruction

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                _allItems.Clear();
                _selectedItems.Clear();
            }

            Disposed = true;
        }

        ~SelectionService()
        {
            Dispose(false);
        }

        #endregion

        #region public functions

        public bool IsAnySelected => _selectedItems.Count > 0;

        public T[] SelectedItems => _selectedItems.ToArray();

        public T? FirstSelectedItem => _selectedItems.FirstOrDefault();

        public IEnumerable<T> AllItems
        {
            get => _allItems.ToArray();
            set => _allItems = new List<T>(value);
        }

        public void SelectOne(T item)
        {
            ClearSelection();
            AddToSelection(item);
        }

        public void AddToSelection(T item)
        {
            _selectedItems.Add(item);
            item.IsSelected = true;

            if (_selectedItems.Count == 1) item.IsFirstSelected = true;
        }

        public void MakeFirstSelected(T item)
        {
            if (item.IsFirstSelected) return;

            _selectedItems.Remove(item);
            if (_selectedItems.Count > 0) _selectedItems[0].IsFirstSelected = false;
            _selectedItems.Insert(0, item);
            item.IsFirstSelected = true;
        }

        public void RemoveFromSelection(T item)
        {
            item.IsSelected = false;
            item.IsFirstSelected = false;
            _selectedItems.Remove(item);
            if (_selectedItems.Count > 0) _selectedItems[0].IsFirstSelected = true;
        }

        public void ClearSelection()
        {
            _selectedItems.ForEach(item =>
            {
                item.IsSelected = false;
                item.IsFirstSelected = false;
            });
            _selectedItems.Clear();
        }

        public void Clear()
        {
            _allItems.Clear();
            _selectedItems.Clear();
        }

        public void Attach(T item)
        {
            if (item is null) return;
            _allItems.Add(item);
        }

        public void Detach(T item)
        {
            _selectedItems.Remove(item);
            _allItems.Remove(item);
        }

        /// <param name="keyModifiers">
        ///     The modifiers held during the event that caused this: Control adds to or removes from the
        ///     selection, Shift extends it to <paramref name="item" />, and neither replaces it.
        /// </param>
        public void UpdateSelection(T? item, KeyModifiers keyModifiers)
        {
            if (item is null)
            {
                if ((keyModifiers & (KeyModifiers.Shift | KeyModifiers.Control)) == KeyModifiers.None)
                    ClearSelection();
                return;
            }

            if ((keyModifiers & KeyModifiers.Control) != KeyModifiers.None)
            {
                if (!item.IsSelected)
                {
                    AddToSelection(item);
                    MakeFirstSelected(item);
                }
                else
                {
                    RemoveFromSelection(item);
                }
            }
            else if ((keyModifiers & KeyModifiers.Shift) != KeyModifiers.None)
            {
                var firstSelectedItem = FirstSelectedItem;
                if (!item.IsFirstSelected && firstSelectedItem is not null)
                {
                    ClearSelection();
                    T[] orderedItems = _allItems.ToArray();
                    var firstIndex = -1;
                    var secondIndex = -1;
                    for (var i = 0; i < orderedItems.Length; i += 1)
                    {
                        var it = orderedItems[i];
                        if (ReferenceEquals(it, firstSelectedItem)) firstIndex = i;
                        if (ReferenceEquals(it, item)) secondIndex = i;
                    }

                    if (firstIndex >= 0 && secondIndex >= 0)
                    {
                        ClearSelection();
                        for (var i = firstIndex;;)
                        {
                            AddToSelection(orderedItems[i]);
                            if (secondIndex > firstIndex)
                            {
                                i += 1;
                                if (i > secondIndex) break;
                            }
                            else
                            {
                                i--;
                                if (i < secondIndex) break;
                            }
                        }
                    }
                }
                else
                {
                    SelectOne(item);
                }
            }
            else
            {
                if (!item.IsSelected)
                    SelectOne(item);
                else
                    MakeFirstSelected(item);
            }
        }

        #endregion

        #region protected functions

        protected bool Disposed { get; private set; }

        #endregion

        #region private fields

        private List<T> _allItems = new();
        private readonly List<T> _selectedItems = new();

        #endregion
    }
}
