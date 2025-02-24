using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiCSharp.Models;
public class GroupModel
{
    [BsonId]
    public ObjectId Id { get; set; }
    [BsonRepresentation(BsonType.String)] // Almacena GUID como string en MongoDB
    public Guid Identifier { get; set; }
    public string OwnerGroup { get; set; }
    public string NameGroup { get; set; }
    public DateTime CreationGroupDate { get; set; }
    public List<IntegrantModel>? Integrants { get; set; }
    public bool GroupStatus { get; set; }
}
