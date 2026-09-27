using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Avalonia.Controls;
using Ssz.Operator.Core.ViewModels;
using Ssz.Operator.Core.VisualEditors.PropertyGridTypeEditors;

namespace Ssz.Operator.Core.VisualEditors
{
    /// <summary>
    ///     One row of the property grid: what the property is called, what it holds now, and whether the
    ///     author may change it.
    ///     <para>
    ///         The value is read and written through the property itself, so everything the shapes
    ///         already do on assignment - validation, notification, redrawing - happens exactly as it
    ///         does from code.
    ///     </para>
    /// </summary>
    public class PropertyItemViewModel : ViewModelBase
    {
        #region construction and destruction

        public PropertyItemViewModel(object owner, PropertyDescriptor propertyDescriptor)
        {
            Owner = owner;
            PropertyDescriptor = propertyDescriptor;

            DisplayName = propertyDescriptor.DisplayName;
            Description = propertyDescriptor.Description;
            // A property with no category of its own joins the one the shapes call Advanced, which is
            // where the WPF grid also put what it could not place.
            Category = String.IsNullOrEmpty(propertyDescriptor.Category) ||
                       propertyDescriptor.Category == CategoryAttribute.Default.Category
                ? Properties.Resources.AdvancedCategory
                : propertyDescriptor.Category;

            var propertyOrderAttribute =
                propertyDescriptor.Attributes.OfType<PropertyOrderAttribute>().FirstOrDefault();
            Order = propertyOrderAttribute?.Order ?? Int32.MaxValue;

            IsReadOnly = propertyDescriptor.IsReadOnly ||
                         propertyDescriptor.Attributes.OfType<ReadOnlyInEditorAttribute>().Any();

            PropertyType = propertyDescriptor.PropertyType;

            Editor = ResolveEditor();
        }

        #endregion

        #region public functions

        public object Owner { get; }

        public PropertyDescriptor PropertyDescriptor { get; }

        public string DisplayName { get; }

        public string? Description { get; }

        public string Category { get; }

        public int Order { get; }

        public bool IsReadOnly { get; }

        public Type PropertyType { get; }

        public object? Value
        {
            get => GetValueSafe();
            set
            {
                if (IsReadOnly) return;

                try
                {
                    PropertyDescriptor.SetValue(Owner, value);
                }
                catch (Exception)
                {
                    // A value the property refuses is simply not taken; the grid shows what is there.
                }

                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(ValueAsString));
            }
        }

        /// <summary>
        ///     The value as the author types it. Anything that has a converter from text is edited as
        ///     text, which is what the WPF grid did for the great majority of the properties.
        /// </summary>
        public string ValueAsString
        {
            get
            {
                object? value = GetValueSafe();
                if (value is null) return @"";

                try
                {
                    return PropertyDescriptor.Converter.ConvertToString(null, CultureInfo.CurrentCulture, value)
                           ?? value.ToString() ?? @"";
                }
                catch (Exception)
                {
                    return value.ToString() ?? @"";
                }
            }
            set
            {
                if (IsReadOnly) return;

                try
                {
                    object? converted =
                        PropertyDescriptor.Converter.ConvertFromString(null, CultureInfo.CurrentCulture, value);
                    PropertyDescriptor.SetValue(Owner, converted);
                }
                catch (Exception)
                {
                    // Text that does not convert leaves the property as it was.
                }

                OnPropertyChanged(nameof(Value));
                OnPropertyChanged(nameof(ValueAsString));
            }
        }

        /// <summary>
        ///     The values a property of an enumeration type may take, for the drop-down.
        /// </summary>
        public IReadOnlyList<object>? EnumValues =>
            PropertyType.IsEnum ? Enum.GetValues(PropertyType).Cast<object>().ToArray() : null;

        public bool IsBoolean => PropertyType == typeof(bool) || PropertyType == typeof(bool?);

        public bool IsEnum => PropertyType.IsEnum;

        /// <summary>
        ///     Whether the row opens into the properties of the value itself, which is how the data
        ///     bindings and the brushes of a shape are edited.
        /// </summary>
        public bool IsExpandable
        {
            get
            {
                if (IsBoolean || IsEnum || PropertyType.IsPrimitive || PropertyType == typeof(string) ||
                    PropertyType == typeof(decimal) || PropertyType == typeof(DateTime))
                    return false;

                if (PropertyDescriptor.Attributes.OfType<ExpandableObjectAttribute>().Any())
                    return true;

                return PropertyType.GetCustomAttribute<ExpandableObjectAttribute>() is not null;
            }
        }

        /// <summary>
        ///     The control this property is edited with, when it asked for one of its own through an
        ///     Editor attribute; null leaves the grid to choose by the type of the property.
        /// </summary>
        public Control? Editor { get; }

        public bool HasEditor => Editor is not null;

        public bool HasNoEditor => Editor is null;

        public void NotifyValueChanged()
        {
            OnPropertyChanged(nameof(Value));
            OnPropertyChanged(nameof(ValueAsString));
        }

        #endregion

        #region private functions

        /// <summary>
        ///     Builds the editor the property named, the way the WPF property grid did: the attribute
        ///     carries the name of a type, and a type that is one of ours is asked for its control.
        /// </summary>
        private Control? ResolveEditor()
        {
            EditorAttribute? editorAttribute =
                PropertyDescriptor.Attributes.OfType<EditorAttribute>().FirstOrDefault();
            if (editorAttribute is null) return null;

            try
            {
                Type? editorType = Type.GetType(editorAttribute.EditorTypeName);
                if (editorType is null) return null;

                if (Activator.CreateInstance(editorType) is not ITypeEditor typeEditor) return null;

                return typeEditor.ResolveEditor(this);
            }
            catch (Exception)
            {
                // An editor that cannot be built leaves the property to the plain one.
                return null;
            }
        }

        private object? GetValueSafe()
        {
            try
            {
                return PropertyDescriptor.GetValue(Owner);
            }
            catch (Exception)
            {
                return null;
            }
        }

        #endregion
    }
}
