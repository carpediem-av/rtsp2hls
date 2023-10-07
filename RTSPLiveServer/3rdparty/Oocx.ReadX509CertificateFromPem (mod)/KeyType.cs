namespace Oocx.ReadX509CertificateFromPem
{
    public static class KeyType
    {
        public const string RSAPrivateKey = "RSA PRIVATE KEY"; // PKCS#1
        public const string PrivateKey = "PRIVATE KEY"; // PKCS#8 RFC5208 RFC5958
        public const string EncryptedPrivateKey = "ENCRYPTED PRIVATE KEY"; // PKCS#8 RFC5208
        public const string ECPrivateKey = "EC PRIVATE KEY"; // RFC5915

        public static readonly string[] KnownTypes = new[] { RSAPrivateKey, PrivateKey, EncryptedPrivateKey, ECPrivateKey };
    }

}
