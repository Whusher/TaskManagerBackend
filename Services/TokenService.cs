using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using ApiCSharp.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Configuration;

namespace ApiCSharp.Services
{
    public class TokenService
    {
        private readonly byte[] _key;
        public TokenService(IConfiguration config)
        {
            var keyBase64 = config["Jwt:Key"];
            if (string.IsNullOrEmpty(keyBase64))
                throw new ArgumentException("Jwt:Key no puede ser nulo o vacío.");

            _key = Convert.FromBase64String(keyBase64);
        }


        public string GenerateToken(UserMongoModel user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(_key);  // Usar la clave en Base64

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.username),
                    new Claim(ClaimTypes.Email, user.email)
                }),
                Expires = DateTime.UtcNow.AddMinutes(10),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}
