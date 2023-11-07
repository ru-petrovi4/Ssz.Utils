using System;
using System.Collections.Generic;

namespace Ssz.Utils.ConfigurationCrypter.ConfigurationCrypters
{
    /// <summary>
    /// Encrypts/Decrypts keys in configuration files.
    /// </summary>
    public interface IConfigurationCrypter : IDisposable
    {
        /// <summary>
        /// Encrypts the key in the given content of a config file.
        /// </summary>
        /// <param name="fileFullName">Сonfig file full name.</param>
        /// <param name="configKeys">Keys of the config entry. The key has to be in YamlPath format.</param>
        /// <returns>The content of the config file where the key has been encrypted.</returns>
        void EncryptKeys(string fileFullName, HashSet<string> configKeys);

        /// <summary>
        /// Decrypts the key in the given content of a config file.
        /// </summary>
        /// <param name="fileFullName">Сonfig file full name.</param>
        /// <param name="configKeys">Keys of the config entry. The key has to be in YamlPath format.</param>
        /// <returns>The content of the config file where the key has been decrypted.</returns>
        void DecryptKeys(string fileFullName, HashSet<string> configKeys);        
    }
}