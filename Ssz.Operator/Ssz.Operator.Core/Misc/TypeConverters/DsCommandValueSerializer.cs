using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using Ssz.Operator.Core.Commands;
using Ssz.Operator.Core.Utils;

namespace Ssz.Operator.Core
{
    public class DsCommandValueSerializer : ValueSerializer
    {
        #region public functions

        public static readonly DsCommandValueSerializer Instance = new();

        public override bool CanConvertFromString(string value, IValueSerializerContext? context)
        {
            if (string.IsNullOrWhiteSpace(value)) return true;
            if (value.StartsWith("Command=") && value.Length > "Command=".Length) return true;
            return false;
        }

        public override object? ConvertFromString(string value, IValueSerializerContext? context)
        {
            var dsCommand = new DsCommand();

            if (string.IsNullOrWhiteSpace(value)) return dsCommand;

            var index = value.IndexOf('&');

            if (index == 0) return dsCommand;

            string? command;
            if (index > 0)
                command = value.Substring(0, index);
            else
                command = value;

            command = command.Split('=').LastOrDefault();
            if (command is null) return dsCommand;

            dsCommand.Command = command;

            string? commandParamsString = null;
            if (index > 0 && index < value.Length - 1) commandParamsString = value.Substring(index + 1);

            if (!string.IsNullOrWhiteSpace(commandParamsString) && dsCommand.DsCommandOptions is not null)
            {
                TypeConverter typeConverter = TypeDescriptor.GetConverter(dsCommand.DsCommandOptions.GetType());
                if (!typeConverter.CanConvertFrom(typeof(string))) return dsCommand;
                dsCommand.DsCommandOptions =
                    typeConverter.ConvertFrom(commandParamsString) as OwnedDataSerializableAndCloneable;
            }

            return dsCommand;
        }

        public override bool CanConvertToString(object value, IValueSerializerContext? context)
        {
            var dsCommand = value as DsCommand;
            if (dsCommand is null) return false;
            if (string.IsNullOrWhiteSpace(dsCommand.Command)) return true;
            if (dsCommand.DsCommandOptions is null) return true;
            if (!dsCommand.IsEnabledInfo.IsConst ||
                dsCommand.IsEnabledInfo.ConstValue == false) return false;

            ValueSerializer valueSerializer =
                GetSerializerFor(dsCommand.DsCommandOptions.GetType());
            if (valueSerializer is null) return false;
            return valueSerializer.CanConvertToString(dsCommand.DsCommandOptions, null);
        }

        public override string? ConvertToString(object value, IValueSerializerContext? context)
        {
            return ConvertToString(value);
        }

        public string? ConvertToString(object value)
        {
            var dsCommand = value as DsCommand;
            if (dsCommand is null) return null;
            if (string.IsNullOrWhiteSpace(dsCommand.Command)) return "";

            var sb = new StringBuilder();

            sb.Append("Command=");
            sb.Append(dsCommand.Command);
            if (dsCommand.DsCommandOptions is not null)
            {
                ValueSerializer valueSerializer =
                    GetSerializerFor(dsCommand.DsCommandOptions.GetType());
                if (valueSerializer is null) return "";
                string serializedDsCommandOptions = valueSerializer.ConvertToString(dsCommand.DsCommandOptions, null);
                if (!string.IsNullOrWhiteSpace(serializedDsCommandOptions))
                {
                    sb.Append("&");
                    sb.Append(serializedDsCommandOptions);
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}