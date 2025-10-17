using MongoDB.Driver;
using RealEstate.API.Models;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RealEstate.API.Repositories
{
    public class PropertyRepository
    {
        private readonly IMongoCollection<Property> _collection;

        public PropertyRepository(IConfiguration config)
        {
            // Leer variables de entorno
            var mongoUri = config.GetValue<string>("MONGO_CONNECTION") 
                           ?? throw new Exception("MONGO_CONNECTION no definida");
            var dbName = config.GetValue<string>("MONGO_DATABASE") 
                         ?? throw new Exception("MONGO_DATABASE no definida");
            var collectionName = config.GetValue<string>("MONGO_COLLECTION_PROPERTIES") 
                                 ?? throw new Exception("MONGO_COLLECTION_PROPERTIES no definida");

            // Crear cliente y obtener la base de datos
            var client = new MongoClient(mongoUri);
            var database = client.GetDatabase(dbName);

            // Obtener la colección
            _collection = database.GetCollection<Property>(collectionName);
        }

        // Obtener todas las propiedades
        public async Task<List<Property>> GetAllAsync() =>
            await _collection.Find(_ => true).ToListAsync();

        // Obtener por Id
        public async Task<Property?> GetByIdAsync(string id) =>
            await _collection.Find(p => p.IdProperty == id).FirstOrDefaultAsync();

        // Crear nueva propiedad
        public async Task CreateAsync(Property property) =>
            await _collection.InsertOneAsync(property);

        // Actualizar propiedad
        public async Task<bool> UpdateAsync(string id, Property property)
        {
            var result = await _collection.ReplaceOneAsync(p => p.IdProperty == id, property);
            return result.ModifiedCount > 0;
        }

        // Eliminar propiedad
        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _collection.DeleteOneAsync(p => p.IdProperty == id);
            return result.DeletedCount > 0;
        }

        // Comprobar si existe
        public async Task<bool> ExistsAsync(string id) =>
            await _collection.CountDocumentsAsync(p => p.IdProperty == id) > 0;
    }
}
