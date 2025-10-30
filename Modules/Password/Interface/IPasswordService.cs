using System.Threading.Tasks;
using RealEstate.API.Infraestructure.Core.Services;

namespace RealEstate.API.Modules.Password.Interface
{
    public interface IPasswordService
    {
        Task<object> SendPasswordRecoveryEmail(string email);
        object VerifyResetToken(string token);
        Task<ServiceResultWrapper<string>> UpdatePasswordById(string id, string newPassword);
    }
}
