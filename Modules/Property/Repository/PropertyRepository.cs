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

        // Obtener todas las propiedades
        public async Task<List<PropertyModel>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();

        // Obtener por Id
        public async Task<PropertyModel?> GetByIdAsync(string id) =>
            await _collection.Find(p => p.Id == id).FirstOrDefaultAsync();

        // Crear nueva propiedad
        public async Task CreateAsync(PropertyModel property) =>
            await _collection.InsertOneAsync(property);

        // Actualizar propiedad
        public async Task<bool> UpdateAsync(string id, PropertyModel property)
        {
            var result = await _collection.ReplaceOneAsync(p => p.Id == id, property);
            return result.ModifiedCount > 0;
        }

        // Eliminar propiedad
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _collection.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }

        // Verificar existencia
        public async Task<bool> ExistsAsync(string id) =>
            await _collection.CountDocumentsAsync(p => p.Id == id) > 0;
    }
}
