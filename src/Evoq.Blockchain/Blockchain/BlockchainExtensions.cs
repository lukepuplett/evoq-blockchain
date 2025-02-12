using System;

namespace Evoq.Blockchain;

public static class BlockchainExtensions
{
    /// <summary>
    /// Concatenates two byte arrays.
    /// </summary>
    /// <param name="first">The first byte array</param>
    /// <param name="second">The second byte array</param>
    /// <returns>A new byte array that is the concatenation of the two input arrays</returns>
    public static byte[] Concat(this byte[] first, byte[] second)
    {
        var result = new byte[first.Length + second.Length];
        Buffer.BlockCopy(first, 0, result, 0, first.Length);
        Buffer.BlockCopy(second, 0, result, first.Length, second.Length);
        return result;
    }

    /// <summary>
    /// Converts a byte array to a Hex string.
    /// </summary>
    /// <param name="bytes">The byte array to convert</param>
    /// <returns>A Hex string representation of the input byte array</returns>
    public static Hex ToHexStruct(this byte[] bytes)
    {
        return new Hex(bytes);
    }
}
