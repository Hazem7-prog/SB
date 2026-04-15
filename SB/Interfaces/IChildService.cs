using SB.DTOs;

namespace SB.Interfaces
{
    public interface IChildService
    {
        Task<ChildResponse> AddChildAsync(ChildRequest request);
        Task<ChildResponse> GetChildByIdAsync(int id);
        Task<IEnumerable<ChildResponse>> GetAllChildrenAsync();
        Task UpdateChildAsync(int id, ChildUpdate request);
        Task DeleteChildAsync(int id);
    }
}
