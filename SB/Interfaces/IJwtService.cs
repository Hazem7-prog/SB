
using SB.Models;

namespace SB.Interfaces
{
    public interface IJwtService
    {
        string GenerateToken(User user, IList<string> roles = null);

        int GetTokenExpirationMinutes();    

    }
}
