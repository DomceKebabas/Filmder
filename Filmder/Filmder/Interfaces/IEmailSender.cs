namespace Filmder.Interfaces;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string message);
}