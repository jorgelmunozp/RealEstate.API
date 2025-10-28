using System.Collections.Generic;
using System.Threading.Tasks;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.PropertyImage.Dto;

namespace RealEstate.API.Modules.PropertyImage.Interface
{
    public interface IPropertyImageService
    {
        Task<ServiceResultWrapper<IEnumerable<PropertyImageDto>>> GetAllAsync(string? idProperty = null, bool? enabled = null, int page = 1, int limit = 6, bool refresh = false);
        Task<ServiceResultWrapper<PropertyImageDto>> GetByIdAsync(string idPropertyImage);
        Task<PropertyImageDto?> GetByPropertyIdAsync(string propertyId);
        Task<ServiceResultWrapper<PropertyImageDto>> CreateAsync(PropertyImageDto image);
        Task<ServiceResultWrapper<PropertyImageDto>> UpdateAsync(string idPropertyImage, PropertyImageDto image);
        Task<ServiceResultWrapper<PropertyImageDto>> PatchAsync(string idPropertyImage, Dictionary<string, object> fields);
        Task<ServiceResultWrapper<bool>> DeleteAsync(string idPropertyImage);
    }
}
