// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;

// using System.Reflection;

// Copied from https://github.com/aspnet/EntityFramework/blob/dev/src/Shared/Check.cs
namespace System.Linq.Dynamic.Core.Validation
{
    [DebuggerStepThrough]
    internal static class Check
    {
        public static T Condition<T>(T value, Predicate<T> condition, string parameterName)
        {
            NotNull(condition, nameof(condition));

            if (!condition(value))
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw new ArgumentOutOfRangeException(parameterName);
            }

            return value;
        }

        public static T NotNull<T>(T? value, string parameterName)
        {
            if (value is null)
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw new ArgumentNullException(parameterName);
            }

            return value;
        }
        
        public static T NotNull<T>(
            T? value,
            string parameterName,
            string propertyName)
        {
            if (ReferenceEquals(value, null))
            {
                NotEmpty(parameterName, nameof(parameterName));
                NotEmpty(propertyName, nameof(propertyName));

                throw new ArgumentException(CoreStrings.ArgumentPropertyNull(propertyName, parameterName));
            }

            return value;
        }

        public static string NotEmpty(string? value, string parameterName)
        {
            Exception? e = null;
            if (value is null)
            {
                e = new ArgumentNullException(parameterName);
            }
            else if (value.Trim().Length == 0)
            {
                e = new ArgumentException(CoreStrings.ArgumentIsEmpty(parameterName));
            }

            if (e != null)
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw e;
            }

            return value!;
        }

        public static IList<T> HasNoNulls<T>(IList<T>? value, string parameterName)
            where T : class
        {
            NotNull(value, parameterName);

            if (value!.Any(e => e == null))
            {
                NotEmpty(parameterName, nameof(parameterName));

                throw new ArgumentException(parameterName);
            }

            return value!;
        }
        
    }
}
