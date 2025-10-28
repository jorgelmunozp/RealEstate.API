using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.Property.Dto;

namespace RealEstate.API.Modules.Property.Interface
{
    public interface IPropertyService
    {
        Task<ServiceResultWrapper<object>> GetCachedAsync(
            string? name, string? address, string? idOwner,
            long? minPrice, long? maxPrice, int page, int limit, bool refresh);

        Task<ServiceResultWrapper<PropertyDto>> GetByIdAsync(string id);
        Task<ServiceResultWrapper<PropertyDto>> CreateAsync(PropertyDto dto);
        Task<ServiceResultWrapper<PropertyDto>> UpdateAsync(string id, PropertyDto dto);
        Task<ServiceResultWrapper<PropertyDto>> PatchAsync(string id, Dictionary<string, object> fields);
        Task<ServiceResultWrapper<bool>> DeleteAsync(string id);
    }
}
