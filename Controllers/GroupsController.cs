using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiCSharp.Services;
using ApiCSharp.Models;
using ApiCSharp.Helpers;
using MongoDB.Driver;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using System.Security.Claims;


namespace ApiCSharp.Controllers;
[Route("api/[controller]")]
[ApiController]
public class GroupsController : ControllerBase
{
    private readonly MongoService _mongoDBService;
    private readonly TokenService _tokenService;

    public GroupsController(MongoService mongoDBService, TokenService tokenService)
    {
        _mongoDBService = mongoDBService;
        _tokenService = tokenService;
    }
    [HttpPost("create")]
    public IActionResult CreateGroup([FromBody] GroupModel newGroup)
    {
        var groupsCollection = _mongoDBService.GetCollection("groups");
        newGroup.Identifier = Guid.NewGuid(); // Genera un nuevo ID
        newGroup.CreationGroupDate = DateTime.UtcNow; // Fecha de creación
        groupsCollection.InsertOne(newGroup.ToBsonDocument());
        return CreatedAtAction(nameof(GetGroupById), new { id = newGroup.Id.ToString() }, newGroup);
    }
    [HttpGet("all")]
    public IActionResult GetAllGroups()
    {
        var groupsCollection = _mongoDBService.GetCollection("groups");

        // Filtramos solo los grupos activos
        var groups = groupsCollection.Find(x => x["GroupStatus"] == true).ToList();

        var result = groups.Select(doc => new
        {
            Id = doc["_id"].AsObjectId, // Convertimos el ObjectId correctamente
            Identifier = doc.Contains("Identifier") && doc["Identifier"] != BsonNull.Value
                ? Guid.Parse(doc["Identifier"].AsString)
                : Guid.Empty, // Usa Guid.Empty si es null

            OwnerGroup = doc.Contains("OwnerGroup") ? doc["OwnerGroup"].AsString : string.Empty,
            NameGroup = doc.Contains("NameGroup") ? doc["NameGroup"].AsString : string.Empty,
            CreationGroupDate = doc.Contains("CreationGroupDate")
                ? doc["CreationGroupDate"].ToUniversalTime()
                : DateTime.MinValue,

            GroupStatus = doc.Contains("GroupStatus") && doc["GroupStatus"] != BsonNull.Value
                ? doc["GroupStatus"].AsBoolean
                : false, // Manejo de valores nulos en booleanos

            // Conversión correcta de Integrants
            Integrants = doc.Contains("Integrants") && doc["Integrants"].IsBsonArray
            ? doc["Integrants"].AsBsonArray.Select(item => new
            {
                Id = item["Id"].ToString(),
                username = item["username"].ToString(),
                email = item["email"].ToString()
            }).Cast<dynamic>().ToList()  // Explicitamente convertimos el tipo a dynamic
            : new List<dynamic>() // Si es null, devuelve lista vacía
        }).ToList();

        return Ok(result);
    }


    [HttpGet("getGroupById/{id}")]
    public IActionResult GetGroupById(string id)
    {
        var groupsCollection = _mongoDBService.GetCollection("groups");

        // Buscamos el grupo por su identificador (convertimos el GUID a string)
        var group = groupsCollection.Find(g => g["Identifier"].ToString() == id).FirstOrDefault();

        if (group == null)
            return NotFound("Grupo no encontrado.");

        // Convertimos el BsonDocument a un modelo GroupModel
        var result = new
        {
            Id = group["_id"].AsObjectId,
            Identifier = Guid.Parse(group["Identifier"].AsString),
            OwnerGroup = group.Contains("OwnerGroup") ? group["OwnerGroup"].AsString : string.Empty,
            NameGroup = group.Contains("NameGroup") ? group["NameGroup"].AsString : string.Empty,
            CreationGroupDate = group.Contains("CreationGroupDate") ? group["CreationGroupDate"].ToUniversalTime() : DateTime.MinValue,
            GroupStatus = group.Contains("GroupStatus") ? group["GroupStatus"].AsBoolean : false,
            Integrants = group.Contains("Integrants") && group["Integrants"] != BsonNull.Value
            ? group["Integrants"].AsBsonArray.Select(item => new
            {
                Id = item["Id"].ToString(),
                username = item["username"].ToString(),
                email = item["email"].ToString()
            }).Cast<dynamic>().ToList()  // Explicitamente convertimos el tipo a dynamic
            : new List<dynamic>()
        };

        return Ok(result);
    }
    [HttpPost("AddIntegrantToGroup/{groupId}")]
    public IActionResult AddIntegrantToGroup(string groupId, [FromBody] string email)
    {
        var groupsCollection = _mongoDBService.GetCollection("groups");
        var usersCollection = _mongoDBService.GetCollection("users");

        // Validamos que el grupo exista. Convertimos el GUID de string a Guid.
        var group = groupsCollection.Find(g => g["Identifier"].ToString() == groupId).FirstOrDefault();
        if (group == null)
            return NotFound("Grupo no encontrado.");

        // Validamos que el usuario exista.
        var user = usersCollection.Find(u => u["email"] == email).FirstOrDefault();
        if (user == null)
            return NotFound("Usuario no encontrado.");

        // Si el campo 'Integrants' no existe o es nulo, lo inicializamos como una lista vacía.
        if (!group.Contains("Integrants") || group["Integrants"].IsBsonNull)
        {
            // Inicializamos Integrants como una lista vacía
            var update = Builders<BsonDocument>.Update.Set("Integrants", new BsonArray());
            groupsCollection.UpdateOne(g => g["Identifier"].ToString() == groupId, update);
            group["Integrants"] = new BsonArray();  // Aseguramos que la variable group también se actualice
        }

        // Verificamos si el usuario ya está en el grupo.
        var integrants = group["Integrants"].AsBsonArray;
        if (integrants.Any(u => u["email"] == email))
            return Conflict("El usuario ya está en el grupo.");

        // Creamos el objeto de integrante como BsonDocument, no como una cadena JSON
        var newIntegrant = new BsonDocument
        {
            { "Id", user["_id"] },   // Asegúrate de que el _id esté bien como ObjectId
            { "username", user["username"] },
            { "email", user["email"] }
        };

        // Agregamos al grupo.
        var updateToAdd = Builders<BsonDocument>.Update.AddToSet("Integrants", newIntegrant);
        groupsCollection.UpdateOne(g => g["Identifier"].ToString() == groupId, updateToAdd);

        return Ok("Usuario agregado al grupo exitosamente.");
    }


    [HttpGet("GetGroupsByUser/{email}")] //Pertinen groups of user like integrant
    public IActionResult GetGroupsByUser(string email)
    {
        var groupsCollection = _mongoDBService.GetCollection("groups");

        // Filtramos los grupos donde el correo electrónico del usuario esté en el campo "Integrants"
        var filter = Builders<BsonDocument>.Filter.ElemMatch<BsonDocument>(
            "Integrants", Builders<BsonDocument>.Filter.Eq("email", email)
        );

        var groups = groupsCollection.Find(filter).ToList();

        if (groups == null || !groups.Any())
            return NotFound("No se encontraron grupos para este usuario.");

        // Convertimos los resultados a una lista dinámica
        var result = groups.Select(group => new
        {
            Id = group["_id"].AsObjectId,
            Identifier = Guid.Parse(group["Identifier"].AsString),
            OwnerGroup = group.Contains("OwnerGroup") ? group["OwnerGroup"].AsString : string.Empty,
            NameGroup = group.Contains("NameGroup") ? group["NameGroup"].AsString : string.Empty,
            CreationGroupDate = group.Contains("CreationGroupDate") ? group["CreationGroupDate"].ToUniversalTime() : DateTime.MinValue,
            GroupStatus = group.Contains("GroupStatus") ? group["GroupStatus"].AsBoolean : false,
            // Convertimos Integrants a List<dynamic> explícitamente
            Integrants = group.Contains("Integrants") && group["Integrants"] != BsonNull.Value
                ? group["Integrants"].AsBsonArray.Select(item => new
                {
                    Id = item["Id"].ToString(),
                    username = item["username"].ToString(),
                    email = item["email"].ToString()
                }).Cast<dynamic>().ToList()  // Explicitamente convertimos el tipo a dynamic
                : new List<dynamic>()
        }).ToList();

        return Ok(result);
    }
    [HttpPost("RemoveIntegrantFromGroup/{groupId}")]
    public IActionResult RemoveIntegrantFromGroup(string groupId, [FromBody] string email)
    {
        var groupsCollection = _mongoDBService.GetCollection("groups");

        // Validamos que el grupo exista. Convertimos el GUID de string a Guid.
        var group = groupsCollection.Find(g => g["Identifier"].ToString() == groupId).FirstOrDefault();
        if (group == null)
            return NotFound("Grupo no encontrado.");

        // Verificamos si el campo 'Integrants' existe y tiene elementos.
        if (!group.Contains("Integrants") || group["Integrants"].IsBsonNull || !group["Integrants"].AsBsonArray.Any())
            return NotFound("No se encontraron integrantes en el grupo.");

        // Verificamos si el usuario está en el grupo.
        var integrants = group["Integrants"].AsBsonArray;
        var integrantToRemove = integrants.FirstOrDefault(i => i["email"].ToString() == email);
        if (integrantToRemove == null)
            return NotFound("El usuario no está en el grupo.");

        // Eliminamos el integrante de la lista 'Integrants' usando Pull
        var update = Builders<BsonDocument>.Update.Pull("Integrants", integrantToRemove);
        groupsCollection.UpdateOne(g => g["Identifier"].ToString() == groupId, update);

        return Ok("Usuario eliminado del grupo exitosamente.");
    }
    [HttpGet("GetGroupsByOwner/{email}")]
    public IActionResult GetGroupsByOwner(string email)
    {
        var groupsCollection = _mongoDBService.GetCollection("groups");

        // Filtramos los grupos donde el campo 'OwnerGroup' coincide con el email del propietario
        var filter = Builders<BsonDocument>.Filter.Eq("OwnerGroup", email);

        // Obtenemos los grupos que coinciden con el filtro
        var groups = groupsCollection.Find(filter).ToList();

        if (groups == null || !groups.Any())
            return NotFound("No se encontraron grupos donde eres el propietario.");

        // Convertimos los resultados a una lista de objetos dinámicos
        var result = groups.Select(group => new
        {
            Id = group["_id"].AsObjectId,
            Identifier = Guid.Parse(group["Identifier"].AsString),
            OwnerGroup = group.Contains("OwnerGroup") ? group["OwnerGroup"].AsString : string.Empty,
            NameGroup = group.Contains("NameGroup") ? group["NameGroup"].AsString : string.Empty,
            CreationGroupDate = group.Contains("CreationGroupDate") ? group["CreationGroupDate"].ToUniversalTime() : DateTime.MinValue,
            GroupStatus = group.Contains("GroupStatus") ? group["GroupStatus"].AsBoolean : false,
            Integrants = group.Contains("Integrants") && group["Integrants"] != BsonNull.Value
            ? group["Integrants"].AsBsonArray.Select(item => new
            {
                Id = item["Id"].ToString(),
                username = item["username"].ToString(),
                email = item["email"].ToString()
            }).Cast<dynamic>().ToList()  // Explicitamente convertimos el tipo a dynamic
            : new List<dynamic>()
        }).ToList();

        return Ok(result);
    }
    [HttpPost("DeleteGroup/{groupId}")]
    public IActionResult DeleteGroup(string groupId, [FromBody] string email)
    {
        var groupsCollection = _mongoDBService.GetCollection("groups");

        // Buscamos el grupo por su identificador
        var group = groupsCollection.Find(g => g["Identifier"].ToString() == groupId).FirstOrDefault();
        if (group == null)
            return NotFound("Grupo no encontrado.");

        // Verificamos si el usuario es el propietario del grupo
        var ownerEmail = group.Contains("OwnerGroup") ? group["OwnerGroup"].AsString : string.Empty;
        if (ownerEmail != email)
            return Unauthorized("No tienes permisos para eliminar este grupo.");

        // Si el usuario es el propietario, eliminamos el grupo
        var deleteResult = groupsCollection.DeleteOne(g => g["Identifier"].ToString() == groupId);

        if (deleteResult.DeletedCount == 0)
            return BadRequest("No se pudo eliminar el grupo.");

        return Ok("Grupo eliminado exitosamente.");
    }

}
