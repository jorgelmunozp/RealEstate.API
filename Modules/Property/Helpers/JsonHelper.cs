using System.Text.Json;
using MongoDB.Bson;
using MongoDB.Driver;
using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Interface;
using RealEstate.API.Modules.Property.Model;
using RealEstate.API.Modules.PropertyImage.Dto;
using RealEstate.API.Modules.PropertyImage.Interface;
using RealEstate.API.Modules.PropertyTrace.Dto;
using RealEstate.API.Modules.PropertyTrace.Interface;

namespace RealEstate.API.Modules.Property.Helpers
{
    public static class JsonHelper
    {
        public static object? NormalizeValue(object? value)
        {
            if (value == null) return null;

            if (value is JsonElement jsonEl)
            {
                switch (jsonEl.ValueKind)
                {
                    case JsonValueKind.Object:
                        return JsonSerializer
                            .Deserialize<Dictionary<string, object>>(jsonEl.GetRawText())
                            ?.ToDictionary(kv => kv.Key, kv => NormalizeValue(kv.Value));

                    case JsonValueKind.Array:
                        return JsonSerializer
                            .Deserialize<List<object>>(jsonEl.GetRawText())
                            ?.Select(NormalizeValue).ToList();

                    case JsonValueKind.String:
                        return jsonEl.GetString();

                    case JsonValueKind.Number:
                        if (jsonEl.TryGetInt64(out long l)) return l;
                        if (jsonEl.TryGetDouble(out double d)) return d;
                        return null;

                    case JsonValueKind.True:  return true;
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

        public static async Task ProcessOwnerPatchAsync(
            object ownerObj,
            string? existingIdOwner,
            string propertyId,
            IOwnerService ownerService,
            IMongoCollection<PropertyModel> propertyCollection)
        {
            var ownerDict = NormalizeValue(ownerObj) as Dictionary<string, object>;
            if (ownerDict == null || ownerDict.Count == 0) return;

            if (string.IsNullOrWhiteSpace(existingIdOwner))
            {
                var dto = JsonSerializer.Deserialize<OwnerDto>(JsonSerializer.Serialize(ownerDict));
                if (dto == null) return;

                var created = await ownerService.CreateAsync(dto);
                var newOwnerId = created.Data?.IdOwner;
                if (!string.IsNullOrWhiteSpace(newOwnerId))
                {
                    var upd = Builders<PropertyModel>.Update.Set(p => p.IdOwner, newOwnerId);
                    await propertyCollection.UpdateOneAsync(p => p.Id == propertyId, upd);
                }
                return;
            }

            foreach (var k in ownerDict.Keys.ToList())
                if (string.Equals(k, "Id", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(k, "IdOwner", StringComparison.OrdinalIgnoreCase))
                    ownerDict.Remove(k);

            await ownerService.PatchAsync(existingIdOwner, ownerDict);
        }

        public static async Task ProcessImagePatchAsync(
            object imageObj,
            string propertyId,
            IPropertyImageService imageService)
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

        public static async Task ProcessTracesPatchAsync(
            object tracesObj,
            string propertyId,
            IPropertyTraceService traceService)
        {
            var tracesList = NormalizeValue(tracesObj) as List<object>;
            if (tracesList == null) return;

            var tasks = tracesList.Select(async t =>
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
                {
                    await traceService.UpdateAsync(traceDto.IdPropertyTrace, traceDto);
                }
            });

            await Task.WhenAll(tasks);
        }
    }
}
