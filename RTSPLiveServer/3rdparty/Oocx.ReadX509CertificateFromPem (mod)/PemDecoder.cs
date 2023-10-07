using Microsoft.AspNetCore.WebUtilities;
using System.IO;
using System.Text;

namespace Oocx.ReadX509CertificateFromPem
{

    public static class PemDecoder
    {

        public static byte[] DecodeSectionFromFile(string fileName, string type)
        {
            var encodedData = File.ReadAllText(fileName);
            return DecodeSection(encodedData, type);
        }

        public static byte[] DecodeSection(string data, string type)
        {
            var lines = data.Replace("\r", "").Split("\n");
            bool inSection = false;
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                if (!inSection)
                {
                    if (line == $"-----BEGIN {type}-----")
                    {
                        inSection = true;
                    }
                }
                else
                {
                    if (line == $"-----END {type}-----")
                    {
                        break;
                    }
                    else
                    {
                        sb.Append(line);
                    }

                }
            }
            return WebEncoders.Base64UrlDecode(sb.ToString());
        }
    }
}
