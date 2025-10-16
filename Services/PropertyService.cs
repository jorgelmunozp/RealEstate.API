using MongoDB.Driver;
using RealEstate.API.Models;

namespace RealEstate.API.Services
{
    public class PropertyService
    {
        private readonly IMongoCollection<Property> _properties;

        public PropertyService(IConfiguration config)
        {
            var connectionString = config["MongoDB:ConnectionString"];
            var databaseName = config["MongoDB:DatabaseName"];

            var client = new MongoClient(connectionString);
            var database = client.GetDatabase(databaseName);
            _properties = database.GetCollection<Property>("properties");
        }

        public async Task<List<Property>> GetAllAsync(string? name, string? address, decimal? minPrice, decimal? maxPrice)
        {
            var filterBuilder = Builders<Property>.Filter;
            var filters = new List<FilterDefinition<Property>>();

            if (!string.IsNullOrEmpty(name))
                filters.Add(filterBuilder.Regex("name", new MongoDB.Bson.BsonRegularExpression(name, "i")));

            if (!string.IsNullOrEmpty(address))
                filters.Add(filterBuilder.Regex("addressProperty", new MongoDB.Bson.BsonRegularExpression(address, "i")));

            if (minPrice.HasValue)
                filters.Add(filterBuilder.Gte("priceProperty", minPrice.Value));

            if (maxPrice.HasValue)
                filters.Add(filterBuilder.Lte("priceProperty", maxPrice.Value));

            var filter = filters.Count > 0 ? filterBuilder.And(filters) : FilterDefinition<Property>.Empty;

            return await _properties.Find(filter).ToListAsync();
        }

        public async Task<Property?> GetByIdAsync(string id) =>
            await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
    }
}
