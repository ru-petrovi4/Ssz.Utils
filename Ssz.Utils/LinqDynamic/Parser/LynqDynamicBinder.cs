﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq.Dynamic.Core.Parser
{
    public class LynqDynamicBinder : Binder
    {
        public static readonly LynqDynamicBinder Instance = new();

        public LynqDynamicBinder() : base()
        {
        }
        private class BinderState
        {
            public object?[] args = null!;
        }
        public override FieldInfo BindToField(
            BindingFlags bindingAttr,
            FieldInfo[] match,
            object value,
            CultureInfo? culture
            )
        {
            //if (match is null) throw new ArgumentNullException("match");
            // Get a field for which the value parameter can be converted to the specified field type.
            for (int i = 0; i < match.Length; i++)
                if (ChangeTypeInternal(value, match[i].FieldType) is not null)
                    return match[i];
            throw new MissingFieldException();
        }
        public override MethodBase BindToMethod(
            BindingFlags bindingAttr,
            MethodBase[] match,
            ref object?[] args,
            ParameterModifier[]? modifiers,
            CultureInfo? culture,
            string[]? names,
            out object state
            )
        {
            // Store the arguments to the method in a state object.
            BinderState myBinderState = new BinderState();
            object[] arguments = new Object[args.Length];
            args.CopyTo(arguments, 0);
            myBinderState.args = arguments;
            state = myBinderState;
            //if (match is null) throw new ArgumentNullException();
            // Find a method that has the same parameters as those of the args parameter.
            for (int i = 0; i < match.Length; i++)
            {
                // Count the number of parameters that match.
                int count = 0;
                ParameterInfo[] parameters = match[i].GetParameters();
                // Go on to the next method if the number of parameters do not match.
                if (args.Length != parameters.Length)
                    continue;
                // Match each of the parameters that the user expects the method to have.
                for (int j = 0; j < args.Length; j++)
                {
                    // If the names parameter is not null, then reorder args.
                    if (names is not null)
                    {
                        if (names.Length != args.Length)
                            throw new ArgumentException("names and args must have the same number of elements.");
                        for (int k = 0; k < names.Length; k++)
                            if (String.Compare(parameters[j].Name, names[k].ToString()) == 0)
                                args[j] = myBinderState.args[k];
                    }
                    // Determine whether the types specified by the user can be converted to the parameter type.
                    if (ChangeTypeInternal(args[j], parameters[j].ParameterType) is not null)
                        count += 1;
                    else
                        break;
                }
                // Determine whether the method has been found.
                if (count == args.Length)
                    return match[i];
            }
            throw new MissingFieldException();
        }
        public override object ChangeType(
            object value,
            Type myChangeType,
            CultureInfo? culture
            )
        {
            // Determine whether the value parameter can be converted to a value of type myType.
            if (CanConvertFrom(value.GetType(), myChangeType))
                // Return the converted object.
                return Convert.ChangeType(value, myChangeType);
            else
                // Return null.
                return value;
        }
        public override void ReorderArgumentArray(
            ref object?[] args,
            object state
            )
        {
            // Return the args that had been reordered by BindToMethod.
            ((BinderState)state).args.CopyTo(args, 0);
        }
        public override MethodBase? SelectMethod(
            BindingFlags bindingAttr,
            MethodBase[] match,
            Type[] types,
            ParameterModifier[]? modifiers
            )
        {
            //if (match is null) throw new ArgumentNullException("match");
            for (int i = 0; i < match.Length; i++)
            {
                // Count the number of parameters that match.
                int count = 0;
                ParameterInfo[] parameters = match[i].GetParameters();
                // Go on to the next method if the number of parameters do not match.
                if (types.Length != parameters.Length)
                    continue;
                // Match each of the parameters that the user expects the method to have.
                for (int j = 0; j < types.Length; j++)
                    // Determine whether the types specified by the user can be converted to parameter type.
                    if (CanConvertFrom(types[j], parameters[j].ParameterType))
                        count += 1;
                    else
                        break;
                // Determine whether the method has been found.
                if (count == types.Length)
                    return match[i];
            }
            return null;
        }
        public override PropertyInfo? SelectProperty(
            BindingFlags bindingAttr,
            PropertyInfo[] match,
            Type? returnType,
            Type[]? indexes,
            ParameterModifier[]? modifiers
            )
        {
            //if (match is null) throw new ArgumentNullException("match");
            int indexesLength = indexes is not null ? indexes.Length : 0;
            for (int i = 0; i < match.Length; i++)
            {
                // Count the number of indexes that match.
                int count = 0;
                ParameterInfo[] parameters = match[i].GetIndexParameters();
                // Go on to the next property if the number of indexes do not match.
                if (indexesLength != parameters.Length)
                    continue;
                // Match each of the indexes that the user expects the property to have.
                for (int j = 0; j < indexesLength; j++)
                    // Determine whether the types specified by the user can be converted to index type.
                    if (CanConvertFrom(indexes![j], parameters[j].ParameterType))
                        count += 1;
                    else
                        break;
                // Determine whether the property has been found.
                if (count == indexesLength)
                {
                    // Determine whether the return type can be converted to the properties type.
                    if (returnType is null || CanConvertFrom(returnType, match[i].PropertyType))
                        return match[i];
                    else
                        continue;
                }
            }
            return null;
        }

        public object? ChangeTypeInternal(
            object? value,
            Type myChangeType
            )
        {
            if (value is null) return null;
            // Determine whether the value parameter can be converted to a value of type myType.
            if (CanConvertFrom(value.GetType(), myChangeType))
                // Return the converted object.
                return Convert.ChangeType(value, myChangeType);
            else
                // Return null.
                return null;
        }

        // Determines whether type1 can be converted to type2. Check only for primitive types.
        private bool CanConvertFrom(Type type1, Type type2)
        {
            if (type1.IsPrimitive && type2.IsPrimitive)
            {
                TypeCode typeCode1 = Type.GetTypeCode(type1);
                TypeCode typeCode2 = Type.GetTypeCode(type2);
                // If both type1 and type2 have the same type, return true.
                if (typeCode1 == typeCode2)
                    return true;
                // Possible conversions from Char follow.
                if (typeCode1 == TypeCode.Char)
                    switch (typeCode2)
                    {
                        case TypeCode.UInt16: return true;
                        case TypeCode.UInt32: return true;
                        case TypeCode.Int32: return true;
                        case TypeCode.UInt64: return true;
                        case TypeCode.Int64: return true;
                        case TypeCode.Single: return true;
                        case TypeCode.Double: return true;
                        default: return false;
                    }
                // Possible conversions from Byte follow.
                if (typeCode1 == TypeCode.Byte)
                    switch (typeCode2)
                    {
                        case TypeCode.Char: return true;
                        case TypeCode.UInt16: return true;
                        case TypeCode.Int16: return true;
                        case TypeCode.UInt32: return true;
                        case TypeCode.Int32: return true;
                        case TypeCode.UInt64: return true;
                        case TypeCode.Int64: return true;
                        case TypeCode.Single: return true;
                        case TypeCode.Double: return true;
                        default: return false;
                    }
                // Possible conversions from SByte follow.
                if (typeCode1 == TypeCode.SByte)
                    switch (typeCode2)
                    {
                        case TypeCode.Int16: return true;
                        case TypeCode.Int32: return true;
                        case TypeCode.Int64: return true;
                        case TypeCode.Single: return true;
                        case TypeCode.Double: return true;
                        default: return false;
                    }
                // Possible conversions from UInt16 follow.
                if (typeCode1 == TypeCode.UInt16)
                    switch (typeCode2)
                    {
                        case TypeCode.UInt32: return true;
                        case TypeCode.Int32: return true;
                        case TypeCode.UInt64: return true;
                        case TypeCode.Int64: return true;
                        case TypeCode.Single: return true;
                        case TypeCode.Double: return true;
                        default: return false;
                    }
                // Possible conversions from Int16 follow.
                if (typeCode1 == TypeCode.Int16)
                    switch (typeCode2)
                    {
                        case TypeCode.Int32: return true;
                        case TypeCode.Int64: return true;
                        case TypeCode.Single: return true;
                        case TypeCode.Double: return true;
                        default: return false;
                    }
                // Possible conversions from UInt32 follow.
                if (typeCode1 == TypeCode.UInt32)
                    switch (typeCode2)
                    {
                        case TypeCode.UInt64: return true;
                        case TypeCode.Int64: return true;
                        case TypeCode.Single: return true;
                        case TypeCode.Double: return true;
                        default: return false;
                    }
                // Possible conversions from Int32 follow.
                if (typeCode1 == TypeCode.Int32)
                    switch (typeCode2)
                    {
                        case TypeCode.Int64: return true;
                        case TypeCode.Single: return true;
                        case TypeCode.Double: return true;
                        default: return false;
                    }
                // Possible conversions from UInt64 follow.
                if (typeCode1 == TypeCode.UInt64)
                    switch (typeCode2)
                    {
                        case TypeCode.Single: return true;
                        case TypeCode.Double: return true;
                        default: return false;
                    }
                // Possible conversions from Int64 follow.
                if (typeCode1 == TypeCode.Int64)
                    switch (typeCode2)
                    {
                        case TypeCode.Single: return true;
                        case TypeCode.Double: return true;
                        default: return false;
                    }
                // Possible conversions from Single follow.
                if (typeCode1 == TypeCode.Single)
                    switch (typeCode2)
                    {
                        case TypeCode.Double: return true;
                        default: return false;
                    }
            }
            return false;
        }
    }
}
