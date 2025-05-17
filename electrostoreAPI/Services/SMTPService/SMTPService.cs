using System.Net;
using System.Net.Mail;

namespace electrostore.Services.SMTPService;

public class SMTPService : ISMTPService
{
    private readonly IConfiguration _configuration;

    public SMTPService(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public async Task SendEmailAsync(string to, string subject, string body)
    {
        // check if SMTP is enabled
        if (_configuration["SMTP:Enabled"] != "true")
        {
            return;
        }
        var smtpClient = new SmtpClient(_configuration["SMTP:Host"])
        {
            Port = int.Parse(_configuration["SMTP:Port"] ?? "587"),
            Credentials = new NetworkCredential(_configuration["SMTP:Username"], _configuration["SMTP:Password"]),
            EnableSsl = true
        };
        var mailMessage = new MailMessage
        {
            From = new MailAddress(_configuration["SMTP:Username"]),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(to);
        // send email
        var sendEmailTask = smtpClient.SendMailAsync(mailMessage);
        await sendEmailTask;
        if (sendEmailTask.IsFaulted)
        {
            // server error
            throw new InvalidOperationException("An error occured while sending the email");
        }
    }
}