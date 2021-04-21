using System;
using System.Diagnostics;

namespace Ssz.Utils.MonitoredUndo.Changes
{
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public class PropertyChange : Change
    {
        #region construction and destruction

        public PropertyChange(object instance, string propertyName, object? oldValue, object? newValue)
            : base(instance, new ChangeKey<object, string>(instance, propertyName))
        {
            _propertyName = propertyName;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     When consolidating events, we want to keep the original "Undo"
        ///     but use the most recent Redo. This will pull the Redo from the
        ///     specified Change and apply it to this instance.
        /// </summary>
        public override void MergeWith(Change latestChange)
        {
            var other = latestChange as PropertyChange;

            if (null != other)
                _newValue = other._newValue;
        }

        public string PropertyName
        {
            get { return _propertyName; }
        }

        public object? OldValue
        {
            get { return _oldValue; }
        }

        public object? NewValue
        {
            get { return _newValue; }
        }

        #endregion

        #region protected functions

        protected override void PerformUndo()
        {
            try
            {
                var p = Target.GetType().GetProperty(PropertyName);
                if (p == null) throw new InvalidOperationException();
                p.SetValue(Target, OldValue, null);
            }
            catch
            {
                //Logger.Verbose(e);
            }
        }

        protected override void PerformRedo()
        {
            try
            {
                var p = Target.GetType().GetProperty(PropertyName);
                if (p == null) throw new InvalidOperationException();
                p.SetValue(Target, NewValue, null);
            }
            catch
            {
                //Logger.Verbose(e);
            }
        }

        #endregion

        #region private functions

        private string DebuggerDisplay
        {
            get
            {
                return string.Format(
                    "PropertyChange(Property={0}, Target={{{1}}}, NewValue={{{2}}}, OldValue={{{3}}})",
                    PropertyName, Target, NewValue, OldValue);
            }
        }

        #endregion

        #region private fields

        private readonly string _propertyName;
        // both should be weak
        private readonly object? _oldValue;
        private object? _newValue;

        #endregion
    }
}