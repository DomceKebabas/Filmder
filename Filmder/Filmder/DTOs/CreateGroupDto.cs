namespace Filmder.DTOs;

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    
    public List<string> FriendEmails { get; set; } = new();
}