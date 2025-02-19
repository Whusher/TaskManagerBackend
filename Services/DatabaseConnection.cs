using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Reflection.Metadata.Ecma335;

namespace ApiCSharp.Services
{
    public class DatabaseConnection
    {
        private static SqlConnection _instance;
        private static readonly object _lock = new object();

        // Constructor privado para evitar la creación de instancias externas
        private DatabaseConnection() { }

        // Propiedad pública que devuelve la instancia única de SqlConnection
        public static SqlConnection Instance()
        {
            // Cargar la configuración desde appsettings.json
            var configuration = new ConfigurationBuilder()
                // Establece la ruta base del directorio
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
            // Obtener la cadena de conexión desde el archivo de configuración
            string? connectionString = configuration.GetConnectionString("SQLServerConnection");
            // Crear la instancia de la conexión
            return new SqlConnection(connectionString);

        }
    }
}
