using System.Net.Mail;

namespace electrostoreAPI.Services.SmtpService;

public interface ISmtpService
{
    public Task SendEmailAsync(string to, string subject, string body);
    Task SendMailAsync(MailMessage message);
}