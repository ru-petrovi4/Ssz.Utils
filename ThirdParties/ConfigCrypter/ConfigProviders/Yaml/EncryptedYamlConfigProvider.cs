using Ssz.Utils.Yaml;

namespace DevAttic.ConfigCrypter.ConfigProviders.Yaml
{
    /// <summary>
    ///  Yaml configuration provider that uses the underlying crypter to decrypt the given keys.
    /// </summary>
    public class EncryptedYamlConfigProvider : YamlConfigurationProvider
    {
        private readonly EncryptedYamlConfigSource _YamlConfigSource;

        /// <summary>
        /// Creates an instance of the EncryptedYamlConfigProvider.
        /// </summary>
        /// <param name="source">EncryptedYamlConfigSource that is used to configure the provider.</param>
        public EncryptedYamlConfigProvider(EncryptedYamlConfigSource source) : base(source)
        {
            _YamlConfigSource = source;
        }

        /// <summary>
        /// Loads the Yaml configuration file and decrypts all configured keys with the given crypter.
        /// </summary>
        public override void Load()
        {
            base.Load();

            using (var crypter = _YamlConfigSource.CrypterFactory(_YamlConfigSource))
            {
                foreach (var key in _YamlConfigSource.KeysToDecrypt)
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