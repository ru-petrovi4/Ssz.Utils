using System;
using System.Collections.Generic;
using System.Reflection;
using Ssz.Utils.CommandLine.Infrastructure;

namespace Ssz.Utils.CommandLine.Attributes
{
    /// <summary>
    ///     Models a list of command line arguments that are not options.
    ///     Must be applied to a field compatible with an <see cref="System.Collections.Generic.IList&lt;T&gt;" /> interface
    ///     of <see cref="System.String" /> instances.
    /// </summary>
    /// <remarks>To map individual values use instead <see cref="ValueOptionAttribute" />.</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ValueListAttribute : Attribute
    {
        #region construction and destruction

        /// <summary>
        ///     Initializes a new instance of the <see cref="ValueListAttribute" /> class.
        /// </summary>
        /// <param name="concreteType">A type that implements <see cref="System.Collections.Generic.IList&lt;T&gt;" />.</param>
        /// <exception cref="System.ArgumentNullException">Thrown if <paramref name="concreteType" /> is null.</exception>
        public ValueListAttribute(Type concreteType)            
        {
            if (concreteType == null)
            {
                throw new ArgumentNullException("concreteType");
            }

            if (!typeof (IList<string>).IsAssignableFrom(concreteType))
            {
                throw new ParserException(SR.CommandLineParserException_IncompatibleTypes);
            }

            _concreteType = concreteType;

            MaximumElements = -1;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     Gets or sets the maximum element allow for the list managed by <see cref="ValueListAttribute" /> type.
        ///     If lesser than 0, no upper bound is fixed.
        ///     If equal to 0, no elements are allowed.
        /// </summary>
        public int MaximumElements { get; set; }

        /// <summary>
        ///     Gets the concrete type specified during initialization.
        /// </summary>
        public Type ConcreteType
        {
            get { return _concreteType; }
        }

        #endregion

        #region internal functions

        internal static IList<string>? GetReference(object target)
        {
            Type? concreteType;
            PropertyInfo? property = GetProperty(target, out concreteType);
            if (property == null || concreteType == null)
            {
                return null;
            }

            property.SetValue(target, Activator.CreateInstance(concreteType), null);

            return property.GetValue(target, null) as IList<string>;
        }

        internal static ValueListAttribute? GetAttribute(object target)
        {
            IList<Pair<PropertyInfo, ValueListAttribute>> list =
                ReflectionHelper.RetrievePropertyList<ValueListAttribute>(target);
            if (list == null || list.Count == 0)
            {
                return null;
            }

            if (list.Count > 1)
            {
                throw new InvalidOperationException();
            }

            Pair<PropertyInfo, ValueListAttribute> pairZero = list[0];
            return pairZero.Right;
        }

        #endregion

        #region private functions

        private static PropertyInfo? GetProperty(object target, out Type? concreteType)
        {
            concreteType = null;
            IList<Pair<PropertyInfo, ValueListAttribute>> list =
                ReflectionHelper.RetrievePropertyList<ValueListAttribute>(target);
            if (list == null || list.Count == 0)
            {
                return null;
            }

            if (list.Count > 1)
            {
                throw new InvalidOperationException();
            }

            Pair<PropertyInfo, ValueListAttribute> pairZero = list[0];
            concreteType = pairZero.Right.ConcreteType;
            return pairZero.Left;
        }

        #endregion

        #region private fields

        private readonly Type _concreteType;

        #endregion
    }
}