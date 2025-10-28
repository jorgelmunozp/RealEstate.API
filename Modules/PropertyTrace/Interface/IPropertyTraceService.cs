// RealEstate.API/Modules/PropertyTrace/Interface/IPropertyTraceService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.PropertyTrace.Dto;

namespace RealEstate.API.Modules.PropertyTrace.Interface
{
    public interface IPropertyTraceService
    {
        Task<ServiceResultWrapper<IEnumerable<PropertyTraceDto>>> GetAllAsync(string? idProperty = null, bool refresh = false);
        Task<ServiceResultWrapper<PropertyTraceDto>> GetByIdAsync(string id);
        Task<ServiceResultWrapper<List<string>>> CreateAsync(IEnumerable<PropertyTraceDto> traces);
        Task<ServiceResultWrapper<PropertyTraceDto>> CreateSingleAsync(PropertyTraceDto trace);
        Task<ServiceResultWrapper<PropertyTraceDto>> UpdateAsync(string id, PropertyTraceDto trace);
        Task<ServiceResultWrapper<PropertyTraceDto>> PatchAsync(string id, Dictionary<string, object> fields);
        Task<ServiceResultWrapper<bool>> DeleteAsync(string id);
    }
}
