using MongoDB.Driver;
using RealEstate.API.Modules.Property.Model;

namespace RealEstate.API.Modules.Property.Repository
{
    public class PropertyRepository
    {
        private readonly IMongoCollection<PropertyModel> _collection;

        public PropertyRepository(IConfiguration config)
        {
            var mongoUri = config["MONGO_CONNECTION"] ?? throw new Exception("MONGO_CONNECTION no definida");
            var dbName = config["MONGO_DATABASE"] ?? throw new Exception("MONGO_DATABASE no definida");
            var collectionName = config["MONGO_COLLECTION_PROPERTY"] ?? throw new Exception("MONGO_COLLECTION_PROPERTY no definida");

            var client = new MongoClient(mongoUri);
            _collection = client.GetDatabase(dbName).GetCollection<PropertyModel>(collectionName);
        }

        public async Task<List<PropertyModel>> GetAllAsync() => await _collection.Find(_ => true).ToListAsync();
        public async Task<PropertyModel?> GetByIdAsync(string id) => await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();
        public async Task CreateAsync(PropertyModel property) => await _collection.InsertOneAsync(property);

        public async Task<bool> UpdateAsync(string id, PropertyModel property)
            => (await _collection.ReplaceOneAsync(p => p.Id == id, property)).ModifiedCount > 0;

        public async Task<bool> DeleteAsync(string id)
            => (await _collection.DeleteOneAsync(p => p.Id == id)).DeletedCount > 0;

        public async Task<bool> ExistsAsync(string id)
            => await _collection.CountDocumentsAsync(p => p.Id == id) > 0;
    }
}
