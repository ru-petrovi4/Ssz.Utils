using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Ssz.Operator.Core.CustomAttributes;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     Shows the properties of one object and lets the author change them, grouped by category.
    ///     <para>
    ///         The WPF editor used the property grid of the Xceed toolkit. Avalonia has no such control
    ///         and none is available here, so this one is written against the same attributes the shapes
    ///         are annotated with - DsCategory, DsDisplayName, LocalizedDescription, PropertyOrder and
    ///         Browsable - and keeps the members the editor called on the Xceed one.
    ///     </para>
    /// </summary>
    public partial class PropertyGrid : UserControl
    {
        #region construction and destruction

        public PropertyGrid()
        {
            InitializeComponent();
        }

        #endregion

        #region public functions

        public static readonly StyledProperty<object?> SelectedObjectProperty =
            AvaloniaProperty.Register<PropertyGrid, object?>(nameof(SelectedObject));

        public static readonly StyledProperty<string?> SelectedObjectTypeNameProperty =
            AvaloniaProperty.Register<PropertyGrid, string?>(nameof(SelectedObjectTypeName));

        public static readonly StyledProperty<string?> SelectedObjectNameProperty =
            AvaloniaProperty.Register<PropertyGrid, string?>(nameof(SelectedObjectName));

        public static readonly StyledProperty<bool> ShowTitleProperty =
            AvaloniaProperty.Register<PropertyGrid, bool>(nameof(ShowTitle), true);

        public object? SelectedObject
        {
            get => GetValue(SelectedObjectProperty);
            set => SetValue(SelectedObjectProperty, value);
        }

        public string? SelectedObjectTypeName
        {
            get => GetValue(SelectedObjectTypeNameProperty);
            set => SetValue(SelectedObjectTypeNameProperty, value);
        }

        public string? SelectedObjectName
        {
            get => GetValue(SelectedObjectNameProperty);
            set => SetValue(SelectedObjectNameProperty, value);
        }

        public bool ShowTitle
        {
            get => GetValue(ShowTitleProperty);
            set => SetValue(ShowTitleProperty, value);
        }

        /// <summary>
        ///     The categories shown, each with the rows that belong to it.
        /// </summary>
        public AvaloniaList<PropertyCategoryViewModel> Categories { get; } = new();

        /// <summary>
        ///     Takes what is half typed in the focused editor and puts it into the property, which is
        ///     what the editor calls before it saves or closes.
        ///     <para>
        ///         The text editors here write on every keystroke, so there is nothing pending; what is
        ///         left to do is to take the focus off the editor, which is also what ends editing in
        ///         the ones that commit on losing it.
        ///     </para>
        /// </summary>
        public void EndEditInPropertyGrid()
        {
            IInputElement? focused = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement();
            if (focused is Control control && IsWithin(control))
                Focus();
        }

        /// <summary>
        ///     Re-reads every value from the object, for the periodic refresh the editor does while a
        ///     properties window is open.
        /// </summary>
        public void RefreshValues()
        {
            foreach (PropertyCategoryViewModel category in Categories)
            foreach (PropertyItemViewModel item in category.Items)
                item.NotifyValueChanged();
        }

        #endregion

        #region protected functions

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SelectedObjectProperty)
                Rebuild();
        }

        #endregion

        #region private functions

        private bool IsWithin(Control control)
        {
            for (Control? c = control; c is not null; c = c.Parent as Control)
                if (ReferenceEquals(c, this))
                    return true;
            return false;
        }

        private void Rebuild()
        {
            Categories.Clear();

            object? selectedObject = SelectedObject;
            if (selectedObject is null) return;

            var categories = new Dictionary<string, PropertyCategoryViewModel>();

            foreach (PropertyDescriptor propertyDescriptor in
                     TypeDescriptor.GetProperties(selectedObject).OfType<PropertyDescriptor>())
            {
                if (!propertyDescriptor.IsBrowsable) continue;

                var item = new PropertyItemViewModel(selectedObject, propertyDescriptor);

                if (!categories.TryGetValue(item.Category, out PropertyCategoryViewModel? category))
                {
                    category = new PropertyCategoryViewModel(item.Category)
                    {
                        Order = GetCategoryOrder(selectedObject, item.Category)
                    };
                    categories[item.Category] = category;
                }

                category.Items.Add(item);
            }

            foreach (PropertyCategoryViewModel category in categories.Values
                         .OrderBy(c => c.Order).ThenBy(c => c.Header))
            {
                foreach (PropertyItemViewModel item in category.Items
                             .OrderBy(i => i.Order).ThenBy(i => i.DisplayName).ToArray())
                {
                    category.Items.Remove(item);
                    category.Items.Add(item);
                }

                Categories.Add(category);
            }
        }

        /// <summary>
        ///     Where a category sits among the others, when the type says so.
        /// </summary>
        private static int GetCategoryOrder(object selectedObject, string category)
        {
            foreach (DsCategoryOrderAttribute attribute in
                     TypeDescriptor.GetAttributes(selectedObject).OfType<DsCategoryOrderAttribute>())
                if (attribute.Category == category)
                    return attribute.Order;

            return Int32.MaxValue;
        }

        #endregion
    }

    public class PropertyCategoryViewModel
    {
        #region construction and destruction

        public PropertyCategoryViewModel(string header)
        {
            Header = header;
        }

        #endregion

        #region public functions

        public string Header { get; }

        public int Order { get; set; }

        public AvaloniaList<PropertyItemViewModel> Items { get; } = new();

        #endregion
    }
}
