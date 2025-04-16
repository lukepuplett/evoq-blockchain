namespace Evoq.Blockchain.Merkle;

using System;

/// <summary>
/// Represents an exception that occurs during JSON processing.
/// </summary>
public class MalformedJsonException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MalformedJsonException"/> class.
    /// </summary>
    public MalformedJsonException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MalformedJsonException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public MalformedJsonException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MalformedJsonException"/> class with a specified error message 
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MalformedJsonException(string message, Exception innerException) : base(message, innerException) { }
}