namespace Evoq.Blockchain.Merkle;

using System;
using System.Runtime.Serialization;

/// <summary>
/// Exception thrown when the root hash of a Merkle tree does not match the computed hash from leaves.
/// </summary>
[Serializable]
public class InvalidRootException : Exception
{
    /// <summary>
    /// Initializes a new instance of the InvalidRootException class.
    /// </summary>
    public InvalidRootException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidRootException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public InvalidRootException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidRootException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public InvalidRootException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidRootException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected InvalidRootException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}

/// <summary>
/// Exception thrown when the hash of a leaf does not match the computed hash.
/// </summary>
[Serializable]
public class InvalidLeafHashException : Exception
{
    /// <summary>
    /// Initializes a new instance of the InvalidLeafHashException class.
    /// </summary>
    public InvalidLeafHashException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidLeafHashException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public InvalidLeafHashException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidLeafHashException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public InvalidLeafHashException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the InvalidLeafHashException class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected InvalidLeafHashException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}