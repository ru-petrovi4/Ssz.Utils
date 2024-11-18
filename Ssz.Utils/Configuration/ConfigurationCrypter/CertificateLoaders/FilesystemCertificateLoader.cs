using System;
using System.IO;
using System.Security.Cryptography.X509Certificates;

namespace Ssz.Utils.ConfigurationCrypter.CertificateLoaders
{
    /// <summary>
    /// Loader that loads a certificate from the filesystem.
    /// </summary>
    public class FilesystemCertificateLoader : ICertificateLoader
    {
        private readonly string _certificatePath;
        private readonly string? _certificatePassword;

        /// <summary>
        /// Creates an instance of the certificate loader.
        /// </summary>
        /// <param name="certificatePath">Fully qualified path to the certificate (.pfx file).</param>
        /// <param name="certificatePassword">Password of the certificate, if available.</param>
        public FilesystemCertificateLoader(string certificatePath, string? certificatePassword = null)
        {
            _certificatePath = certificatePath;
            _certificatePassword = certificatePassword;
        }

        /// <summary>
        /// Loads a certificate from the given location on the filesystem.
        /// </summary>
        /// <returns>A X509Certificate2 instance.</returns>
        public X509Certificate2? LoadCertificate()
        {
            if (!File.Exists(_certificatePath))
                return null;
#if NET9_0_OR_GREATER            
            return X509CertificateLoader.LoadPkcs12FromFile(
                _certificatePath,
                _certificatePassword,
                X509KeyStorageFlags.DefaultKeySet);
#else
            return string.IsNullOrEmpty(_certificatePassword) ?
                new X509Certificate2(_certificatePath) :
                new X509Certificate2(_certificatePath, _certificatePassword);
#endif            
        }
    }
}