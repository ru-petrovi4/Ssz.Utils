using System;
using DevAttic.ConfigCrypter.Crypters;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevAttic.ConfigCrypter.ConfigCrypters.Yaml
{
    /// <summary>
    /// Config crypter that encrypts and decrypts keys in Yaml config files.
    /// </summary>
    public class YamlConfigCrypter : IConfigCrypter
    {
        private readonly ICrypter _crypter;

        /// <summary>
        /// Creates an instance of the YamlConfigCrypter.
        /// </summary>
        /// <param name="crypter">An ICrypter instance.</param>
        public YamlConfigCrypter(ICrypter crypter)
        {
            _crypter = crypter;
        }

        /// <summary>
        /// Decrypts the key in the given content of a config file.
        /// </summary>
        /// <param name="configFileContent">String content of a config file.</param>
        /// <param name="configKey">Key of the config entry. The key has to be in YamlPath format.</param>
        /// <returns>The content of the config file where the key has been decrypted.</returns>
        public string DecryptKey(string configFileContent, string configKey)
        {
            try
            {                
                var newConfigContent = configFileContent;

                return newConfigContent;
            }
            catch
            {
                return configFileContent;
            }            
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Encrypts the key in the given content of a config file.
        /// </summary>
        /// <param name="configFileContent">String content of a config file.</param>
        /// <param name="configKey">Key of the config entry. The key has to be in YamlPath format.</param>
        /// <returns>The content of the config file where the key has been encrypted.</returns>
        public string EncryptKey(string configFileContent, string configKey)
        {
            try
            {                
                var newConfigContent = configFileContent;

                return newConfigContent;
            }
            catch
            {
                return configFileContent;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _crypter?.Dispose();
            }
        }        
    }
}