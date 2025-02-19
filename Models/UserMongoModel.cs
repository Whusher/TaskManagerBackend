using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiCSharp.Models
{
    public class UserMongoModel
    {
        [BsonId]
        public ObjectId Id { get; set; }  // El campo _id mapeado a ObjectId

        public string? username { get; set; }

        public string? email { get; set; }

        [BsonIgnore]
        public string? password { get; set; }

        public DateTime? last_login { get; set; }

        public string? passwordHash { get; set; }  // Guardamos el hash de la contraseña
    }
}
