using MongoDB.Driver;
using MongoDB.Bson;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.Property.Dto;
using Microsoft.Extensions.Caching.Memory;

namespace RealEstate.API.Modules.Properties.Service
{
    public class PropertiesService
    {
        private readonly IMongoCollection<PropertyModel> _properties;
        private readonly IMemoryCache _cache;
        private readonly IWebHostEnvironment _env;

        public PropertiesService(IMongoDatabase database, IMemoryCache cache, IWebHostEnvironment env)
        {
            _cache = cache;
            _env = env;

            // var collectionName = config["MONGO_COLLECTION_PROPERTIES"]
            //             ?? throw new Exception("MONGO_COLLECTION_PROPERTY no definida");

            // _properties = database.GetCollection<PropertyModel>(collectionName);
            _properties = database.GetCollection<PropertyModel>("properties");

            // Crear índices
            _properties.Indexes.CreateOne(
                new CreateIndexModel<PropertyModel>(
                    Builders<PropertyModel>.IndexKeys
                        .Ascending(p => p.Name)
                        .Ascending(p => p.Address)
                        .Ascending(p => p.Price)
                )
            );
        }

        // ===========================================================
        // Obtener con filtros y caché
        // ===========================================================
        public async Task<object> GetCachedAsync(
            string? name, string? address, long? minPrice, long? maxPrice,
            int page = 1, int limit = 6)
        {
            page = Math.Max(1, page);
            limit = Math.Clamp(limit, 1, 100);

            var cacheKey = $"{name}-{address}-{minPrice}-{maxPrice}-{page}-{limit}";
            if (_cache.TryGetValue(cacheKey, out object cached))
                return cached;

            var (data, totalItems) = await GetAllWithMetaAsync(name, address, minPrice, maxPrice, page, limit);

            var result = new
            {
                page,
                limit,
                totalItems,
                totalPages = (int)Math.Ceiling((double)totalItems / limit),
                data
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
            return result;
        }

        // ===========================================================
        // Obtener lista con metadatos
        // ===========================================================
        public async Task<(List<PropertyDto> Data, long TotalItems)> GetAllWithMetaAsync(
            string? name, string? address, long? minPrice, long? maxPrice,
            int page = 1, int limit = 6)
        {
            var filterBuilder = Builders<PropertyModel>.Filter;
            var filters = new List<FilterDefinition<PropertyModel>>();

            if (!string.IsNullOrEmpty(name))
                filters.Add(filterBuilder.Regex(p => p.Name, new BsonRegularExpression(name, "i")));

            if (!string.IsNullOrEmpty(address))
                filters.Add(filterBuilder.Regex(p => p.Address, new BsonRegularExpression(address, "i")));

            if (minPrice.HasValue)
                filters.Add(filterBuilder.Gte(p => p.Price, minPrice.Value));

            if (maxPrice.HasValue)
                filters.Add(filterBuilder.Lte(p => p.Price, maxPrice.Value));

            var filter = filters.Count > 0 ? filterBuilder.And(filters) : FilterDefinition<PropertyModel>.Empty;

            var totalItems = await _properties.CountDocumentsAsync(filter);

            var dataEntities = await _properties
                .Find(filter)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            var dataDtos = dataEntities.Select(MapToDto).ToList();
            return (dataDtos, totalItems);
        }

        // ===========================================================
        // Obtener por ID
        // ===========================================================
        public async Task<PropertyDto?> GetByIdAsync(string Id)
        {
            if (!ObjectId.TryParse(Id, out _))
                throw new Exception($"El Id '{Id}' no es válido.");

            var property = await _properties.Find(p => p.Id == Id).FirstOrDefaultAsync();
            return property != null ? MapToDto(property) : null;
        }

        // ===========================================================
        // Actualizar (con posible nueva imagen)
        // ===========================================================
        public async Task<PropertyModel?> UpdateAsync(string Id, PropertyModel updatedProperty)
        {
            // Obtener propiedad existente
            var existing = await _properties.Find(p => p.Id == Id).FirstOrDefaultAsync();
            if (existing == null)
                return null;

            // Mantener IDs correctos
            updatedProperty.Id = existing.Id;
            if (string.IsNullOrEmpty(updatedProperty.Id))
                updatedProperty.Id = existing.Id;

            // Nota: Owner.Photo se maneja en el Controller
            await _properties.ReplaceOneAsync(p => p.Id == Id, updatedProperty);
            return updatedProperty;
        }


        // ===========================================================
        // Guardar imagen físicamente
        // ===========================================================
        // public async Task<string> SaveImageAsync(IFormFile file)
        // {
        //     var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
        //     if (!Directory.Exists(uploadsDir))
        //         Directory.CreateDirectory(uploadsDir);

        //     var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
        //     var fullPath = Path.Combine(uploadsDir, fileName);

        //     using (var stream = new FileStream(fullPath, FileMode.Create))
        //     {
        //         await file.CopyToAsync(stream);
        //     }

        //     return $"/uploads/{fileName}";
        // }

        public async Task<string> SaveImageAsync(string Image)
        {
            if (string.IsNullOrEmpty(Image))
                throw new ArgumentException("La imagen Base64 no puede estar vacía.");

            // Separar encabezado (data:image/png;base64,...) si existe
            var base64Data = Image.Contains(",") 
                ? Image.Substring(Image.IndexOf(',') + 1) 
                : Image;

            // Convertir Base64 a bytes
            byte[] imageBytes = Convert.FromBase64String(base64Data);

            // Directorio de subida
            var uploadsDir = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads");
            if (!Directory.Exists(uploadsDir))
                Directory.CreateDirectory(uploadsDir);

            // Nombre único de archivo con extensión PNG
            var fileName = $"{Guid.NewGuid()}.png";
            var fullPath = Path.Combine(uploadsDir, fileName);

            // Guardar archivo
            await File.WriteAllBytesAsync(fullPath, imageBytes);

            // Retornar ruta relativa
            return $"/uploads/{fileName}";
        }

        // ===========================================================
        // Eliminar propiedad
        // ===========================================================
        public async Task<bool> DeleteAsync(string Id)
        {
            if (!ObjectId.TryParse(Id, out _))
                throw new Exception($"El Id '{Id}' no es válido.");

            var result = await _properties.DeleteOneAsync(p => p.Id == Id);
            return result.DeletedCount > 0;
        }

        // ===========================================================
        // Mapeos DTO
        // ===========================================================
        private static PropertyDto MapToDto(PropertyModel property)
        {
            return new PropertyDto
            {
                Name = property.Name,
                Address = property.Address,
                Price = property.Price,
                CodeInternal = property.CodeInternal,
                Year = property.Year,
                IdOwner = property.IdOwner
            };
        }
    }
}
