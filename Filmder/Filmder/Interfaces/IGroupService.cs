using Filmder.DTOs;

namespace Filmder.Interfaces;

public interface IGroupService
{
    Task<object> CreateGroupAsync(CreateGroupDto dto, string userId);
    Task<object> GetMyGroupsAsync(string userId);
    Task AddToSharedMovieListAsync(int groupId, int movieId, string comment, string userId);
    Task<object> GetGroupByIdAsync(int groupId, string userId);
    Task<object> GetSharedMoviesAsync(int groupId, string userId);
    Task AddMemberAsync(int groupId, string email, string requesterId);
    Task KickMemberAsync(int groupId, string userId, string requesterId);
    Task DeleteGroupAsync(int groupId, string userId);
    Task<string> LeaveGroupAsync(int groupId, string userId);
}