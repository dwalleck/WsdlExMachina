using System;

namespace WsdlExMachina.CSharpGenerator
{
    /// <summary>
    /// Represents an error that occurs during code generation.
    /// </summary>
    public class CodeGenerationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerationException"/> class.
        /// </summary>
        public CodeGenerationException() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerationException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public CodeGenerationException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerationException"/> class with a specified error message
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public CodeGenerationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
