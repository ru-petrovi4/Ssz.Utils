using System;
using System.ComponentModel;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core.Constants
{
    public class DsConstant : IOwnedDataSerializable
    {
        #region construction and destruction

        public DsConstant()
        {
        }

        public DsConstant(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public DsConstant(DsConstant dsConstant)
        {
            _name = dsConstant._name;
            _value = dsConstant._value;
            _defaultValue = dsConstant._defaultValue;
            _type = dsConstant._type;
            _desc = dsConstant._desc;
            IsDsProjectDsConstant = dsConstant.IsDsProjectDsConstant;
        }

        #endregion

        #region public functions

        [Searchable(false)]
        public string Name
        {
            get => _name;
            set
            {
                if (value is null) value = @"";
                _name = value;
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (value is null) value = @"";
                _value = value;
            }
        }

        [DefaultValue(@"")] // For XAML serialization
        public string DefaultValue
        {
            get => _defaultValue;
            set
            {
                if (value is null) value = @"";
                _defaultValue = value;
            }
        }

        [DefaultValue(@"")] // For XAML serialization
        public string Type
        {
            get => _type;
            set
            {
                if (value is null) value = @"";
                _type = value;
            }
        }

        [DefaultValue(@"")] // For XAML serialization
        public string Desc
        {
            get => _desc;
            set
            {
                if (value is null) value = @"";
                _desc = value;
            }
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public bool IsDsProjectDsConstant { get; set; }

        public bool Equals(DsConstant other)
        {
            return _name == other._name && _value == other._value &&
                   _defaultValue == other._defaultValue &&
                   _type == other._type && _desc == other._desc;
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(3))
            {
                writer.Write(_name);
                writer.Write(_value);
                writer.Write(_defaultValue);
                writer.Write(_type);
                writer.Write(_desc);
            }
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 3:
                        _name = reader.ReadString();
                        _value = reader.ReadString();
                        _defaultValue = reader.ReadString();
                        _type = reader.ReadString();
                        _desc = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        #endregion

        #region private fields

        private string _name = @"";

        private string _value = @"";

        private string _defaultValue = @"";

        private string _type = @"";

        private string _desc = @"";

        #endregion
    }
}