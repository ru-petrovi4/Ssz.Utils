using Ssz.Utils.ConfigurationCrypter.CertificateLoaders;
using Ssz.Utils.ConfigurationCrypter.ConfigurationProviders.Yaml;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;

namespace Ssz.Utils.ConfigurationCrypter.Extensions
{
    public static class ConfigurationBuilderExtensions
    {
        /// <summary>
        /// Adds a provider to decrypt keys in the appsettings.yml file.
        /// </summary>
        /// <param name="builder">A ConfigurationBuilder instance.</param>
        /// <param name="configAction">An action used to configure the configuration source.</param>
        /// <returns>The current ConfigurationBuilder instance.</returns>
        public static IConfigurationBuilder AddEncryptedAppSettings(
            this IConfigurationBuilder builder, Action<EncryptedYamlConfigurationSource> configAction)

        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var configurationSource = new EncryptedYamlConfigurationSource { Path = "appsettings.yml" };
            configAction?.Invoke(configurationSource);

            return AddEncryptedYamlConfig(builder, configurationSource);
        }

        /// <summary>
        /// Adds a provider to decrypt keys in the appsettings.yml and the corresponding environment appsettings files.
        /// </summary>
        /// <param name="builder">A ConfigurationBuilder instance.</param>
        /// <param name="configAction">An action used to configure the configuration source.</param>
        /// <param name="hostEnvironment">The current host environment. Used to add environment specific appsettings files. (appsettings.Development.yml, appsettings.Production.yml)</param>
        /// <returns>The current ConfigurationBuilder instance.</returns>
        public static IConfigurationBuilder AddEncryptedAppSettings(
            this IConfigurationBuilder builder, IHostEnvironment hostEnvironment, Action<EncryptedYamlConfigurationSource> configAction)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            string environmentName = hostEnvironment.EnvironmentName;
            if (String.IsNullOrEmpty(environmentName))
                environmentName = @"Production";

            var configurationSource = new EncryptedYamlConfigurationSource { Path = "appsettings.yml", ReloadOnChange = true };
            var environmentConfigurationSource = new EncryptedYamlConfigurationSource { Path = $"appsettings.{environmentName}.yml", Optional = true, ReloadOnChange = true };
            configAction?.Invoke(configurationSource);
            configAction?.Invoke(environmentConfigurationSource);

            AddEncryptedYamlConfig(builder, configurationSource);
            AddEncryptedYamlConfig(builder, environmentConfigurationSource);

            return builder;
        }

        /// <summary>
        /// Adds a provider to decrypt keys in the given Yaml config file.
        /// </summary>
        /// <param name="builder">A ConfigurationBuilder instance.</param>
        /// <param name="configAction">An action used to configure the configuration source.</param>
        /// <returns>The current ConfigurationBuilder instance.</returns>
        public static IConfigurationBuilder AddEncryptedYamlConfig(
                    this IConfigurationBuilder builder, Action<EncryptedYamlConfigurationSource> configAction)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            var configurationSource = new EncryptedYamlConfigurationSource();
            configAction?.Invoke(configurationSource);

            InitializeCertificateLoader(configurationSource);

            builder.Add(configurationSource);
            return builder;
        }

        /// <summary>
        /// Adds a provider to decrypt keys in the given Yaml config file by using the passed EncryptedYamlConfigSource.
        /// </summary>
        /// <param name="builder">A ConfigurationBuilder instance.</param>
        /// <param name="configurationSource">The fully configured config source.</param>
        /// <returns>The current ConfigurationBuilder instance.</returns>
        public static IConfigurationBuilder AddEncryptedYamlConfig(this IConfigurationBuilder builder, EncryptedYamlConfigurationSource configurationSource)
        {
            InitializeCertificateLoader(configurationSource);
            builder.Add(configurationSource);

            return builder;
        }

        private static void InitializeCertificateLoader(EncryptedYamlConfigurationSource configurationSource)
        {
            if (!string.IsNullOrEmpty(configurationSource.CertificatePath))
            {
                configurationSource.CertificateLoader = new FilesystemCertificateLoader(configurationSource.CertificatePath, configurationSource.CertificatePassword);
            }
            else if (!string.IsNullOrEmpty(configurationSource.CertificateSubjectName))
            {
                configurationSource.CertificateLoader = new StoreCertificateLoader(configurationSource.CertificateSubjectName);
            }

            if (configurationSource.CertificateLoader == null)
            {
                throw new InvalidOperationException(
                    "Either CertificatePath or CertificateSubjectName has to be provided if CertificateLoader has not been set manually.");
            }

            if (string.IsNullOrEmpty(configurationSource.Path))
            {
                throw new InvalidOperationException(
                    "The \"Path\" property has to be set to the path of a config file.");
            }
        }
    }
}