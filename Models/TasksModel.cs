using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApiCSharp.Models
{
    public class TasksModel
    {
        [BsonId]
        public ObjectId Id { get; set; }
        public string DescriptionTask { get; set; }
        public string NameTask { get; set; }
        public string? TaskGroup { get; set; }
        public DateTime DeadLine { get; set; }
        public string Status { get; set; }//-Status : In Progress/Done/Paused/Revision
        public string Category { get; set; }
        public string? EmailOwner { get; set; }
    }
}
