using System;
using System.Collections.Generic;
using Ssz.Utils.ConfigurationCrypter.CertificateLoaders;
using Ssz.Utils.ConfigurationCrypter.Crypters;
using Microsoft.Extensions.Configuration;
using Ssz.Utils.Yaml;

namespace Ssz.Utils.ConfigurationCrypter.ConfigurationProviders.Yaml
{
    /// <summary>
    /// ConfigurationSource for encrypted Yaml config files.
    /// </summary>
    public class EncryptedYamlConfigurationSource : YamlConfigurationSource
    {
        /// <summary>
        /// A certificate loader instance. Custom loaders can be used.
        /// </summary>
        public ICertificateLoader CertificateLoader { get; set; } = null!;
        /// <summary>
        /// The fully qualified path of the certificate.
        /// </summary>
        public string CertificatePath { get; set; } = null!;
        /// <summary>
        /// The subject name of the certificate (Issued for).
        /// </summary>
        public string CertificateSubjectName { get; set; } = null!;
        /// <summary>
        /// The password of the certificate or null, if the certificate has no password.
        /// </summary>
        public string? CertificatePassword { get; set; } = null;
        /// <summary>
        /// Factory function that is used to create an instance of the crypter.
        /// The default factory uses the RSACrypter and passes it the given certificate loader.
        /// </summary>
        public Func<EncryptedYamlConfigurationSource, ICrypter> CrypterFactory { get; set; } =
            cfg => new RSACrypter(cfg.CertificateLoader);

        /// <summary>
        /// List of keys that should be decrypted. Hierarchical keys need to be separated by colon.
        /// <code>Example: "Nested:Key"</code>
        /// </summary>
        public List<string> KeysToDecrypt { get; set; } = new List<string>();

        public EncryptedYamlConfigurationSource()
        {
            ReloadOnChange = true;
        }

        /// <summary>
        /// Creates an instance of the EncryptedYamlConfigProvider.
        /// </summary>
        /// <param name="builder">IConfigurationBuilder instance.</param>
        /// <returns>An EncryptedYamlConfigProvider instance.</returns>
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            base.Build(builder);
            return new EncryptedYamlConfigurationProvider(this);
        }
    }
}