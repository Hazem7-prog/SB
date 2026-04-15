using SB.Models;

namespace SB.Interfaces
{
    public interface IPasswordService
    {
        string Hash(User user, string password);
        bool Verify(User user, string password);
        
    }
}
