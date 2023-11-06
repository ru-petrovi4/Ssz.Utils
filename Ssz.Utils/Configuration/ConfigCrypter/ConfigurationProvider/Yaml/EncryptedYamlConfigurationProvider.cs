using Ssz.Utils.Yaml;

namespace Ssz.Utils.ConfigCrypter.ConfigurationProviders.Yaml
{
    /// <summary>
    ///  Yaml configuration provider that uses the underlying crypter to decrypt the given keys.
    /// </summary>
    public class EncryptedYamlConfigurationProvider : YamlConfigurationProvider
    {
        private readonly EncryptedYamlConfigurationSource _configurationSource;

        /// <summary>
        /// Creates an instance of the EncryptedYamlConfigProvider.
        /// </summary>
        /// <param name="configurationSource">EncryptedYamlConfigSource that is used to configure the provider.</param>
        public EncryptedYamlConfigurationProvider(EncryptedYamlConfigurationSource configurationSource) : base(configurationSource)
        {
            _configurationSource = configurationSource;
        }

        /// <summary>
        /// Loads the Yaml configuration file and decrypts all configured keys with the given crypter.
        /// </summary>
        public override void Load()
        {
            base.Load();

            using (var crypter = _configurationSource.CrypterFactory(_configurationSource))
            {
                foreach (var key in _configurationSource.KeysToDecrypt)
                {
                    if (Data.TryGetValue(key, out var encryptedValue))
                    {
                        try
                        {
                            Data[key] = crypter.DecryptString(encryptedValue);
                        }
                        catch
                        {
                        }
                    }
                }
            }
        }
    }
}