using System;
namespace QuickGraph
{
    /// <summary>
    /// Exception raised when an algorithm detects a non-strongly connected graph.
    /// </summary>
#if !SILVERLIGHT
    [Serializable]
#endif
    public class NonStronglyConnectedGraphException 
        : QuickGraphException
    {
        public NonStronglyConnectedGraphException() { }
        public NonStronglyConnectedGraphException(string message) : base( message ) { }
        public NonStronglyConnectedGraphException(string message, System.Exception inner) : base( message, inner ) { }
#if !SILVERLIGHT
        protected NonStronglyConnectedGraphException(
          System.Play.Serialization.SerializationInfo info,
          System.Play.Serialization.StreamingContext context) : base( info, context ) { }
#endif
    }
}
