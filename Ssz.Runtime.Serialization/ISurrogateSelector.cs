//#nullable enable
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Runtime.Serialization;
//using System.Text;
//using System.Threading.Tasks;

//namespace Ssz.Runtime.Serialization
//{
//    public interface ISurrogateSelector
//    {
//        //
//        // Summary:
//        //     Specifies the next System.Runtime.Serialization.ISurrogateSelector for surrogates
//        //     to examine if the current instance does not have a surrogate for the specified
//        //     type and assembly in the specified context.
//        //
//        // Parameters:
//        //   selector:
//        //     The next surrogate selector to examine.
//        //
//        // Exceptions:
//        //   T:System.Security.SecurityException:
//        //     The caller does not have the required permission.
//        void ChainSelector(ISurrogateSelector selector);
//        //
//        // Summary:
//        //     Returns the next surrogate selector in the chain.
//        //
//        // Returns:
//        //     The next surrogate selector in the chain or null.
//        //
//        // Exceptions:
//        //   T:System.Security.SecurityException:
//        //     The caller does not have the required permission.
//        ISurrogateSelector? GetNextSelector();
//        //
//        // Summary:
//        //     Finds the surrogate that represents the specified object's type, starting with
//        //     the specified surrogate selector for the specified serialization context.
//        //
//        // Parameters:
//        //   type:
//        //     The System.Type of object (class) that needs a surrogate.
//        //
//        //   context:
//        //     The source or destination context for the current serialization.
//        //
//        //   selector:
//        //     When this method returns, contains a System.Runtime.Serialization.ISurrogateSelector
//        //     that holds a reference to the surrogate selector where the appropriate surrogate
//        //     was found. This parameter is passed uninitialized.
//        //
//        // Returns:
//        //     The appropriate surrogate for the given type in the given context.
//        //
//        // Exceptions:
//        //   T:System.Security.SecurityException:
//        //     The caller does not have the required permission.
//        ISerializationSurrogate? GetSurrogate(Type type, StreamingContext context, out ISurrogateSelector selector);
//    }
//}
