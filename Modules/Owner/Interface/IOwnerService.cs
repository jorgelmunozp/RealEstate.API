using System.Collections.Generic;
using System.Threading.Tasks;
using RealEstate.API.Infraestructure.Core.Services;
using RealEstate.API.Modules.Owner.Dto;

namespace RealEstate.API.Modules.Owner.Interface
{
    public interface IOwnerService
    {
        Task<ServiceResultWrapper<List<OwnerDto>>> GetAsync(string? name = null, string? address = null, bool refresh = false);
        Task<ServiceResultWrapper<OwnerDto>> GetByIdAsync(string id);
        Task<ServiceResultWrapper<OwnerDto>> CreateAsync(OwnerDto owner);
        Task<ServiceResultWrapper<OwnerDto>> UpdateAsync(string id, OwnerDto owner);
        Task<ServiceResultWrapper<OwnerDto>> PatchAsync(string id, Dictionary<string, object> fields);
        Task<ServiceResultWrapper<bool>> DeleteAsync(string id);
    }
}
