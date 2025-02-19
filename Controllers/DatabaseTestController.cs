using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ApiCSharp.Services;
using System;

namespace ApiCSharp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DatabaseTestController : ControllerBase
    {
        // Método para probar la conexión a la base de datos
        [HttpGet("test-connection")]
        public IActionResult TestDatabaseConnection()
        {
            try
            {
                // Obtener la instancia de la conexión a la base de datos
                using (var connection = DatabaseConnection.Instance())
                {
                    // Abrir la conexión
                    connection.Open();

                    // Si la conexión se abre correctamente, devolvemos un mensaje de éxito
                    return Ok("Conexión a la base de datos exitosa");
                }
            }
            catch (Exception ex)
            {
                // Si ocurre un error, devolvemos un mensaje de error
                return StatusCode(500, $"Error al conectar a la base de datos: {ex.Message}");
            }
        }
    }
}
