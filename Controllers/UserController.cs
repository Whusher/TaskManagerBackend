using ApiCSharp.Models;
using ApiCSharp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
namespace ApiCSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        // List of users
        private static List<UserModel> Users = new();

        [HttpGet("GetAllUsers")]
        public ActionResult GetUsers() => Ok(Users);


        [HttpGet("AuthUser")]
        public async Task<ActionResult> AuthUserCredentials(
            [FromHeader(Name = "x-username")] string Username, 
            [FromHeader(Name = "X-password")] string password)
        {
            if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(password))
            {
                return BadRequest(new { message = "username and password must be provided." });
            }

            // Consultar si el usuario existe
            string authQuery = "SELECT COUNT(*) FROM Usuario WHERE username = @Username AND password = @Password";

            try
            {
                using (var connection = DatabaseConnection.Instance())
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(authQuery, connection))
                    {
                        // Agregar los valores por medio de parametros declarados
                        command.Parameters.AddWithValue("@Username", Username);
                        command.Parameters.AddWithValue("@Password", password); 

                        int userCount = (int)await command.ExecuteScalarAsync();

                        if (userCount > 0)
                        {
                            return StatusCode(200, new { message = $"Welcome user {Username}" });
                        }
                        else
                        {
                            return StatusCode(401, new { message = "Invalid Credentials" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while authenticating the user.", error = ex.Message });
            }
        }

        [HttpPost("RegisterUser")]
        public async Task<IActionResult> RegisterUser([FromBody] UserModel newUser)
        {
            if (newUser == null)
            {
                return BadRequest(new { message = "Invalid user data." });
            }

            // Verifica si el usuario ya existe por Username o Email
            string checkUserQuery = "SELECT COUNT(*) FROM Usuario WHERE username = @Username OR email = @Email";

            try
            {
                using (var connection = DatabaseConnection.Instance())
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand(checkUserQuery, connection))
                    {
                        // Agregar los valores mediante parametros
                        command.Parameters.AddWithValue("@Username", newUser.Username);
                        command.Parameters.AddWithValue("@Email", newUser.Email);

                        int userCount = (int)await command.ExecuteScalarAsync();

                        if (userCount > 0)
                        {
                            return BadRequest(new { message = "The user already exists!" });
                        }
                    }

                    // Si no existe agregamos al usuario
                    string insertUserQuery = "INSERT INTO Usuario (username, password, email, birthday, fullname) VALUES (@Username, @Password, @Email, @Birthday, @FullName)";

                    using (var command = new SqlCommand(insertUserQuery, connection))
                    {
                        // Parametrizar los valores para agregar al usuario
                        command.Parameters.AddWithValue("@Username", newUser.Username);
                        command.Parameters.AddWithValue("@Password", newUser.Password);
                        command.Parameters.AddWithValue("@Email", newUser.Email);
                        command.Parameters.AddWithValue("@Birthday", newUser.Birthday.ToString("yyyy-MM-dd"));
                        command.Parameters.AddWithValue("@FullName", newUser.FullName);
                        await command.ExecuteNonQueryAsync();
                    }

                    return Ok(new { message = "User registered successfully!", users = newUser });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while registering the user.", error = ex.Message });
            }
        }



    }
}
