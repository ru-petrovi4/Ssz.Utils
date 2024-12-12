/////////////////////////////////////////////////////////////////////////////
//
//                              COPYRIGHT (c) 2021
//                                    SIMCODE.
//                              ALL RIGHTS RESERVED
//
//  This software is a copyrighted work and/or information protected as a
//  trade secret. Legal rights of Simcode. in this software is distinct
//  from ownership of any medium in which the software is embodied. Copyright
//  or trade secret notices included must be reproduced in any copies
//  authorised by Simcode.
//
/////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Ssz.Operator.Core.Constants;
using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;

namespace Ssz.Operator.Core
{
    /// <summary>
    ///     When adding values, check DataBindingItemViewModel.IsEmpty()
    /// </summary>
    public enum DataSourceType
    {
        OpcVariable = 0,
        Constant = 1,
        ParamType = 2,
        AlarmUnacked = 4,
        AlarmCategory = 5,
        PageExists = 6,
        CsvDbFileExists = 7,
        GlobalVariable = 8,
        WindowVariable = 9,
        RootWindowNum = 10,
        AlarmBrush = 11,
        AlarmCondition = 12,
        Random = 13,
        CurrentTimeSeconds = 14,
        TagNameToDisplay = 15,
        TagDescription = 16,
        PageNameOfRootWindowWithNum = 17,
        PageDescOfRootWindowWithNum = 18,
        PageGroupOfRootWindowWithNum = 19,
        AlarmsCount = 20,
        StatusCode = 21,
        BuzzerState = 22,
        BuzzerIsEnabled = 23,
        Passthrough = 24,
    }

    public class DataBindingItem : OwnedDataSerializableAndCloneable,
        IDsItem
    {
        #region construction and destruction

        public DataBindingItem()
        {
        }

        public DataBindingItem(string idString, DataSourceType type, string defaultValue = "")
        {
            Type = type;
            IdString = idString;
            DefaultValue = defaultValue;
        }

        public DataBindingItem(DataBindingItem dataBindingItem)
        {
            _type = dataBindingItem._type;
            _idString = dataBindingItem._idString;
            DefaultValue = dataBindingItem.DefaultValue;
        }

        #endregion

        #region public functions

        public const string DataSourceStringSeparator = "|||";

        public DataSourceType Type
        {
            get => _type;
            set
            {
                if (value == DataSourceType.GlobalVariable || value == DataSourceType.WindowVariable)
                {
                    _idString = _idString.Trim();
                    if ((_idString.StartsWith(@"%(") || _idString.StartsWith(@"$(")) && _idString.EndsWith(@")"))
                        _idString = _idString.Substring(2, _idString.Length - 3);
                    // Generic params are NOT global or window variables.
                    _idString = "$(" + _idString.Trim() + ")";
                }

                _type = value;
            }
        }

        public string IdString
        {
            get => _idString;
            set
            {
                if (value is null)
                {
                    value = @"";
                }
                else if (_type == DataSourceType.GlobalVariable || _type == DataSourceType.WindowVariable)
                {
                    value = value.Trim();
                    if ((value.StartsWith(@"%(") || value.StartsWith(@"$(")) && value.EndsWith(@")"))
                        value = value.Substring(2, value.Length - 3);
                    // Generic params are NOT global or window variables.
                    value = "$(" + value.Trim() + ")";
                }

                _idString = value;
            }
        }

        [DefaultValue(@"")] // For XAML serialization
        public string DefaultValue { get; set; } = @"";

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        [field: Searchable(false)]
        public IDsItem? ParentItem { get; set; }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization        
        public bool RefreshForPropertyGridIsDisabled { get; set; }

        public void RefreshForPropertyGrid()
        {
            RefreshForPropertyGrid(ParentItem.Find<IDsContainer>());
        }

        public void EndEditInPropertyGrid()
        {
        }

        public override void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(5))
            {
                writer.Write((int) _type);
                writer.Write(_idString);
                writer.Write(DefaultValue);
            }
        }

        public override void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {
                    case 5:
                        _type = (DataSourceType) reader.ReadInt32();
                        _idString = reader.ReadString();
                        DefaultValue = reader.ReadString();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(IdString, constants);
            ConstantsHelper.FindConstants(DefaultValue, constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            IdString = DataItemHelper.ComputeDataSourceIdString(Type, IdString, container) ?? @"";
            DefaultValue = ConstantsHelper.ComputeValue(container, DefaultValue) ?? @"";
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        public override string ToString()
        {
            return Type + ":" + IdString;
        }

        public bool Equals(DataBindingItem other)
        {
            return other._type == _type && other._idString == _idString && other.DefaultValue == DefaultValue;
        }

        #endregion

        #region private fields

        private DataSourceType _type = DataSourceType.OpcVariable;

        private string _idString = @"";

        #endregion
    }
}