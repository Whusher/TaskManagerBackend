using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiCSharp.Models
{
    public class IntegrantModel
    {
        [BsonId]
        public ObjectId Id { get; set; }

        public string username { get; set; }

        public string email { get; set; }
    }
}
