using RealEstate.API.Modules.Owner.Dto;
using RealEstate.API.Modules.Owner.Model;
using MongoDB.Bson;

namespace RealEstate.API.Modules.Owner.Mapper
{
    public static class OwnerMapper
    {
        // ===========================================================
        // 🔹 Model → DTO
        // ===========================================================
        public static OwnerDto ToDto(this OwnerModel model) => new()
        {
            IdOwner = model.Id,
            Name = model.Name,
            Address = model.Address,
            Photo = model.Photo,
            Birthday = model.Birthday
        };

        public static List<OwnerDto> ToDtoList(IEnumerable<OwnerModel> models)
            => models?.Select(ToDto).ToList() ?? new List<OwnerDto>();

        // ===========================================================
        // 🔹 DTO → Model
        // ===========================================================
        public static OwnerModel ToModel(this OwnerDto dto)
        {
            var id = !string.IsNullOrEmpty(dto.IdOwner)
                ? dto.IdOwner
                : ObjectId.GenerateNewId().ToString();

            return new OwnerModel
            {
                Id = id,
                Name = dto.Name,
                Address = dto.Address,
                Photo = dto.Photo,
                Birthday = dto.Birthday
            };
        }

        public static List<OwnerModel> ToModelList(IEnumerable<OwnerDto> dtos)
            => dtos?.Select(ToModel).ToList() ?? new List<OwnerModel>();
    }
}
