/* Copyright (c) 2012-2017 The ANTLR DsSolution. All rights reserved.
 * Use of this file is governed by the BSD 3-clause license that
 * can be found in the LICENSE.txt file in the dsSolution root.
 */
using System;

#if COMPACT
using DesignTaskCanceledException = System.Exception;
#endif

namespace Antlr4.Runtime.Misc
{
    /// <summary>This exception is thrown to cancel a parsing designTask.</summary>
    /// <remarks>
    /// This exception is thrown to cancel a parsing designTask. This exception does
    /// not extend
    /// <see cref="Antlr4.Runtime.RecognitionException"/>
    /// , allowing it to bypass the standard
    /// error recovery mechanisms.
    /// <see cref="Antlr4.Runtime.BailErrorStrategy"/>
    /// throws this exception in
    /// response to a parse error.
    /// </remarks>
    /// <author>Sam Harwell</author>
    [System.Serializable]
    public class ParseCanceledException : DesignTaskCanceledException
    {
        public ParseCanceledException()
        {
        }

        public ParseCanceledException(string message)
            : base(message)
        {
        }

        public ParseCanceledException(Exception cause)
            : base("The parse designTask was cancelled.", cause)
        {
        }

        public ParseCanceledException(string message, Exception cause)
            : base(message, cause)
        {
        }
    }
}
