using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Ssz.Utils.CommandLine.Attributes;
using Ssz.Utils.CommandLine.Infrastructure;


namespace Ssz.Utils.CommandLine.Parsing
{
    /// <summary>
    ///     Maps unnamed options to property using <see cref="ValueOptionAttribute" /> and <see cref="ValueListAttribute" />.
    /// </summary>
    internal sealed class ValueMapper
    {
        #region construction and destruction

        public ValueMapper(object target, CultureInfo parsingCulture)
        {
            _target = target;
            _parsingCulture = parsingCulture;
            InitializeValueList();
            InitializeValueOption();
        }

        #endregion

        #region public functions

        public bool MapValueItem(string item)
        {
            if (IsValueOptionDefined &&
                _valueOptionIndex < _valueOptionAttributeList.Count)
            {
                Pair<PropertyInfo, ValueOptionAttribute> valueOption = _valueOptionAttributeList[_valueOptionIndex++];

                var propertyWriter = new PropertyWriter(valueOption.Left, _parsingCulture);

                return ReflectionHelper.IsNullableType(propertyWriter.Property.PropertyType)
                    ? propertyWriter.WriteNullable(item, _target)
                    : propertyWriter.WriteScalar(item, _target);
            }

            return IsValueListDefined && AddValueItem(item);
        }

        public bool CanReceiveValues
        {
            get { return IsValueListDefined || IsValueOptionDefined; }
        }

        #endregion

        #region private functions

        private bool AddValueItem(string item)
        {
            if (_valueListAttribute == null) throw new InvalidOperationException();

            if (_valueListAttribute.MaximumElements == 0 ||
                _valueList.Count == _valueListAttribute.MaximumElements)
            {
                return false;
            }

            _valueList.Add(item);
            return true;
        }

        private void InitializeValueList()
        {
            _valueListAttribute = ValueListAttribute.GetAttribute(_target);
            if (IsValueListDefined)
            {
                var valueList = ValueListAttribute.GetReference(_target);
                if (valueList != null)
                {
                    _valueList = valueList;
                }
            }
        }

        private void InitializeValueOption()
        {
            IList<Pair<PropertyInfo, ValueOptionAttribute>> list =
                ReflectionHelper.RetrievePropertyList<ValueOptionAttribute>(_target);

            // default is index 0, so skip sorting if all have it
            _valueOptionAttributeList = list.All(x => x.Right.Index == 0)
                ? list
                : list.OrderBy(x => x.Right.Index).ToList();
        }

        private bool IsValueListDefined
        {
            get { return _valueListAttribute != null; }
        }

        private bool IsValueOptionDefined
        {
            get { return _valueOptionAttributeList.Count > 0; }
        }

        #endregion

        #region private fields

        private readonly CultureInfo _parsingCulture;
        private readonly object _target;
        private IList<string> _valueList = new List<string>();
        private ValueListAttribute? _valueListAttribute;
        private IList<Pair<PropertyInfo, ValueOptionAttribute>> _valueOptionAttributeList = new List<Pair<PropertyInfo, ValueOptionAttribute>>();
        private int _valueOptionIndex;

        #endregion
    }
}