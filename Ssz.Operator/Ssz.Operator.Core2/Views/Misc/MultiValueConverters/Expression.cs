/////////////////////////////////////////////////////////////////////////////
//
//                              COPYRIGHT (c) 2021
//                                    SSZ.
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
using System.Linq;
using System.Linq.Expressions;
using Ssz.Operator.Core.Constants;

using Ssz.Operator.Core.Utils;
using Ssz.Operator.Core.Utils.Serialization;
using Ssz.Utils;
//using Ssz.Utils.Wpf;

namespace Ssz.Operator.Core.MultiValueConverters
{
    [TypeConverter(typeof(ExpressionTypeConverter))]
    public class Expression : SszExpression, IOwnedDataSerializable,
        IDsItem, IDisposable
    {
        #region construction and destruction

        public Expression()
        {            
        }

        public Expression(string expressionString) :
            base(expressionString)
        {
        }

        public Expression(Expression that) :
            base(that)
        {
            _isUiToDataSourceWarning = that._isUiToDataSourceWarning;
        }

        /// <summary>
        ///     This is the implementation of the IDisposable.Dispose method.  The client
        ///     application should invoke this method when this instance is no longer needed.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void Dispose(bool disposing)
        {
            if (Disposed) return;

            if (disposing)
            {
                ParentItem = null;                
            }

            Disposed = true;
        }

        /// <summary>
        ///     Invoked by the .NET Framework while doing heap managment (Finalize).
        /// </summary>
        ~Expression()
        {
            Dispose(false);
        }

        /// <summary>
        /// 
        /// </summary>
        [Browsable(false)]
        public bool Disposed { get; private set; }

        #endregion

        #region public functions

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

        public object? Evaluate(object?[]? dataSourceValues, object? userValue)
        {
            return base.Evaluate(dataSourceValues, userValue, DsProject.LoggersSet);
        }

        public override bool Equals(object? obj)
        {
            var other = obj as Expression;
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other);
        }

        public override int GetHashCode()
        {
            return 0;
        }

        public void SerializeOwnedData(SerializationWriter writer, object? context)
        {
            using (writer.EnterBlock(3))
            {
                writer.Write(ExpressionString);
                writer.Write(IsValidInternal);
            }
        }

        public void DeserializeOwnedData(SerializationReader reader, object? context)
        {
            using (Block block = reader.EnterBlock())
            {
                switch (block.Version)
                {                    
                    case 3:
                        ExpressionString = reader.ReadString();
                        IsValidInternal = reader.ReadBoolean();
                        break;
                    default:
                        throw new BlockUnsupportedVersionException();
                }
            }
        }

        public void FindConstants(HashSet<string> constants)
        {
            ConstantsHelper.FindConstants(ExpressionString, constants);
        }

        public void ReplaceConstants(IDsContainer? container)
        {
            ExpressionString = ConstantsHelper.ComputeValue(container, ExpressionString) ?? @"";
        }

        public void RefreshForPropertyGrid(IDsContainer? container)
        {
        }

        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public override bool IsValid => base.IsValid;


        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // For XAML serialization
        public bool IsUiToDataSourceWarning
        {
            get => _isUiToDataSourceWarning;
            private set => SetValue(ref _isUiToDataSourceWarning, value);
        }

        public override string ExpressionString
        {
            get => base.ExpressionString;
            set
            {
                if (!String.IsNullOrEmpty(value))
                    IsUiToDataSourceWarning = value.Contains('[') && value.Contains(']');
                else
                    IsUiToDataSourceWarning = false;

                base.ExpressionString = value;
            }
        }

        #endregion

        #region private fields

        private bool _isUiToDataSourceWarning;

        #endregion        
    }
}