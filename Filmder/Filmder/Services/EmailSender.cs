using System.Net;
using System.Net.Mail;
using Filmder.Interfaces;

namespace Filmder.Services;

public class EmailSender : IEmailSender
{
    private readonly IConfiguration _config;

    public EmailSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendEmailAsync(string toEmail, string subject, string message)
    {
        var fromAddress = _config["EmailSettings:SenderEmail"];
        var password = _config["EmailSettings:SenderPassword"];

        var mail = new MailMessage(fromAddress, toEmail, subject, message);

        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(fromAddress, password),
            EnableSsl = true
        };

        await smtp.SendMailAsync(mail);
    }
}