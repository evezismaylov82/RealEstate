using RealEstateAPI.Domain.Entities;
using System.Threading.Tasks;

namespace RealEstateAPI.Domain.Interfaces.Repositories
{
    public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken?> GetByTokenAsync(string token);
        Task RevokeAllActiveTokensForUserAsync(int userId, string? revokedByIp = null, string? reason = null);
    }
}
