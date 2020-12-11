using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Ssz.Utils.CommandLine.Attributes;
using Ssz.Utils.CommandLine.Infrastructure;


namespace Ssz.Utils.CommandLine.Parsing
{
    [DebuggerDisplay("ShortName = {ShortName}, LongName = {LongName}")]
    internal sealed class OptionInfo
    {
        #region construction and destruction

        public OptionInfo(BaseOptionAttribute attribute, PropertyInfo property, CultureInfo parsingCulture)
        {            
            _required = attribute.Required;
            _shortName = attribute.ShortName;
            _longName = attribute.LongName;
            _mutuallyExclusiveSet = attribute.MutuallyExclusiveSet;
            _defaultValue = attribute.DefaultValue;
            _hasDefaultValue = attribute.HasDefaultValue;
            _attribute = attribute;
            _property = property;
            _parsingCulture = parsingCulture;
            _propertyWriter = new PropertyWriter(_property, _parsingCulture);
        }

        #endregion

        #region public functions

        public object? GetValue(object target)
        {
            return _property.GetValue(target, null);
        }

        public object CreateInstance(object target)
        {            
            try
            {
                object? instance = Activator.CreateInstance(_property.PropertyType);

                if (instance == null) throw new ParserException(SR.CommandLineParserException_CannotCreateInstanceForVerbCommand);

                _property.SetValue(target, instance, null);

                return instance;
            }
            catch (Exception e)
            {
                throw new ParserException(SR.CommandLineParserException_CannotCreateInstanceForVerbCommand, e);
            }
        }

        public bool SetValue(string value, object options)
        {
            if (_attribute is OptionListAttribute)
            {
                return SetValueList(value, options);
            }

            if (ReflectionHelper.IsNullableType(_property.PropertyType))
            {
                return ReceivedValue = _propertyWriter.WriteNullable(value, options);
            }

            return ReceivedValue = _propertyWriter.WriteScalar(value, options);
        }

        public bool SetValue(IList<string> values, object options)
        {
            Type elementType = _property.PropertyType.GetElementType() ?? typeof (object);
            Array array = Array.CreateInstance(elementType, values.Count);

            for (int i = 0; i < array.Length; i++)
            {
                try
                {
                    array.SetValue(Convert.ChangeType(values[i], elementType, _parsingCulture), i);
                    _property.SetValue(options, array, null);
                }
                catch (FormatException)
                {
                    return false;
                }
            }

            return ReceivedValue = true;
        }

        public bool SetValue(bool value, object options)
        {
            _property.SetValue(options, value, null);
            return ReceivedValue = true;
        }

        public void SetDefault(object? options)
        {
            if (_hasDefaultValue)
            {
                try
                {
                    _property.SetValue(options, _defaultValue, null);
                }
                catch (Exception e)
                {
                    throw new ParserException("Bad default value.", e);
                }
            }
        }

        public char? ShortName
        {
            get { return _shortName; }
        }

        public string LongName
        {
            get { return _longName; }
        }

        public string MutuallyExclusiveSet
        {
            get { return _mutuallyExclusiveSet; }
        }

        public bool Required
        {
            get { return _required; }
        }

        public bool IsBoolean
        {
            get { return _property.PropertyType == typeof (bool); }
        }

        public bool IsArray
        {
            get { return _property.PropertyType.IsArray; }
        }

        public bool IsAttributeArrayCompatible
        {
            get { return _attribute is OptionArrayAttribute; }
        }

        public bool IsDefined { get; set; }

        public bool ReceivedValue { get; private set; }

        public bool HasBothNames
        {
            get { return _shortName != null && _longName != null; }
        }

        public bool HasParameterLessCtor { get; set; }

        #endregion

        #region private functions

        private bool SetValueList(string value, object options)
        {
            _property.SetValue(options, new List<string>(), null);
            var fieldRef = _property.GetValue(options, null) as IList<string>;
            if (fieldRef == null) throw new InvalidOperationException();
            string[] values = value.Split(((OptionListAttribute) _attribute).Separator);
            foreach (string item in values)
            {
                fieldRef.Add(item);
            }

            return ReceivedValue = true;
        }

        #endregion

        #region private fields

        private readonly CultureInfo _parsingCulture;
        private readonly BaseOptionAttribute _attribute;
        private readonly PropertyInfo _property;
        private readonly PropertyWriter _propertyWriter;
        private readonly bool _required;
        private readonly char? _shortName;
        private readonly string _longName;
        private readonly string _mutuallyExclusiveSet;
        private readonly object _defaultValue;
        private readonly bool _hasDefaultValue;

        #endregion
    }
}