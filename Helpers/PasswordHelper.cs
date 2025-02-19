using BCrypt.Net;

namespace ApiCSharp.Helpers
{
    public static class PasswordHelper
    {
        // Encripta la contraseña
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        // Verifica si la contraseña ingresada coincide con el hash almacenado
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }
    }
}
