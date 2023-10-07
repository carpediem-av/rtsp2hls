using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace Oocx.ReadX509CertificateFromPem
{
    public class CertificateFromPemReader
    {
        /// <summary>
        /// Creates a X509Certificate2 from a certificate from the file <paramref name="certificateFileName"/>
        /// and a private key from the file <paramref name="privateKeyFileName"/>.
        /// </summary>
        /// <param name="certificateFileName">A PEM-encoded certificate</param>
        /// <param name="privateKeyFileName">A PEM-encoded private key</param>
        /// <returns>A new X509Certificate2 instance based on the key and certificate files</returns>
        public X509Certificate2 LoadCertificateWithPrivateKey(string certificateFileName, string privateKeyFileName)
        {
            var certString =  File.ReadAllText(certificateFileName);
            var privateKeyString = File.ReadAllText(privateKeyFileName);
            return LoadCertificateWithPrivateKeyFromStrings(certString, privateKeyString);
        }

        /// <summary>
        /// Creates a X509Certificate2 from a certificate from the PEM string <paramref name="certificateData"/>
        /// and a private key from the PEM string <paramref name="privateKeyData"/>.
        /// </summary>
        /// <param name="certificateData">A PEM-encoded certificate</param>
        /// <param name="privateKeyData">A PEM-encoded private key</param>
        /// <returns>A new X509Certificate2 instance based on the key and certificate files</returns>
        public X509Certificate2 LoadCertificateWithPrivateKeyFromStrings(string certificateData, string privateKeyData)
        {
            var privateKey = LoadPrivateKey(privateKeyData);

            var certificateBytes = PemDecoder.DecodeSection(certificateData, "CERTIFICATE");
            var certificate = LoadCertificate(certificateBytes);

            return CreateCertificateWithPrivateKey(certificate, privateKey);
        }

        private static AsymmetricAlgorithm LoadPrivateKey(string privateKeyPem)
        {
            var keyType = DetectKeyType(privateKeyPem);
            var privateKeyBytes = PemDecoder.DecodeSection(privateKeyPem, keyType);
            var privateKey = GetPrivateKey(keyType, new ReadOnlyMemory<byte>(privateKeyBytes));
            return privateKey;
        }

        private static string DetectKeyType(string pem)
        {
            var keyTypeRegEx = new Regex("-----BEGIN ([A-Z\\s]*)");
            var matches = keyTypeRegEx.Matches(pem);

            if (matches.Count == 0) { throw new InvalidDataException("No private key found in file.");  }
            for (int mc= 0; mc < matches.Count; mc++)
            {
                for (int g = 0; g < matches[mc].Groups.Count; g++)
                {
                    if (KeyType.KnownTypes.Contains(matches[mc].Groups[g].Value))
                    {
                        return matches[mc].Groups[g].Value;
                    }
                }
            }
            throw new InvalidDataException("No supported key detected in key file");
        }

        private static AsymmetricAlgorithm GetPrivateKey(string keyType, ReadOnlyMemory<byte> privateKeyBytes)
        {
            AsymmetricAlgorithm ECKey(ReadOnlySpan<byte> bytes)
            {
                var ecdh = ECDiffieHellman.Create();
                ecdh.ImportECPrivateKey(bytes, out _);
                return ecdh;
            }

            AsymmetricAlgorithm ECKeyPkcs8(ReadOnlySpan<byte> bytes)
            {
                var ecdh = ECDiffieHellman.Create();
                ecdh.ImportPkcs8PrivateKey(bytes, out _);
                return ecdh;
            }

            AsymmetricAlgorithm RSAKey(ReadOnlySpan<byte> bytes)
            {
                var rsa = RSA.Create();
                rsa.ImportRSAPrivateKey(bytes, out _);
                return rsa;
            }

            switch (keyType)
            {

                case KeyType.ECPrivateKey: return ECKey(privateKeyBytes.Span);
                case KeyType.RSAPrivateKey: return RSAKey(privateKeyBytes.Span);
                case KeyType.PrivateKey:
                    var key = Pkcs8PrivateKeyInfo.Decode(privateKeyBytes, out _);
                    if (key.AlgorithmId.FriendlyName == "RSA")
                    {
                        return RSAKey(key.PrivateKeyBytes.Span);
                    }

                    if (key.AlgorithmId.FriendlyName == "ECC")
                    {
                        return ECKeyPkcs8(privateKeyBytes.Span);
                    }
                    throw new InvalidDataException($"Unsupported private key type {key.AlgorithmId.FriendlyName}");
                default:
                    throw new InvalidDataException($"Unsupported key type: {keyType}.");
            }
        }

        public X509Certificate2 LoadCertificate(string certificateFileName)
        {
            var certificateBytes = PemDecoder.DecodeSectionFromFile(certificateFileName, "CERTIFICATE");
            return LoadCertificate(certificateBytes);
        }

        public X509Certificate2 LoadCertificate(byte[] certificateBytes)
        {
            var certificate = new X509Certificate2(certificateBytes);
            return certificate;
        }

        private static X509Certificate2 CreateCertificateWithPrivateKey(X509Certificate2 certificate, AsymmetricAlgorithm privateKey)
        {
            var builder = new Pkcs12Builder();
            var contents = new Pkcs12SafeContents();
            contents.AddCertificate(certificate);
            contents.AddKeyUnencrypted(privateKey);
            builder.AddSafeContentsUnencrypted(contents);

            // OpenSSL requires the file to have a mac, without mac this will run on Windows but not on Linux
            builder.SealWithMac("temp", HashAlgorithmName.SHA1, 1);
            var pkcs12bytes = builder.Encode();

            var certificateWithKey = new X509Certificate2(pkcs12bytes, "temp");
            return certificateWithKey;
        }
    }
}
