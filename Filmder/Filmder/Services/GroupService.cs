using Filmder.DTOs;
using Filmder.Interfaces;

namespace Filmder.Services;

public class GroupService(IGroupRepository groupRepository) : IGroupService
{
    public async Task<object> CreateGroupAsync(CreateGroupDto dto, string userId)
        => await groupRepository.CreateGroupAsync(dto, userId);

    public async Task<object> GetMyGroupsAsync(string userId)
        => await groupRepository.GetMyGroupsAsync(userId);

    public async Task AddToSharedMovieListAsync(int groupId, int movieId, string comment, string userId)
        => await groupRepository.AddToSharedMovieListAsync(groupId, movieId, comment, userId);

    public async Task<object> GetGroupByIdAsync(int groupId, string userId)
        => await groupRepository.GetGroupByIdAsync(groupId, userId);

    public async Task<object> GetSharedMoviesAsync(int groupId, string userId)
        => await groupRepository.GetSharedMoviesAsync(groupId, userId);

    public async Task AddMemberAsync(int groupId, string email, string requesterId)
        => await groupRepository.AddMemberAsync(groupId, email, requesterId);

    public async Task KickMemberAsync(int groupId, string userId, string requesterId)
        => await groupRepository.KickMemberAsync(groupId, userId, requesterId);

    public async Task DeleteGroupAsync(int groupId, string userId)
        => await groupRepository.DeleteGroupAsync(groupId, userId);

    public async Task<string> LeaveGroupAsync(int groupId, string userId)
        => await groupRepository.LeaveGroupAsync(groupId, userId);
}