using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiCSharp.Services;
using ApiCSharp.Models;
using ApiCSharp.Helpers;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using System.Security.Claims;


namespace ApiCSharp.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class MongoDBController : ControllerBase
    {
        private readonly MongoService _mongoDBService;
        private readonly TokenService _tokenService;

        public MongoDBController(MongoService mongoDBService, TokenService tokenService)
        {
            _mongoDBService = mongoDBService;
            _tokenService = tokenService;
        }
        // REGISTRO DE USUARIO
        [HttpPost("register")]
        public IActionResult Register([FromBody] UserMongoModel user)
        {
            var collection = _mongoDBService.GetCollection("users");
            // Verifica si el usuario ya existe
            var existingUser = collection.Find(x => x["email"] == user.email).FirstOrDefault();
            if (existingUser != null)
                return BadRequest("El usuario ya existe.");
            // Encripta la contraseña antes de guardarla
            user.passwordHash = PasswordHelper.HashPassword(user.password);
            user.password = null;

            // No guardamos la contraseña en texto plano
            user.last_login = DateTime.UtcNow;

            // Insertamos el usuario en MongoDB
            collection.InsertOne(user.ToBsonDocument());
            return Ok("Usuario registrado exitosamente.");
        }

        // INICIO DE SESIÓN
        [HttpPost("login")]
        public IActionResult Login([FromBody] UserMongoModel loginRequest)
        {
            var collection = _mongoDBService.GetCollection("users");
            // Buscar usuario en la base de datos
            var userBson = collection.Find(x => x["email"] == loginRequest.email).FirstOrDefault();
            if (userBson == null)
                return Unauthorized("Credenciales inválidas.");
            var user = BsonSerializer.Deserialize<UserMongoModel>(userBson);
            // Verificar contraseña
            if (!PasswordHelper.VerifyPassword(loginRequest.password, user.passwordHash))
                return Unauthorized("Credenciales inválidas.");
            // Generar token
            var token = _tokenService.GenerateToken(user);
            // Actualizar la última fecha de inicio de sesión
            var update = Builders<BsonDocument>.Update.Set("last_login", DateTime.UtcNow);
            collection.UpdateOne(x => x["_id"] == user.Id, update);
            return Ok(new { Token = token });
        }

        // ENDPOINT PROTEGIDO
        [HttpGet("profile")]
        [Authorize]
        public IActionResult GetProfile()
        {
            Console.WriteLine("===== Nueva Petición a /profile =====");

            // Mostrar método HTTP
            Console.WriteLine($"Método: {Request.Method}");

            // Mostrar encabezados
            Console.WriteLine("Encabezados:");
            foreach (var header in Request.Headers)
            {
                Console.WriteLine($"{header.Key}: {header.Value}");
            }

            // Decodificar manualmente los claims del usuario autenticado
            Console.WriteLine("Claims recibidos:");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"{claim.Type}: {claim.Value}");
            }

            // Obtener claims específicos
            var username = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            var email = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            Console.WriteLine($"Usuario autenticado: {username}");
            Console.WriteLine($"Correo del usuario: {email}");

            return Ok(new { message = $"Hola, {username}. Tu correo es: {email}. Este es un endpoint protegido." });
        }




        // Obtener todos los usuarios
        [HttpGet("GetUsers")]
        public IActionResult GetUsers()
        {
            var collection = _mongoDBService.GetCollection("users");
            var users = collection.Find(new BsonDocument()).ToList();
            // Deserializar los documentos BSON a objetos User
            var result = users.Select(doc => BsonSerializer.Deserialize<UserMongoModel>(doc)).ToList();
            return Ok(result);
        }

        #region "Task Manager"
        [HttpPost("createTask")]
        [Authorize]
        public IActionResult CreateTask([FromBody] TasksModel task)
        {
            var collection = _mongoDBService.GetCollection("tasks");
            var emailOwner = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(emailOwner))
            {
                return Unauthorized("No se pudo obtener el email del usuario.");
            }

            task.EmailOwner = emailOwner;

            var bsonTask = task.ToBsonDocument();
            Console.WriteLine("Tarea antes de insertar: " + bsonTask.ToJson());

            try
            {
                collection.InsertOne(bsonTask);
                Console.WriteLine("✅ Tarea insertada correctamente.");
            }
            catch (Exception e)
            {
                Console.WriteLine("❌ Error al insertar tarea: " + e.Message);
                return StatusCode(500, "Error al guardar la tarea.");
            }

            return Ok("Tarea creada exitosamente.");
        }

        [HttpGet("getTasks")]
        [Authorize] // Solo usuarios autenticados pueden ver sus tareas
        public IActionResult GetTasks()
        {
            var emailOwner = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value; // Usamos el email del usuario autenticado

            var collection = _mongoDBService.GetCollection("tasks");

            // Buscamos las tareas que pertenecen al usuario autenticado
            var tasks = collection.Find(x => x["EmailOwner"] == emailOwner).ToList();

            // Deserializamos los documentos BSON a objetos TasksModel
            var result = tasks.Select(doc => BsonSerializer.Deserialize<TasksModel>(doc)).ToList();

            return Ok(result);
        }

        [HttpPut("updateTask/{id}")]
        [Authorize] // Solo usuarios autenticados pueden actualizar sus tareas
        public IActionResult UpdateTask(string id, [FromBody] TasksModel updatedTask)
        {
            var emailOwner = User.Identity.Name; // Usamos el email del usuario autenticado

            var collection = _mongoDBService.GetCollection("tasks");

            // Verificamos si la tarea pertenece al usuario
            var task = collection.Find(x => x["_id"] == new ObjectId(id) && x["EmailOwner"] == emailOwner).FirstOrDefault();

            if (task == null)
                return NotFound("Tarea no encontrada o no pertenece al usuario.");

            // Actualizamos la tarea
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
            var update = Builders<BsonDocument>.Update
                .Set("DescriptionTask", updatedTask.DescriptionTask)
                .Set("NameTask", updatedTask.NameTask)
                .Set("DeadLine", updatedTask.DeadLine)
                .Set("Status", updatedTask.Status)
                .Set("Category", updatedTask.Category);

            var result = collection.UpdateOne(filter, update);

            return Ok("Tarea actualizada exitosamente.");
        }

        [HttpDelete("deleteTask/{id}")]
        [Authorize] // Solo usuarios autenticados pueden eliminar sus tareas
        public IActionResult DeleteTask(string id)
        {
            var emailOwner = User.Identity.Name; // Usamos el email del usuario autenticado

            var collection = _mongoDBService.GetCollection("tasks");

            // Verificamos si la tarea pertenece al usuario
            var task = collection.Find(x => x["_id"] == new ObjectId(id) && x["EmailOwner"] == emailOwner).FirstOrDefault();

            if (task == null)
                return NotFound("Tarea no encontrada o no pertenece al usuario.");

            // Eliminamos la tarea
            var filter = Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(id));
            var result = collection.DeleteOne(filter);

            return Ok("Tarea eliminada exitosamente.");
        }
        #endregion

    }
}
