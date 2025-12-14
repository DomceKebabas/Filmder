using Filmder.Interfaces;

namespace Filmder.Services;

public class MessageService(IMessageRepository repository) : IMessageService
{
    public async Task<List<object>> GetGroupMessagesAsync(int groupId, string userId)
        => await repository.GetGroupMessagesAsync(groupId, userId);

    public async Task<object> CreateMessageAsync(CreateMessageDto dto, string userId)
        => await repository.CreateMessageAsync(dto, userId);

    public async Task DeleteMessageAsync(int messageId, string userId)
        => await repository.DeleteMessageAsync(messageId, userId);
}