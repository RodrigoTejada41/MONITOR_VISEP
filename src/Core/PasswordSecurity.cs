using System;
using System.Security.Cryptography;
using System.Xml.Linq;
namespace Visep
{
    internal static class PasswordSecurity
    {
        const int Iterations = 100000;
        static byte[] Derive(string password, byte[] salt)
        {
            using (var pbkdf = new Rfc2898DeriveBytes(password, salt, Iterations))
                return pbkdf.GetBytes(32);
        }

        internal static XElement CreateUser(string user, string password, string role)
        {
            if (password == null || password.Length < 12 || password.Length > 256)
                throw new ArgumentException("Password must have 12 to 256 characters.");
            var salt = new byte[16];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(salt);
            return new XElement("User", new XAttribute("Name", user), new XAttribute("Role", role),
                new XAttribute("Salt", Convert.ToBase64String(salt)),
                new XAttribute("Hash", Convert.ToBase64String(Derive(password, salt))));
        }

        internal static bool Verify(XElement user, string password)
        {
            var salt = user == null ? new byte[16] : Convert.FromBase64String((string)user.Attribute("Salt"));
            var expected = user == null ? new byte[32] : Convert.FromBase64String((string)user.Attribute("Hash"));
            var actual = Derive(password, salt);
            if (expected.Length != actual.Length) return false;
            int difference = 0;
            for (int index = 0; index < actual.Length; index++) difference |= actual[index] ^ expected[index];
            return user != null && difference == 0;
        }
    }
}
