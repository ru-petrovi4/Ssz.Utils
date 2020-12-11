﻿using Ssz.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ssz.Xi.Client
{
    public interface IValueSubscription
    {
        /// <summary>        
        ///     When item don't exist in data source, invokes with new Any(DBNull.Value)
        ///     When disconnected from data source, invokes with new Any(null).
        /// </summary>
        void Update(Any value);

        /// <summary>
        ///     Property to use internally in ModelDataProvider. You need to
        ///     implement this field, but you shouldn't change its value.
        /// </summary>
        object? Obj { get; set; }
    }
}
