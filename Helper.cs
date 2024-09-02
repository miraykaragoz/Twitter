using System.Security.Cryptography;
using System.Text;

namespace twitter
{
    public class Helper
    {
        public static string Hash(string input)
        {
            using HMACSHA256 hmac = new HMACSHA256(Encoding.ASCII.GetBytes("anahtarkelime"));
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = hmac.ComputeHash(inputBytes);

            var sb = new StringBuilder();
            foreach (byte b in hashBytes)
            {
                sb.Append(b.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}
 