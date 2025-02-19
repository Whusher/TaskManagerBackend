using MongoDB.Bson;
using MongoDB.Driver;

namespace ApiCSharp.Services
{
    public class TaskService
    {
        private readonly IMongoDatabase _database;
        public TaskService()
        {
            var connectionString = "mongodb://admin:password@localhost:27017";
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase("Pollito");
        }
        public IMongoCollection<BsonDocument> GetCollection(string collectionName)
        {
            return _database.GetCollection<BsonDocument>(collectionName);
        }
    }

}
