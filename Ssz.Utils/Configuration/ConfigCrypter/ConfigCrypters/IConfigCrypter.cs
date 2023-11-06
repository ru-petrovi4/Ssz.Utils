using System;
using System.Collections.Generic;

namespace Ssz.Utils.ConfigCrypter.ConfigCrypters
{
    /// <summary>
    /// Encrypts/Decrypts keys in configuration files.
    /// </summary>
    public interface IConfigCrypter : IDisposable
    {
        /// <summary>
        /// Decrypts the key in the given content of a config file.
        /// </summary>
        /// <param name="configFileContent">String content of a config file.</param>
        /// <param name="configKeys">Keys of the config entry.</param>
        /// <returns>The content of the config file where the key has been decrypted.</returns>
        /// <remarks>It up to the implementer how to interpret the format of the config key.</remarks>
        string DecryptKeys(string configFileContent, IEnumerable<string> configKeys);

        /// <summary>
        /// Encrypts the key in the given content of a config file.
        /// </summary>
        /// <param name="configFileContent">String content of a config file.</param>
        /// <param name="configKeys">Keys of the config entry.</param>
        /// <returns>The content of the config file where the key has been encrypted.</returns>
        /// <remarks>It up to the implementer how to interpret the format of the config key.</remarks>
        string EncryptKeys(string configFileContent, IEnumerable<string> configKeys);
    }
}