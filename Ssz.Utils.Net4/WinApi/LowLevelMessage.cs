using System;

namespace Ssz.Utils.WinApi
{
    /// <summary>
    ///     A message that has been intercepted by a low-level hook
    /// </summary>
    public abstract class LowLevelMessage
    {
        #region construction and destruction

        internal LowLevelMessage(int msg, int flags, int time, IntPtr dwExtraInfo)
        {
            _msg = msg;
            _flags = flags;
            Time = time;
            _extraInfo = dwExtraInfo;
        }

        #endregion

        #region public functions

        /// <summary>
        ///     The time this message happened.
        /// </summary>
        public int Time { get; set; }

        /// <summary>
        ///     Flags of the message. Its contents depend on the message.
        /// </summary>
        public int Flags
        {
            get { return _flags; }
        }

        /// <summary>
        ///     The message identifier.
        /// </summary>
        public int Message
        {
            get { return _msg; }
        }

        /// <summary>
        ///     Extra information. Its contents depend on the message.
        /// </summary>
        public IntPtr ExtraInfo
        {
            get { return _extraInfo; }
        }

        /// <summary>
        ///     Replays this event as if the user did it again.
        /// </summary>
        public abstract void ReplayEvent();

        #endregion

        #region private fields

        private readonly int _flags;
        private readonly int _msg;
        private readonly IntPtr _extraInfo;

        #endregion
    }
}