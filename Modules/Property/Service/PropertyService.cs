using MongoDB.Bson;
using MongoDB.Driver;
using FluentValidation;
using Microsoft.Extensions.Caching.Memory;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.Property.Dto;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.Property.Mapper;
using RealEstate.API.Modules.Property.Interface;
using RealEstate.API.Modules.Owner.Model;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Mapper;
using RealEstate.API.Modules.Owner.Interface;
using RealEstate.API.Modules.PropertyImage.Model;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Mapper;
using RealEstate.API.Modules.PropertyImage.Interface;
using RealEstate.API.Modules.PropertyTrace.Model;
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Mapper;
using RealEstate.API.Modules.PropertyTrace.Interface;
using System.Text.Json;

namespace RealEstate.API.Modules.Property.Service
{
    public class PropertyService : IPropertyService
    {
        private readonly IMongoCollection<PropertyModel> _properties;
        private readonly IMongoCollection<OwnerModel> _owners;
        private readonly IMongoCollection<PropertyImageModel> _images;
        private readonly IMongoCollection<PropertyTraceModel> _traces;
        private readonly IValidator<PropertyDto> _validator;
        private readonly IMemoryCache _cache;
        private readonly TimeSpan _cacheTtl;
        private readonly IOwnerService _ownerService;
        private readonly IPropertyImageService _imageService;
        private readonly IPropertyTraceService _traceService;

        public PropertyService(
            IMongoDatabase database,
            IValidator<PropertyDto> validator,
            IConfiguration config,
            IMemoryCache cache,
            IOwnerService ownerService,
            IPropertyImageService imageService,
            IPropertyTraceService traceService)
        {
            _properties = database.GetCollection<PropertyModel>(config["MONGO_COLLECTION_PROPERTY"] ?? throw new Exception("MONGO_COLLECTION_PROPERTY no definida"));
            _owners = database.GetCollection<OwnerModel>(config["MONGO_COLLECTION_OWNER"] ?? throw new Exception("MONGO_COLLECTION_OWNER no definida"));
            _images = database.GetCollection<PropertyImageModel>(config["MONGO_COLLECTION_PROPERTYIMAGE"] ?? throw new Exception("MONGO_COLLECTION_PROPERTYIMAGE no definida"));
            _traces = database.GetCollection<PropertyTraceModel>(config["MONGO_COLLECTION_PROPERTYTRACE"] ?? throw new Exception("MONGO_COLLECTION_PROPERTYTRACE no definida"));
            _validator = validator;
            _cache = cache;
            _ownerService = ownerService;
            _imageService = imageService;
            _traceService = traceService;

            var ttlStr = config["CACHE_TTL_MINUTES"];
            _cacheTtl = int.TryParse(ttlStr, out var minutes) && minutes > 0
                ? TimeSpan.FromMinutes(minutes)
                : TimeSpan.FromMinutes(5);
        }

        // GET (con filtros, paginación y caché)
        public async Task<ServiceResultWrapper<object>> GetCachedAsync(
            string? name, string? address, string? idOwner,
            long? minPrice, long? maxPrice, int page = 1, int limit = 6, bool refresh = false)
        {
            try
            {
                page = Math.Max(1, page);
                limit = Math.Clamp(limit, 1, 100);
                var cacheKey = $"{name}-{address}-{idOwner}-{minPrice}-{maxPrice}-{page}-{limit}";

                if (!refresh && _cache.TryGetValue(cacheKey, out object? cached))
                    return ServiceResultWrapper<object>.Ok(cached!, "Propiedades obtenidas desde caché");

                var (data, totalItems) = await GetAllWithMetaAsync(name, address, idOwner, minPrice, maxPrice, page, limit);

                var result = new
                {
                    data,
                    meta = new
                    {
                        page,
                        limit,
                        total = totalItems,
                        last_page = (int)Math.Ceiling((double)totalItems / limit)
                    }
                };

                _cache.Set(cacheKey, result, _cacheTtl);
                return ServiceResultWrapper<object>.Ok(result, "Propiedades obtenidas correctamente");
            }
            catch (Exception ex)
            {
                return ServiceResultWrapper<object>.Error(ex);
            }
        }

        // GET BY ID
        public async Task<ServiceResultWrapper<PropertyDto>> GetByIdAsync(string id)
        {
            try
            {
                var property = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (property == null)
                    return ServiceResultWrapper<PropertyDto>.Fail("Propiedad no encontrada", 404);

                var dto = property.ToDto();

                var ownerTask = !string.IsNullOrWhiteSpace(property.IdOwner)
                    ? _owners.Find(o => o.Id == property.IdOwner).FirstOrDefaultAsync()
                    : Task.FromResult<OwnerModel?>(null);

                var imageTask = _images.Find(i => i.IdProperty == property.Id).FirstOrDefaultAsync();
                var tracesTask = _traces.Find(t => t.IdProperty == property.Id).ToListAsync();

                await Task.WhenAll(ownerTask, imageTask, tracesTask);

                if (ownerTask.Result != null)
                    dto.Owner = OwnerMapper.ToDto(ownerTask.Result);

                if (imageTask.Result != null)
                    dto.Image = PropertyImageMapper.ToDto(imageTask.Result);

                if (tracesTask.Result.Any())
                    dto.Traces = tracesTask.Result.Select(PropertyTraceMapper.ToDto).ToList();

                return ServiceResultWrapper<PropertyDto>.Ok(dto, "Propiedad obtenida correctamente");
            }
            catch (Exception ex)
            {
                return ServiceResultWrapper<PropertyDto>.Error(ex);
            }
        }

        // CREATE
        public async Task<ServiceResultWrapper<PropertyDto>> CreateAsync(PropertyDto dto)
        {
            try
            {
                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return ServiceResultWrapper<PropertyDto>.Fail(validation.Errors.Select(e => e.ErrorMessage), 400, "Errores de validación");

                if (dto.Owner != null)
                {
                    if (string.IsNullOrWhiteSpace(dto.Owner.IdOwner))
                    {
                        var result = await _ownerService.CreateAsync(dto.Owner);
                        dto.IdOwner = result.Data?.IdOwner;
                    }
                    else dto.IdOwner = dto.Owner.IdOwner;
                }

                var model = dto.ToModel();
                await _properties.InsertOneAsync(model);

                if (dto.Image != null && !string.IsNullOrWhiteSpace(dto.Image.File))
                    await _imageService.CreateAsync(new PropertyImageDto
                    {
                        IdProperty = model.Id,
                        File = dto.Image.File,
                        Enabled = dto.Image.Enabled
                    });

                if (dto.Traces?.Any() == true)
                {
                    var traceTasks = dto.Traces.Select(trace =>
                    {
                        trace.IdProperty = model.Id;
                        trace.IdPropertyTrace = ObjectId.GenerateNewId().ToString();
                        return _traceService.CreateAsync(new List<PropertyTraceDto> { trace });
                    });
                    await Task.WhenAll(traceTasks);
                }

                var created = await GetByIdAsync(model.Id);
                return ServiceResultWrapper<PropertyDto>.Created(created.Data, "Propiedad creada correctamente");
            }
            catch (Exception ex)
            {
                return ServiceResultWrapper<PropertyDto>.Error(ex);
            }
        }

        // UPDATE (PUT)
        public async Task<ServiceResultWrapper<PropertyDto>> UpdateAsync(string id, PropertyDto dto)
        {
            try
            {
                var validation = await _validator.ValidateAsync(dto);
                if (!validation.IsValid)
                    return ServiceResultWrapper<PropertyDto>.Fail(validation.Errors.Select(e => e.ErrorMessage), 400, "Errores de validación");

                var existing = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (existing == null)
                    return ServiceResultWrapper<PropertyDto>.Fail("Propiedad no encontrada", 404);
                Console.WriteLine("dto.Owner 1!!!!!!!: " + dto.Owner);

                if (dto.Owner != null)
                {
                    if (string.IsNullOrWhiteSpace(dto.Owner.IdOwner))
                    {
                        Console.WriteLine("dto.Owner 2!!!!!!!: " + dto.Owner);

                        var ownerCreated = await _ownerService.CreateAsync(dto.Owner);
                        Console.WriteLine("ownerCreated.Data?.IdOwner!!!!!!!: " + ownerCreated.Data?.IdOwner);

                        dto.IdOwner = ownerCreated.Data?.IdOwner;
                        Console.WriteLine("dto.IdOwner!!!!!!!: " + dto.IdOwner);
                    }
                    else
                        await _ownerService.UpdateAsync(dto.Owner.IdOwner, dto.Owner);
                }

                existing.Name = dto.Name;
                existing.Address = dto.Address;
                existing.Price = dto.Price;
                existing.CodeInternal = dto.CodeInternal;
                existing.Year = dto.Year;
                existing.IdOwner = dto.IdOwner;

                await _properties.ReplaceOneAsync(p => p.Id == id, existing);

                if (dto.Image != null && !string.IsNullOrWhiteSpace(dto.Image.File))
                {
                    var existingImage = await _imageService.GetByPropertyIdAsync(id);
                    if (existingImage != null)
                        await _imageService.UpdateAsync(existingImage.IdPropertyImage!, dto.Image);
                    else
                        await _imageService.CreateAsync(new PropertyImageDto
                        {
                            IdProperty = id,
                            File = dto.Image.File,
                            Enabled = dto.Image.Enabled
                        });
                }

                if (dto.Traces?.Any() == true)
                {
                    var traceTasks = dto.Traces.Select(async trace =>
                    {
                        trace.IdProperty = id;
                        if (string.IsNullOrWhiteSpace(trace.IdPropertyTrace))
                        {
                            trace.IdPropertyTrace = ObjectId.GenerateNewId().ToString();
                            await _traceService.CreateAsync(new List<PropertyTraceDto> { trace });
                        }
                        else
                            await _traceService.UpdateAsync(trace.IdPropertyTrace, trace);
                    });
                    await Task.WhenAll(traceTasks);
                }

                var updated = await GetByIdAsync(id);
                return ServiceResultWrapper<PropertyDto>.Updated(updated.Data, "Propiedad actualizada correctamente");
            }
            catch (Exception ex)
            {
                return ServiceResultWrapper<PropertyDto>.Error(ex);
            }
        }

        // PATCH (modular, delegando a servicios)
        public async Task<ServiceResultWrapper<PropertyDto>> PatchAsync(string id, Dictionary<string, object> fields)
        {
            try
            {
                var existing = await _properties.Find(p => p.Id == id).FirstOrDefaultAsync();
                if (existing == null)
                    return ServiceResultWrapper<PropertyDto>.Fail("Propiedad no encontrada", 404);

                var builder = Builders<PropertyModel>.Update;
                var updates = new List<UpdateDefinition<PropertyModel>>();
                var baseFields = new[] { "Name", "Address", "Price", "CodeInternal", "Year", "IdOwner" };

                foreach (var f in fields)
                {
                    var fieldName = char.ToUpperInvariant(f.Key[0]) + f.Key.Substring(1);
                    if (baseFields.Contains(fieldName))
                    {
                        var normalized = JsonHelper.NormalizeValue(f.Value);
                        if (normalized != null)
                            updates.Add(builder.Set(fieldName, BsonValue.Create(normalized)));
                    }
                }

                if (updates.Count != 0)
                    await _properties.UpdateOneAsync(p => p.Id == id, builder.Combine(updates));

                if (fields.TryGetValue("owner", out object? valueOwner))
                    await JsonHelper.ProcessOwnerPatchAsync(valueOwner, existing.IdOwner, id, _ownerService, _properties);

                if (fields.TryGetValue("image", out object? valueImage))
                    await JsonHelper.ProcessImagePatchAsync(valueImage, id, _imageService);

                if (fields.TryGetValue("traces", out object? valueTraces))
                    await JsonHelper.ProcessTracesPatchAsync(valueTraces, id, _traceService);

                var updated = await GetByIdAsync(id);
                return ServiceResultWrapper<PropertyDto>.Updated(updated.Data, "Propiedad actualizada parcialmente");
            }
            catch (Exception ex)
            {
                return ServiceResultWrapper<PropertyDto>.Error(ex);
            }
        }

        // DELETE
        public async Task<ServiceResultWrapper<bool>> DeleteAsync(string id)
        {
            try
            {
                var deleteProperty = _properties.DeleteOneAsync(p => p.Id == id);
                var deleteImages = _images.DeleteManyAsync(i => i.IdProperty == id);
                var deleteTraces = _traces.DeleteManyAsync(t => t.IdProperty == id);

                await Task.WhenAll(deleteProperty, deleteImages, deleteTraces);

                if (deleteProperty.Result.DeletedCount == 0)
                    return ServiceResultWrapper<bool>.Fail("Propiedad no encontrada", 404);

                return ServiceResultWrapper<bool>.Deleted("Propiedad eliminada correctamente");
            }
            catch (Exception ex)
            {
                return ServiceResultWrapper<bool>.Error(ex);
            }
        }

        // Helper: consulta completa
        private async Task<(List<PropertyDto> Data, long TotalItems)> GetAllWithMetaAsync( string? name, string? address, string? idOwner, long? minPrice, long? maxPrice, int page, int limit)
        {
            var fb = Builders<PropertyModel>.Filter;
            var filters = new List<FilterDefinition<PropertyModel>>();

            if (!string.IsNullOrEmpty(name)) filters.Add(fb.Regex(p => p.Name, new BsonRegularExpression(name, "i")));
            if (!string.IsNullOrEmpty(address)) filters.Add(fb.Regex(p => p.Address, new BsonRegularExpression(address, "i")));
            if (!string.IsNullOrEmpty(idOwner)) filters.Add(fb.Eq(p => p.IdOwner, idOwner));
            if (minPrice.HasValue) filters.Add(fb.Gte(p => p.Price, minPrice.Value));
            if (maxPrice.HasValue) filters.Add(fb.Lte(p => p.Price, maxPrice.Value));

            var filter = filters.Count > 0 ? fb.And(filters) : FilterDefinition<PropertyModel>.Empty;
            var totalItems = await _properties.CountDocumentsAsync(filter);
            var data = await _properties.Find(filter)
                .Skip((page - 1) * limit)
                .Limit(limit)
                .ToListAsync();

            var dtoTasks = data.Select(async prop =>
            {
                var dto = prop.ToDto();
                var ownerTask = !string.IsNullOrWhiteSpace(prop.IdOwner)
                    ? _owners.Find(o => o.Id == prop.IdOwner).FirstOrDefaultAsync()
                    : Task.FromResult<OwnerModel?>(null);
                var imageTask = _images.Find(i => i.IdProperty == prop.Id).FirstOrDefaultAsync();
                var tracesTask = _traces.Find(t => t.IdProperty == prop.Id).ToListAsync();

                await Task.WhenAll(ownerTask, imageTask, tracesTask);

                if (ownerTask.Result != null) dto.Owner = OwnerMapper.ToDto(ownerTask.Result);
                if (imageTask.Result != null) dto.Image = PropertyImageMapper.ToDto(imageTask.Result);
                if (tracesTask.Result.Any()) dto.Traces = tracesTask.Result.Select(PropertyTraceMapper.ToDto).ToList();

                return dto;
            });

            var dtoList = await Task.WhenAll(dtoTasks);
            return (dtoList.ToList(), totalItems);
        }

        // Helper interno para JSON
        private static class JsonHelper
        {
            public static object? NormalizeValue(object? value)
            {
                if (value == null) return null;
                if (value is JsonElement jsonEl)
                {
                    switch (jsonEl.ValueKind)
                    {
                        case JsonValueKind.Object:
                            return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonEl.GetRawText())
                                ?.ToDictionary(kv => kv.Key, kv => NormalizeValue(kv.Value));
                        case JsonValueKind.Array:
                            return JsonSerializer.Deserialize<List<object>>(jsonEl.GetRawText())
                                ?.Select(NormalizeValue).ToList();
                        case JsonValueKind.String:
                            return jsonEl.GetString();
                        case JsonValueKind.Number:
                            if (jsonEl.TryGetInt64(out long l)) return l;
                            if (jsonEl.TryGetDouble(out double d)) return d;
                            return null;
                        case JsonValueKind.True: return true;
                        case JsonValueKind.False: return false;
                        default: return null;
                    }
                }
                if (value is Dictionary<string, object> dictObj)
                    return dictObj.ToDictionary(kv => kv.Key, kv => NormalizeValue(kv.Value));
                if (value is List<object> listObj)
                    return listObj.Select(NormalizeValue).ToList();
                return value;
            }

            public static async Task ProcessOwnerPatchAsync(object ownerObj, string? existingIdOwner, string propertyId, IOwnerService ownerService, IMongoCollection<PropertyModel> propertyCollection)
            {
                var ownerDict = NormalizeValue(ownerObj) as Dictionary<string, object>;
                if (ownerDict == null) return;

                var ownerDto = JsonSerializer.Deserialize<OwnerDto>(JsonSerializer.Serialize(ownerDict));
                if (ownerDto == null) return;

                if (!string.IsNullOrWhiteSpace(existingIdOwner))
                {
                    await ownerService.UpdateAsync(existingIdOwner, ownerDto);
                }
                else
                {
                    var created = await ownerService.CreateAsync(ownerDto);
                    var builder = Builders<PropertyModel>.Update.Set(p => p.IdOwner, created.Data!.IdOwner);
                    await propertyCollection.UpdateOneAsync(p => p.Id == propertyId, builder);
                }
            }

            public static async Task ProcessImagePatchAsync(object imageObj, string propertyId, IPropertyImageService imageService)
            {
                var imageDict = NormalizeValue(imageObj) as Dictionary<string, object>;
                if (imageDict == null) return;

                var imageDto = JsonSerializer.Deserialize<PropertyImageDto>(JsonSerializer.Serialize(imageDict));
                if (imageDto == null) return;
                imageDto.IdProperty = propertyId;

                var existingImage = await imageService.GetByPropertyIdAsync(propertyId);
                if (existingImage != null)
                {
                    await imageService.UpdateAsync(existingImage.IdPropertyImage!, imageDto);
                }
                else
                {
                    await imageService.CreateAsync(imageDto);
                }
            }

            public static async Task ProcessTracesPatchAsync(object tracesObj, string propertyId, IPropertyTraceService traceService)
            {
                var tracesList = NormalizeValue(tracesObj) as List<object>;
                if (tracesList == null) return;

                var traceTasks = tracesList.Select(async t =>
                {
                    var traceDto = JsonSerializer.Deserialize<PropertyTraceDto>(JsonSerializer.Serialize(t));
                    if (traceDto == null) return;
                    traceDto.IdProperty = propertyId;
                    if (string.IsNullOrWhiteSpace(traceDto.IdPropertyTrace))
                    {
                        traceDto.IdPropertyTrace = ObjectId.GenerateNewId().ToString();
                        await traceService.CreateAsync(new List<PropertyTraceDto> { traceDto });
                    }
                    else
                        await traceService.UpdateAsync(traceDto.IdPropertyTrace, traceDto);
                });
                await Task.WhenAll(traceTasks);
            }
        }
    }
}
