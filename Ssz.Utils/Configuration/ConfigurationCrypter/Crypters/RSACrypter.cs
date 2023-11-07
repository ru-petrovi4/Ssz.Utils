using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Ssz.Utils.ConfigurationCrypter.CertificateLoaders;

namespace Ssz.Utils.ConfigurationCrypter.Crypters
{
    /// <summary>
    /// RSA based crypter that uses the public and private key of a certificate to encrypt and decrypt strings.
    /// </summary>
    public class RSACrypter : ICrypter
    {
        private readonly ICertificateLoader _certificateLoader;
        private RSA? _privateKey;
        private RSA? _publicKey;

        /// <summary>
        ///  Creates an instance of the RSACrypter.
        /// </summary>
        /// <param name="certificateLoader">A certificate loader instance.</param>
        public RSACrypter(ICertificateLoader certificateLoader)
        {
            _certificateLoader = certificateLoader;

            using (var certificate = _certificateLoader.LoadCertificate())
            {
                _privateKey = certificate!.GetRSAPrivateKey();
                _publicKey = certificate!.GetRSAPublicKey();
            }
        }

        /// <summary>
        /// Encrypts the given string with the private key of the loaded certificate.
        /// </summary>
        /// <param name="value">String to decrypt.</param>
        /// <returns>Encrypted string.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        public string? DecryptString(string? value)
        {
            if (String.IsNullOrEmpty(value))
                return value;

#if NET7_0_OR_GREATER
            Span<byte> buffer = new Span<byte>(new byte[value.Length]);
            if (!Convert.TryFromBase64String(value, buffer, out int bytesParsed))
                return value;
            var decryptedBytes = _privateKey!.Decrypt(buffer.Slice(0, bytesParsed), RSAEncryptionPadding.OaepSHA512);
#else       
            var encryptedBytes = Convert.FromBase64String(value);
            var decryptedBytes = _privateKey!.Decrypt(encryptedBytes, RSAEncryptionPadding.OaepSHA512);            
#endif

            return Encoding.UTF8.GetString(decryptedBytes);
        }

        /// <summary>
        /// Disposes the instance.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Decrypts the given string with the public key of the loaded certificate.
        /// </summary>
        /// <param name="value">String to encrypt.</param>
        /// <returns>Encrypted string.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        public string? EncryptString(string? value)
        {
            if (String.IsNullOrEmpty(value)) 
                return value;

            var encryptedBytes = _publicKey!.Encrypt(Encoding.UTF8.GetBytes(value), RSAEncryptionPadding.OaepSHA512);

            return Convert.ToBase64String(encryptedBytes);
        }

        /// <summary>
        /// Disposes the underlying keys.
        /// </summary>
        /// <param name="disposing">True if called from user code, false if called by finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _privateKey?.Dispose();
                _publicKey?.Dispose();
            }
        }        
    }
}