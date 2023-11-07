using System;
using System.Diagnostics.CodeAnalysis;

namespace Ssz.Utils.ConfigurationCrypter.Crypters
{
    /// <summary>
    /// A crypter that is used to encrypt and decrypt simple strings.
    /// </summary>
    public interface ICrypter : IDisposable
    {
        /// <summary>
        /// Decrypts the given string.
        /// </summary>
        /// <param name="value">String to decrypt.</param>
        /// <returns>Encrypted string.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        string? DecryptString(string? value);

        /// <summary>
        /// Encrypts the given string.
        /// </summary>
        /// <param name="value">String to encrypt.</param>
        /// <returns>Encrypted string.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        string? EncryptString(string? value);
    }
}