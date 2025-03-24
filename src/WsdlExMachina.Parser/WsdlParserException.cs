using System;

namespace WsdlExMachina.Parser;

/// <summary>
/// Represents an error that occurs during WSDL parsing.
/// </summary>
public class WsdlParserException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WsdlParserException"/> class.
    /// </summary>
    public WsdlParserException() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WsdlParserException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WsdlParserException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WsdlParserException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WsdlParserException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
