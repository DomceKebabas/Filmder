namespace Filmder.Interfaces;

public interface IMessageService
{
    Task<List<object>> GetGroupMessagesAsync(int groupId, string userId);
    Task<object> CreateMessageAsync(CreateMessageDto dto, string userId);
    Task DeleteMessageAsync(int messageId, string userId);
}