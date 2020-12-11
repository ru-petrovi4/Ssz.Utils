using System;


namespace Ssz.Utils.CommandLine.Attributes
{
    /// <summary>
    ///     Indicates that the property can receive an instance of type <see cref="CommandLine.IParserState" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class ParserStateAttribute : Attribute
    {
    }
}