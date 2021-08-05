using System;
using System.Collections.Generic;
using System.Threading;

namespace Ssz.Utils
{
    /// <summary>
    ///     http://collaboration.cmc.ec.gc.ca/science/rpn/biblio/ddj/Website/articles/DDJ/2008/0801/071201hs01/071201hs01.html
    /// </summary>
    public sealed class LeveledLock
    {
        #region construction and destruction

        static LeveledLock()
        {
            LockLevelTlsSlot = Thread.AllocateNamedDataSlot("__current#lockLevel__");
        }

        public LeveledLock(int level) : this(level, true)
        {
        }

        public LeveledLock(int level, bool reentrant) : this(level, reentrant, null)
        {
        }

        public LeveledLock(int level, bool reentrant, string name)
        {
            _level = level;
            _reentrant = reentrant;
            _name = name;
        }

        #endregion

        #region public functions

        public IDisposable Enter()
        {
            return Enter(false);
        }

        public IDisposable Enter(int millisecondsTimeout)
        {
            return Enter(false, millisecondsTimeout);
        }

        public IDisposable Enter(bool permitIntraLevel, int millisecondsTimeout = Timeout.Infinite)
        {
#if DEBUG
            Thread.BeginThreadAffinity();
            Thread.BeginCriticalRegion();

            bool taken = false;
            try
            {
                PushLevel(permitIntraLevel);
                taken = Monitor.TryEnter(_lock, millisecondsTimeout);
                if (!taken)
                    throw new TimeoutException("Timeout occurred while attempting to acquire monitor");
            }
            finally
            {
                if (!taken)
                {
                    Thread.EndCriticalRegion();
                    Thread.EndThreadAffinity();
                }
            }

            return new LeveledLockCookie(this);
#else
            bool taken = Monitor.TryEnter(_lock, millisecondsTimeout);
            if (!taken)
                throw new TimeoutException("Timeout occurred while attempting to acquire monitor");

            return new LeveledLockCookie(this);
#endif
        }

        public void Exit()
        {
#if DEBUG
            Monitor.Exit(_lock);
            try
            {
                PopLevel();
            }
            finally
            {
                Thread.EndCriticalRegion();
                Thread.EndThreadAffinity();
            }
#else
            Monitor.Exit(_lock);
#endif
        }

        public override string ToString()
        {
            return string.Format("<level={0}, reentrant={1}, name={2}>", _level, _reentrant.ToString(), _name);
        }

        public int Level
        {
            get { return _level; }
        }

        public bool Reentrant
        {
            get { return _reentrant; }
        }

        public string Name
        {
            get { return _name; }
        }

        #endregion

        #region private functions

        private void PushLevel(bool permitIntraLevel)
        {
            Stack<LeveledLock> currentLevelStack;
            try
            {
                currentLevelStack = Thread.GetData(LockLevelTlsSlot) as Stack<LeveledLock>;
            }
            catch (Exception)
            {
                return;
            }

            if (currentLevelStack == null)
            {
                // We've never accessed the TLS data yet; construct a new Stack for our levels
                // and stash it away in TLS.
                currentLevelStack = new Stack<LeveledLock>();
                Thread.SetData(LockLevelTlsSlot, currentLevelStack);
            }
            else if (currentLevelStack.Count > 0)
            {
                // If the stack in TLS already recorded a lock, validate that we are not violating
                // the locking protocol. A violation occurs when our lock is higher level than the
                // current lock, or equal to the level (when the reentrant bit has not been set on
                // at least one of the locks involved).
                LeveledLock currentLock = currentLevelStack.Peek();
                int currentLevel = currentLock._level;

                if (_level > currentLevel ||
                    (ReferenceEquals(currentLock, this) && !_reentrant) ||
                    (!ReferenceEquals(currentLock, this) && _level == currentLevel && !permitIntraLevel))
                {
                    throw new LockLevelException(currentLock, this);
                }
            }

            // If we reached here, we are OK to proceed with locking. Stash the current level in TLS.
            currentLevelStack.Push(this);
        }

        private void PopLevel()
        {
            Stack<LeveledLock> currentLevelStack;
            try
            {
                currentLevelStack = Thread.GetData(LockLevelTlsSlot) as Stack<LeveledLock>;
            }
            catch (Exception)
            {
                return;
            }

            // Just pop the latest level placed into TLS.
            if (currentLevelStack != null)
            {
                if (currentLevelStack.Peek() != this)
                    throw new InvalidOperationException(
                        "You released a lock out of order. This is illegal with leveled locks.");
                currentLevelStack.Pop();
            }
        }

        #endregion

        #region private fields

        private static readonly LocalDataStoreSlot LockLevelTlsSlot;

        // Fields
        private readonly object _lock = new object();
        private readonly int _level;
        private readonly string _name;
        private readonly bool _reentrant;

        #endregion

        private class LeveledLockCookie : IDisposable
        {
            #region construction and destruction

            internal LeveledLockCookie(LeveledLock lck)
            {
                _lck = lck;
            }

            void IDisposable.Dispose()
            {
                _lck.Exit();
            }

            #endregion

            #region private fields

            private readonly LeveledLock _lck;

            #endregion
        }
    }

    public class LockLevelException : Exception
    {
        #region construction and destruction

        public LockLevelException()
        {
        }

        public LockLevelException(string m) : base(m)
        {
        }

        public LockLevelException(string m, Exception innerException) : base(m, innerException)
        {
        }

        public LockLevelException(LeveledLock currentLock, LeveledLock newLock) :
            base(string.Format("You attempted to violate the locking protocol by acquiring lock {0} " +
                               "while the thread already owns lock {1}.", newLock, currentLock))
        {
        }

        #endregion
    }
}